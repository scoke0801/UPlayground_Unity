# 드롭 월드 모델 확장 — 2026-11-13

**기술 제작·연결 완료 / 실제 플레이·최종 미술 인수 대기.** 모델65종(신규6·구매 P09 가공59)을 만들고 같은 원형의 추가9종에 공유하여 ItemSO74종을 연결했다. 최종 전체 ItemSO127종은 worldPrefab96종·기존 장착 프리팹 대체31종으로 모델 없는 항목0이다.

[전체 Unity 편집 렌더 검수표](../../ArtSource/DropItems/Expansion20261113/ReviewSheet.png) · [아이템별 출처·삼각형·상태](../../ArtSource/DropItems/Expansion20261113/MANIFEST.md)

## 범위와 정본

사용자의 “모델 제작 스킬로 만들어서 연결” 요청에 따라 [이전 조사](drop-item-presentation.md)의 미연결 아이템을 제작했다. 시작 대상은 Head_001을 포함한65종이었다. 동시 진행 작업에서 Head_011 및 무기B2/B3 파생8종이 추가되어, 아이콘·장비 슬롯·무기 종류가 같은 모델을9종에 공유했다.

- [시작65종](../../ArtSource/DropItems/Expansion20261113/targets.initial65.tsv), [최종74종](../../ArtSource/DropItems/Expansion20261113/targets.tsv), [9종 공유 원형](../../ArtSource/DropItems/Expansion20261113/alias-plan.tsv).
- [저작 지침](../../Assets/docs/guide/DROP_ITEM_WORLD_MODEL_AUTHORING_GUIDE.md), [제작 스킬](../../.claude/skills/drop-item-world-model/SKILL.md), [공통 하네스](../../Tools/Harness/README.md), [기존22종 manifest](../../ArtSource/DropItems/MANIFEST.md).
- 기존 스킬의 기본 비장비 범위는 이번 사용자 요청에 한해 미연결 장비의 드롭 외형까지 확장했다. equipmentPrefab·장착 설정은 수정하지 않았다.

## 제작과 출처

| 구분 | 수 | 반영 |
|---|---:|---|
| 신규 저작 | 6 | 쌍도끼·쌍검·창·채찍·당근·호박. Blender 원본/FBX/512² ImageGen 공용 알베도/URP Lit 재질/표시 프리팹 |
| 구매 원형 가공 | 59 | P09 방어구. 피부 서브메시·리깅 제거, UV/원본 재질 유지, 팔 하향·장갑 모으기·신발 빈 구간 압축을 정적 메시로 굽기 |
| 모델 공유 | 9 | 동일 아이콘·슬롯·무기 종류를 실제 데이터로 대조하여 위 모델 참조 공유 |

구매 원본은 P09_250802.fbx이며 [방어구 인수 기록](../../ArtSource/DropItems/Expansion20261113/Armor/handoff-summary.json)에 GUID·원형·제거 피부·재질·저작 .blend·FBX 경로를 남겼다. 새로 창작한 모델로 계산하지 않는다. Pants_002는 이름이 비슷한 외부 재질 대신 구매 FBX 내부 재질 GUID `e5da2379dc44b4e4d9a3b9a9eac5e5b9`/fileID `-3042969203894896299`를 정확히 참조한다.

신규6종은416~1,148삼각형, 방어구59종은104~3,061삼각형(합111,952)이다. 모두 정적 메시다. 단순한 창/당근은 불필요한 면 추가 없이500삼각형 미만을 유지했다. 원본 의상의 열린 목/소매/허리 경계와 미러 UV는 보존한다. 원본 비다양체8종은 가공 전후 수를 확인해 추가 결함 없이 보존했으며, 면적0면만 제거했다.

[실제 ImageGen 프롬프트](../../ArtSource/DropItems/Expansion20261113/NewModels/imagegen-prompt.txt), 생성 원본·512² 축소본을 보존했다. 현재 개발 지침에 따라 내장 ImageGen을 사용했으며 절차 텍스처로 대체하지 않았다. [신규 모델별 기술 결과](../../ArtSource/DropItems/Expansion20261113/new-model-results.json)를 따른다.

## 연결·자동 검증

<!-- DROP_PROGRESS_BEGIN -->
최신 통합: 대상74종 중 **74종 연결·표시 생성 검사 통과**. 개별 출처·삼각형·렌더는 [전체 manifest](../../ArtSource/DropItems/Expansion20261113/MANIFEST.md)를 따른다. 실제 플레이와 최종 미술 인수는 사용자 테스트 대기다.
<!-- DROP_PROGRESS_END -->

