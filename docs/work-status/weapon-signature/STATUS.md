# 무기 시그니처 구현 상태

- 설계: [무기 9종 시그니처](../../../Assets/docs/design/combat/player/WEAPON_SIGNATURE_SKILL_CLIP_DESIGN.md)
- 위키: [전투와 실행 데이터](../../wiki/combat.md)
- 시작 2026-11-14, 재개 2026-11-15. 질문 없이 Astra 자문을 반영한다. 기존 미커밋 코드·데이터·구매/MCP 원본·meta를 보존하며 커밋·Player/Addressables 빌드는 하지 않는다.

## 현재 단계

| 단계 | 판정 | 근거·남은 항목 |
|---|---|---|
| S1 계약 | 반영 | R1/R3/R4/R8, 일반 공격형9종. [기존 매핑](BASELINE_MOTIONS.txt)·[시작 해시](BASELINE_HASHES.json) |
| S2 모션 | 적용·정량 확인 | 신규FBX5종·라온 무기본 이식판. [임포트](CLIP_IMPORT.txt), [추출 루트 보정](ROOT_RECEIPT.txt), [창 접지](SPEAR_HEIGHT_RECEIPT.txt), [12명120Hz](sampling/SUMMARY.txt). 실제 그립·KCC 합성·연속동작 인수 대기 |
| S3 런타임·GAS | 9종 연결 반영 | [3종 제작](BUILD_A.txt)·[6종 제작](BUILD_REMAINING.txt)·[9종 적용](APPLY_RECEIPT.txt). 11MotionSet, Spear 파생, 원본 시퀀스·공유 Default 보존 |
| S4 연출 | 적용·정지 표시 확인 | FX24·투사체4·카메라·[12명 컷인](CUTIN_CONNECT.txt). [지원FX](SUPPORT_FX_RECEIPT.txt), [PNG·GUID](CUTIN_IMPORT.txt), [소품 크기](PROP_BOUNDS.txt). Astra 추가 감사 누락 보강 완료: [연출](FX_POLISH_RECEIPT.txt)·[복귀/착탄 예고](RETURN_FX_RECEIPT.txt) |
| S5 검증 | 일부 통과·일부 실패 | [최종 EditMode29/29](EDITMODE_POST_FX.json), [최종 PlayMode12/13](PLAYMODE_POST_FX.json). 보스 리안 별도 검사1건 실패. [사용자 테스트](USER_TESTS.md)는 대기 |

## Astra 결정과 실제 측정

- Katana는 일반형으로 실행하고 라온·화린 원본 Sequence/Payload는 삭제하지 않는다. 컷인 전체 시간은 in+hold+out, faceDelay는 in 내부 지연이다. 기존 유라 PNG를 포함해 12명 모두 연결했다.
- Spear 원래 공유 Default Ultimate의 HitPhase가 0개라 피해 합 보존을 주장하지 않는다. 원본 SourcePayload_Spear를 보존하고 GreatSword 원본 단일타의 damage141 및 poise·break만 초기 기준으로 삼았다. 밸런스는 사용자 인수 대기다.
- 최초 유효 [보정 전 측정](sampling-before-root/SUMMARY.txt)에서 Katana 옆축 이동, DoubleAxe21.74m·DualBlade5.27m 전진, Spear 몸 상승 잔류를 발견했다. 구매/이식 원본과 공용 이동 코드는 보존하고 native 파생 클립을 연결했다.
- Katana 두 모델은 최종 `(-0.0930581,0,7.99945927)`에 준하는 전방 약8m다. Astra가 약9.3cm 횡편차를 허용오차로 채택했다. 수학적 X=0이라고 보고하지 않는다. DoubleAxe·DualBlade 추출 평면 이동·yaw는0이다.
- Spear는 원본 RootT.y만 매시각 낮은 발0.08m로 보정했다. 241샘플 최대 발 오차1.94e-7m, 골반 기준 손·발 상대벡터 오차1.79e-7m. 근육·RootQ·손/무릎 자세는 보존한다. Launch9m/s·공격 중력0.3의 실제 KCC 합성 높이와 자연스러움은 사용자 테스트 대기다.
- [실제 모델6명 정지 프리뷰](preview)는 직접 열어 확인했다. 보정 후 소품3종도 직접 열었다. 대검 AABB3×3×9m, 방패 최종 지름9m이며 미술 인수와 별개다.

## 실행 검사와 범위 밖 발견

- EditMode29/29는 9종 실제 바인딩·12명 컷인·피해 합·반복 횟수·원본 보존·슬롯/중력/입력/얼굴 소유권·원거리 락온을 검사한다. [SDK Editor](DOTNET_BUILD.log)·[PlayMode](DOTNET_PLAYMODE_BUILD.log)·[Combat](DOTNET_COMBAT_BUILD.log) 보조 컴파일 오류0. FX 변경 뒤 최종 정상 Unity 컴파일·리로드와 Console 컴파일 오류0도 확인했다.
- PlayMode13개 중12개 통과. 새 시그니처 투사체 수명6개·착탄 카메라1개 및 얼굴/일반 GAS 회귀가 포함된다. `Boss_Lian_간격압박_공격트리거는_상태와_Ability를_시작한다`는 StateTransitionRejected로 실패했고 [단독 재실행](BOSS_LIAN_RETEST.json)도 같은 위치에서 실패했다. 이 검사는 MonsterBossLian 및 MonsterActor_Lian_Whip 경로를 사용한다. 시그니처 테스트 영향으로 단정하거나 범위 밖 보스 코드를 임의 수정하지 않는다.
- 전수 Ability 검사 Error7/Warning601/Info718: [전문](ABILITY_VALIDATION.txt). Error는 기존 공유 Default2개 무타격 페이즈와 CharacterPassiveDatabase 중복·누락5개이며 [원본 시작 해시](PRESERVATION.txt)가 일치한다. 새 시그니처 경로 Error는 없다. 방패 보호 GE의 HUD fallback 아이콘 경고는 남아 있다.

