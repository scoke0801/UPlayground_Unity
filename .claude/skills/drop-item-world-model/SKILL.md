---
name: drop-item-world-model
description: "UPlayGround 바닥 드롭 아이템(ItemSO)에 3D 월드 모델을 만들어 연결한다 — Codex CLI 알베도 타일 → Blender 백그라운드 CLI 모델·아틀라스·FBX → Unity 임포트·머티리얼·프리팹·ItemSO.worldPrefab 연결 → Play Mode 드롭 검증·시각 검수 → MANIFEST 기록. 다음 상황에서 사용한다: '드롭 아이템 모델 만들어줘', '바닥 아이템 3D로 바꿔줘', '메달/아이콘 대신 모델로', 'worldPrefab 연결해줘', '다음 배치 진행해줘(드롭 모델)', 'Leather/Feather/포션 월드 모델'. 장비(EquipmentSO) 월드 모델 변경, GroundDropRepresentation 코드 자체 설계 변경, 일반 환경 소품 제작에는 사용하지 않는다."
---

# 드롭 아이템 월드 모델 제작

정본 지침은 `Assets/docs/guide/DROP_ITEM_WORLD_MODEL_AUTHORING_GUIDE.md`다. **작업 전 지침을 끝까지 읽고**, 진행 상태는 `ArtSource/DropItems/MANIFEST.md`에서 이어받는다. 이 스킬은 지침을 실제로 통과시킨 실행 절차와 함정을 담는다(파일럿 AnimalMeat, 2026-11-11).

## 0. 전제 (이미 구현됨 — 다시 만들지 않는다)

- `ItemSO.worldPrefab`(GameObject) 필드가 있다.
- `GroundDropRepresentation`은 `worldPrefab`이 있으면 메달 대신 모델을 띄운다. 긴 변을 `tokenSize`(0.55m)에 맞추고 바운드 중심을 회전축에 둔다. 콜라이더·스크립트·애니메이터는 제거하고, 모델이 없으면 메달로 폴백한다.
- 따라서 모델의 **절대 크기는 무관하고 비율·피벗만 중요**하다.

## 1. 사전 점검 (세션마다)

| 확인 | 방법 |
|---|---|
| Blender | `mcp__blender__get_blendfile_summary_path_info`로 열린 파일을 본다. 사용자 작업이 열려 있으면 **대화형 세션은 건드리지 않고** 백그라운드 CLI `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe -b --factory-startup -P <script>`로 작업한다. |
| Codex | `codex exec`로 실제 PNG가 생성되는지 확인한다. 실패하면 절차 텍스처로 대체하지 말고 중단·보고한다. |
| Unity | `read_console` error 0, Play Mode 아님. 다른 작업의 미완성 코드로 컴파일 에러가 나면 **고치지 말고** 기다린 뒤 강제 refresh한다. |
| 진행 상태 | MANIFEST의 상태표와 "다음 작업"을 읽는다. |

## 2. 텍스처 (Codex CLI)

출력은 `ArtSource/DropItems/<ItemId>/codex/`로 제한한다.

```bash
mkdir -p "ArtSource/DropItems/<ItemId>/codex" && codex exec --skip-git-repo-check --sandbox workspace-write \
  -C "C:/UsingProject/UnityProject/UPlayground/ArtSource/DropItems/<ItemId>/codex" "<프롬프트>" 2>&1 | tail -5
```

프롬프트는 지침 §6 템플릿에 아래 두 줄을 앞에 붙인다(파일럿에서 검증된 형태):

```text
이미지 생성 도구로 이미지를 1장 생성하고, 512x512 PNG로 리사이즈해 현재 작업 디렉터리에 <ItemId>_Tile.png 이름으로 저장하라. 다른 파일은 만들지 마라.
표면: <재질>. 손그림 느낌의 스타일라이즈드 게임 아트, 사실적 사진 아님.
```

- 결과는 Read로 직접 보고 판정한다(평평한 알베도인지, 하이라이트·글자 없는지).
- 실제 사용한 프롬프트 전문을 MANIFEST에 붙인다.

## 3. 모델 (Blender CLI 스크립트)

