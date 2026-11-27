# 궁극기 모션 캐릭터별 재검증·재제작

2026-11-25 사용자 승인으로 시작. [설계 정본](../../../Assets/docs/design/combat/player/ULTIMATE_MOTION_REDESIGN.md) · [이전 제작 이력](MOTION_PRODUCTION.md) · [전투 위키](../../wiki/combat.md).

## 원인 진단과 방식 전환

12명 실행 클립(`Assets/07.Animations/Player/UltimateRedesign/*_GripRevision.anim`)은 모두 각자의 `UltimateRedesign/*_Ultimate.asset`에 연결돼 있다(GUID 대조). 이 클립들은 다음 방식으로 만들어졌다(`ArtSource/Animation/UltimateRedesign/author_ultimate.py`, `author_remaining.py`).

- 구매 클립은 리그·bind로만 쓰고 `animation_data_clear()`로 동작을 지웠다. 설계 §4-2의 "구매 클립을 재료로"와 어긋난다.
- 몸 전체를 골반 5개 값(높이·전후·회전·기울기·비틀림)과 8~10개 키의 smoothstep 보간으로 합성했다. 키마다 속도가 0이 되어 포즈마다 멈칫한다. 오버슈트·팔로스루·예비동작이 구조적으로 나오지 않는다.
- 척추는 같은 값에 고정 비율(.45/.35/.2)을 곱해 동시에 움직이고, 팔·발은 목표점 IK다. 설계가 요구한 순차 가속이 없다.
- 손가락은 원본 1프레임에 고정, 화린 회전은 단일 공식이다.

GripRevision은 이 몸 동작 위에서 손 위치만 고쳤으므로 어색함이 남았다. **사용자 승인(2026-11-25): 절차 합성을 폐기하고, 구매 모캡·키프레임 클립을 바탕으로 구간 편집·리타임·블렌드와 필요한 레이어 보정만 한다.** 절차 저작은 원본에 없는 짧은 소품 동작(방패 이탈, 시위 해제 등)에만 보조로 쓴다. 생성기 키 튜닝은 재시도하지 않는다.

## 캐릭터별 루프

후보 2~3개 → 실제 모델·장비, 3/4 고정 카메라, FX·컷인 없는 **30fps 연속 영상** → 사용자 판정 → 통과 후 이벤트·판정·FX·카메라·컷인 재연결, MotionWarp(전투 지침 6.1)·MotionImpact 재베이크(6.2) → 실전 확인(UR 항목) → 문서 갱신. 12Hz 정지 샘플은 판정 근거로 쓰지 않는다.

보존: 현재 클립 GUID·백업·구매 원본·기존 미커밋 변경. Ability/Payload/MotionKey, 피해·비용·쿨다운 불변. 커밋·Player/Addressables 빌드 없음.

| 순서 | 캐릭터 | 상태 |
|---|---|---|
| 1 | 라온(구 Bokusei) | A+전진·실루엣 잔상 **실행 연결 완료**. 자동 검사·MotionWarp·MotionImpact 검증 통과, **사용자 실전 확인 대기**(R01~R06) |
| 2 | 레이네(Spear) | 도약 조준 투척 `Reine_LeapThrow` **실행 연결 완료**. 자동 검사·MotionWarp·MotionImpact 검증 통과, **사용자 실전 확인 대기**(N01~N06) |
| 3~ | 나머지 10명 | 미착수 |

## 라온

설계 요구: 칼집 손 고정·낮은 골반의 축적 → 뒷발 추진과 발도 일섬 → 뻗은 검끝 끝 자세 → 명중 뒤 납도. 발도 때 칼집이 따라 돌면 실패.

### 후보 조사

라온 전용 Frank 무기 본 이식 클립(`Assets/07.Animations/Player/Raon/FrankWeapon/`)이 이미 칼날(`Weapon_Blade`)·칼집(`Sword_Case`) 트랙을 갖고 있다. 재설계 전 라온 궁극기 `Katana_Skill_Ultimate_RaonWeapon`은 이 중 `S3_Skill02`를 구간 체인으로 썼다. 변환된 Frank 클립 중 칼집에 넣은 상태로 시작하는 것은 S3 계열뿐이다. 미변환 Frank `S3_Attack01~03`, `Stance1_to_3`은 후속 후보다. Anime Katana Combat Pack·Grruzam Katana의 칼집 트랙 여부는 **미확인**이다(Anime `Charge_Attack` FBX 1개에서만 칼집 이름 문자열 미검출). 아린 후보 조사 때 확인한다.

이력 주의: 11-15에 반려된 시그니처 라온 클립(`Signature/Katana/Katana_Raon_Signature.anim`, `WeaponSignatureClipImport.cs`)은 `S3_Skill02`에서 **칼날·칼집 트랙만** 가져왔다. 몸 동작은 `ArtSource/Animation/Signature/author_signature.py`의 같은 절차 합성(`animation_data_clear` + smoothstep)이다. 따라서 S3 모캡의 몸 동작은 재설계 이후 실행본으로 쓰인 적이 없다. 재설계 이전 실행본(B 계열)에 대한 사용자 평가는 기록에 없다.

