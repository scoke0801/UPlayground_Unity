# Player Katana 공격 FX 보완

2026-11-14. 상태: MotionSet 21개에 FX 24개(베기 23, 발차기 1) 반영 및 변경분 보조 검사 완료. 사용자 플레이 인수 대기.

- 지침: [전투 저작](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [검증](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md).
- 실행 원본: `Assets/10.Datas/Actor/Footsteps/Motions/Set_Katana.asset`. 기본 `Humanoid_Player_KatanaAnimationSet`과 달리 라온 전용 Flow/FrankWeapon 모션으로 덮어쓴다.
- 베기 기준: 구매 팩 `Hovl Studio/Sword slash VFX/Prefabs/Sword Slash 11.prefab`. 찌르기 기준: 같은 팩 `Prick 3.prefab`. 구매 원본은 수정하지 않는다.
- 반영한 누락: Light.06 첫 발차기, Jump.00~06, Dash.00~01, Entry, EntryAirborne, SwapEvade, SwapSpecial의 4타 및 라온 고유 공격 7종.
- 기존 스킬의 BeginParticleEvent는 연결된 FX를 조사해 중복 추가를 피한다. 판정·캔슬·피해 수치는 변경하지 않는다.
- Unity가 조사 중 Play로 전환되어 일시 대기한 뒤 편집 상태 복귀를 확인했다. 기존 `CombatMotionSampler`로 실제 PlayerModel_Raon과 현재 MotionSet을 120Hz로 측정하고 격리된 프리팹 화면에서 포즈와 FX를 확인했다. 사용자 Play를 강제 종료하지 않았다.
- 기존 Light.00의 수동 조정 FX를 기준으로 칼날 축·상대 칼끝 속도가 만드는 베기 평면을 맞췄다. 신규 베기는 scale 0.45, actor 기준 회전/위치, actor 이동 추종, Katana 스포너 명시. FX 시작은 각 기존 판정 구간 내 칼끝 최대 속도 직전이며 판정 자체는 변경하지 않았다.
- Light.06 첫 타격은 실제 `RightFoot` 판정이다. `Foot_R`에서 기존 `CombatHit_Enemy_LightAttack.prefab`을 짧게 재생하도록 연결했다. Jump.01은 발차기 포즈와 칼 휘두르기가 함께 있지만 실제 `MainWeapon` 판정과 큰 칼날 궤적이 있어 베기로 처리했다.
- 조사한 신규 누락 칼날 타격은 모두 베기였다. 기존 찌르기/차지의 `Prick 3`, 기존 스킬·궁극기의 `BeginParticleEvent`는 보존했다. 구매 프리팹·모델·AbilitySet·Payload·MotionKey는 변경하지 않았다.
- 저장 직후 Unity 로드 검사: 21개 에셋의 missing managed reference 0, null 이벤트 0.

## 최종 검사와 근거

- [저장된 에셋의 FX 미리보기](player-katana-fx/fx-preview.png): 왼쪽에서 오른쪽, 위에서 아래로 24개 타격. 순서는 [검사 원본](player-katana-fx/validation.json)의 `validation.details.index`와 같다. 발화 포즈와 파티클 0.08초 수동 평가 화면이며 실제 Game View·전투 입력 검증은 아니다.
- 현재 모델 120Hz 포즈 샘플과 저장된 새 FX 24개를 대조했다. FX 시작이 해당 판정 구간 안에 있고 종료가 모션 범위 안에 있음을 확인했다. 베기 23개 × 액터 회전 4방향 및 발차기 소켓 1개, 총 93회 포즈/참조 검사 오류 0.
- 기존 MotionSet 구조 검사: 오류 1, 경고 21. 독립 메모리 사본에서 새 FX만 제거한 이전 상태도 **동일 오류 1·경고 21**이다. 신규 오류/경고 0. 기존 오류는 `Bokuse_Katana_JumpAttack_2`의 PlaySoundEvent 범위 0.122~0.622가 모션 길이를 넘는 문제이며 요청 범위 밖이므로 보존했다. 경고는 21개 모션의 기존 안정 ID 누락이다.
- Console Error 0건 확인. C#을 변경하지 않아 재컴파일·코드 회귀 테스트는 수행하지 않았다. Player·Addressables 빌드는 수행하지 않았다.
- 대상 21개 asset의 diff는 새 이벤트와 managed reference 추가뿐이며 기존 판정·클립·이벤트 삭제/수정은 0. meta 및 구매 원본 변경 없음. Unity YAML의 빈 문자열 뒤 공백은 직렬화 형식을 보존했다.
- 중간 120Hz 원본·실험 이미지·임시 이벤트 JSON은 `output/katana-fx/`에서 정리한다. 최종 화면·검사 원본은 위 상태 폴더에 보존한다. 사용자 기존 변경·씬·프리팹은 정리 대상이 아니다.

## 남은 확인

실제 Play 입력·공격 취소·반대 방향 공격과 화면 가독성은 사용자 테스트 대기다. 기존 공중 2타 사운드 범위 오류와 안정 ID 경고는 별도 저작 정리 대상이다.

## 사용자 테스트

CombatTestMap에서 라온/Katana를 사용한다. 기존 저장 데이터를 변경할 필요는 없다.

| ID | 조작 | 기대 결과와 실패 기준 |
|---|---|---|
| KFX01 | 지상 약공격/강공격을 끝까지 연결 | 타격마다 검기가 칼날 이동 방향과 맞는다. 반대 방향 검기·누락·과도한 중복은 실패 |
| KFX02 | 공중 연타와 공중 돌진, 지상 돌진 | 칼날 높이에 베기/찌르기 FX가 표시되고 이동을 따라간다. 허공 잔류·역방향은 실패 |
| KFX03 | 교대 진입/회피/특수 공격과 라온 고유 공격 | 타격별 FX가 보이고 기존 판정·연계가 유지된다 |
| KFX04 | 캐릭터를 반대 방향으로 돌려 같은 공격, 공격 도중 피격/회피 | FX가 캐릭터 방향을 따르고 끝난 후 잔류·Console 오류가 없다 |

회신: `ID / 통과·실패·미수행 / 실제 증상 / 가능하면 영상과 Console 오류`. 화면과 전투 감각은 사용자 테스트 대기다.