| 검사 | 결과·근거 |
|---|---|
| 표시 생성 함수 | 실제 GroundDropRepresentation.BuildVisual을 편집 환경에서 호출, 모델 생성·긴 변0.55m·유효 재질/셰이더·표시 하위 로직/콜라이더/Animator0, [74/74 통과](../../ArtSource/DropItems/Expansion20261113/unity-model-audit.tsv) |
| 저장 참조 지속 | 대상74 ItemSO 강제 재임포트 후74/74 유지. 전체127종의 월드 표시 경로 누락0 |
| Unity 렌더 | [74개 고정 카메라 편집 렌더](../../ArtSource/DropItems/Expansion20261113/UnityReview/) 생성·검수표 확인. 실제 Play 화면으로 간주하지 않음 |
| 신규6종 Blender | UV범위외·비다양체·영면·안쪽면0, 메시/재질 각1·스케일1 |
| 방어구59종 | 최종 .blend/FBX/개별 JSON/프리뷰 각59개, FBX 재읽기 통과. [Unity FBX/meta·제작 원본 해시59/59](../../ArtSource/DropItems/Expansion20261113/Armor/unity-files-final.json) |
| 구매 원본 보존 | FBX/meta·원본 재질/텍스처 등275개 현재 해시가 가공 전과275/275 동일 |
| 연결 전후 데이터 | [70개 고유 아이템/80쌍](../../ArtSource/DropItems/Expansion20261113/connect-data-preservation.json)에서 worldPrefab 외 필드 동일 |
| EditMode 드롭 표현 | job `8e6632b2f7334da1af9ddfe8a4316bad`, 8/8 통과 |
| EditMode 드롭 규칙/원장 | job `091156c6be614a8e9413ca320de61537`, 13/13 통과 |
| 최종 Editor | 컴파일 중 아님, Console 오류0. 기존 툴 등록/퀘스트 메뉴 정책 경고가 있어 경고0으로 보고하지 않음 |
| 독립 QA | 74 GUID/저장 참조·공유9·65모델·재질·275원본 해시·70종 데이터·Test Runner 결과 원본 대조, 차단 결함0. 실제 Play/미술 인수 전이므로 전체 판정은 부분 완료 |

아티스트 인수 JSON의 Unity56종 스냅샷은 마지막3종 연결 전의 기록이다. 최종 수치는 위의 `unity-files-final.json`59/59가 정본이다. 최초4종은 동일한 직전/직후 JSON을 확보하기 전 연결되어70종 비교 판정에 합산하지 않는다. 농작물2종의 시작 YAML은 worldPrefab 외 동일이다. 상의001/쌍도끼는 동시 밸런스 변경/기본 필드 직렬화와 섞여 전체 동일 판정을 하지 않는다. 다른 작업의 장비 수치/옵션 변경은 원복하지 않았다.

## 결함과 복구

- 첫 당근 잎 회전축 오류는 실패 프리뷰/원본을 보존하고 v2에서 수정했다.
- 구매 원형의 T포즈 소매는 원본 리그로 팔45도 하향 후 정적화했다. 장갑은 손끝을 아래로 모았고, 일부 신발은 지오메트리가 완전히 없는 구간만 압축해 상단 옷이 멀리 떠 있지 않게 했다.
- AssetPreview의 높은 시점을 축 오류로 오인한 판단은 실제 Unity 고정 카메라 렌더로 철회했다.
- 배치의 다음 FBX 재임포트 전에 미저장 SO 참조가 사라지는 현상10건을 최종 검사에서 발견했다. 기존 제작 프리팹 참조만 복구하고 연결 즉시 SetDirty/SaveAssetIfDirty하도록 절차를 보완했다. 재임포트 후74/74로 지속을 확인했다.
- Hot Reload 대상 밖이던 이전 코드 변경은 정상 컴파일로 확인했다. 이전 테스트 초기화 실패·테스트 Reflection 오버로드/비활성 콜백 호출 오류를 수정한 뒤8/8 결과를 얻었다. EditMode 비활성 콜백은 직접 호출했으므로 실제 Play 수명 확인은 별도다.

## 사용자 확인 — 미수행

기존 본편의 드롭/인벤토리 버리기 재현 경로를 사용한다. 저장 초기화는 필요 없다. 다음은 모두 사용자 테스트 대기이며 자동 검사로 대신하지 않는다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| DM01 | 상의·장갑·모자·하의·신발과 신규 무기/농작물을 각각 드롭 | 메달 대신 맞는 모델. 등장·착지·회전 중 피부/리그/분리된 잘못된 부품·핑크 재질이 보이면 실패 |
| DM02 | 3m/8m, 풀밭·흙, 낮/밤에서 관찰 | 아이콘과 정체성이 이어지고 모델·이름표가 구분됨. 미술 품질은 사용자 판단 |
| DM03 | 이름표 클릭·패드 포커스/획득·펫 수거·거리 이탈·팝업 열고 닫기 | 기존 획득/입력 유지, 숨긴 이름표의 연결선이 남지 않음 |
| DM04 | B2/B3 무기와 머리 장비 파생형을 드롭하고 저장/재진입 | 올바른 공용 모델·해당 아이템의 이름/희귀도/획득 결과 유지 |

회신: `DM번호 / 통과·실패·미수행 / 실제 결과 / 입력 장치·화면`. SpawnItems 실제 등장/획득, 실기 성능·최종 미술 인수는 수행하지 않았다. Player/Addressables 빌드와 커밋도 하지 않았다.

## 부산물·보존

[정리 기록](../../ArtSource/DropItems/Expansion20261113/cleanup.json): 최종 독립 씬 원본59개를 보존한 뒤 재생성 가능한 중간 객체 라이브러리70개/33,819,934bytes(32.25MiB)를 정리했다. 미참조를 확인한 최초 실패 당근 FBX/meta도 Assets에서 제거했다. 실패 프리뷰·로그와 자세 비교14폴더/16,414,562bytes(15.65MiB)는 재현·비교 근거로 보존했다. 구매 원본·최종 .blend/FBX/meta·생성 이미지·데이터 기준선·사용자 변경을 보존했다.

최종 준비표: `output/harness/20261113-231434-bc04de78/`. 동시 밸런스 데이터 변경으로 이전 준비표의 ItemSO 해시가 달라져 다시 생성했고,74개 모델 참조가 유지됨을 별도로 재확인했다. 이전 준비표 두 묶음은 파일2개씩만 있는지 확인하고 정리했다. 모델/프리팹/연결 ItemSO/meta·최종 원본·프롬프트·렌더 검수표를 포함한다. 준비표 해시는 검사 실행 증명이 아니며 위의 실제 검증 결과와 구분한다.