> 2차 세션부터는 공용 라이브러리 `ArtSource/DropItems/_lib/dropitem_lib.py`를 쓴다(아이템 스크립트는 `build_shape`·영역·칠하기만, 예: `Feather/build_Feather.py`). 일괄 실행 `bash ArtSource/DropItems/_lib/build_all.sh <ItemId>...`, Codex는 `bash ArtSource/DropItems/_prompts/run_codex.sh <ItemId>`. 라이브러리에는 섬별 법선 자동 반전, `solidify_apply(even=False)`, 납작한 물체용 `tilt_parts`(45° 세우기 — 눕힌 판은 플레이 카메라에서 선으로만 보임), `make_slab`·`make_tube`·`make_sheet`·`uv_caps_and_sides`가 있다. 아래 템플릿은 라이브러리 이전 방식이다.

`templates/build_template.py`를 `ArtSource/DropItems/<ItemId>/build_<ItemId>.py`로 복사하고 `build_shape()`와 아틀라스 영역만 아이템에 맞게 바꾼다. 실제 사례는 `ArtSource/DropItems/AnimalMeat/build_AnimalMeat.py`다.

- **단일 머티리얼 + 512² 아틀라스**: Codex 타일은 큰 영역(예: 좌 3/4)에 1회 매핑한다. 뼈·단면·라벨 같은 색 구획은 Blender에서 numpy로 단색 또는 절차 생성해 별도 영역에 넣는다. 부품마다 `smart_project` 후 영역 사각형에 맞춰 축소하므로 UV가 겹치지 않는다.
- 부품을 따로 만든 뒤 join하고, 피벗은 **바닥 중심**으로 정점을 옮긴다(+Z 위, 긴 변 X).
- 스크립트는 기존 FBX·텍스처가 있으면 종료한다. 내가 만든 파일을 다시 만들 때만 `DROPITEM_ALLOW_REBUILD=1`을 쓴다.
- 출력 `DROPITEM_RESULT {json}`의 기준: tris 500~1,500 / uv_out_of_range 0 / non_manifold_edges 0 / zero_area_faces 0 / inward_faces 0 / material_slots 1 / scale 1.
- 법선 검사는 **섬별 부호 부피**로 한다. 전체 중심 기준 휴리스틱은 여러 부품 메시에서 오탐한다(파일럿에서 뼈 마디 60면 오탐).
- `_preview.png`(EEVEE)는 실루엣 확인용일 뿐 검수 근거가 아니다.

실행:

```bash
"/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup \
  -P "C:/UsingProject/UnityProject/UPlayground/ArtSource/DropItems/<ItemId>/build_<ItemId>.py" 2>&1 \
  | grep -E "DROPITEM_RESULT|Error|Traceback|EXISTS"
```

## 4. Unity 연결

`templates/unity_connect.cs`의 본문을 `mcp__UnityMCP__execute_code`로 실행한다. 상단의 `item`과 `soPath`만 바꾼다.

- 하는 일: 텍스처 sRGB·Mip·max 512·압축 / ModelImporter scale 1·애니·카메라·라이트·콜라이더 끔·머티리얼 임포트 None / `Materials/<ItemId>.mat`(URP/Lit, Smoothness 0.25) 생성 / `Assets/03.Prefabs/Item/World/WorldItem_<ItemId>.prefab` 저장 / `SerializedObject`로 `worldPrefab`만 설정.
- 머티리얼이나 프리팹이 이미 있으면 `EXISTS`를 반환하고 아무것도 하지 않는다(덮어쓰기 금지).
- 임포트 직후 MCP 연결이 끊기거나 에디터가 무응답이 될 수 있다. 다시 실행하지 말고 응답 복귀를 기다린 뒤 **결과만 조회**한다. 생성 여부는 `File.Exists`와 `LoadAssetAtPath`로 확인한다.
- 데이터 보존은 `git diff -- <ItemSO 경로>`로 확인한다. 추가된 줄이 `worldPrefab` 한 줄뿐이어야 한다.
- 변형군(HP 포션 등)은 FBX·메시를 공유하고 머티리얼(`_BaseColor` 또는 텍스처)만 나눈다. 프리팹은 아이템마다 만든다.

## 5. Play Mode 검증과 시각 검수

**맵 선택**: CombatTestMap은 `SceneManager.CurrentMapID`가 비어 있어 `GroundDropManager.RefreshWorld`가 외형을 만들지 않는다. 그래서 드롭 검증에는 쓸 수 없다. `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`를 **단독(Single)으로** 열고 Play하면 LakeOfLifeScenario 월드로 바로 들어간다. `Boot.unity`를 함께 열면 BootLoader가 Title로 보내므로 열지 않는다. 진입 확인은 `SceneManager.Instance.CurrentMapID == "LakeOfLifeScenario"`와 `PlayerActor`(네임스페이스 `UPlayGround`) 존재로 한다. Play 진입·종료 후 에디터가 한동안 무응답이 되므로 응답 복귀를 기다린다.