## FX 추가 감사와 남은 실행

Astra가 기본 실행과 FX 충실도를 구분하여 다음 제작 누락을 지적했고 모두 전용 자산으로 보강했다. Katana 잔상은 기존 AfterimageEvent로 이미 연결돼 있어 누락이 아니다.

- GreatSword: 전용 발광/디졸브 재질, 모션 시간 출현·소멸, 분해 파편.
- Spear/SwordShield: 비행 트레일, 착탄 균열·파편·먼지 보강, 복귀 섬광. Shield holy 문양과 실제 착탄 지점 예고.
- DoubleAxe: 양손 궤적·바닥 먼지·F87 마무리 충격.
- DualBlade: 바깥으로 퍼지는 칼날 메시 파편.
- Whip: 두 휩쓸기 외곽·끝 트레일·전방 마무리 충격.

TimedPropEvent에 선택적 MPB 디졸브 곡선, ProjectileDefinitionSO/Runtime에 선택적 착탄 예고와 회수 정리를 구현했다. ReturnFxAuthoring은 창 F50/방패 F55 손 섬광 및 방패 실제 착탄 예고를 저작한다. FxPolishAuthoring은 나머지 전용 FX를 저작한다. 두 보강 저작기를 Unity에서 적용했고 실제 자산·디졸브·외향 파편·착탄 및 외곽 링 캡처를 직접 열어 확인했다. 파편 UV 누락도 캡처에서 발견해 보완했다. [최종 데이터 검사](FINAL_DATA_VALIDATION.txt)는 9종 준비 검사와 투사체4종 오류0이다. 정지 캡처를 실제 전투의 미술 인수로 승격하지 않는다.

다른 MCP 작업과 Editor를 공유하며 Play·컴파일·테스트·응답 지연 때 해당 작업을 중단하지 않고 대기했다. 편집 모드 복귀 후 보강 적용과 허용된 자동 검사를 마쳤다. 구현 범위는 인계 가능하다. 보스 리안 공격 진입 실패1건은 범위 밖 발견으로 보고하며, 실제 그립·KCC 도약·멀미/반복 피로·카메라/FX 미술 인수는 USER_TESTS.md의 사용자 테스트 대기다.

## 보존·정리

ArtSource/Animation/Signature의 FBX·blend·제작 대장과 [아트 출처](FX_SOURCES.txt), 애니메이션·프리팹·meta·측정 CSV·검증 영수증은 산출물/다음 단계 입력으로 보존한다. 주요 아트 경로120파일 약79.2MB(보강 후). 초기 무효 정지CSV는 유효 실측으로 대체했다. 실패 BUILD_B/BUILD_C/SPEAR_BUILD_RECEIPT는 후속 성공 영수증으로 대체해 제거했다. SDK sources.targets는 Editor 프로젝트 갱신 전 보조 컴파일 입력으로 보존한다. 하네스: output/harness/20261114-222231-8a9c915f/.
## 최종 완료 대조

| 요구 | 실제 근거·판정 |
|---|---|
| 기존 궁극기 슬롯9종·공유 무기12명·Spear 분리 | 최종 EditMode의 실제 모델/정의/AbilitySet 바인딩 검사 통과 |
| R1 입력 이동·R3 숨김 복구·R4 목표 폴백·R8 컷인 | 관련 소유권·사망·스왑·무타깃·원거리 락온·얼굴 실시간 검사 통과 |
| 모션5종·라온 무기 트랙·Whip 몸/무기쌍 | 임포트 기록·독립 Avatar·11MotionSet 연결·12모델120Hz·Whip 길이 검사, 그립/자연스러움은 사용자 인수 대기 |
| 9종 피해·타격 창·캔슬·장판 상한 | 최종 데이터 검사·실제 반복 합산 테스트·최대6/12틱 및 비행접촉 중복 방지 PlayMode 통과 |
| FX·카메라·컷인 | 전용24프리팹·12명Sprite·SO/예고/섬광 연결, MPB디졸브/토네이도파편 캡처, 착탄 효과 자기핸들/예고 회수 검사 통과 |
| 원본·GUID·공유 Default 보존 | 시작SHA·SourcePayload·GA GUID·12PNG 보존 검사/독립 QA 통과 |
| 보류·폐기 요구 | R2/R9는 보류 유지. R5/R6/R7/R10/R11은 채택하지 않음 |
| 검사·문서·정리 | 최종 Edit29/29·Play12/13 사실 기록, 보스리안 실패 별도 보고, 위키·설계·하네스·사용자 명세 갱신. 커밋/Player/Addressables 빌드 미실행 |

구현과 허용된 자동 검증은 종료한다. 보스 리안 실패는 전체 통과로 덮지 않으며 사용자 인수가 끝났다는 의미도 아니다.
