# ExternalAssets 모델의 P09 캐릭터 제작 지침

## 1. 목적과 완료 범위

ExternalAssets의 모델을 제작 기반으로 활용하여 **새 인물의 얼굴과 P09 헤어·의상을 갖춘 캐릭터**를 만든다. 원본 얼굴에 옷만 바꾼 결과를 새 캐릭터라고 제출하지 않는다. 원본의 정체성을 유지하라는 별도 요청이 있을 때만 얼굴 유지 방식을 선택한다.

기본 경로는 **별도 조형한 얼굴을 P09 바디·스켈레톤에 이식하고, P09 카탈로그의 헤어·방어구를 사용하는 방식**이다. 정규 반영까지 요청받았다면 검수 모델에서 멈추지 않고 정규 프리팹·제작 프리셋·월드 인스턴스·초상화까지 연결한다. 구현 반영과 미술 승인·Play Mode 검증은 별도로 보고한다.

일반 외형 제작은 [NPC 의상·외형 제작 지침](guide/NPC_APPEARANCE_AUTHORING_GUIDE.md), 검증은 [테스트 작업 지침](guide/TEST_VALIDATION_GUIDE.md), 코드와 콘텐츠 변경은 해당 저작 지침을 함께 따른다. 이 문서는 외부 모델의 얼굴 이식에 관한 추가 기준이다. 복사용 요청은 [요청 프롬프트](EXTERNAL_MODEL_P09_REQUEST_PROMPT.txt)를 사용한다.

## 2. 시작할 때 작성할 제작 카드

| 항목 | 기록할 내용 |
|---|---|
| 대상 | 캐릭터 이름, 역할, 신규 제작/기존 정규 교체 여부 |
| 원본 | ExternalAssets 실제 프리팹·FBX 경로, 리그, UV, 재질, 표정 목록 |
| 얼굴 방향 | 눈매·간격, 눈썹, 코, 입, 볼·턱선, 연령감, 유지할 식별 특징 |
| P09 기반 | 성별, 베이스 프리팹, 본 배열·bindpose, 얼굴 경로, 목 경계 |
| 슬롯 | 기본 헤어·색상·상의·팔·허리·다리·모자와 검수할 대체 조합 |
| 정규 연결 | 프리팹 GUID·fileID, BuildPreset, ActorDefinition, NPC 데이터, 대화·초상화 |
| 월드 | 씬별 인스턴스, SceneEntityId, 위치·회전, 외형·데이터 override |
| 공유 영향 | Variant, 중첩 프리팹, 다른 인물·농장·스폰 데이터의 공용 참조 |
| 복구 | 수정 파일 목록, 파일별 충돌 없는 백업 경로, SHA256, `.meta` |
| 검증 상태 | 미술 확인 / Editor / Play Mode / Player / 성능 각각의 상태 |

원본이 지정되지 않았으면 ExternalAssets를 직접 조사해 얼굴·표정·UV 품질이 적합한 후보를 고른다. 파일명이나 썸네일만으로 선택하지 않는다. BlendShape가 없는 모델은 표정 추가 저작이 필요하며, 이름만 채워 표정 지원으로 간주하지 않는다.

## 3. 경로 선택

| 경로 | 용도와 경계 |
|---|---|
| 새 얼굴 + P09 바디·헤어·의상 | 기본 경로. P09 스킨·의상 체형·카탈로그를 유지하고 얼굴을 맞춘다. 다온에서 사용한 방식이다. |
| 원본 바디에 P09 의상 피팅 | 원본 체형 유지가 명시된 경우. 의상 전체의 피팅·웨이트·관절·몸 가림을 다시 저작해야 한다. 본 이름 변경만으로 호환되지 않는다. |
| ExternalCharacterProfile | 원본 리그의 의상 그룹과 강체 장신구 조립. P09 의상 호환이나 새 얼굴 제작을 대신하지 않는다. 현재 외부 모델의 Actor 생성 경로는 NPC만 지원한다. |

P09 네이티브 경로는 `CharacterBuildConfig.ExternalModelPrefab`을 비우고 `FaceMeshProfile`을 사용한다. P09 바디가 되었다고 Player/Enemy의 무기·GAS·모션·충돌까지 검증된 것은 아니다.

## 4. 실제 연결 구조

