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
| 2~ | 나머지 11명 | 미착수. 순서는 라온 결과 뒤 결정 |

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

리드 소견(판정 아님): A가 설계 키포즈와 가장 가깝다. 약1.2초 낮은 축적 → 1.3초 순간 돌진 발도 → 칼끝을 뒤에 남긴 끝 자세 → 1.7초 납도다. 축적 구간이 정적이어서 컷인 0.24+1.1+0.3초와 겹치는 배치가 필요하다. 끝 자세 유지(약0.3초)는 짧아 연출에서 보강 여지가 있다. 대상까지 거리는 원본 1.57m이며 나머지는 기존 MotionWarp로 메운다.