라온 모델에 30Hz 샘플링해 측정했다(Editor 메모리 샘플, 게임 이벤트·KCC 미실행). 칼날 속도는 클립의 `Weapon_Blade` 본 원점 기준이며, 현재본의 `WeaponGripOffsetEvent` 보정과 ParentConstraint 오프셋은 적용하지 않았다. 수치 대비는 유효하지만 실제 칼끝 속도와 같지 않다.

| 클립 | 길이 | 루트 전진 | 골반 높이 | 발도→납도 | 칼날 최고 속도 |
|---|---|---|---|---|---|
| A `S3_Skill01` | 2.08초 | 1.57m | 0.52~0.74 | 1.30→1.70초 | 49.6m/s @1.30 |
| B `S3_Skill02` (재설계 전 실행본) | 3.45초 | 0 | 0.52~0.74 | 1.30→2.90초 | 32.1m/s @1.33 |
| C `S3_Combo01_All` | 2.17초 | 1.57m | 0.54~0.74 | 다단, 마지막 납도 | 40.8m/s @1.27 |
| 현재 `Raon_GripRevision` | 2.80초 | 0 | 0.75~0.92 | 칼집에서 거의 이탈 안 함 | **5.8m/s** |

칼날-칼집 거리는 S3 계열 시작 0.13m(납도 상태), 현재본 최대 0.30m다. 현재본은 발도 일섬이 실질적으로 일어나지 않는다.

### 연속 영상

`output/ultimate-redesign/raon-candidates/`: [A](../../../output/ultimate-redesign/raon-candidates/A_S3_Skill01.mp4) · [B](../../../output/ultimate-redesign/raon-candidates/B_S3_Skill02.mp4) · [C](../../../output/ultimate-redesign/raon-candidates/C_S3_Combo01_All.mp4) · [현재](../../../output/ultimate-redesign/raon-candidates/Z_Current_GripRevision.mp4) · [동시 비교 2×2](../../../output/ultimate-redesign/raon-candidates/Raon_compare_2x2.mp4)(좌상 A, 우상 B, 좌하 C, 우하 현재) · [A 2각도](../../../output/ultimate-redesign/raon-candidates/A_S3_Skill01_2angle.mp4)(왼쪽 기본, 오른쪽 축적 자세 t=1.0초의 몸 정면 3/4 고정). 720px, 30fps, 실제 PlayerModel_Raon과 Katana, 매 프레임 BakeMesh, 정면 3/4 고정 카메라, 1m 간격 바닥선. FX·KCC·MotionWarp·컷인은 실행하지 않는다. 미술·실전 인수가 아니라 후보 비교 자료다.

어깨선 기준 몸 방향(이동 방향 +Z=0°): A는 시작 -106°(칼집 쪽을 대상으로 향한 옆 자세) → 축적 중 -164°까지 감김 → 1.33초 +33°로 약200° 한 번에 풀림. 뒤집힌 클립이 아니라 감았다가 푸는 전신 동작이다. 현재본은 시작 11°로 정면에 가깝다.

캡처는 사용 중이던 Play 세션에서 PreviewRenderUtility 격리 장면으로 수행했다. Play는 종료하지 않았으며 Console 오류·경고에 이번 작업 관련 항목이 없었다(기존 TerrainCollider 경고만 반복).

### 2차: A + 전진 연장 + 잔상 (2026-11-25)

사용자 피드백: "조금 더 전진 + 잔상 남게". A를 채택하고 다음을 반영했다.

- 파생 클립 `Assets/07.Animations/Player/UltimateRedesign/Raon_Iai_S3Skill01.anim`(원본 `Raon/FrankWeapon/Raon_Frank_RPG_Katana_S3_Skill01.anim` 복사, 원본 보존). `RootT.z`만 수정했고 나머지 186개 커브는 원본과 동일하다. 1.27~1.43초 돌진 구간에서 원본 이동 진행률에 비례해 +1.5m를 더해 모캡 속도 곡선 모양을 유지했다. 최대 1.95→3.45m, 끝 위치 1.75→3.25m. 추가 거리는 한 값으로 조정 가능하다.
- 잔상 미리보기: 런타임 `AfterimageEvent`의 재질 생성 함수(`TryBuildAfterimageMaterials`)를 리플렉션으로 그대로 호출해 같은 외형으로 렌더링했다. 적용 예정 값은 1.25~1.45초, 생성 간격 1/60초, 알파 0.45, 유지 0.3초, 페이드 0.4초다(현재 MotionSet 값 0.05/0.08/0.15초 대비 오래 남음). 미리보기에서 시간에 따른 생성·유지·페이드를 직접 계산했으며, 실제 이벤트 실행은 아니다.
- 영상: [2각도(3/4·측면)](../../../output/ultimate-redesign/raon-candidates/A2_Iai_Extended_Afterimage_2angle.mp4) · [돌진 구간 시트](../../../output/ultimate-redesign/raon-candidates/A2_sheet.png).
- 발견: 반투명 잔상에서 옷 안쪽 몸 메시(다리 피부)가 비쳐 보인다. 런타임 재질을 그대로 쓴 결과이므로 게임에서도 같다. 현재 잔상 구현(원본 텍스처 Unlit 반투명, 깊이 선기록 없음)의 한계이며 모든 캐릭터 잔상에 해당한다. 수정은 코드 변경이 필요하다.
- 아직 `Raon_Ultimate.asset`에 연결하지 않았다. 사용자 판정 뒤 클립 교체와 판정·잔상·MotionWarp·컷인·카메라 시점 재배치를 진행한다.

