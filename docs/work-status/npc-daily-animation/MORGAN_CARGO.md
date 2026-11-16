# 모건 화물 점검 클립 보정

## 접수와 판정

- 사용자 원문: “모건 NPC 행동 클립 개선 요청”, 첨부 화면에서 자루 앞에 양팔을 든 자세.
- FEEL-MORGAN-01: 화물 점검 동작의 목적과 접촉 개선 요청. 연출 다듬기로 분류. 빈도·회귀·입력 장치·저장 슬롯은 모름. 정지 화면만으로 런타임 결함을 확정하지 않았다.
- 지침: [NPC 일상 애니메이션](../../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md).
- 아래는 첫 보정 기록이다. 사용자 지적 후 실제 접촉 기준으로 다시 수정했다. **현재 정본은 [접촉 재보정 결과](MORGAN_CONTACT_REVISION.md)**이며 아래 최초 표본을 현재 품질 근거로 쓰지 않는다.

## 반영

- 조사 당시 열린 `LakeVillageWorld`의 `NPC_Morgan`, Avatar `P09_250802Avatar`, 현재 의상·화물을 확인했다. 두 작업점 `lake.transport.inspect/cart` 모두 `LW_CargoInspect`를 사용했다.
- [기존 클립](../../../Assets/07.Animations/LivingWorld/NPC/LW_InspectCargoLoop.anim)은 보존하고 [독립 보정본](../../../Assets/07.Animations/LivingWorld/NPC/LW_InspectCargoLoop_Morgan.anim)을 만들었다. GUID `17bb55cb764283c40a555ff9b6e090f2`, local ID `7400000`.
- 양팔 높이·어깨·팔꿈치·손목을 낮추고 상체 굽힘을 완화했다. 목의 과한 들림을 줄이고 좌우 시선 변화를 추가했다. 2.6667초에서 5.3333초로 늘렸으며 기존 MotionSet 속도 0.85에서 약 6.27초 주기로 돈다. 최초/끝 곡선 값을 맞췄다.
- [MotionSet](../../../Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/LW_CargoInspect.asset)의 클립 참조만 변경했다. 기존 씬·프리팹·생성 입력이 같은 MotionSet을 사용하므로 GUID와 일과 계약을 보존한다. 다른 주민·운반·밧줄 모션은 변경하지 않았다.
- 신규 Blender 애니메이션 제작은 수행하지 않았다. 기존 Humanoid 곡선을 Unity Editor에서 독립 복제·보정했다. 새 런타임 코드 없음.

## 검증과 한계

- Unity에서 보정본 저장 후 `length=5.33333349`, `isLooping=true`, `dirty=false` 확인. 첫 보정 단계에서 `isHumanMotion=true` 확인.
- [비교 표본 도구](../../../ArtSource/NpcDailyAnimations/MorganCargo/review.csx)는 실행 스크립트 없이 실제 모델 계층·스킨과 화물을 복제한다. [원본 중간 자세](../../../output/NpcDailyAnimations/MorganCargo/0-1.png), [첫 보정 중간 자세](../../../output/NpcDailyAnimations/MorganCargo/1-1.png)에서 팔을 낮춰 자루 근처에 둔 차이를 확인했다. 각각 세 표본을 보존했다.
- 표본에서 머리/머리카락 분리가 보인다. 격리 복제 프리뷰 결함으로 분류하며 실제 외형 통과 근거로 쓰지 않는다. 블렌드셰이프 복사를 보완했으나 이후 재촬영은 모건 언로드로 실패했다. 위 캡처는 **시간 연장·좌우 시선 추가 이전 revision**이며 최종 시각 증거가 아니다.
- Editor가 `CombatTestMap`으로 바뀌어 모건이 사라진 후 MCP가 `No Unity Editor instances found`를 반환했다. 씬 전환·Editor 재실행은 하지 않았다. 마지막 MotionSet 참조는 파일에서 수정했으므로 최신 Unity 재로드·Missing 참조·실제 전환 검사는 미수행이다.
- 원본·meta·기존 미커밋 변경 보존. 비교 PNG 여섯 장과 재현용 도구를 보존했다. 삭제할 임시 부산물 없음. 커밋·Player/Addressables 빌드 없음.

## 사용자 확인

현재 수정 파일을 임포트한 `LakeVillageWorld`에서 모건의 낮 시간 화물 작업을 확인한다. 기존 저장과 사용 중인 입력 장치를 유지한다.

| ID | 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| M01 | 모건이 화물 앞에서 3회 반복할 때 관찰 | 팔을 과하게 든 자세가 줄고 시선이 화물을 살핌. 손·자루 관통, 몸통·손 꼬임, 발 미끄러짐은 실패 |
| M02 | 양쪽 작업점의 접근→작업→이탈 확인 | 운반과 작업 사이 튐·소품 잔상 없음. 새 집기/내려놓기 클립은 이번 범위에서 제작하지 않음 |
| M03 | 작업 중 대화 후 종료·복귀 | 정상 복귀, 머리·의상 분리 없음, Console 새 오류 없음 |

회신: `M01/M02/M03 / 통과·실패·미수행 / 실제 증상 / 필요 시 화면`.
