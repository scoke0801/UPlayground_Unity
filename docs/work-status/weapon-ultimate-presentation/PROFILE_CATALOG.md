# 무기 프로필과 미보유 궁극기

원본: `Assets/10.Datas/Combat/WeaponPresentation/WeaponPresentation_<무기>.asset`. 5비트는 모션 길이 비율0 /0.08 /0.24 /아래 피니시 /0.95다. 예고에는 얼굴, 피니시에는 기존 CameraShake 이벤트를 복제한다. 원래 MotionSet·Ability·판정·콜백·TimeScale·Stage는 생성하지 않으며 변경하지 않는다. 진입·전개·복귀 비트의 모션/VFX는 확정한 데이터가 없으므로 빈 템플릿으로 남겼다. 카메라는 P4의 Snapshot 프로필로 별도 저작한다.

| 무기 | 기본 Tier(추천) | 피니시 비율 | 흔들림 Pitch(도) | 길이(초) | 프리즈 제안(초,실행안함) |
|---|---|---|---|---|---|
| Sword | T1 | .72 | .8 | .12 | .045 |
| GreatSword | T2 | .78 | 1.5 | .22 | .09 |
| SwordShield | T1 | .72 | 1 | .16 | .06 |
| Katana | T2 | .80 | .45 | .08 | .025 |
| DualBlade | T2 | .74 | .65 | .10 | .035 |
| DoubleAxe | T2 | .78 | 1.6 | .24 | .10 |
| Spear | T1 | .70 | .5 | .09 | .03 |
| Bow | T2 | .76 | .6 | .10 | .035 |
| Staff | T2 | .78 | 1.1 | .18 | .07 |
| Whip | T2 비접촉 | .76 | .8 | .13 | .045 |

Yaw는 Pitch의0.6배, Roll0, Wave 감쇠다. Katana·Spear < Sword < GreatSword·DoubleAxe 순서가 강도·흔들림 길이·프리즈 제안 모두 일관된다. 실제 프리즈는 추가하지 않으므로 제안값으로 체감 결과를 주장하지 않는다. 무게감은 사용자 테스트 대기다.

## 실제 보유 에셋 연결

- Raon → Katana. 기존 TimeScale 이벤트 보존 + 얼굴/흔들림2종 생성.
- Hwarin → DoubleAxe. 기존 Callback/CameraEffect 이벤트 보존 + 얼굴/흔들림2종 생성.
- Tier 추천은 기존 CinematicStage 설정을 자동 변경하지 않는다. 카메라 추천도 생성기에서 자동 적용하지 않는다.

## 필요한 궁극기 목록

실제 정의는 요청서의12개 가정과 달리 **13개**다. 현재 Sequence는2개뿐이며 나머지11개는 새로 만들지 않았다. 캐릭터 정의 파일명을 식별자로 기록한다.

| 정의 파일명 뒷부분 | 실제 무기 | 필요 항목 |
|---|---|---|
| Arin | Katana | 궁극기 Ability/MotionKey·Sequence·얼굴 |
| H09 | Staff | 동일, 정식 캐릭터 외형 확인도 필요 |
| Lian | Whip | 동일, 접촉/T3 결정 별도 |
| Lili | GreatSword | 궁극기 Ability/MotionKey·Sequence·얼굴 |
| Myomyo | Staff | 동일 |
| MyoRyeong | SwordShield | 동일 |
| Reine | Spear | 동일 |
| SeolA | Bow | 동일, 실제 화살 추적 카메라는 별도 미구현 |
| Sera | GreatSword | 동일 |
| YeonHoa | Staff | 동일 |
| Yura | DualBlade | 동일 |

얼굴11종은 P0 스타일 승인 전 생성하지 않는다. 없는 Ability/MotionSet을 추정해 연결하거나 기존 공격 판정/수치를 새로 만들지 않는다.