### 3차: 실행 연결 (2026-11-25, 사용자 승인 "ㄱㄱ")

`Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/UltimateRedesign/Raon_Ultimate.asset`의 클립을 `Raon_Iai_S3Skill01`(2.083초)로 교체하고 이벤트 14개를 새 동작에 맞춰 옮겼다. 이벤트 수·종류, HitPhase 0/1(판정 2회), 카메라 효과·FX 에셋, Ability/Payload/MotionKey, 피해·비용·쿨다운은 바꾸지 않았다. 이 에셋과 클립은 `.gitignore` 대상이라 수동 백업을 남겼다: [변경 전](../../../ArtSource/Animation/RaonIaiRebuild/Before/) sha256 `e619eb08…`, [변경 후](../../../ArtSource/Animation/RaonIaiRebuild/After/) `ae203745…`.

| 이벤트 | 이전(초) | 변경(초) | 비고 |
|---|---|---|---|
| 얼굴 컷인 | 0~2.8, 유지1.1 | 0~2.083, 유지0.7 | 0.24+0.7+0.3=1.24초에 끝나 1.27초 발도를 가리지 않음 |
| 축적 카메라 효과 | 0~1.133 | 0~1.2 | |
| 카메라 입력 잠금 | 0~1.533 | 0~1.6 | |
| MotionWarp(회전 전용) | 1.133~1.333 | 1.0~1.267 | 돌진 직전 대상 방향 정렬. 이동 가중치 0 유지 |
| 무적 | 1.133~1.433 | 1.2~1.45 | 돌진 구간 |
| 잔상 | 1.133~1.333 | 1.25~1.45 | 간격 1/60, 최대 13개, 풀 16, 유지 0.3, 페이드 0.4 |
| 판정 1 (phase0) | 1.333~1.433, 앞 0.5m 깊이2 | 1.267~1.383, 뒤 0.6m 중심 깊이3.2 | 돌진 경로 전체 포함 |
| 참격 FX | 1.333~1.883 | 1.267~1.817 | |
| 타격 카메라 | 1.333~1.533 | 1.267~1.467 | |
| 판정 2 (phase1) | 1.433~1.533, 앞 2m | 1.683~1.767, 중심 뒤 1.2m, 3×2×4.8, 방사 방향 | 납도 순간의 지연 베기. 지나친 경로와 앞쪽 모두 포함 |
| 2타 카메라 2종 | 1.433~1.592 | 1.683~1.842 | |
| 캔슬 창 2종 | 2.0~2.8 / 2.333~2.8 | 1.85~2.083 / 1.95~2.083 | 마지막 판정 뒤 동일 간격대 |

자동 검사:
- 연결 확인: 클립 해석, 이벤트 14개, 판정 모양 `Validate` 2/2 통과.
- MotionWarp(전투 지침 6.1): 누락 프로필 준비 후 `TryValidateMappedProfiles` 통과. 미매핑 경고 12건은 모두 다른 기존 모션이다. [준비](raon-rebuild/motionwarp-bake.txt) · [검증](raon-rebuild/motionwarp-validate.txt).
- MotionImpact(6.2): 라온 카탈로그 `RaonKatana`에 궁극기 항목이 없어 재베이크 대상이 아니다. 전체 빌드 검증은 `Player.Katana.Light.00` stale로 **실패**했다. 원본 에셋으로 일시 복원해도 같은 실패라 **이번 변경과 무관한 기존 결함**이다. 범위 밖이라 재베이크하지 않았다. 첫 실패에서 멈추므로 이후 항목은 미확인이다. [원문](raon-rebuild/motionimpact-validate.txt).
- EditMode `SignatureAbilityDataTests` + `KatanaMotionWarpTests` 46/46 통과. Console 오류 0.
- 미수행: PlayMode 실전, Player/Addressables 빌드.

### 4차: 실루엣 잔상·Katana 재베이크 (2026-11-25, 사용자 선택 "잔상 2번, 재베이크도")

**잔상 실루엣 모드(코드).** `Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_Afterimage.cs`에 이벤트별 `renderMode`(원본 텍스처/단색 실루엣)를 추가했다. 기본값은 원본 텍스처라 기존 잔상 데이터와 `PlaySingleAt` 호출은 바뀌지 않는다.
- 실루엣은 틴트 색 단색이고, 같은 메시를 깊이 전용 재질로 먼저 그린 뒤 색을 그린다. 그래서 옷 안쪽 메시가 겹쳐 보이지 않고 바깥 면만 남는다. 원본에서 투명한 재질 슬롯은 기존처럼 숨긴다.
- 렌더 순서는 깊이 3100, 색 3101이다. 일반 투명 FX(3000)를 먼저 그리므로, 보이지 않는 잔상 깊이가 참격 FX를 가리지 않는다. 대신 잔상 앞에 있는 FX 위에 잔상이 덧그려질 수 있다.
- 깊이 패스 렌더러는 처음 실루엣을 쓸 때만 만들고, 이후 풀에서 재사용한다. 캡처마다 사용 여부를 다시 정하므로, 다른 모드 이벤트가 같은 풀을 써도 상태가 남지 않는다.
- 새 필드·enum이라 Hot Reload 대상이 아니어서 일반 컴파일했다. 컴파일 오류 0.

