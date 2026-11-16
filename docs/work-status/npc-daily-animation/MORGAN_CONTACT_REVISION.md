# 모건 화물 접촉 재보정

사용자 후속 지시: “팔 위치가 짐과 안맞아서 요청한건데 수정버전에서는 맞니?”, “확인하고 개선해”. 앞선 보정본은 손이 한쪽에 모여 실제 접촉을 충족하지 못했다. 이번 변경은 양손과 실제 자루 표면의 위치를 기준으로 재보정한다.

## 반영

- 실제 `NPC_Morgan` / `P09_250802Avatar` / 현 의상 / `MorganCargo_0,1`을 사용했다. 구매 원본과 최초 클립은 보존했다.
- 첫 작업점을 짐 방향으로 24cm 옮겼다. 두 번째는 경사면의 높은 쪽에서 손이 닿지 않아 짐 반대쪽의 낮은 지면으로 옮겼다(방향 120도). KCC 관찰 높이를 반영해 작업점 높이를 지면 +8.5cm로 맞췄다. 짐 자체는 이동하지 않았다.
- 첫 작업점 `(1086.392,56.27435,410.680)` / 240도, 두 번째 `(1088.014,56.26175,410.680)` / 120도. 도착 허용 반경은 6cm에서 1.5cm로 줄였다.
- 양손을 각각 다른 표면점에 놓도록 실제 손바닥 중심(손 40%, 검지·소지 첫마디 각 30%)으로 팔 Humanoid 곡선을 맞췄다. 표면 법선 방향으로 1.8cm 여유를 두며 손 메시 전체의 접촉 오차를 뜻하지 않는다. 다리·발 곡선은 유지하고 팔·척추·가슴을 보정했다.
- 첫 짐은 [모건 클립](../../../Assets/07.Animations/LivingWorld/NPC/LW_InspectCargoLoop_Morgan.anim)과 기존 `LW_CargoInspect`, 두 번째는 [전용 클립](../../../Assets/07.Animations/LivingWorld/NPC/LW_InspectCargoLoop_MorganCart.anim)과 [전용 MotionSet](../../../Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/LW_CargoInspect_MorganCart.asset)을 사용한다. 길이 5.3333초, 재생 속도 0.85 유지.
- 원본 [MorganRoutine](../../../Assets/03.Prefabs/LivingWorld/NPC/MorganRoutine.prefab)의 작업점 위치·방향·두 번째 모션·도착 반경을 반영했다. 기존 다른 미커밋 코드·데이터는 보존했다. 런타임 C# 변경 없음.

## 검증

| 범위 | 결과 | 근거 |
|---|---|---|
| 실제 몸·짐 표면으로 접촉 보정 | 각 81표본 | [저작 도구](../../../ArtSource/NpcDailyAnimations/MorganCargo/fit-contact.csx), cart를 true로 바꿔 두 번째 검사 |
| 저장 클립 재생 / 보간 포함 321표본 | 첫 짐 손바닥 중심 목표 오차 최대 1.829mm, 두 번째 1.547mm | [첫 짐](morgan-contact-validation.tsv), [두 번째](morgan-contact-cart-validation.tsv) |
| 앞·뒤·옆 Unity 격리 화면 | 양손 분리·자루 근처 접촉 확인. 기존 머리 분리는 RotationConstraint 복제 누락이었으며 프리뷰 수정 후 정상 | [첫 짐 옆](../../../output/NpcDailyAnimations/MorganCargo/1-2.png), [두 번째 옆](../../../output/NpcDailyAnimations/MorganCargo/cart/1-2.png), [촬영 도구](../../../ArtSource/NpcDailyAnimations/MorganCargo/review.csx) |
| 실제 일과 짧은 관찰 | 기록은 남았으나 작업 재생 표본 0개. 실제 접근·접촉·중단 통과로 판정하지 않음 | [상태](../../../output/NpcDailyAnimations/MorganCargo/live/status.txt), [기록](../../../output/NpcDailyAnimations/MorganCargo/live/frames.tsv) |

최종 씬 저장은 15:40:49 파일 시각으로 확인했다. Editor의 import/reload와 stdio 응답 유실이 반복되어 단일 호출의 반환만으로 성공을 판단하지 않았으며 실제 저장 파일과 검사 TSV를 대조했다. 두 번째 모션은 기존 씬 인스턴스 override가 남아 있던 것을 마지막 저장에서 명시적으로 다시 연결했다. 이후 실제 일과 재생은 미검증이다.

마지막 Unity 조회에서 `playing=false`, 두 번째 작업점 `Motion=LW_CargoInspect_MorganCart`, `scene.isDirty=false`를 확인했다. 씬은 프리팹 상속 참조를 사용하므로 씬 텍스트에 새 MotionSet GUID가 직접 나타나는지로 연결 성공을 판단하지 않는다. [준비표·인계](../../../output/harness/20261115-152541-7ff4bfe7/review.md).

이 수치는 정해진 작업점에서 재생한 모델의 손바닥 중심 오차다. 실제 접근 오차·블렌드·KCC 높이·장갑 전체·손가락 관통을 자동으로 통과시키는 수치가 아니다. 실제 일과 관찰 결과는 아래에 별도로 기록한다. 사용자 미술 인수·대화/중단/전체 동선은 별도다.

## 보존과 후속 확인

- 최초 보정본과 캡처는 `output/NpcDailyAnimations/MorganCargo/before-contact/`에 보존해 전후 비교에 쓴다. 원본 구매 데이터·기존 씬 작업·meta 보존. Player/Addressables 빌드·커밋 없음.
- `M01`: 양쪽 짐에서 3회 반복 시 손이 떠 있거나 관통하는지 확인. `M02`: 접근→작업→이탈 및 변경한 두 번째 접근 동선 확인. `M03`: 대화·중단 후 복귀 확인. 정상 속도 최종 품질은 사용자 테스트 대기.
- [상태](MORGAN_CARGO.md) · [NPC 위키](../../wiki/npc.md).
