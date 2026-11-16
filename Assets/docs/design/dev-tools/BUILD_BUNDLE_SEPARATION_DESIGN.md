# 빌드·번들 분리 설계와 빌드 매니저

작성일 2026-11-15. 상태 문서: [빌드 매니저 작업 상태](../../../../docs/work-status/build-manager.md). 위키: [개발과 검증](../../../../docs/wiki/development.md).

## 1. 목적

"빌드마다 통빌드를 해야 하는가"에 답하고, 콘텐츠(번들)만 바꾼 변경을 코드·플레이어 재빌드 없이 반영하는 경로와 그 도구(빌드 매니저)를 정한다. 이 문서의 결정은 설계 검토(Opus급 검토자와 논의)를 거쳐 확정했다.

## 2. 현황 (2026-11-15 조사)

| 항목 | 확인 결과 | 근거 |
|---|---|---|
| 그룹 | Default Local Group 40항목(PackTogether), Player Models 13(PackTogetherByLabel), Player Character Definitions 13(PackSeparately), Zones_InGame 0항목 | Unity 실측 |
| 경로 | 전 그룹 Local(`StreamingAssets/aa`). 원격 카탈로그 없음, `Remote.LoadPath` 미정의 | AddressableAssetSettings |
| 플레이어 빌드 시 콘텐츠 | `PreferencesValue`(0). 에디터 환경설정을 따른다 | enum 실측: Preferences=0, BuildWithPlayer=1, DoNotBuildWithPlayer=2 |
| 씬 목록 | Boot·Title·Loading·CombatTestMap·CameraTestMap·LakeVillageWorld 활성. 테스트 맵이 활성 상태 | EditorBuildSettings |
| 월드 구역 | `OpenWorld.*.Content` 항목이 Default Local Group에 있음. 로딩은 `ContinuousWorldRegion`의 `Addressables.LoadSceneAsync` | 코드·그룹 |
| 빌드 스크립트 | 임시 스크립트 5개가 각자 `BuildPlayer`를 호출(씬 교체·복원 로직 중복) | Grep |
| Build Profiles | Unity 6 `BuildProfile`·`BuildPlayerWithProfileOptions` 존재, 프로젝트 에셋 0개 | 리플렉션·에셋 검색 |

정정: 앞선 답변은 `m_BuildAddressablesWithPlayerBuild: 0`을 "플레이어 빌드 때 번들을 자동으로 만들지 않음"이라 했으나 틀렸다. 0은 `PreferencesValue`이며 환경설정이 정한다. 따라서 이 설정에 의존하지 않고 빌드 도구가 콘텐츠 빌드 여부를 명시한다.

핵심 관찰: `Builds/WorldCities_20261024`의 `StreamingAssets/aa`에는 `link.xml`과 `settings.json`만 있고 `catalog.bin`과 번들이 없다. 플레이어가 콘텐츠 카탈로그 없이 만들어졌다는 뜻이며 실행 시 Addressables 로드 실패가 예상된다(실행은 하지 않았다). 지금의 임시 빌드 흐름이 만드는 실제 실패이며 사전 점검이 필요한 근거다.

## 3. 질문에 대한 답