**라온 데이터.** 잔상을 `renderMode: 1`, 틴트 `(0.55, 0.8, 1)`로 바꿨다. 첫 렌더에서 돌진이 끝난 뒤(1.33~1.45초)에도 거의 멈춘 본체 위에 잔상이 생겨 라온을 푸르게 덮는 문제를 확인했다. 그래서 생성 구간을 돌진 구간 1.25~1.34초, 최대 6개로 줄였다. 유지 0.3초, 페이드 0.4초는 그대로다. 마지막 잔상이 등 뒤 코트 끝에만 살짝 겹친다. 영상: [실루엣 2각도](../../../output/ultimate-redesign/raon-candidates/A3_Iai_Silhouette_2angle.mp4)(새 코드의 실루엣 재질 함수를 호출하고 에셋의 실제 값을 읽어 렌더링. 런타임 코루틴 실행은 아님). 변경 후 백업은 `ArtSource/Animation/RaonIaiRebuild/After/` sha256 `14f8b83c…`로 갱신했다.

**Katana MotionImpact 재베이크.** `CombatMotionImpactBaker.Bake(RaonKatana)`를 실행했다. 백업: `ArtSource/Animation/RaonIaiRebuild/BeforeImpactBake/`. Git diff 확인 결과 `RaonKatanaMotionImpact.asset`은 출처 지문 4개만, `RaonKatana.asset`은 측정 시각 4개만 바뀌었다. 운동 표본·속도 값은 그대로다. 오래된 지문만 갱신된 것이다.

**검증.**
- MotionWarp `TryValidateMappedProfiles` 통과, MotionImpact 전체 빌드 검증 통과 ([원문](raon-rebuild/final-validate.txt)).
- EditMode `AfterimageTextureTests` + `SignatureAbilityDataTests` + `KatanaMotionWarpTests` 50/50 통과. Console 오류 0.
- 미수행: PlayMode, 실제 런타임 코루틴에서 실루엣 잔상 표시, Player 빌드.

### 사용자 실전 확인 (라온)

열린 전투 테스트 흐름에서 라온으로 궁극기를 사용한다. 기본 전투 카메라로 처음부터 복귀까지 본다.

| ID | 확인 | 실패 기준 |
|---|---|---|
| R01 | 적 하나를 3~5m 앞에 두고 사용 | 웅크린 축적 → 순간 돌진 발도 → 납도가 끊기지 않아야 한다. 돌진 전에 적 방향으로 돌지 않거나 칼집이 칼과 같이 빠지면 실패 |
| R02 | 같은 상황에서 잔상 | 돌진 경로에 청백색 단색 실루엣 잔상 약6개가 남았다 사라져야 한다. 옷 안쪽이 비치거나, 본체를 덮거나, 참격 FX가 잔상에 가려지면 실패 |
| R03 | 판정 2회 | 돌진 중 1타, 납도 순간 2타가 들어가야 한다. 적을 지나쳤을 때와 적에게 막혔을 때 모두 확인 |
| R04 | 얼굴 컷인 | 컷인이 발도 순간(약 1.27초) 전에 사라져야 한다 |
| R05 | 무타깃·벽 앞·여러 적 | 벽을 뚫거나 공중에 뜨거나 엉뚱한 방향으로 돌진하면 실패 |
| R06 | 축적 중 피격, 종료 직후 이동·회피·교대 | 입력이 막히거나 카메라 잠금·잔상이 남으면 실패 |

결과 회신: `ID / 통과·실패 / 증상`.

라온 리드 소견(판정 아님): A가 설계 키포즈와 가장 가깝다. 약1.2초 낮은 축적 → 1.3초 순간 돌진 발도 → 칼끝을 뒤에 남긴 끝 자세 → 1.7초 납도다. 축적 구간이 정적이어서 컷인 0.24+1.1+0.3초와 겹치는 배치가 필요하다. 끝 자세 유지(약0.3초)는 짧아 연출에서 보강 여지가 있다. 대상까지 거리는 원본 1.57m이며 나머지는 기존 MotionWarp로 메운다.

## 레이네 (Spear)

설계 요구: 창끝·목표 정렬 → 반대 손을 떼고 창을 머리 뒤로 싣기 → 도약하며 골반·가슴 역회전(활 모양) → 골반→가슴→팔꿈치→손 순서 투척 → 놓은 뒤에도 팔·몸통이 목표 쪽으로 따라감 → 착지로 회수. 손 위치·발사 프레임 이동만으로 완료 금지.

### 원본 조사 (2026-11-26, Blender MCP)

레이네 창 MotionSet이 쓰는 원본은 모두 Frank Spear 팩(`Frank_SlashPack_Spear/FBX_Animation`)이다. 이 팩에는 투척 동작이 없다. 투척 원본은 RPG Animations Pack `Item_Throw_1/2`와 Platform `Stander@Sub_Throw1`뿐이다. Blender 5.2에서 6개를 가져와 30Hz로 골반·오른손 위치와 분절별 각속도 최고 시점을 쟀다. 투척 판정 기준은 설계의 순차 가속 여부다.