```text
ExternalAssets 모델의 복제 메시·UV·표정
    → Blender 새 얼굴 조형 + ImageGen 얼굴/눈 알베도
    → P09 머리·목·본 배열에 맞춘 프로젝트 소유 Mesh/Material
    → P09FaceMeshProfileSO
    → CharacterBuildConfig.FaceMeshProfile / BuildPreset
    → AppearanceApplier.Apply
    → P09 헤어·방어구 선택 + 새 얼굴
    → 정규 프리팹의 외형만 갱신 → 기존 월드 배치에 상속
```

| 코드 | 현재 역할 |
|---|---|
| `P09FaceMeshProfileSO` | 얼굴 메시·재질·본·렌더러 경로와 선택적인 `hairAttachments`(부착 위치·배율), `fittedMeshes`(원본/피팅 메시 연결)를 보관한다. 모든 대상의 계약을 먼저 검사한 뒤 적용한다. |
| `CharacterBuildConfig` | 얼굴 프로파일을 프리셋에 보존하고 성별·외부 모델 경로와의 충돌을 검사한다. |
| `AppearanceApplier.Apply` | P09 슬롯·색상·톤 선택 후 얼굴 프로파일을 적용한다. 얼굴 재질은 프로파일의 값이 우선한다. |
| `PreviewSceneController` | 얼굴 프로파일 변경도 미리보기 갱신 조건에 포함한다. |
| P09 빌더 외형 탭 | `교체 얼굴 메시` 필드로 프로파일을 지정한다. 기존 통합 툴 런처에서 실행한다. |

프로파일은 자동 리타게터가 아니다. 올바른 P09 본 순서·bindpose·좌표로 저작된 메시를 입력해야 한다. 다온의 여성 77본 배열, 36개 P09 표정, 배율 0.93을 다른 모델의 공통 상수로 복사하지 않는다. 실제 선택한 베이스에서 다시 읽는다.

## 5. Blender·ImageGen 제작 절차

1. 원본과 정규 에셋을 직접 변형하지 않고 프로젝트 소유 복제본을 만든다. 원본·복제본의 정면, 35도, 측면을 같은 카메라·조명으로 기록한다.
2. ImageGen으로 얼굴 방향을 정하고 필요한 알베도를 만든다. UV 편집은 기존 아일랜드·눈·입 위치를 보존하며, 동공 아틀라스의 타일 배열과 별도 하이라이트도 확인한다. 생성 이미지는 3D 제작 참고 또는 텍스처이지 게임 적용 결과가 아니다.
3. Blender에서 눈·눈썹·코·입·광대·턱선을 실제로 조형한다. 새 얼굴의 차이는 텍스처 색이나 화장만으로 만들지 않는다. 중립형과 관련 BlendShape에 일관된 변형을 적용한다.
4. 머리만 추출할 때 얼굴·눈·속눈썹·눈썹·입안·목을 확인한다. 재질 슬롯 번호를 다른 모델에 그대로 복사하거나 몸통과 목을 함께 잘라 틈을 만들지 않는다.
5. 소스와 P09의 실제 좌표계·단위·스킨 행렬을 확인한 뒤 이식한다. 정점 위치뿐 아니라 델타·노멀·탄젠트, 웨이트 인덱스와 bindpose를 대응시킨다. Humanoid라는 이유로 본 배열이 같다고 가정하지 않는다.
6. 얼굴·두피의 자연스러운 형태를 보존하고 헤어의 부착 위치·앞뒤 폭부터 맞춘다. 틈을 메우려고 두피를 늘리거나 P09 표면까지 강제로 투영하지 않는다. 위치·배율로 해결되지 않는 경우에만 프로젝트 소유 헤어 메시를 Blender 등에서 피팅한다. 실제 선택된 캐릭터·프리셋·얼굴 프로필·헤어 번호와 씬 override를 먼저 확인하고, 다른 캐릭터의 화면을 근거로 승인된 모델을 다시 변형하지 않는다. Lili의 적용 사례는 10절을 따른다.
7. P09 목 연결면을 맞춘다. 목 회전·입 벌림에서도 틈과 중복 표면이 없는지 확인한다. 검은 띠나 장식으로 결함을 가리지 않는다.
8. P09 표정 이름·순서를 사용하는 소비자가 있는지 조사하고 대응 표정을 저작한다. 이름 alias는 실제 표정의 시각적 동등성을 보장하지 않는다. 누락 표정은 저작하거나 미지원으로 명시한다.
9. 웨이트 합, 유효 본 인덱스, UV·재질 슬롯, 표정 프레임·유한 좌표, 셰이더와 컬링 bounds를 검사한다. 필요 없는 몸통 BlendShape·중복 본·재질은 사용 여부를 확인한 뒤 정리한다.
10. `.blend`, 교환 데이터/FBX 또는 Unity 메시, 제작 스크립트·파라미터, 텍스처·프롬프트·출처를 저장한다. Blender MCP 응답이 끊기면 파일과 작업 상태를 먼저 확인한다. 같은 연산을 중복 적용하지 않는다. 사용 가능한 background Blender CLI로 대체했다면 실제 사용 경로를 기록한다.