**대기 스크립트 주의**: Bash에서 `powershell -Command "$p=..."`처럼 큰따옴표 안에 `$`를 쓰면 bash가 먼저 전개해 깨진다. `(Get-Process -Id <pid>).Responding`처럼 `$` 없는 형태로 쓴다.

순서(`execute_code`, 네임스페이스는 `UPlayGround.Manager.Services` / `UPlayGround.Manager.IGroundDropService`):

1. **펫 비활성화**: `GameObject.Find("Pet_Monkey(Clone)").SetActive(false)`. 펫이 드롭을 즉시 자동 수거해서, 생성 직후 `collected=True`가 되고 외형이 사라진다. 이 결과 자체로 펫 획득 경로가 정상임을 확인할 수 있다.
2. **생성**: `SpawnItems(list, player.position + camFwd*3f, "<고유 sourceId>", null)`. sourceId가 겹치면 무시되므로 `Time.frameCount`를 붙인다. 비교용으로 메달 아이템 1개(예: Leather)를 옆 1.3m에 함께 생성한다.
3. **확인**: `Visual/Token/WorldModel` 트랜스폼의 scale × 원본 긴 변이 0.55m인지 본다.
4. **캡처**(`manage_camera` screenshot, `output_folder: ArtSource/DropItems/<ItemId>/Review`):
   - 근거리: `view_position` = 토큰에서 플레이어 쪽 3m, 높이 +1.6m / `view_target` = 토큰
   - 중거리: 8m, 높이 +2.7m
   - 밤: `GameTimeManager.SetTotalGameMinutes(total - minuteOfDay + 1380)`(23:00) 후 근거리를 다시 캡처
   - 회전: `batch: orbit`, `orbit_angles 6`, `orbit_elevations [25]`, `orbit_distance 1.2`, `orbit_fov 40`
   - 풀숲: 풀 위 지점에 다시 생성해 근거리 캡처(파일럿에서 누락된 항목)
   - 실제 카메라 리그 시점은 피치가 제각각이라 판독이 어려울 수 있다. 위치 지정 캡처를 썼다면 보고에 그렇게 밝힌다.
5. **획득**: 리플렉션으로 `_representations`에서 WorldModel이 있는 dropId를 찾아 `CanPlayerCollect` → `TryPlayerCollect`. 루트 GroundDrop 프리팹의 자체 콜라이더 1개는 정상이고, WorldModel 하위 콜라이더·스크립트는 0이어야 한다.
6. **원복**: 시간 601분 등 원래 값, 펫 재활성(`Resources.FindObjectsOfTypeAll<Transform>`로 비활성 오브젝트를 찾는다), Play 종료, 씬 구성 원복.
7. **테스트**: Play 종료 후 EditMode `ItemDropResolverTests`, `GroundDropLedgerTests`를 실행한다(Play 중에는 실행이 거부된다). `run_tests`의 `test_names`에 짧은 클래스 이름을 넣으면 시작되지 않으므로 `group_names: ["UPlayGround\\.Item\\.Tests\\.(ItemDropResolverTests|GroundDropLedgerTests)"]`, `init_timeout: 90000`으로 실행한다(13개).

검수 표는 지침 §9 항목을 MANIFEST에 아이템별로 남긴다. 하나라도 미수행이면 상태를 `완료`가 아니라 `검수(조건부)`로 둔다.

## 6. 진행 규칙

- **파일럿 게이트**: 파일럿이 §5를 모두 통과하기 전에는 배치를 시작하지 않는다. 배치는 최대 5종이며, 배치마다 연결·검수까지 끝낸다.
- 상태(`대기/모델링/텍스처/연결/검증/검수/완료/보류`)는 단계가 바뀔 때마다 MANIFEST에 바로 기록한다.
- 같은 검수 항목이 2회 불합격이면 접근을 바꾸고, 3회째에는 `보류`로 둔 뒤 다음 아이템으로 넘어간다.
- 금지: 기존 FBX·머티리얼·프리팹·ItemSO 덮어쓰기, ID·GUID 변경, 에셋 YAML 직접 편집, 장비 변경, 사용자가 연 Blender 파일 조작, 다른 작업의 코드 수정, 미검증을 완료로 보고.
- 최종 보고는 지침 §11 양식으로 하고 수행·실패·미수행을 구분한다.