| 원본 | 길이 | 동작 | 각속도 최고 시점(골반→가슴→위팔→손) |
|---|---|---|---|
| `Item_Throw_1` | 2.00초 | 한 발 내딛는 오버핸드 투척, 깊은 팔로스루, 전진 0.6m | 0.73 → 0.87 → 0.87 → 0.87 (골반 선행, 팔·손은 30Hz에서 동시) |
| `Item_Throw_2` | 1.33초 | 제자리 짧은 투척 | 0.43 → 0.60 → 0.60 → 0.60 |
| 창 `Skill02` | 2.67초 | 웅크림 → 큰 도약 → 내려찍기 → 착지 회수. 높이가 본에 구워짐(Unity 골반 최고 3.23m) | 가슴·위팔·손 1.12~1.15 동시(투척 아님) |
| 창 `Skill01` | 2.00초 | 낮은 직선 찌르기 | 1.03~1.05 동시 |
| 창 `Skill03` | 3.00초 | 다단 찌르기 후 도약 내려찍기 | 다단 피크 |
| `Sub_Throw1` | 0.8초 | Biped 리그, 스케일 불일치 | 제외 |

### 1차 후보 영상

`output/ultimate-redesign/reine-candidates/`: [A Item_Throw_1](../../../output/ultimate-redesign/reine-candidates/A_ItemThrow1.mp4) · [B Item_Throw_2](../../../output/ultimate-redesign/reine-candidates/B_ItemThrow2.mp4) · [C 창 Skill02](../../../output/ultimate-redesign/reine-candidates/C_SpearSkill02.mp4) · [현재 GripRevision](../../../output/ultimate-redesign/reine-candidates/Z_Current.mp4) · [동시 비교 2×2](../../../output/ultimate-redesign/reine-candidates/Reine_compare_2x2.mp4)(좌상 A, 우상 B, 좌하 C, 우하 현재. 짧은 클립은 끝 프레임 유지). 프레임 시트는 같은 폴더의 `*_sheet.png`. 720px, 30fps, 실제 `PlayerModel_Reine` + Spear(ParentConstraint 오프셋을 매 프레임 수동 적용), 정면 3/4 고정 카메라, 1m 바닥선. C만 도약 높이 때문에 화각을 52°로 넓혔다(나머지 34°). FX·KCC·MotionWarp·컷인 미실행. Play 중인 Editor에서 PreviewRenderUtility 격리 장면으로 렌더했고 Play는 건드리지 않았다.

알려진 한계(후보 비교 자료일 뿐 미술 인수 아님):
- 모든 원본은 원본 그대로다. 창은 손에 붙어 있고 놓지 않는다. 손가락 열림·무기 숨김·재파지는 채택 뒤 소품 레이어로 저작한다.
- A·B는 한손 물건 던지기라 준비 자세에서 창이 머리 뒤로 가로놓인다(대기 자세 기준 그립 오프셋). 창끝이 목표를 향하는 손목 보정과, 왼손이 창끝으로 조준하다 떼는 동작이 원본에 없다. 창 대기 자세에서 시작하는 블렌드도 필요하다.
- C의 높이는 본에 구워져 있어 실행 시 KCC 도약과 이중 상승이 된다. 쓰려면 루트 높이를 빼고 KCC가 높이를 맡게 해야 한다.

### 2차: 도약 조준 투척 합성·실행 연결 (2026-11-26, 사용자 지시 "도약 + 짚어던지기 구현 완료까지")

**합성 방식.** 리그가 다른 두 원본(Frank 74본, RPG 68본)을 리타깃 오차 없이 잇기 위해 Unity Humanoid 근육 공간에서 합성했다. Blender MCP는 원본 측정(순차 가속 판정)과 검수 보조에 썼다. 근육 값은 원본 그대로 쓰고 구간 리타임·가중 블렌드만 했다. 절차 저작은 투척 손목 방향 보정과 해제 순간 오른손 손가락 열림뿐이다. 재현 코드는 [`Tools/UltimateRedesign/BuildReineLeapThrow.cs.txt`](../../../Tools/UltimateRedesign/BuildReineLeapThrow.cs.txt)(MCP `execute_code` 본문)다.

| 출력 시각 | 하체(다리 근육) | 상체·몸통 회전 |
|---|---|---|
| 0~0.75초 | `Skill02` 0~0.36 웅크림 | `Skill02` 양손 창끝 조준 |
| 0.75~0.85 | `Skill02` 0.36~0.50 추진 | 0.70~0.90 `Item_Throw_1`로 전환 |
| 0.85~1.58 공중 | `Skill02` 0.50~0.92 체공 | `Item_Throw_1` 0.40~0.73 창 싣기(감속) → 0.73~0.87 투척(원속) |
| 1.433 해제 | | 손 속도 최고 13.2m/s @1.433 |
| 1.44~1.82 낙하 | 1.58~1.82 `Item_Throw_1` 런지 다리로 전환(착지 뻗기) | `Item_Throw_1` 0.87~1.00 팔로스루가 낙하 내내 목표로 이어짐 |
| 1.80 착지 | `Item_Throw_1` 1.00 런지 최저가 1.82에 오도록 대응 | |
| 1.82~2.95 | `Item_Throw_1` 1.00~2.00 흡수 → 일어섬. 다리는 끝까지 이 보폭 유지 | 2.25~2.60 `Skill02` 2.25~2.67 양손 창 대기로 복귀 |