시선과 피부색 검수에는 다음을 추가한다.

- 원본 프리팹의 눈 본 회전이 중립인지 확인한다. 다온은 양쪽 눈의 17도 시선 포즈가 정점에 구워져 옆을 보는 결함이 있었다. 이식 기준은 바인드 자세이며, 눈·하이라이트와 표정 델타를 함께 변환한다. 두피 투영은 얼굴 표면에만 적용하고 안구·하이라이트는 제외한다.
- 얼굴과 몸의 알베도뿐 아니라 최소 조도·그림자 색·경계·강도를 같은 조명에서 비교한다. 외부 모델 재질의 부모에서 상속한 설정도 읽는다. 공용 몸 재질 대신 해당 인물 얼굴 재질을 조정한다.
- 다온의 기존 메시 복구는 `ArtSource/ExternalCharacterBuilder/DaonFace/repair_p09_gaze.csx`, 피부 재질 보정은 `match_p09_skin.csx`, 보존·대칭 검사는 `validate_gaze_skin.csx`에 기록되어 있다. 상세 결과와 미검증 범위는 같은 폴더의 `GAZE_SKIN_RESULT.txt`를 따른다.

## 6. P09 빌더와 슬롯 검수

- 프로젝트 소유 Mesh/Material을 `P09FaceMeshProfileSO`에 연결하고 `ValidateProfile()`과 실제 베이스의 `Apply()`를 확인한다.
- 기본 헤어·방어구를 고정한 BuildPreset과 독립 검수 프리팹을 만든다. 프리셋을 다시 불러온 뒤 헤어·상의·하의를 바꾸어도 새 얼굴이 유지되어야 한다.
- 기본 구성 외에 최소한 실루엣이 다른 헤어와 의상 조합을 확인한다. 긴 머리·투구·망토는 별도 관통·물리 검수가 필요하다. 일부 조합의 통과를 전체 P09 카탈로그 호환으로 보고하지 않는다.
- 얼굴 색·눈 재질은 얼굴 프로파일이 소유한다. P09 피부색 선택과 별개이므로 목·손·노출 피부의 색 차이를 확인한다.
- 무효 경로·본 순서·성별은 저장 전에 실패시킨다. 메시 교체 실패 후 일부 얼굴만 새 메시로 남기지 않는다.

### 기존 모델의 명시적 사용 제약

**Bokusei·Hichi·Nenmir의 P09 얼굴은 두피·뒤통수 일부가 열린 구조이며, 이를 가리는 헤어를 전제로 사용한다. 민머리 또는 열린 부위가 드러나는 노출형 헤어는 현재 지원하지 않는다.** 해당 구성을 사용하려면 별도 두피·뒤통수 메시 저작과 표정·스킨·다각도 검수를 먼저 완료해야 한다. 목 접합 검사나 빌더 슬롯 연결 통과를 두상 전체가 닫혀 있다는 판정으로 사용하지 않는다.

| 모델 | 기본 프리셋 헤어 | 대체 프리셋 헤어 |
|---|---|---|
| Bokusei | Hair 13 | Hair 03 |
| Hichi | Hair 01 | Hair 13 |
| Nenmir | Hair 03 | Hair 11 |

P09 캐릭터 빌더의 `프리셋 불러오기`에서 `Assets/10.Datas/Actor/ExternalModels/{이름}_P09_BuildPreset.asset` 또는 `{이름}_P09_Alternate_BuildPreset.asset`을 선택한다. 두 프리셋 모두 같은 `{이름}_P09_Face.asset`을 `FaceMeshProfile`로 참조하고 `ExternalModelPrefab`은 비어 있는 P09 네이티브 경로다. 사용 제약은 해당 프리셋의 `description`에도 기록한다. 헤어 선택을 자동 차단하는 기능은 아니므로 제작자가 조합별 가림 상태를 확인한다.

