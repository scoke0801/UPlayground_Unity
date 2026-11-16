# 빌드 매니저 작업 상태

설계 정본: [빌드·번들 분리 설계](../../Assets/docs/design/dev-tools/BUILD_BUNDLE_SEPARATION_DESIGN.md). 위키: [개발과 검증](../wiki/development.md).

기준일 2026-11-15.

| 항목 | 상태 | 근거·남은 일 |
|---|---|---|
| 설계·결정 D1~D7 | 작성 완료 | 설계 문서. 검토자와 논의해 확정 |
| 도구 코드(Types/Preflight/Files/Profile/Runner/Window) | 작성 완료 | 컴파일 결과는 아래 |
| 툴 런처 "빌드" 등록 | 반영 | `UPlaygroundToolsLauncher.cs` |
| 이전 EditMode 테스트 18개 | 이전 버전 통과 | 최신 UI 확장본은 Editor 미실행. 아래 UI 후속 검증과 구분 |
| Player·Addressables 빌드 | 미수행 | `사용자 테스트 대기` (별도 지시 없이 실행하지 않음) |
| 그룹 재편(D5) | 에셋 반영 | 사용자 후속 지시로 37개 이동, 전체 66개 주소·GUID·레이블 보존 확인. 실제 로드는 사용자 테스트 대기 |
| 임시 빌드 스크립트 이전(D7) | 구현 반영 | 5개 진입점이 공통 러너 호출. 직접 BuildPlayer는 러너에만 남김 |
| WorldCities 빌드의 카탈로그 누락 | 확인(파일 조사) | 실행 시 로드 실패 예상, 실행은 하지 않음 |

## 검증 결과

- 2026-11-15 컴파일: **미확인.** 파일 작성 후 강제 새로고침을 요청했으나 Unity MCP가 반복해서 응답하지 않았다(`Timeout receiving Unity response`). 컴파일 오류 0을 확인한 것이 아니다.
- EditMode 테스트 10개: **미실행.**
- 이후 재시도: 새로고침이 복구됐고 `read_console` 오류 조회가 0건을 반환했으나, 리로드로 콘솔이 비었을 수 있어 컴파일 성공의 증거로 보지 않는다. 이어서 테스트 실행을 요청했을 때 `No Unity Editor instances found`로 실패했다.
- 2026-11-15 재연결 후: 콘솔 오류 0건, EditMode `UPlayGround.BuildManager.Tests` 10/10 통과(0.7초). 어셈블리 컴파일 성공을 확인했다. 실제 Player·Addressables 빌드와 창 UI는 미검증이다(`사용자 테스트 대기`).
- (이전 기록) 다음 행동: Unity가 응답하면 `read_console`로 오류를 확인하고 `UPlayGround.BuildManager.Tests`를 실행한다.

## 후속 구현 (2026-11-15)

- 그룹: Combat_Data 8 / Content_Data 9 / UI_Data 8 / Audio_Data 3 / Camera_Data 6 / Zones_InGame 3(PackSeparately). Default Local Group에는 SettingsData·DebugGizmoSettings·DissolveMaterial 3개만 남고 캐릭터 두 그룹은 유지했다. 전 그룹 로컬 경로 확인.
- 이전 원본: [그룹 이전 스크립트](../../Tools/Build/reorganize-local-groups.csx). 첫 시도는 Addressables가 스키마 목록을 순회 중 재정렬해 실패했다. Undo로 기존 상태 복구 후 이번 시도의 빈 신규 그룹만 정리하고, 목록 사본 전달로 수정한 동일 작업이 성공했다.
- AddressableAssetsData 전체가 기존 gitignore 대상이었다. 그룹·설정·meta는 추적 가능한 소스로 전환하고 Windows 콘텐츠 상태·link.xml 생성물은 계속 제외했다.
- 공통 러너: 검증 진입점의 기존 씬·출력·옵션·보고서는 유지하며 콘텐츠 빌드·지문·카탈로그 사전 점검을 공통 처리한다. 전투 도구의 CRC·제품 이름·씬 복원은 유지한다. `-reuseCombatContent`는 PlayerOnly로 연결하며 오래된 결과는 거절한다.
- 그룹·스키마 변경 시각을 콘텐츠 신선도 점검에 포함한다. 컴파일·임포트·미저장 씬에서는 빌드를 거절한다. Edit Mode·필드 추가·asmdef 변경이므로 Hot Reload 대신 정상 컴파일 경로를 사용한다.
- QualitySettings의 기존 그림자·텍스처 품질 변경은 보존한다. 이번 작업이 작성한 값이 아니다.
- 구현 단계 부산물: 실패한 빈 Audio_Data 그룹은 이번 생성물만 정리했다. 이전 스크립트와 준비표는 재현·검증 근거로 보존한다. 사용자 파일 삭제 없음.
## 후속 자동 검사·인계