- 오른손 손가락은 해제 전까지 `Skill02`의 자루 파지를 유지한다(`Item_Throw_1`은 작은 물건 파지).
- 높이: 매 프레임 가장 낮은 발을 `Skill02` 첫 프레임 서기 발 높이(0.017m)에 맞췄다. 원본에 구워진 도약 높이는 남지 않으며 높이는 KCC가 맡는다. 클립 설정은 기존 실행본과 같다(XZ·Y·회전 모두 포즈에 굽기).
- 수평: 끝 프레임이 캡슐 원점이 되도록 원본 간 차이(0.97m)를 체공 구간 강체 이동으로 흡수했다. 접지 구간은 디딘 발끝을 고정하도록 몸 XZ를 보정했다(웅크림은 앞에서, 착지~끝은 끝에서 거꾸로). 그 차이도 체공 구간에서 메운다. 착지 뒤 디딘 왼발 속도는 1.87~2.93초 동안 0이다. 남은 접지 미끄럼 최대 0.375m는 0.37초에 생긴다. 원본 창 모캡이 도약 보폭으로 발을 모으는 낮은 스텝이며, 원본에도 같은 동작이 있다.
- 1차 연결본의 결함 두 가지를 고쳤다. 하나는 런지 최저가 공중 1.62초(이론 높이 약 1.15m)에 오던 문제이고, 다른 하나는 선형 보정으로 접지 발이 0.28~0.38m, 끝 블렌드에서 약 1m 미끄러지던 문제다. 착지 직전 다리 전환은 0.24초로 늘려 발끝 속도를 13.5 → 8.1m/s로 낮췄다(공중 구간).
- 손목: 0.80~1.44초 창끝(+Y, 히트박스 중심 +0.28·위쪽 끝이 더 넓음으로 판정)이 싣기 때 앞·위(0,0.25,1), 해제 때 앞·아래(0,-0.35,1)를 향하도록 오른 전완 비틀기·손목 두 축을 탐색했다. 최대 오차 131.4° → 5.7°. 탐색 뒤 3탭 평활했다.
- 클립: `Assets/07.Animations/Player/UltimateRedesign/Reine_LeapThrow.anim` GUID `9a8ea1f0d73983946846a5333850182f`, 2.95초, 30fps, 근육 95 + RootT/RootQ. 저장 클립 재샘플과 합성 포즈의 골반 위치 차이 0. IK 목표 커브는 넣지 않았다(공격 중 발 IK 비활성). 기존 `Reine_GripRevision.anim`은 보존했다.
- 영상: [도약 투척](../../../output/ultimate-redesign/reine-candidates/L_LeapThrow.mp4) · [시트](../../../output/ultimate-redesign/reine-candidates/L_LeapThrow_sheet.png) · [렌더 입력값](../../../output/ultimate-redesign/reine-candidates/L_LeapThrow_render.txt). 실제 모델·창. 에셋의 AddForce·중력 이벤트 값으로 계산한 KCC 이론 궤적(정점 1.35초 1.8m, 착지 1.80초)을 모델 위치에 적용했고, 에셋의 창 숨김 1.433~2.45초를 반영했다. 빨간 원은 6m 앞 목표 표시다. KCC·투사체·FX를 실제로 실행한 것은 아니며, 리드는 주요 프레임만 확인했다(영상 전체 재생 검수 아님).

**실행 연결.** `Reine_Ultimate.asset`의 클립을 교체하고 이벤트 12개를 새 동작에 맞췄다. 이벤트 수·종류, 투사체 정의·피해·targetMode, Ability/Payload/MotionKey, 비용·쿨다운은 바꾸지 않았다. 백업: [변경 전](../../../ArtSource/Animation/ReineLeapThrowRebuild/Before/) sha256 `79c00268…`, [변경 후](../../../ArtSource/Animation/ReineLeapThrowRebuild/After/) 에셋 `251a71d7…`·클립 `94b080cc…`.

| 이벤트 | 이전(초) | 변경(초) | 비고 |
|---|---|---|---|
| 도약 AddForce(위) | 0.867, 6.353 | 0.85, 7.2 | 정점 0.5초 뒤 1.8m |
| 상승 중력 | 0.867~1.433, ×0.2491 | 0.85~1.35, ×0.32 | 유효 중력 45×배율 기준 |
| 하강 중력 | 1.433~2.3, ×0.1065 | 1.35~2.10, ×0.3951 | 이론 착지 1.80초. 체공 1.43→0.95초 |
| 투사체 발사 + 창 숨김 | 1.633 | 1.433 | 손 속도 최고 프레임 |
| 창 복원 + 손 섬광 | 2.8 | 2.45~2.70 | 양손 파지 복귀 블렌드 중간. 2.40에서는 창이 손에서 늘어진 자세라 늦춤 |
| 도약 카메라 | 0.867~1.524 | 0.85~1.433 | |
| 발사 카메라 | 1.633~1.808 | 1.433~1.608 | |
| 얼굴 컷인 | 0~3.6, 유지1.1 | 0~2.95, 유지0.85 | 0.24+0.85+0.3=1.39초에 끝나 1.433 발사를 가리지 않음 |
| 캔슬 창 2종 | 2.96 / 3.28~3.6 | 2.45 / 2.65~2.95 | 창 복원과 같은 시각부터 |
| 파지 보정 | 0~3.6 | 0~2.95 | |