이 연결은 외형 저작용이다. 정규 NPC·플레이어 연결, 대화·GAS·무기·물리 통합 완료를 뜻하지 않는다. 검증 근거는 `output/P09Validation/RESULT.txt`와 해당 모델의 `*_BareNeck.png`를 따른다.

### Review 씬의 기본 몸체 검수본

- 의상을 갖춘 기본·대체 모델과 별도로 **갑옷 없는 기본 몸체 검수본**을 같은 Review 씬에 배치한다. 목도리·칼라·머리카락이 얼굴과 목의 접합선을 가리지 않게 한다.
- 여성 P09의 기준은 `Mesh/Female/Body` 아래 `Female_Body_Head`(선택된 얼굴 하나), `Female_Body_Chest`, `Female_Body_Leg`만 표시하는 구성이다. `Female_Body_Arm` 및 손·발 보조 파츠, Armor·Hair·무기·장식과 `Nakid_Body` 대체 메시를 숨긴다. 다른 베이스는 실제 대응 경로를 조사한다.
- 검수본은 동일한 얼굴 메시·재질·리그를 참조하는 별도 씬 인스턴스로 만든다. 가시성 override를 정규 프리팹에 Apply하지 않는다. 기존 의상 모델·씬 GUID·사용자 배치를 보존한다.
- 모델 사이에 간격을 두고 검수 카메라에서 함께 구분되도록 배치한다. 정면·좌우·후면·턱 아래에서 피부색, 목 경계, 겹면·열린 틈을 확인하고 고개 회전·입 벌림에서도 검사한다.
- 저장 후 재로드하여 활성 렌더러가 의도한 Body 부위뿐인지 확인한다. 갑옷이 틈을 가린 상태의 통과를 목 연결 검수로 대체하지 않는다.

## 7. 정규 프리팹과 월드 반영

1. 모든 참조를 먼저 조사한다. 같은 프리팹을 다른 이름의 NPC가 사용하거나 Variant·농장 작업자가 공유할 수 있다. `actorId`와 실제 배치 데이터를 함께 확인한다.
2. 수정 대상마다 독립 백업 파일을 만들고 SHA256으로 원본 일치를 검증한다. 배열 인덱스를 재사용하여 백업을 덮어쓰지 않는다. 복사 시 덮어쓰기를 거부하고 manifest의 경로·개수를 검사한다.
3. 검수본의 외형만 `LoadPrefabContents`로 연 정규 프리팹에 적용한다. 전체 빌더 재생성으로 NPC 데이터·GUID·fileID·행동·서비스 연결을 교체하지 않는다.
4. 정규 BuildPreset의 `FaceMeshProfile`과 최종 슬롯을 함께 갱신한다. ActorDefinition·NpcData·GAS·대화·모션 데이터는 외형 교체만으로 다시 생성하지 않는다.
5. 다른 인물이 공유하는 원본이면 기존 얼굴·헤어를 해당 Variant/인스턴스 override 또는 별도 원본으로 보존한다. 재질만 다르고 메시를 상속하는 인물도 포함한다.
6. 실제 다온과 같은 대상 인스턴스는 새 외형을 상속하도록 한다. 외형 override만 검토하고 위치·회전·SceneEntityId·NPC 데이터·대화 override는 보존한다. 모든 override를 일괄 Apply/Revert하지 않는다.
7. 예전 제작표의 SceneEntityId와 실제 씬이 다르면 현재 씬의 식별을 보존한다. 오래된 연결 도구를 그대로 실행해 같은 인물을 새 ID로 중복 배치하지 않는다.
8. 초상화가 이전 얼굴이면 현재 모델을 기준으로 갱신하되 Sprite GUID와 화자 연결을 보존한다. 이미지를 편집할 때 ImageGen을 사용하고 실제 게임 캡처와 구분한다.
9. 저장 후 프리팹·씬을 다시 로드한다. 참조·배치·새 얼굴·다른 인물 외형을 확인하고, 검증이 만든 무관한 재직렬화만 분리해 검토한다. 미저장 사용자 씬을 임의 저장하거나 닫지 않는다.

P09 빌더의 삭제·교체 경로를 수정하는 작업은 별도 스테이징·백업/복구·중간 실패 테스트 설계를 먼저 세운다. 외형 승격에 전체 빌더를 다시 실행하는 것을 기본 절차로 삼지 않는다.

## 8. 검증과 결과 보고