- 컴파일: 요청 이후 시작 1794747954335 → 완료 1794747967060 → 동일 Editor 리로드 1794747981199 확인. Console 오류 0건. 최종 변경 후에도 시작 1794748257529 → 완료 1794748259217 → 리로드 1794748270124 및 최종 테스트 성공으로 다시 확인했다.
- Unity EditMode: **18/18 통과, 실패·건너뜀 0, 1.89초**. [MCP 원본 결과](build-manager-tests.json), 최종 작업 ID `a6c6827c0bec4519b42122fdbe6bc4e9`. 첫 작업 `af188726346d476c85a565274069e4fc`는 초기화 시간 초과(실행 0)로 실패했다. 상태 조회로 종결을 확인한 뒤 초기화 제한을 120초로 늘린 재시도가 성공했다. Editor 재시작 없음.
- 하네스 자체 검사: **43개 통과**. Unity·Player 실행 없음.
- [그룹 감사 원본](build-manager-groups.txt): 66개, 중복 주소 0, 37개 이동 전후 주소·GUID·레이블 동일. 구역 키 3개는 실제 LakeVillageWorld SceneKey와 같고 `ContinuousWorldRegion`은 기존 Addressables.LoadSceneAsync 경로를 유지한다. 번들 Provider 로드 성공의 증거는 아니다.
- 창: 실제 EditorWindow 열기·프로필 연결·Repaint 후 Console 오류 0. 생성된 [Release 프로필](../../Assets/10.Datas/Build/BuildProfile_Release.asset)은 Boot → Title → Loading → LakeVillageWorld, `/Test/` 제외. Full 사전 점검 0건. 자동 열기 확인이며 사람의 시각·클릭 검증은 사용자 테스트 대기.
- QualitySettings·EditorBuildSettings는 작업 초기에 기록한 SHA256과 동일하다. 기존 미커밋 변경은 보존했다. 커밋 없음.
- 준비표: `output/harness/20261115-221116-25371755/`(전체 범위), 초기 설정 지문은 `output/harness/20261115-215917-636b5cb1/`. 참조용이며 완료 증명은 위 원본 결과와 현재 에셋이다. NUnit 합성 임시 폴더는 테스트 finally로 정리했고, 하네스 합성 자료도 자체 정리했다. 신규 원본 설정·프로필·meta·이전 스크립트·검증 기록은 보존한다.

## 사용자 직접 테스트 (모두 대기)

대상: Windows64, 저장된 Edit Mode, 키보드·마우스. 툴 런처 **빌드 > 빌드 매니저**, 기본 Release 프로필. 현재 버전 콘텐츠를 새로 빌드해야 그룹 재편이 실제 번들에 반영된다.

| ID | 조작 | 기대 결과·실패 기준 |
|---|---|---|
| B01 | 전체 모드 실행 후 `Builds/Release/UPlayground.exe` 실행 | 지문·카탈로그·번들 생성, Boot→Title→LakeVillageWorld 진입. 카탈로그 누락·로드 오류면 실패 |
| B02 | 데이터 하나 수정 후 콘텐츠만 실행 | 지문 일치 시 번들·카탈로그 반영, exe 수정 시각 유지. 코드 변경 이후에는 전체 빌드 요구 |
| B03 | B02 뒤 플레이어만 실행 | 콘텐츠 재빌드 없이 Player 성공, 생성 Player의 DB·UI·오디오·카메라 로드 정상 |
| B04 | 구역 3개 진입·복귀 | 기존 주소로 각 구역 로드·언로드, 누락·중복·로드 오류 없음 |
| B05 | 그룹·스키마 변경 후 플레이어만 사전 점검 | 오래된 콘텐츠로 거절. 되돌린 뒤 필요 시 콘텐츠 다시 빌드 |
| B06 | 창에서 프로필·모드 전환·사전 점검 | 글자·레이아웃·버튼 정상. 사람 시각 확인 필요 |

회신: ID / 통과·실패·미수행 / 실제 결과 / 로그·화면. 실제 빌드·게임플레이·패치 크기는 자동 검사 통과와 구분한다.
- 최종 검토에서 그룹 재편과 연결되는 기존 콘텐츠 반영 결함을 보완했다. 카탈로그 복사 전에 옛 번들을 지우던 순서를 카탈로그·settings 성공 뒤로 옮기고, 사라진 그룹 폴더의 고아 번들도 정리한다. 카탈로그 복사 실패 주입과 사라진 폴더 회귀 검사를 추가했다. 이는 다중 파일 전체 원자성 보장은 아니다.

## 창 편집·폴더별 재빌드 안내 후속 (코드 반영, Editor 확인 대기)