**검증.**
- EditMode `SignatureAbilityDataTests` + `WeaponAimTests` 29/29 통과(발사=숨김 동시, 캔슬은 발사 뒤, 중력 구간 비중첩 포함).
- MotionWarp `TryValidateMappedProfiles` 통과(미매핑 경고 12건은 라온 때와 같은 기존 모션), MotionImpact 전체 빌드 검증 통과. [원문](reine-rebuild/final-validate.txt). 레이네 궁극기에는 MotionWarp 이벤트가 없다.
- 위 테스트와 두 검증은 최종 클립·이벤트(창 복원 2.45초)로 다시 실행해 같은 결과를 얻었다. Console 오류 0. 검사는 Play가 꺼진 상태에서 실행했다. 그 사이 사용자가 Play를 오가며 MCP 연결이 몇 차례 끊겨 재실행했다.
- git 상태: 이 작업의 추적 파일 변경은 문서 2개(이 문서, 전투 위키)뿐이다. MotionSet·클립은 `.gitignore` 대상이라 백업으로 관리한다. 작업 중 보인 RiverBasin4km 씬과 월드 문서 3개의 변경은 같은 시간대(21:46~21:47)의 다른 강변 분지 작업이며 건드리지 않았다. `InitTestScene07609c79…`는 작업 전부터 있던 파일이다.
- 미수행: PlayMode 실전(KCC 실제 높이·착지 시점, 투사체 비행·착탄, 카메라), Player/Addressables 빌드.

알려진 위험(판정 아님): 실제 KCC 착지가 이론값 1.80초보다 빠르거나 늦으면 런지와 접지가 어긋날 수 있다. 가속 투척이 0.14초로 짧아 실제 카메라에서 읽히는지 확인이 필요하다. 클립 끝 보폭은 투척 원본의 서기 보폭이라, 대기 전환 블렌드에서 발이 움직일 수 있다.

### 3차: 착탄점 조준 IK·회전 워프 (2026-11-26, 사용자 피드백 "창을 되게 멀리 던지는 것처럼 보임")

**원인.** 투사체는 이미 보조 대상 발밑(없으면 정면 8m)을 지면에 투영한 점으로 날아간다(`TargetPosition` + `projectTargetToGround`). 몸은 원본 투척 그대로 수평 앞을 향한다. 측정(KCC 이론 궤적, 6m 앞 지면 목표)에서 해제 순간 창 축과 착탄점 사이 오차는 26.6°였다. 해제 뒤 팔로스루에서는 손목이 꺾여 창 축이 목표에서 75° 넘게 벗어나 기존 조준 가중치가 0이 되고, 팔은 착탄점보다 최대 43° 위를 가리켰다. 기존 `WeaponAimController`(발사·타격 구간에 가슴·팔을 돌려 무기 축을 목표에 맞추는 런타임 IK)는 지면 투영 발사를 조준 대상에서 제외하고 있었고, 레이네 발사 이벤트는 `suppressWeaponAim`도 켜져 있었다.

**검토 결론.** 위아래 조준은 런타임 IK, 수평 방향은 MotionWarp 회전으로 나눈다. MotionWarp 이동으로 도약 거리를 당기는 방식은 이 클립이 루트 XZ를 포즈에 굽고 높이를 KCC가 맡는 구조라 맞지 않아 제외했다. 클립에 고정 각도를 구우면 목표 거리마다 어긋나므로 쓰지 않았다.

**코드(옵트인, 기본값은 기존 동작).**
- `SpawnProjectileEvent.aimPoseAtResolvedTarget` 추가. 켜면 지면 투영·TargetPosition 발사에서도 자세 조준을 하며, 조준점은 발사와 같은 규칙(오프셋 + 지면 투영)으로 구한다. 대상이 없으면 정면 폴백 거리의 지면점을 쓴다. 발사 방향 계산(`TryResolveWeaponAim`)은 바꾸지 않았다. 조준 판정은 `UsesWeaponAimPose`로 한곳에 모았다.
- `WeaponAimController`: 옵트인 투척은 발사 뒤 손을 떠난 무기 축 대신 어깨→손 방향을 조준한다(팔로스루). `WeaponAimSettings.offHandGripDistance` 추가: 0(기본)이면 기존처럼 반대손이 항상 주손을 따르고, 0보다 크면 반대손이 무기 축에서 그 거리 안일 때만 따른다.
- 영향 측정: 레이네 일반 창 조준 이벤트 17건의 반대손-축 거리는 최대 0.146m라 레이네 값 0.3에서도 동작이 같다. H09 지팡이 22건 중 19건은 반대손이 축에서 0.44~0.92m 떨어져 있는데도 기존 규칙으로 주손을 따라간다. H09는 기본값 0이라 바뀌지 않으며, 의도된 동작인지는 범위 밖이라 확인하지 않았다(보고만 함).

