# 전용 궁극기 연출 미보유 플레이어블 10명 컨셉 설계

> 작성일: 2026-11-14 · 범위: 캐릭터별 연출 설계. 구현·리소스 제작을 지시하는 문서와 구분한다.
> 클립 조사 반영: **[§6 제작 방향·확정 입력 에셋](#6-클립-조사-후-확정한-제작-방향과-에셋)**을 동작 제안보다 우선한다. 기존 클립/MotionSet 조합 우선, 미충족 구간만 Blender MCP 제작. 소스 채택과 실제 실행/미술 인수는 구분한다.
> [공통 연출 계약](WEAPON_ULTIMATE_PRESENTATION_DESIGN.md) · [전투 위키](../../../../../docs/wiki/combat.md) · [조사·Astra 논의·검증 상태](../../../../../docs/work-status/weapon-ultimate-presentation/MISSING_ULTIMATE_CONCEPTS.md)

## 1. 목표와 근거

얼굴을 가려도 무기의 움직임과 피니시의 형태만으로 누구의 궁극기인지 구분되게 한다. 무기 공용 템플릿의 색만 바꾸는 대신, 같은 무기 안에서도 힘을 모으는 방향과 풀어내는 방식을 나눈다. 구조·운송·책임이라는 인물의 행동은 동작의 동기로 참고하며 궁극기 안에서 생활 사건을 그대로 재현하지 않는다.

이 문서의 ‘미보유’는 **캐릭터 전용 `UltimateSequenceAsset` 부재**다. 공용 궁극기 Ability·해금·실행 모션이 없다는 뜻이 아니다. 아래 동작·작업용 이름·표정은 새 연출 제안이며 기존 능력·성격의 정사로 소급하지 않는다.

| 근거 | 적용 범위 |
|---|---|
| [플레이어 정의 에셋](../../../../10.Datas/Party/PlayerCharacters/) | 캐릭터 식별값, 무기, AbilitySet 참조 |
| [붉은 나무 정규 스토리 §6](../../content/red-tree/LAKE_OF_LIFE_RED_TREE_STORY_PLAN.md) | 사용 대상 12명과 인물별 등장·기여. H09 제외 |
| [인물·생활 사건 기준 §2·§4](../../content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_CAST.md) | 행동 모티프·과장 캐릭터화 방지. 말투·세부 성격은 잠정안 |
| [기존 궁극기 폴더](../../../../10.Datas/Actor/Ultimate/) | Raon/Hwarin 전용 시퀀스 2종 존재. 이번 신규 대상에서 제외 |
| [모션 재사용 조사](../../../../../docs/work-status/weapon-ultimate-presentation/MOTION_REUSE_REVIEW.md) | 기존 후보 탐색의 출발점. 대상 모델의 실제 적합성 판정과 구분 |

### 1.1 대상과 공용 실행 경로

정의 에셋은 13개, 정규 사용 대상은 12명이다. Raon·Hwarin을 뺀 다음 10명이 대상이다. 내부 키·정수·GUID·세이브 별칭을 변경하지 않는다. Reine의 문서상 이름은 정규 스토리의 ‘레이네’를 쓰되 `Player_Reien_Full` 등 파일명을 바꾸지 않는다.

| 인물 / 키 | 무기 | 정의가 참조하는 AbilitySet | 이번 연출의 화면 핵심 |
|---|---|---|---|
| 아린 / Arin | Katana | Migrated/PlayerKatanaAttackData | 한 사선의 지속 |
| 묘령 / MyoRyeong | SwordShield | Migrated/PlayerSwordShieldAttackData | 방패의 면 → 검의 선 |
| 레이네 / Reine | Spear | Migrated/PlayerDefaultAttackData | 수평 정렬 → 직선 도달 → 회수 |
| 리안 / Lian | Whip | Migrated/PlayerWhipAttackData | 넓은 곡선 → 팽팽한 선 |
| 설아 / SeolA | Bow | Migrated/PlayerBowAttackData | 조준의 여백 → 한 줄의 궤적 |
| 세라 / Sera | GreatSword | Migrated/PlayerGreatSwordAttackData | 고정된 중심 → 세로 중량 |
| 릴리 / Lili | GreatSword | Migrated/PlayerGreatSwordAttackData | 낮은 준비 → 가로의 넓이 |
| 연화 / YeonHoa | Staff | Player/Staff | 단일 수직 도형 → 전방 방출 |
| 묘묘 / Myomyo | Staff | Player/Staff | 여러 작은 궤적 → 한 점 → 전방 방출 |
| 유라 / Yura | DualBlade | Migrated/PlayerDualBladeAttackData | 좌우 두 궤적 → 교차 |

AbilitySet 열은 `Assets/10.Datas/Ability/` 아래 폴더를 줄여 적었다. 세라/릴리, 연화/묘묘는 각각 같은 AbilitySet을 참조한다. 레이네는 Spear 정의지만 Set 이름은 Default다. 폴더명으로 검 사용이나 전용 기술 부재를 추정하지 않는다.

**H09:** Staff 정의는 있으나 정규 스토리에서 미사용이다. 외형·정식 편입을 새로 결정하지 않으며 이번 컨셉에 포함하지 않는다. Sword 공용 템플릿도 현재 대상에 Sword 사용자가 없으므로 캐릭터를 임의 배정하지 않는다.

## 2. 모든 캐릭터에 적용할 연출 경계

### 2.1 기존 실행과 새 동작 제안을 나눈다

- 이번 문서는 비용·피해·사거리·타격 수·무적·강제 이동·경직·실시간 길이를 정하지 않는다. 판정과 타이밍의 원본은 AbilitySet → Payload의 `AbilityAttackInfo.motionKey` → 액터 MotionSet 매핑이다.
- 아래 ‘예고 → 진입 → 전개 → 피니시 → 복귀’는 **스토리보드 순서**다. 초 단위 시간표나 타격 횟수 지시가 아니다. 실제 실행 모션을 확인한 뒤 존재하는 동작과 판정 순간에 맞춘다. 여러 실제 타격이 있으면 피니시는 그중 시각적 정점이며 나머지 명중 피드백도 유지한다.
- **기존 실행에 얹는 범위:** 얼굴·판정과 일치하는 궤적/VFX·음향·안전한 구도. VFX가 실제 공격보다 넓게 닿거나, 회수되는 빛이 적을 끌거나, 없는 탄이 날아가는 표현은 제외한다.
- **전용 동작 후속안:** 새 준비 자세, 발놀림, 세라/릴리의 서로 다른 베기 방향 등. 공유 모션과 다르면 그대로 얹지 않는다. 재사용 클립을 먼저 확인하고 공격 이벤트·타격 방향·그립·루트 이동 정합을 별도 저작한다. 맞지 않는 동안에는 기존 자세/검 궤적을 유지하고 새 포즈는 보류한다.
- 캐릭터 이름 분기로 실행하지 않는다. 전용 Sequence·프로필과 기존 데이터 참조로 저작한다. 공용 Set을 수정해 같은 무기 전원에게 전용 연출이 번지게 하지 않는다.

### 2.2 얼굴과 화면

얼굴은 궁극기 전용 좌측 2D 패널이다. ImageGen 제작과 기존 캐릭터 레퍼런스 대조를 따르며 3D 페이스캠·전체 화면 얼굴 컷인을 추가하지 않는다. 본문의 표정은 표정 연기 방향이며 그림의 최종 미술 승인을 뜻하지 않는다. 패널은 짧게 발동 의지를 전달하고 물러나며 긴 궁극기의 피니시까지 강제로 유지하지 않는다. 기존 얼굴 패널 표시 계약(Actor → Contracts → `Svc.FaceStrip` / UI의 `UISvc.FaceStrip` 별칭)을 유지하고 모든 트윈은 `SetUpdate(true)`를 따른다.

색은 원화·현재 무기/VFX와 대조할 아트 항목이다. 화염·얼음·치유 등 속성을 색이나 이름만으로 부여하지 않는다. 색·화면 섬광·패널을 빼도 실루엣과 음의 리듬이 달라야 한다. 확정 대사·새 성직/군사 직함·가족사·신의 축복을 호출하는 보이스는 여기서 만들지 않는다.

### 2.3 카메라와 복귀

최근 발밑 중심 구도 회귀 때문에 자동 Snapshot 연결은 해제된 상태다. [복구 기록](../../../../../docs/work-status/weapon-ultimate-presentation/CAMERA_FRAMING_FIX.md)을 따른다. 기존 후보 10종을 검증된 앵글로 간주하지 않는다.

1. 기본은 시전자 몸통·무기 끝·실제 대상/명중 영역이 함께 보이는 안정된 3/4 구도다. 저각은 발밑이 아니라 무기 중량을 읽히게 하는 제한적 후보다.
2. 회전·크레인·극단적 줌·화살 추적을 컨셉 성립 조건으로 삼지 않는다. 안 맞으면 기존 전투 카메라에서 같은 궤적과 피드백을 읽히게 한다.
3. 신규 샷은 실제 모델의 몸통 기준점, 충돌 보정 뒤 주시 방향, 경사·벽·대형 적·화면비를 확인한 뒤 `CameraSnapshotProfile`로 저작한다. T2 무대도 구도 품질을 보장하지 않는다.
4. T1/T2는 아래에서 제작 후보로만 적는다. 기존 Stage를 자동 변경하거나 없는 적 대역·접촉 리그를 요구하지 않는다. 종료·중단·사망·씬 이탈 때 기존 소유권에 따라 카메라·시간·UI·대역을 복구한다.
5. 새 히트스톱·슬로·흔들림 수치를 컨셉만으로 추가하지 않는다. 타격음의 공격부와 잔향, 궤적의 두께·소멸 속도로 우선 차별화한다.

## 3. 캐릭터별 스토리보드

### 3.1 아린 — 「끝까지 잇는 칼끝」(작업용 이름)

**핵심:** 짧게 그은 사선이 마지막까지 선명하게 남는다. 보호 대상을 두고 먼저 떠나지 않는 정규 스토리의 선택을, 과장된 연타보다 끊기지 않는 동작의 의지로 번역한다. 과묵함·검객 직업·가문의 검술을 새로 설정하지 않는다.

- **예고:** 시선이 실제 목표에 멈춘다. 패널은 턱을 들고 과시하기보다 눈의 초점을 좁히는 표정.
- **진입:** 칼과 몸통의 사선이 겹치는 순간을 잡는다. 새 발도/전진은 기존 모션에 있을 때만 쓴다.
- **전개:** 실제 검 궤적을 따라 얇은 한 방향 잔광이 이어진다. 가짜 추가 베기나 적 뒤로 순간이동하는 잔상을 넣지 않는다.
- **피니시:** 실제 정점 타격에서 사선이 가장 선명해지고 칼끝 방향으로 짧게 걷힌다. 넓은 다중 절단 폭발을 기본으로 삼지 않는다.
- **복귀:** 검과 어깨의 긴장이 풀리며 전투 자세가 다시 보인다. 납도 클립이 없으면 납도를 요구하지 않는다.

**화면·소리:** 측후방 3/4에서 검끝의 출발·도착을 한 화면에 둔다. 얇은 금속 공격음 뒤 짧은 마찰 잔향. T1 우선 후보. 라온과의 차이는 템플릿의 ‘정적 납도’ 설명만으로 확정하지 않고 현행 실제 영상을 나란히 비교한다.

**리소스:** 현재 Katana 실행 궤적, 검·칼집 위치 확인, 전용 패널과 사선 FX. 전용 발도/마무리 포즈는 클립 재사용 결과 후 판단.

### 3.2 묘령 — 「물러서지 않는 선」

**핵심:** 방패의 넓은 면이 시야를 잠깐 정리하고, 그 옆에서 검의 좁은 선이 드러난다. 루시아와의 옛 관계를 힘의 출처나 권위로 만들지 않는다.

- **예고:** 방패 바깥으로 적을 보는 눈. 패널은 이를 악무는 분노보다 상대를 놓치지 않는 집중.
- **진입:** 방패를 세우는 준비 자세가 후보. 방패가 화면 전체와 얼굴을 덮지 않는 3/4 방향.
- **전개:** 방패 가장자리의 짧은 반사광 뒤 실제 검 궤적이 나타난다. 방패 타격이 실제로 없다면 충돌 섬광·충격음을 만들지 않는다.
- **피니시:** 검의 실제 타격에 명확한 짧은 금속음을 준다. ‘막고 되받는’ 인상은 준비·음향의 대비로 만들며 실제 피격 대기를 강제하지 않는다.
- **복귀:** 방패가 전투 높이로 내려오며 검과 목표가 다시 함께 보인다.

**화면·소리:** 방패 전면보다 검 쪽 측면. 낮은 둔음과 짧은 검음을 분리한다. T1 후보. 무적·자동 가드·반격 조건·아군 보호막은 추가하지 않는다.

**리소스:** SwordShield 공용 실행과 방패/손 그립, 방패 테두리 광·검격 FX, 전용 패널. 새 적 접촉 클립은 필요 조건이 아니다.

### 3.3 레이네 — 「곧게 닿는 창」

**핵심:** 창대의 수평선, 창끝의 도달, 손으로 되돌아오는 회수를 읽힌다. 주민 보호와 명령을 구분하는 선택은 ‘힘을 정확히 보낸다’는 동작 모티프다. 군대식 맹세·직급은 부여하지 않는다.

- **예고:** 창끝과 목표가 같은 방향에 놓인다. 패널은 정면 도발보다 목표를 재확인하는 시선.
- **진입:** 허리~몸통 높이에서 창대를 정렬하는 자세가 후보. 지면에 카메라를 붙이지 않는다.
- **전개:** 실제 찌르기에 맞춰 가늘고 곧은 선이 뻗는다. 적을 모으거나 창뒤로 끌어오지 않는다.
- **피니시:** 창끝의 실제 도달점에 작은 압축 섬광. 피니시를 보여주기 위해 시전자를 적 뒤로 이동시키지 않는다.
- **복귀:** 선은 창끝부터 손 방향으로 걷히고 창이 전투 거리로 돌아온다. 회수 시점은 실제 모션을 따른다.

**화면·소리:** 측후방 3/4에서 창대 전체와 대상 몸통을 확보. 짧고 단단한 관통음 뒤 건조한 회수음. T1 후보. 실제 관통 판정이 없으면 뒤쪽 적에게 닿는 긴 선은 만들지 않는다.

**리소스:** Default 이름의 Set에서 실제 Payload/MotionKey를 해석한 뒤 Spear 모델과 그립 대조, 직선 FX와 전용 패널. Set 이름만으로 Sword 모션을 재활용하지 않는다.

### 3.4 리안 — 「흩어진 선의 끝」

**핵심:** 넓은 곡선이 한 줄로 팽팽해진 뒤 회수된다. 경로와 기록을 맞춰 가는 행동에서 모티프를 얻되 문서·장부를 공격 소품으로 소환하지 않는다.

- **예고:** 시선이 채찍 끝에서 목표로 옮겨간다. 패널은 비웃음·유혹보다 확인을 마친 집중.
- **진입:** 몸 옆에서 충분히 큰 곡선을 보여준다. 캐릭터 중심을 도는 카메라 없이 채찍의 동작이 곡선을 만든다.
- **전개:** 실제 채찍 본/궤적의 S자 흐름이 길어졌다 좁혀진다. 장식 선을 적 몸에 감지 않는다.
- **피니시:** 유효 타격 순간 채찍 선이 팽팽해지며 날카로운 파열음을 낸다. 적을 고정한 것처럼 보이는 고리·접촉점은 제외한다.
- **복귀:** 끝부터 회수되는 곡선으로 다음 자세에 연결한다. 적이 끌려오거나 던져지는 연기를 요구하지 않는다.

**화면·소리:** 채찍 평면과 비스듬한 고정 3/4 와이드. 공기 가르는 소리 → 단일한 정점 파열음 → 짧은 회수 마찰. T1 또는 비접촉 T2 후보.

**리소스:** 기존 Whip Critical 후보의 무기 본/리타게팅과 실제 궤적 확인, 비접촉 리본 FX, 전용 패널. **T3 적 속박·당기기·던지기는 이번 기본안에 없으며 사용자 결정 대기를 유지한다.**

### 3.5 설아 — 「한 줄의 끝」

**핵심:** 충분한 여백 속에서 조준한 방향과 도달점이 일치한다. 물길을 직접 확인하는 역할은 관찰과 정확함의 모티프이며 물/얼음 마법을 새로 부여하지 않는다.

- **예고:** 패널은 눈을 크게 뜨는 놀람보다 먼 목표에 초점을 맞춘다. 손·활시위를 얼굴 패널로 가리지 않는다.
- **진입:** 어깨 너머에서 실제 조준 방향을 읽힌다. 별도의 긴 정지 영창을 넣지 않는다.
- **전개:** 활시위가 당겨지는 실제 구간에서 주변 장식음을 줄이고 긴장을 쌓는다. 존재하지 않는 화살을 추가로 세지 않는다.
- **피니시:** 실제 발사·타격 방식에 맞는 주된 한 줄 궤적과 도달음을 강조한다. 실제 다중탄이면 다른 탄의 존재·명중도 숨기지 않는다.
- **복귀:** 도달점에서 잔광을 짧게 걷고 활을 든 시전자·전장 정보가 함께 남는다.

**화면·소리:** 어깨 너머 고정 또는 완만한 측면. 손을 놓는 작은 소리와 멀리 도달하는 타격음을 공간적으로 구분. T1 우선, T2는 실제 대상 정합 확인 후 후보. 화살 추적·유도·슬로를 필수로 삼지 않는다.

**리소스:** Bow 클립의 시위/손/화살 소켓, 실제 발사 이벤트와 탄 수·도달 판정 확인, 전용 패널·방향성 궤적. 조준선이 판정과 다른 목표를 가리키면 사용하지 않는다.

### 3.6 세라 — 「여기서 버틴다」

**핵심:** 흔들리는 준비에서 무게중심이 단단히 잡히고, 힘이 세로로 눌린다. 축복 없이 살 대안을 확인하는 인물을 무모한 광전사나 공포 때문에 각성하는 인물로 바꾸지 않는다.

- **예고:** 짧은 들숨과 집중하는 눈. 패널은 고통·절규보다 힘을 받아낼 결심.
- **진입:** 두 손 그립과 발의 지지가 보이는 자세가 후보. 없는 차징 시간·방어 판정을 추가하지 않는다.
- **전개:** 검 주위 장식이 퍼지기보다 좁아지며 중량감이 쌓인다. 흔들림 대신 몸통과 검의 대비로 보여준다.
- **피니시:** **전용 동작 목표는 고정된 중심의 세로 베기.** 두꺼운 짧은 충격 형태와 낮은 음으로 정점을 만든다. 실제 지면 접촉이 없으면 크레이터·바닥 분쇄는 제외한다.
- **복귀:** 중량이 풀리고 전투 자세가 돌아오는 과정이 보인다. 별도 긴 숨고르기로 조작 복귀를 늦추지 않는다.

**화면·소리:** 몸통과 검의 위아래를 함께 잡는 3/4. 바닥에서 올려다보는 극저각 제외. 낮은 공격부·짧고 두꺼운 잔향. T1 또는 T2 후보.

**리소스:** GreatSword 기존 후보의 그립·베기 방향 확인, 압축 충격 FX·전용 패널. 공유 실행이 횡베기라면 세로 FX를 덧씌우지 않고 현재 검 궤적에 압축감만 적용한다. 세로 전용 모션은 후속안이다.

### 3.7 릴리 — 「돌아갈 길」

**핵심:** 큰 검을 넓게 가로질러 몸 앞 공간을 연다. 고향 사람을 포기하지 않는 왕복 구조에서 모티프를 얻는다. 사람을 강제로 데려가거나 정착/귀향의 답을 대신 정하는 연출은 넣지 않는다.

- **예고:** 검 너머를 확인하고 어깨를 돌릴 준비. 패널은 세라의 정면 고정과 달리 실제 공격 방향으로 기운 시선.
- **진입:** 낮은 준비에서 어깨와 검이 함께 열리는 자세가 후보. 반걸음 전진도 실제 이동이 있을 때만 사용한다.
- **전개:** 넓은 횡방향의 검 궤적을 읽힌다. 공중회전·곡예·연속 점멸로 검의 무게를 지우지 않는다.
- **피니시:** **전용 동작 목표는 몸 앞을 크게 가르는 횡베기.** 부채 전체를 폭발로 덮기보다 실제 칼날이 통과한 가장자리를 강조한다.
- **복귀:** 잔광이 좌우로 걷히며 현재 전장이 드러난다. 실제 적 밀기·길 생성·지형 파괴는 없다.

**화면·소리:** 검이 화면 끝에서 잘리지 않는 측후방 와이드. 넓은 공기음과 금속 공격음, 세라보다 옆으로 길게 걷히는 잔향. T1 또는 T2 후보.

**리소스:** GreatSword 기존 횡베기 후보, 가로 궤적 FX·전용 패널. 세라와 공유 실행이 세로 베기라면 가로 공격인 척하지 않고 소리·패널·안전 구도를 우선 차별화한다. 다른 베기 방향은 후속 모션 저작 범위다.

### 3.8 연화 — 「내 손으로 거둔다」

**핵심:** 한 장의 얇은 수직 도형이 지팡이의 시전축으로 정리된 뒤 전방으로 방출된다. 봉헌 협조를 철회하고 물자를 인계하는 선택에서 모티프를 얻는다. 신의 권능·성직 직함·광역 정화를 신설하지 않는다.

- **예고:** 고개를 숙인 기도가 아니라 실제 목표를 보는 표정. 패널은 타인의 응답을 기다리기보다 직접 정하는 눈.
- **진입:** 지팡이 주변의 좁은 수직 빛 도형 하나를 제시한다. 바닥 대형 마법진을 깔지 않는다.
- **전개:** 위에서 아래로 짧게 정리되는 도형이 지팡이의 실제 발사축을 드러낸다. 닫히는 동작은 FX 문법이며 적을 압착하는 판정이 아니다.
- **피니시:** 기존 Staff 궁극기의 **전방 마력 방출**에 맞춰 곧게 나간다. 원형 광역 폭발·적 전체 정화로 바꾸지 않는다.
- **복귀:** 도형이 끊김 없이 걷히고 지팡이·시전자의 실루엣이 먼저 남는다.

**화면·소리:** 단일 수직 도형과 전방 발사축이 모두 보이는 3/4. 한 음이 선명하게 맺힌 뒤 전방 공격음, 짧은 잔향. T1 후보. 상승 크레인과 대형 신전 배경은 불필요하다.

**리소스:** [Staff 공용 궁극기](../../../../10.Datas/Ability/Player/Staff/GA_PlayerStaff_Ultimate.asset)·[실행 모션 후보](../../../../10.Datas/Ability/Player/Staff/Motions/Staff_Ultimate.asset)의 실제 매핑 확인, 수직 단일 도형 FX·전용 패널. 새 영창 포즈는 필요할 때만 후속 검토한다.

### 3.9 묘묘 — 「흩어진 빛을 모아」

**핵심:** 여러 작은 궤적이 지팡이 끝으로 모이고 한 방향으로 풀린다. 전달한 물건을 확인·회수하는 책임을 움직임 모티프로 삼는다. 귀 장식에서 동물어미·유아어·장난감 소환을 도출하지 않는다.

- **예고:** 흩어진 지점을 짧게 보고 목표에 시선을 모은다. 패널은 장난스러운 윙크보다 확인과 집중.
- **진입:** 시전자 가까운 공간의 작은 빛 조각들이 서로 다른 출발점에서 움직인다. 적 몸에 표식을 붙이지 않는다.
- **전개:** 빛이 지팡이 끝 한 점으로 모인다. 적·투사체·체력·물품을 실제로 흡수하는 소리나 묘사는 피한다.
- **피니시:** 모인 빛이 공용 Staff의 실제 전방 방출과 동시에 풀린다. 방출 후 다른 방향에서 추가 명중하는 가짜 빛탄을 만들지 않는다.
- **복귀:** 몸 주변의 조각이 먼저 사라지고 발사 잔광이 실제 공격 수명에 맞춰 걷힌다.

**화면·소리:** 근거리 여러 점 → 한 점 → 전방의 흐름이 한 화면에서 보이는 3/4. 작은 음들이 하나의 음으로 모인 뒤 방출. 연화의 단일 수직 도형과 색 없이 구분한다. T1 후보.

**리소스:** 연화와 동일한 공용 실행 경로를 보존하며 다점 수렴 FX·전용 패널·음향 공격부를 달리한다. 전용 춤·소환물·새 치유 효과는 필요 조건이 아니다.

### 3.10 유라 — 「엇갈린 칼끝」

**핵심:** 왼쪽과 오른쪽의 짧은 궤적이 교차한 뒤 서로 다른 방향으로 걷힌다. 구조를 위해 오가는 행동은 양측 공간을 확인하는 동기로 삼는다. 민첩함의 과시·도발·순간이동을 캐릭터 성격으로 고정하지 않는다.

- **예고:** 두 칼의 방향과 실제 대상을 확인한다. 패널은 크게 웃는 도발보다 빠르게 초점을 잡는 눈.
- **진입:** 양손 칼이 서로 다른 높이에 놓이는 준비가 후보. 짧은 발놀림도 실제 모션 범위 안에서만 사용한다.
- **전개:** 실제 좌우 검 궤적을 서로 다른 두 선으로 보여준다. 카메라가 주위를 도는 대신 무기가 움직인다.
- **피니시:** 실제 정점 타격이 교차 동작이면 접점에 좁은 섬광을 맺는다. 순차 타격이면 동시에 맞은 것처럼 합치지 않고 좌우 도달음을 유지한다.
- **복귀:** 두 잔광이 갈라져 걷히며 중심에 여백을 남긴다. 여백은 미술 표현이며 적을 밀어내는 통로가 아니다.

**화면·소리:** 두 손과 적을 모두 볼 수 있는 안정된 3/4. 좌우 공격부의 짧은 응답과 교차 순간의 합쳐지는 음. T1 또는 T2 후보. 빠른 컷 반복·전 화면 점멸은 제외한다.

**리소스:** DualBlade 실제 좌우 타격/그립 확인, 두 선 궤적·교차 정점 FX·전용 패널. 기존 클립에 교차가 없으면 전용 교차 포즈는 후속안으로 남긴다.

## 4. 제작과 비교의 우선순위

| 순서 | 묶음 | 먼저 해소할 질문 |
|---|---|---|
| 1 | 세라·릴리 | 공유 GreatSword 실행에서 유지할 표현과 전용 베기 모션이 필요한 표현을 분리할 수 있는가? |
| 2 | 연화·묘묘 | 같은 전방 방출을 보존하면서 단일 수직 도형/다점 수렴이 회색 화면에서도 구분되는가? |
| 3 | 아린·기존 라온 비교 | 실제 라온 영상과 비교해 사선 강조만으로 차이가 나는가? 연타·납도 가정은 없는가? |
| 4 | 묘령·레이네 | 방패와 창의 정렬을 몸통 기준 안전 구도에서 읽을 수 있는가? |
| 5 | 설아·유라 | 실제 발사/좌우 타격과 궤적·음향의 위치·순서가 맞는가? |
| 6 | 리안 | 기존 채찍 본/클립으로 비접촉 곡선→직선→회수가 성립하는가? |

기존 컷인을 우선 대조·재사용한다. 각 절의 ‘전용 패널’은 캐릭터별 표시 리소스를 뜻하며 새 그림 제작이나 기존 그림 교체 지시가 아니다. [아린·묘령 최신 작화](../../../../../docs/work-status/weapon-ultimate-presentation/FRESH_ARIN_MYORYEONG.md)와 [얼굴 제작 기록](../../../../../docs/work-status/weapon-ultimate-presentation/IMAGE_PROMPTS.md), 실제 저장된 `Assets/04.Images/SkillCutIn/` 파일을 함께 확인한다. 외형은 오래된 R01 이미지 관찰보다 최신 사용자 피드백·제작본을 우선한다. 부족한 리소스와 실제 표시 연결은 별도 확인하며 이 문서의 표정 방향만으로 새 스타일 승인을 대신하지 않는다. 신규 Blender 클립은 기존 클립 재조합의 부족이 대상 모델에서 확인된 뒤 필요한 피니시만 artist에게 위임한다.

## 5. 구현 후 사용자 확인 기준

아래는 향후 제작물의 확인 기준이며 지금 테스트 가능한 완료 콘텐츠라는 뜻이 아니다. 실제 준비 씬·입력·에셋 식별은 구현 시 [상태 문서](../../../../../docs/work-status/weapon-ultimate-presentation/MISSING_ULTIMATE_CONCEPTS.md)에 연결한다.

| ID | 확인 방법 | 통과 기준 |
|---|---|---|
| C01 | Staff2·GreatSword2를 같은 적/장소에서 차례로 발동, 얼굴·색 강조를 뺀 비교 | 무기 동작/도형/소리로 두 연출을 구분. 실제 동작이 공용이면 그 한계를 기록 |
| C02 | 아린과 현재 라온을 같은 조건에서 비교 | 단순 색 변경 이상으로 구분되며 실제 연타·납도 여부를 거짓으로 표현하지 않음 |
| C03 | 경사·벽 가까이·큰 적·16:9/21:9에서 발동 | 얼굴 패널 밖에 몸통·주요 무기·명중 영역이 보이고 발밑 구도·지형 관통·무기 잘림이 없음 |
| C04 | 실제 명중·빗나감·복수 적에서 확인 | 없는 타격·구속·흡인·치유를 암시하지 않음. 무효/범위 밖 적에게 명중 섬광 없음 |
| C05 | 연속 사용 후 조작, 중단/사망/씬 이탈/일시정지 확인 | 실제 조작 복귀가 늦어지지 않으며 패널·카메라·시간·대역이 남지 않음 |
| C06 | 열 번 반복해 화면·음향 피로 확인 | 과도한 섬광·회전·큰 음량으로 명중 정보가 묻히지 않고 무게감 차이가 유지됨 |

미술 품질·무게감·최종 이름은 사용자 확인 항목이다. 컨셉 선택과 자동 데이터 검사가 실제 플레이 인수를 대신하지 않는다.

## 6. 클립 조사 후 확정한 제작 방향과 에셋

이 절이 §3의 동작 제안보다 우선한다. **기존 클립을 MotionSet으로 조합하는 경로를 기본으로 확정한다.** 실제 클립의 자세·무기 본·연결·타격 궤적을 만족하지 못하는 구간만 Blender MCP로 제작한다. 신규 제작을 피하려고 판정을 옮기거나, 무기 FX로 잘못된 자세를 가리지 않는다.

여기서 **소스 채택 확정**은 실제 존재하는 제작 입력을 정했다는 뜻이다. 신규 궁극기 SO가 만들어졌거나 실제 플레이·미술 인수를 통과했다는 뜻이 아니다. 이번에는 실행 에셋을 변경하지 않고 두 대검 조합을 메모리상 MotionSet으로 샘플링했다. 조사 결과와 프리뷰 한계는 [모션 조사 기록](../../../../../docs/work-status/weapon-ultimate-presentation/MOTION_ASSET_DECISIONS.md)을 따른다.

### 6.1 캐릭터별 채택

| 인물 | 소스 결정 | MotionSet 구성·연출 보정 | 확정 범위와 남은 조건 |
|---|---|---|---|
| 아린 | **A1 현행 체인 유지** | 같은 클립 0~2.13 → 1.35~2.13 → 1.35~3.45초, 속도 1. 총 5.010001초 | 16개 Collision 이벤트를 보존한다. ‘한 사선’은 강조할 궤적의 문법이며 단일 타격으로 축소하지 않는다 |
| 묘령 | **M1 현행 유지** | 0~2.750000초. 방패 준비·검격·방패 복귀를 기존 동작 위에서 읽힌다 | 충돌 0.772534~0.978173초와 기존 Warp 유지. 실제 반격/추가 방패 타격 신설 없음 |
| 레이네 | **R0 현행 보존, R1 직선 동작 입력 채택** | R0는 점프가 있는 2.666667초 동작. R1은 2초의 낮은 자세·전방 찌르기 후보 | **직선 궁극기로의 교체는 미확정.** R0/R1 모두 이 MotionSet의 이벤트 목록이 비어 있어 실제 공격 소비 경로 확인이 선행한다. 명중 FX·새 판정을 임의 추가하지 않음 |
| 리안 | **L1 + LW1 쌍으로 채택** | 몸 Humanoid와 채찍 Generic를 같은 시계·같은 구간·속도 1로 재생. 총 3.166668초 | 공중 동작과 7개 Collision 유지. 별도 무기 본 클립을 빼고 몸만 재생한 결과를 채찍 완성으로 보지 않음 |
| 설아 | **B1 현행 유지** | 기존 공중 동작·2회 발사 유지. 카메라는 몸과 실제 발사 방향을 함께 확보 | 발사 0.798942/0.971856초. ‘한 줄’은 각 발의 방향 집중을 뜻하며 1발로 줄이지 않는다. 지상 단발 Bow Skill1은 이번 채택에서 제외 |
| 세라 | **GP + GS + GR 조합 입력 채택** | 준비 1.162419초 → 내려베기 2.433338초 → 복귀 0.070930초. 세부는 §6.3 | **조건부 조합.** 기존 0.5초 타격 창 전체의 유효 궤적 미확인. 부적합하면 세로 피니시만 Blender 제작으로 전환 |
| 릴리 | **GP + GL + GR 조합 입력 채택** | 준비 0.423532초 → 넓은 베기 3초 → 복귀 0.243155초 | **시간 정합 확인, 적용 미확정.** 타격 창 시각은 맞지만 그립·공간적 명중 범위·전환 검증 필요 |
| 연화 | **S1 전체와 손가락 레이어 유지** | Magic_Attack_2 0~2.166667초. 단일 수직 도형만 차별화 | 실제 발사 1.05초 유지. 새 영창/원형 광역 공격 없이 기존 전방 방출 사용 |
| 묘묘 | **S1 전체와 손가락 레이어 유지** | 연화와 동일 실행, 다점 수렴 FX/음향으로 차별화 | 손가락 마스크·holdLastFrame 보존. 피부/의상·전장 장비를 완성한 실제 표시 인수는 별도 |
| 유라 | **Y1 현행 유지** | 기존 Rolling과 양손 동작, 5개 Collision 유지. 시전자 대신 카메라가 회전하지 않음 | 실제 순차 타격을 임의로 동시 교차 한 번으로 합치지 않는다. 교차 FX는 실제 양손 궤적이 만나는 순간에만 사용 |

아린·묘령·리안·설아·연화·묘묘·유라 **7명은 현행 실행 소스를 재사용하는 것으로 확정**한다. 레이네는 현행 소스 보존과 직선 후보 확보까지, 세라·릴리는 조합 입력까지 확정했다. 나머지 3명의 교체 완료를 선언하지 않는다.

### 6.2 확정 입력 에셋 대장

경로는 실제 디스크·Unity AssetDatabase에서 확인했다. 원본 FBX의 서브 에셋은 이름 대신 GUID + local file ID로 식별한다. 아래 초 단위 값은 원본 구간이며 타격 시점을 새로 정하는 값이 아니다.

| ID | MotionSet 입력 | 길이(초) | 연결 클립 / 원본 구간 |
|---|---|---:|---|
| A1 | [Katana_Skill_Ultimate.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Skill_Ultimate.asset) | 5.010001 | Take 001 `0.000000~2.130000` ×1<br>Take 001 `1.350000~2.130000` ×1<br>Take 001 `1.350000~3.450000` ×1 |
| M1 | [Humanoid_SwordShield_Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Skill_2.asset) | 2.750000 | Take 001 `0.000000~2.750000` ×1 |
| R0 | [Humanoid_Spear_Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Spear/Humanoid_Spear_Skill_2.asset) | 2.666667 | Take 001 `0.000000~2.666667` ×1 |
| R1 | [Humanoid_Spear_Skill_1.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Spear/Humanoid_Spear_Skill_1.asset) | 2.000000 | Take 001 `0.000000~2.000000` ×1 |
| L1 | [WhipCharacter_Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Skill_2.asset) | 3.166668 | Frank_Whip@Skill02 `0.000000~3.166668` ×1 |
| LW1 | [Whip_Skill_2.asset](../../../../10.Datas/Actor/Animation/WeaponMotion/Whip/Whip_Skill_2.asset) | 3.166668 | Frank_Whip_WPN_Skill02 `0.000000~3.166668` ×1 |
| B1 | [Humanoid_Bow_Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Skill_2.asset) | 1.900000 | Bow_Skill_2 `0.000000~1.900000` ×1 |
| G0 | [Humanoid_GreatSwordAnimationSet_Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Skill_2.asset) | 3.666687 | GreatSword_Skill02 `0.000000~3.666687` ×1 |
| GP | [Humanoid_GreatSwordAnimationSet_Guard.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Guard.asset) | 1.166664 | GreatSword_Guard `0.000000~1.166664` ×1 |
| GS | [Humanoid_GreatSwordAnimationSet_HeavyAttack_3.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_3.asset) | 2.433338 | GreatSword_Combo04_3 `0.000000~2.433338` ×1 |
| GL | [Humanoid_GreatSwordAnimationSet_Skill_1.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Skill_1.asset) | 3.000000 | GreatSword_Skill01 `0.000000~3.000000` ×1 |
| GR | [Humanoid_GreatSwordAnimationSet_Idle.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Idle.asset) | 4.500000 | GreatSword_Equip_Idle `0.000000~4.500000` ×1 |
| S1 | [Staff_Ultimate.asset](../../../../10.Datas/Ability/Player/Staff/Motions/Staff_Ultimate.asset) | 2.166667 | Magic_Attack_2 `0.000000~2.166667` ×1 |
| Y1 | [Skill_2.asset](../../../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Skill_2.asset) | 2.466667 | Take 001 `0.000000~2.466667` ×1 |

**연결된 원본 클립 식별자**

| ID | 실제 원본 파일 | 클립 GUID / local file ID |
|---|---|---|
| A1 | [Frank_RPG_Katana_S3_Skill02.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/Root_Motion/Frank_RPG_Katana_S3_Skill02.FBX>) | `4f6e227852a64394398010170e126410` / `7400000` |
| M1 | [Frank_RPG_Warrior_ShieldSkill02.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Warrior/FBX Animation/Root_Motion/Frank_RPG_Warrior_ShieldSkill02.FBX>) | `323c23268b429794990135c96b378665` / `7400000` |
| R0 | [Frank_RPG_Spear_Skill02.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Spear/FBX_Animation/Frank_RPG_Spear_Skill02.FBX>) | `f6abea2c8bbce644eb2b55bb95777c62` / `7400000` |
| R1 | [Frank_RPG_Spear_Skill01.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Spear/FBX_Animation/Frank_RPG_Spear_Skill01.FBX>) | `b1e5624fc8d057f43b8c491bc6360eed` / `7400000` |
| L1 | [08_Frank_Whip_Combo_Skill_All.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Whip/Anim_FBX_Pack/08_Frank_Whip_Combo_Skill_All.FBX>) | `72b53cbc8461bff46aadf52e39c95364` / `-5926376059806170833` |
| LW1 | [11_Frank_Whip_Weapon_Animation_All.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Whip/Anim_FBX_Pack/11_Frank_Whip_Weapon_Animation_All.FBX>) | `8edf693a1cd367545bc1527ddfa62e26` / `9166450028386467682` |
| B1 | [Bow_Skill_2.fbx](<../../../../ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Bow/Skill/Bow_Skill_2.fbx>) | `8094ef08bf3e15a458c6e1f96974b1b9` / `4748606091391170684` |
| G0 | [Frank_RPG@GreatSword_Skill02.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Skill02.FBX>) | `b35e7a67bf05a564f95a9b7728bf0a6d` / `7400000` |
| GP | [Frank_RPG@GreatSword_Guard.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Guard.FBX>) | `48098c048bea8f74687c82dab8377b8a` / `1827226128182048838` |
| GS | [Frank_RPG@GreatSword_Combo04_3.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Combo04_3.FBX>) | `98b10849b4e56c54fb53814fb484f1a0` / `7400000` |
| GL | [Frank_RPG@GreatSword_Skill01.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Skill01.FBX>) | `a6d9f62edf9f5a548b1e2dc4215c314e` / `7400000` |
| GR | [Frank_RPG@GreatSword_Equip_Idle.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Equip_Idle.FBX>) | `7825c15d1b2edef4b963e6f854208973` / `1827226128182048838` |
| S1 | [Magic_Attack_2.fbx](<../../../../ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Magic/Magic_Attack_2.fbx>) | `4f84db152d3ce3f499df99b7adb28056` / `-8151563879859848126` |
| Y1 | [Frank_RPG_Dual_Skill02_Rolling.FBX](<../../../../ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Dual/FBX Animation/Root_Motion/Frank_RPG_Dual_Skill02_Rolling.FBX>) | `50dfc57f34504224b834c230718fde29` / `7400000` |

S1은 위 본체 외에 `StaffRightFingers.mask`를 사용하는 오른손 쥐기 레이어가 있다. 원본은 `Assets/ExternalAssets/AnimationOnly/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H04_R.fbx`, 클립 구간 0~0.033333초·속도 1·`holdLastFrame=true`다. 이를 빼고 본체만 복사하지 않는다. 과거 P09Staff의 Magic_Attack_1 후보는 이번 플레이어 Staff 채택 근거가 아니다.

### 6.3 대검 조합의 정확한 경계

두 캐릭터의 현행 기준 G0는 총 3.666687초, Collision 1.812321~2.312321초다. **공유 G0·AbilitySet을 직접 수정하지 않고** 구현 단계에서 캐릭터 전용 MotionSet으로 분리한다. 새 매핑 전에는 현행 G0를 유지한다.

| 구간 | 세라 소스 구간 / 속도 | 릴리 소스 구간 / 속도 |
|---|---|---|
| 준비 | GP 0~1.162419 / 1 | GP 0~0.423532 / 1 |
| 공격·기본 회수 | GS 0~2.433338 / 1 | GL 0~3.000000 / 1 |
| 자세 복귀 | GR 0~0.070930 / 1 | GR 0~0.243155 / 1 |
| 총길이 | 3.666687 | 3.666687 |

준비 길이는 `G0 Collision.start − donor Collision.start`, 마지막 구간은 `G0 총길이 − 준비 − donor 전체길이`로 계산한다. 표의 반올림 값을 수작업으로 재입력해 오차를 누적하지 않는다.

- **릴리:** GL의 Collision 1.388789~1.888789초에 준비 0.423532초를 더하면 기존 창과 일치한다. 메모리상 조합에서 넓게 베는 몸 자세는 확인했으나 무기 궤적의 공간적 동일성까지 확인한 것은 아니다.
- **세라:** GS의 Collision을 이동하면 **1.812321~1.954899초**다. 기존 창 끝까지 **0.357422초의 차이**가 남는다. 단순히 기존 창을 복사하면 회수 중 맞거나 이전과 다르게 빗맞을 수 있다. 이벤트 숫자·전역 시각 유지와 판정 결과 보존을 구분한다.
- donor의 공격·VFX·Warp·소리 이벤트를 통째로 복사하지 않는다. 기존 실제 궁극기의 판정·콜백·시간 정책을 원본으로 대조하며, 타격 밖 준비/복귀에 새 공격을 만들지 않는다. 로컬 이벤트를 전역으로 옮길 필요가 있으면 현재 MotionTimelineResolver의 시간 해석을 사용한다.
- 준비→공격→복귀의 블렌드, 손잡이/보조손 접촉, 칼날이 지나는 사거리 경계, 종료 자세와 다음 입력을 검증한다. 현재 6포즈 프리뷰는 연속 블렌드·루트 이동·실제 명중 검증을 대신하지 않는다.
- 세라의 GS를 단순 감속해 0.142578초 타격을 0.5초로 늘리는 방식은 채택하지 않는다. 기존의 즉각적인 내려베기 감각을 훼손할 수 있으므로 구간 조합으로 맞지 않으면 §6.4로 전환한다.

### 6.4 Blender MCP 전환 규칙과 납품 범위

**방향은 확정한다:** 기존 클립의 재생 구간·체인·마스크·그립 보정으로 컨셉과 실제 판정 창을 함께 만족하면 재사용한다. 이를 만족하지 못한 부분은 Blender MCP에서 제작해 같은 MotionSet 경로로 사용한다. 이번 조사만으로 10명 전체 신규 클립을 발주하지 않는다.

| 대상 | Blender 전환 조건 | 제작할 최소 클립 |
|---|---|---|
| 세라 | GS 조합이 기존 1.812321~2.312321초의 칼날/HitBox 유효 궤적과 고정된 중량감을 함께 만족하지 못함 | 땅을 딛는 세로 피니시와 그 앞뒤 연결. 공중 도약은 넣지 않고 기존 총길이/판정 창에 맞춤 |
| 릴리 | GL 조합의 손 그립·넓은 횡베기·전환을 구간/보정으로 해결하지 못함 | 횡베기 피니시 또는 부족한 준비/복귀 연결만. 검 메시·전체 기존 콤보를 재제작하지 않음 |
| 레이네 | 실제 공격 실행 경로를 먼저 확인한 뒤, R1이 요구되는 타격 창·창 그립·회수에 맞지 않음 | 필요한 직선 찌르기/회수 연결만. 이벤트 부재를 새 클립 제작으로 해결한 것으로 처리하지 않음 |
| 나머지 7명 | 채택 소스의 실제 대상 리그·장비 조합에서 구체적인 실패가 재현되고 기존 재사용 보정으로 해결되지 않음 | 실패가 발생한 준비/피니시/복귀의 해당 구간만. 리안 T3 접촉은 별도 사용자 결정 유지 |

신규 제작 시 artist에게 원본/대상 리그·무기 축·그립 기준·현재 타격 창·루트 이동 정책·시작/끝 자세를 함께 넘긴다. Blender 원본은 `ArtSource/Animation/Ultimate/<캐릭터>/`, Unity 납품은 `Assets/07.Animations/Player/Ultimate/<캐릭터>/` 아래 독립 FBX/클립으로 두는 **신규 제작 예정 경로**다. 아직 존재하는 확정 에셋처럼 링크하거나 사용하지 않는다. 구매 원본 FBX·Importer·기존 Avatar는 변경하지 않는다.

납품에는 몸 Humanoid와 필요한 Generic 무기 본 클립을 분리하고, 손가락/소품 추가 커브를 보존한다. Blender 프리뷰와 Unity 대상 모델의 그립·실행 확인을 나눠 기록한다. 정상 종료뿐 아니라 피격·취소·장비 전환 때의 복구를 확인한 후 실제 전용 MotionSet에 연결한다. 미술 품질은 사용자 확인이다.