사용자가 Dev·씬·빌드 폴더 편집과 폴더별 재빌드 필요 안내를 요청했다. 구현 도중 **빌드·임포트 진행 중이므로 코드만 우선 작업**하라고 지시해 추가 Editor 요청과 빌드를 중단했다. 앞서 요청한 Refresh의 응답 지연은 로그의 MCP 메인 스레드 처리 시간 초과로 확인했으며, 사용자 빌드·임포트와 구분해 도구 결함으로 단정하지 않는다. 재시작·두 번째 Editor 실행 없음.

- 반영: 창의 Development/플랫폼/exe 이름/정확한 출력 폴더 편집, 씬 드래그·추가·삭제·가져오기, 자동 저장·Undo, 폴더별 exe·번들 상태와 이유·권장 모드·실제 실행 경로.
- 지문: exe 이름·Player 옵션/씬 의존성/프로젝트 설정·콘텐츠 의존성/그룹/스키마/Addressables 프로필을 비교한다. 기록 없는 폴더/구 지문은 최초 전체 빌드를 안내한다. 캐시 재사용 지문과 카탈로그가 맞을 때만 exe만 모드를 권장한다. 빈/중복 씬·잘못된 exe 이름은 거절한다.
- 코드: `BuildManagerWindow`, `ProfileSO`, `Runner`, `Preflight`, `Types`, `Files` 확장, `BuildManagerFingerprint.cs`와 meta 추가. 기존 Release 에셋의 경로·씬·Dev 값과 진행 중인 BuildOutput 파일은 수정하지 않았다.
- **독립 컴파일 통과**: 현재 Unity Roslyn과 기존 Bee 참조/RSP를 사용해 도구·테스트 코드를 별도 출력에 컴파일했다. 오류 0, 종료 코드 0. `USG0001` 정보성 메시지는 추가 파일 입력 없는 독립 컴파일에서 발생했다. Editor 도메인 반영 성공을 뜻하지 않는다. [도구 컴파일 로그](build-manager-ui-editor-compile.txt) · [테스트 코드 컴파일 로그](build-manager-ui-tests-compile.txt).
- **순수 로직 19/19 통과**: 기존 NUnit 메서드를 독립 프로세스에서 직접 호출했다. 폴더 기록 없음/구 지문, Dev·씬·설정 변화, 콘텐츠만 변화, exe 누락·카탈로그 차이, 캐시 없음, exe 이름 변경, 사전 점검, 기존 콘텐츠 복사 실패 보존 검사를 포함한다. [원본 로그](build-manager-ui-pure-tests.txt) · [입력 해시·검증 방식](build-manager-ui-verification.json).
- **미수행**: 최신 코드의 Unity 컴파일·도메인 리로드 확인, 전체 EditMode 30개 케이스 Test Runner, 실제 창 표시/클릭/Undo/씬 드래그, 새 지문을 기록하는 실제 Player·Addressables 빌드. 모두 사용자 빌드 종료 후 확인 대상이다. 이전 18/18을 최신 코드의 통과로 승계하지 않는다.
- 준비표: 초기 `output/harness/20261115-222947-5a835ddb/`, 최종 전체 범위 `output/harness/20261115-223954-9580a467/`. 별도 컴파일용 DLL·PDB·RSP·임시 실행 스크립트 260185바이트는 검사 후 이번 생성 폴더만 정리했다. 원본 로그는 위 상태 폴더로 보존했다. 기존 Library/Bee·ScriptAssemblies·사용자 빌드 출력은 건드리지 않는다.

### 빌드 종료 후 확인할 UI 테스트

| ID | 조작 | 기대 결과 |
|---|---|---|
| U01 | Development 전환·씬 드래그·추가·삭제 후 창 재열기, Ctrl+Z | 프로필 저장·순서/Dev 복원, 빈/중복 씬은 빌드 차단 |
| U02 | 폴더 선택으로 `C:/UsingProject/UnityProject/BuildOutput` 지정 | 폴더 뒤에 Release가 추가되지 않고 `.../BuildOutput/UPlayground.exe` 표시, 구 기록이면 전체 권장 |
| U03 | 전체 빌드 후 동일 입력으로 상태 갱신, Dev만 변경 | 처음은 기록 일치, Dev만 변경하면 exe 재빌드. 재사용 캐시가 맞으면 exe만 권장 |
| U04 | 주소 지정 SO/간접 참조/그룹 설정 변경 | 번들 변경 표시. 플레이어 입력도 바뀌면 전체 권장 |

현재 프로젝트 설정은 보수적으로 비교하므로 에디터 전용 설정 변경도 exe 재빌드를 권장할 수 있다. 커스텀 전처리의 외부 입력과 출력 번들 전체 파일의 무결성은 판정 범위 밖이다.