**데이터.**
- `Reine_Ultimate` 발사 이벤트: `suppressWeaponAim` 0, `aimPoseAtResolvedTarget` 1, 조준 블렌드 0.20/0.12 → 0.30/0.35초(해제 전 싣기~투척, 해제 뒤 착지 직전까지 팔로스루).
- 회전 전용 MotionWarp 0.40~0.85초 추가(라온 궁극기 워프 설정 복제: 이동 가중치 0, 회전 1, 최대 8m). 도약 전 웅크림 동안 목표 방향으로 몸을 돌린다.
- `PlayerModel_Reine.prefab` `_weaponAim.offHandGripDistance` 0.3(한 줄 추가). 조준 각도 한계(팔 30°, 가슴 10°×0.3)는 일반 창 공격과 공유하므로 바꾸지 않았다.
- 백업: [조준 전](../../../ArtSource/Animation/ReineLeapThrowRebuild/BeforeAim/) 에셋 `251a71d7…`·프리팹 `add91789…`, [최종](../../../ArtSource/Animation/ReineLeapThrowRebuild/After/) 에셋 `f120615d…`·프리팹 `a5a7db5e…`.

**효과 측정(미리보기 재현).** 미리보기 렌더는 런타임 IK 콜백을 실행하지 않으므로, 컨트롤러와 같은 `WeaponAimMath`와 모델 설정값으로 가슴·팔 보정을 프레임마다 재현했다.

| 목표(앞 지면) | 해제 순간 창 축 오차 | 팔로스루(1.43~1.70초) 팔-착탄점 최대각 |
|---|---|---|
| 6m | 26.6° → 0.0° | 43° → 21° |
| 3.5m | 30.8° → 0.0° | 57° → 42°(팔 보정 한계 30°에 걸림) |

영상: [보정 전·6m·3.5m 나란히](../../../output/ultimate-redesign/reine-candidates/Reine_aim_compare_3up.mp4) · [6m](../../../output/ultimate-redesign/reine-candidates/L2_Aim6.0m.mp4) · [3.5m](../../../output/ultimate-redesign/reine-candidates/L2_Aim3.5m.mp4) · [측정 원문](../../../output/ultimate-redesign/reine-candidates/L2_Aim_render.txt). 정면 3/4 카메라라 위아래 각도 차이가 화면에서 크게 보이지 않는다. 회전 워프는 미리보기에 반영되지 않았다.

**검증.** 컴파일 오류 0(새 직렬화 필드라 일반 컴파일). EditMode `WeaponAimTests`(옵트인·억제 우선·폴백 조준점 신규 검사 포함) + `SignatureAbilityDataTests` + `KatanaMotionWarpTests` 63/63 통과. MotionWarp 누락 프로필 준비 후 `TryValidateMappedProfiles` 통과(미매핑 경고 12건은 기존 모션), MotionImpact 전체 검증 통과 [원문](reine-rebuild/aim-warp-validate.txt). 미수행: PlayMode 실전(실제 IK·워프·보조 대상 선택), Player 빌드.

남은 판단(판정 아님): 3.5m처럼 가까운 목표는 팔 보정 한계 30° 때문에 팔로스루가 여전히 목표보다 약 40° 위를 가리킨다. 더 내려야 하면 이벤트별 조준 한계 덮어쓰기를 추가하거나, 클립 팔로스루 자체를 더 아래로 재저작해야 한다. 지금은 일반 창 공격과 공유하는 한계를 유지했다.

### 사용자 실전 확인 (레이네)

| ID | 확인 | 실패 기준 |
|---|---|---|
| N01 | 적 하나를 5~8m 앞에 두고 사용 | 웅크려 창끝 조준 → 도약 → 공중에서 창을 머리 뒤로 싣기 → 던짐 → 착지 → 창 대기가 끊기지 않아야 한다. 이중 상승·공중 정지면 실패 |
| N02 | 투척 순간 | 창끝이 목표 쪽을 향한 채 손에서 떠나야 한다. 창이 옆으로 누워 날아가 보이거나 발사보다 먼저 사라지면 실패 |
| N07 | 가까운(3~4m)·먼(7~8m) 적, 적 없음 | 웅크리는 동안 몸이 적 쪽으로 돌고, 던지는 팔과 팔로스루가 적 발밑 쪽으로 내려가야 한다. 창이 적보다 훨씬 먼 곳을 향해 던져지는 것처럼 보이거나, 해제 순간 팔이 튀거나, 왼팔이 오른팔에 끌려가면 실패 |
| N03 | 착지 | 착지 순간 런지로 무게를 받아야 한다. 발이 땅에 박히거나 뜨거나, 착지 전에 런지가 먼저 오면 실패 |
| N04 | 얼굴 컷인·카메라 | 컷인이 발사 전에 사라져야 한다. 도약·발사 카메라가 캐릭터와 착탄점을 놓치면 실패 |
| N05 | 경사·벽 앞·무타깃 | 지형 아래 착탄, 벽 관통, 창 미복구면 실패 |
| N06 | 종료 직후 이동·회피·교대 | 입력이 막히거나 창이 숨은 채 남으면 실패 |

결과 회신: `ID / 통과·실패 / 증상`.

### 1차 리드 제안 (이력)

리드 제안(판정 아님): 순차 가속을 갖춘 원본은 A뿐이다. 설계 전체(도약 투척)는 원본 하나로 나오지 않는다. 다음 조합을 Blender에서 레이네 리그(`Spear_RestBind.fbx`)로 리타깃해 만드는 것을 2차 후보로 제안한다: 창 대기 → C의 웅크림·도약 하체(루트 높이 제거, KCC 담당) + A의 상체 투척 궤적 → A의 팔로스루 → C의 착지 회수. 지상판(A + 창 대기 블렌드 + 손목·창끝 보정)도 함께 만들어 비교한다.