- **분리되어 있는가:** 도구 지원은 없다. 번들은 exe 폴더 안(StreamingAssets)에 같이 나가고 별도 교체 경로가 없었다.
- **분리 가능한가:** 가능하다. 코드(C#)와 씬 목록은 플레이어에 남고, 번들은 콘텐츠(데이터·프리팹·주소 지정 씬)만 담는다. 코드·씬 목록이 같을 때만 번들 교체가 안전하다.

## 4. 결정

### D1 배포는 로컬 전용, 원격 카탈로그 미도입
- 근거: 싱글플레이 PC 게임이다. 스토어 패치는 파일 단위 델타라 잘게 나눈 로컬 번들만으로 패치 크기가 줄어든다. CDN·카탈로그 갱신·버전 호환 운영 비용이 이득보다 크다.
- 기각: 원격 카탈로그. Remote 프로필 항목은 미래 선택지로만 남긴다.

### D2 "번들만 빌드"의 정의와 안전 장치
- 정의: 콘텐츠를 다시 빌드해 `Addressables.BuildPath` 결과(번들+카탈로그+settings)를 기존 플레이어의 `<exe>_Data/StreamingAssets/aa`에 반영한다.
- 안전 장치: 플레이어 빌드 때 `BuildManager.manifest.json`을 쓴다(Unity 버전, 빌드 대상, 씬 목록, 플레이어 어셈블리 해시, 카탈로그 해시). 콘텐츠만 교체할 때 현재 지문과 다르면(코드 변경·씬 목록 변경·Unity 버전·대상 변경) 거부하고 전체 빌드를 요구한다.
- 코드 해시: `CompilationPipeline.GetAssemblies(PlayerWithoutTestAssemblies)`의 컴파일 결과 DLL 내용 해시. 에디터 전용 코드 변경은 반영하지 않는다.
- 복사 순서: 번들 → 카탈로그 → settings.json. 중간에 실패해도 새 카탈로그가 없는 번들을 가리키지 않는다. 새 빌드에 없는 옛 `.bundle`은 정리한다.
- 기각: 해시 없이 사용자 판단에 맡기기. 타입 불일치 번들은 로드 시점에야 실패해 원인 추적이 어렵다.

### D3 작업 세 종류
| 모드 | 동작 | 사전 점검 |
|---|---|---|
| 전체 | 콘텐츠 빌드 → 플레이어 빌드 → 지문 기록 | Play Mode·미저장 씬·씬 파일 존재 |
| 콘텐츠만 | 콘텐츠 빌드 → 기존 플레이어에 반영 | 위 지문 일치 |
| 플레이어만 | 마지막 콘텐츠 결과로 플레이어 빌드 | 카탈로그 존재, 콘텐츠가 주소 지정 에셋 수정 시각보다 오래되지 않음(근사치, 우회 가능) |

콘텐츠 빌드는 도구가 명시적으로 수행하고 플레이어 빌드 동안 `DoNotBuildWithPlayer`로 고정한 뒤 복원한다(환경설정에 의한 이중 빌드 방지).

### D4 씬 목록의 정본은 프로필
- 전역 Build Settings를 손으로 고치지 않고 `BuildManagerProfileSO`(씬·출력·실행 파일 이름·대상·Development)를 정본으로 한다. 기본 프로필은 `/Test/` 씬을 제외한다.
- lilToon 전처리가 `BuildPlayerOptions.scenes`가 아닌 전역 씬 목록을 읽으므로 빌드 중에만 `EditorBuildSettings.scenes`를 교체하고 `finally`에서 복원한다(기존 스크립트와 같은 방식).

### D5 도메인·변경 단위별 로컬 그룹
- 사용자 후속 지시로 재편한다. 전투(Combat_Data 8), 콘텐츠(Content_Data 9), UI(UI_Data 8), 오디오(Audio_Data 3), 카메라(Camera_Data 6)는 PackTogether로 각각 묶는다. 공통 SettingsData·DebugGizmoSettings·DissolveMaterial은 Default Local Group에 남긴다.
- `OpenWorld.*.Content` 구역 3개는 Zones_InGame으로 옮겨 PackSeparately로 설정한다. 기존 캐릭터 그룹 두 개는 유지한다.
- 이동은 GUID로 식별하고 예상 주소가 다르면 거절한다. 주소·GUID·레이블·로컬 경로·카탈로그 주소 포함 옵션은 보존한다. `ContinuousWorldRegion`의 런타임 로드 코드는 변경하지 않는다.
- 이전 스크립트는 `Tools/Build/reorganize-local-groups.csx`다. 실패 시 해당 Undo group을 복구하고 생성 그룹을 정리한다. 인간용 상시 도구로 등록하지 않는다.
- 그룹·스키마 파일 변경 시각을 PlayerOnly의 기존 콘텐츠 신선도 검사에 포함한다. 설정 파일의 임시 플레이어 빌드 옵션 복원 시각은 비교에서 제외한다. 간접 에셋·전역 프로필 설정 변경 탐지는 여전히 근사치의 범위 밖이다.
- 그룹 에셋·설정·meta는 버전 관리 대상이다. 생성된 Windows 콘텐츠 상태와 link.xml은 제외한다.
### D6 Unity Build Profiles 대신 얇은 프로필 SO
- 확인: `BuildProfile`·`BuildPlayerWithProfileOptions`는 존재하지만 이 프로젝트에 프로필이 0개이고, lilToon이 전역 씬 목록을 읽는 제약 때문에 프로필 경로가 전처리와 맞는지 검증되지 않았다.
- 결정: `BuildManagerProfileSO`(`UPlayGround/Build/Profile`)를 쓴다. 후속에서 Build Profile 경로가 lilToon과 호환되면 SO를 어댑터로 바꾼다.

### D7 기존 검증 빌드 진입점 통합
`AllWeaponsValidationBuild`, `CombatEncounterValidationBuild`, `LakeAlternateSelfAuthoring`, `LayaTrainingAuthoring`, `ScenarioMapAuthoring.PlayerBuild`는 `BuildManagerRunner.RunValidation`으로 연결한다. 기존 씬·출력·BuildOptions·개별 보고서와 전투의 CRC/제품 이름/씬 복원을 보존한다. 공통 러너는 임시 SO로 기존 옵션을 전달받아 명시적 콘텐츠 빌드·사전 점검·지문 기록·전역 Build Settings 복원을 수행한다. 기본은 Full이며 `-reuseCombatContent`만 PlayerOnly로 연결하고 오래된 결과는 거절한다. 기존에 콘텐츠를 생략하던 Laya도 기본 Full로 만들어 카탈로그 없는 Player 생성을 막는다.
## 5. 빌드 매니저 구조

위치 `Assets/02.Scripts/Tool/Editor/Build/` (asmdef `UPlayGround.BuildManager.Editor`, Editor 전용). 사람이 반복해 여는 도구라 툴 런처 "빌드" 카테고리에 등록했고 최상위 메뉴는 추가하지 않았다. 진입 `[UPlaygroundTool("UPlayGround/빌드/빌드 매니저")]`.

| 파일 | 역할 |
|---|---|
| `BuildManagerTypes.cs` | 모드·점검 입력/결과·지문 타입 |
| `BuildPreflight.cs` | 에디터 API 없는 순수 사전 점검(테스트 대상) |
| `BuildManagerFiles.cs` | 경로 규칙, 지문 입출력, 해시, 콘텐츠 반영 |
| `BuildManagerProfileSO.cs` | 씬·출력·대상 프로필(`Assets/10.Datas/Build/`) |
| `BuildManagerRunner.cs` | 모드별 실행. 전역 상태 복원 `finally` |
| `BuildManagerWindow.cs` | 프로필 선택, 사전 점검, 그룹 현황, 실행 |

테스트: `Assets/Tests/EditMode/BuildManager/BuildManagerLogicTests.cs`(점검 규칙, 경로, 콘텐츠 반영·고아 번들 정리, 해시).

## 6. 한계와 위험

- 재빌드 판정은 저장된 프로젝트 설정·씬/주소 지정 에셋의 의존성 해시·그룹/스키마·Addressables 프로필 변수·카탈로그를 비교한다. 자동 탐지하지 못하는 커스텀 빌드 전처리 입력·환경 변수·플러그인 외부 입력과 출력 번들 전체 바이트 무결성 검사는 범위 밖이다. ProjectSettings는 보수적으로 비교하므로 에디터 전용 설정 변경도 exe 재빌드를 권장할 수 있다.
- 코드 해시는 컴파일 결과 바이트 비교다. 컴파일러 출력이 비결정적이면 오탐(불필요한 전체 빌드 요구)이 난다. 안전한 쪽이다.
- 플레이어만 모드는 Addressables가 기존 `Library` 빌드 결과를 StreamingAssets에 복사한다는 동작에 기댄다(기존 임시 스크립트와 같은 가정). 실제 산출물은 사용자 빌드로 확인한다.
- 한 폴더 `Builds/<프로필>`에 덮어쓰므로 이전 빌드 보존이 필요하면 프로필 이름을 바꾼다.

## 7. 검증 상태

- 컴파일·EditMode 테스트: 상태 문서 참조.
- Player·Addressables 빌드: 별도 사용자 지시가 없어 실행하지 않았다. `사용자 테스트 대기`.

## 8. 후속 단계

1. 사용자가 전체 빌드 → 번들만 → 플레이어만 순으로 실행해 지문·반영·실행 확인.
2. 실제 번들 Provider로 구역 3개와 DB·UI·오디오·카메라 로드 확인. 패치 크기·중복 의존성은 Build Layout으로 확인.

## 9. 창의 프로필 편집·출력 폴더별 상태 계약

- 프로필 선택 후 창에서 Development 여부, 플랫폼, exe 이름, 씬 추가·삭제·드래그 순서, 빌드 폴더를 편집한다. SerializedObject로 자동 저장·Undo를 지원한다. Build Settings 활성 씬 가져오기(테스트 씬 제외 선택)와 열린 씬 추가를 제공한다. 첫 씬이 시작 씬이며 빈 씬·중복 씬은 실행을 막는다.
- `BuildManagerProfileSO.outputDirectory`는 사용자가 선택한 **정확한 출력 폴더**다. 비어 있는 구 프로필만 `outputRoot/profileName` 규칙을 유지한다. 폴더 선택 뒤 프로필 이름을 덧붙이지 않는다. 모든 러너·검증 진입점은 동일한 경로 해석을 사용한다. 출력은 현재 Windows64 exe만 지원한다.
- Development 토글이 Dev 플래그의 정본이다. 다른 BuildOptions는 보존한다. 기존 검증 진입점의 BuildPlayerOptions도 이 규칙으로 변환한다.
- 창은 선택한 폴더에 대해 exe 재빌드 여부·번들 빌드/반영 여부·이유·권장 전체/번들만/exe만 모드·실제 exe 경로를 표시한다. 프로필 변경/포커스 복귀/프로젝트 변경 뒤 갱신하고 수동 새로고침도 제공한다. 오래된 판정으로 실행할 수 없고 러너에서도 다시 점검한다.
- 새 지문은 기존 Unity/대상/코드/씬 목록/카탈로그 외에 exe 이름·Player 설정/씬 의존성·콘텐츠 입력 해시를 기록한다. Dev/씬 내용 변경은 exe, 콘텐츠 입력/대상 카탈로그 변경은 번들, 둘 다 바뀌면 전체를 권장한다. 새 비교 필드가 없는 구 기록이나 기록 없는 BuildOutput은 최초 전체 빌드를 권장한다.
- 콘텐츠 빌드 성공 시 `Library/BuildManager/<플랫폼>/BuildManager.manifest.json`에 콘텐츠 캐시 지문을 남긴다. exe만 모드는 실제 캐시 카탈로그·Unity·플랫폼·코드·콘텐츠 지문을 확인한다. 재사용 가능한 캐시가 없으면 전체를 권장한다. 고급 불일치 재사용은 경고를 표시하고 실제 재사용한 캐시의 입력 해시를 기록한다.
- 판정은 저장된 입력·기록 기준이다. 실행 성공이나 출력 번들 파일 전수 무결성 검사를 대신하지 않는다. 사용자 빌드·임포트 중에는 Agent가 Editor 검증/추가 빌드를 요청하지 않는다.