| 단계 | 확인할 것 |
|---|---|
| 정적/Editor | Unity 컴파일, Missing Script/본/재질 누락, Avatar, 웨이트·bindpose, 표정 계약·유한 좌표, 컬링 |
| 실제 렌더 | 정면·35도·측면·후면·전신, 눈 감기·미소·발음, 목 좌우 회전, Idle·보행 표본 |
| 저장/참조 | 정규 GUID·외형 외 fileID/컴포넌트 값, 프리셋 재로드, 씬별 ID·위치·대화 override, 공유 인물 보존 |
| Play Mode | 정규 등장 경로, 대화·야간·이동·시선·립싱크·카메라 거리 변화·재진입·슬롯 변경 |
| 성능/Player | 실제 NPC 수의 활성 렌더러·삼각형·재질·본·BlendShape·메모리, 변경 위험에 따른 Player 검증 |

사람의 판단이 필요한 Play Mode·미술 검수는 테스트 지침에 따라 사용자 테스트 명세를 제공한다. Editor 샘플, 이름 검사, Console 오류 0만으로 미술·대화·게임 플레이 검증을 통과했다고 하지 않는다. 실패·미수행·수행 주체를 구분한다.

## 9. 다온 적용 사례

- 정규: `Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab`
- 정규 프리셋: `Assets/10.Datas/Actor/ScenarioResidents/Npc_Daon_BuildPreset.asset`
- 얼굴: `Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset`
- 독립 모델/프리셋: `Daon_P09Compatible.prefab`, `Daon_P09_BuildPreset.asset`
- 제작 원본: [DaonFace](../../ArtSource/ExternalCharacterBuilder/DaonFace/README.txt), [P09 호환 기록](../../ArtSource/ExternalCharacterBuilder/DaonFace/P09_COMPATIBILITY.txt)
- 정규 반영 근거: `output/DaonProductionIntegration/`의 `promotion.txt`, `world_before.txt`, `world_after.txt`, `World_Lake_Daon.png`.

정규 GUID `dc565f06dcbb1904099feee3bba83d1c`, local fileID 1,862개와 외형 외 컴포넌트 906개의 직렬화 값을 보존했다. 생명의 호수와 R03 검증 씬의 실제 다온은 새 얼굴을 상속한다. 보라·유나의 항구 배치 및 두 Variant, FarmWorker_0 Variant는 이전 외형을 보존한다. 실제 대화·야간 Play Mode·군중 성능·Player 실행까지 승인된 사례로 확대 해석하지 않는다.

## 10. Lili 얼굴·헤어 맞춤 사례

사용자가 현재 Lili의 얼굴·헤어 맞춤 상태를 확인했다. 직전 추가 결함 신고는 대상 착오로 정정되었으므로 추가 피팅은 진행하지 않는다. 이 확인을 다른 캐릭터 또는 전체 헤어 카탈로그의 미술 승인으로 확대하지 않는다.

- 얼굴 프로필: `Assets/10.Datas/Actor/ExternalModels/Lili_P09_Face.asset`
- 기본/대체 프리셋: `Assets/10.Datas/Actor/ExternalModels/Lili_P09_BuildPreset.asset`, `Lili_P09_Alternate_BuildPreset.asset`
- Review 씬: `Assets/01.Scenes/Tests/Equipment/Lili_P09_Review.unity`. 의상 모델 두 개와 `Lili_P09_BodyOnly` 기본 몸체 검수본을 함께 배치한다.
- 두피를 P09 표면으로 늘리던 처리는 제거하고 목 접합 보정은 유지했다. 헤어 위치와 앞뒤 폭은 얼굴 프로필의 `hairAttachments`에서 저작한다.
- 현재 Hair 03은 머리 본 로컬 오프셋 `(0.02, -0.01, 0)`, Hair 11은 `(0.02, 0, 0)`, 두 항목의 배율은 `(0.88, 1, 1)`이다. 해당 Lili 조합의 값이며 다른 얼굴·헤어의 공통 상수가 아니다.
- 제작·재현 기록: [LiliFace README](../../ArtSource/ExternalCharacterBuilder/LiliFace/README.txt), `fit_hair.csx`, `create_body_review.csx`. 기본 몸체 표시 변경은 Review 씬 인스턴스에만 저장한다.
- 사용자 확인은 현재 외형에 대한 것이다. Play Mode·MagicaCloth·전체 헤어 조합 검증까지 완료된 것으로 보고하지 않는다.
