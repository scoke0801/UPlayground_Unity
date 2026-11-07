# 신규 몬스터 데이터 Mac 확인

12/12건의 실제 Laya 오프라인 추론을 완료했다. 후보 밖 선택 0건, 토큰 잘림을 성공으로 반환한 사례 0건, 오류·미수행 0건이다. 전체 단위 검사 34개와 패키지 무결성 검사가 통과했다.

## 식별 정보

- 입력 manifest SHA256: `28aa073e62de8d7d2fefbe835dab5a55cd20754353bfb1342e541defab6e9045`
- 코드 커밋: `d13e4a97d90b45b7dc0177b3f355e771cf46ffd4`
- 모델 revision: `ba40c87fcb357f1643d04d71323af9cdc3b9e591`
- 원시 입력·응답은 requests.jsonl / predictions.jsonl에 보존했다. 이 파일의 출처는 request-provenance.json과 본 문서, 각 행의 모델 metadata에 기록했다.
- 기본 캐시 경로에서 첫 모델 로드는 실패했다. 기존 별도 HF 캐시를 지정한 재시도는 성공했다. 실패 원문은 inference-process.json, 재시도는 inference-retry.json에 보존했다. 새 다운로드·모델·프롬프트 변경 없음.

## 정적 연결과 점수 경계

골렘 4개·엔트 5개 공격의 Definition → Behavior → BT·공통 IW_Default_Melee → Ability/Payload → Motion 참조를 실제 내보낸 파일과 객체 그래프에서 확인했다. 총 486개 객체와 패키지 파일 해시가 유효하며 9개 공격 모두 Motion 하나로 연결된다. 두 actor의 role은 Melee이며 phase는 없다. 12개 Context와 candidate pool은 native fixture 및 변환 요청과 일치한다.

IntentScoreComputer.cs는 base + continuous + 조건 bonus를 합한 후 multiplier를 적용한다. EnemyCombatDecisionEvaluator.cs는 예측·페이즈·역할·그룹·반복 보정을 거쳐 clamp하고 상위 3개에서 max(0.01, score) 비례 난수 선택한다. 이번 native fixture는 예측 신뢰도 0, 중립 그룹, 직전 Intent 없음 범위의 원시·clamp 점수다. 일반 런타임 최종 점수와 동등하다고 확대하지 않는다. 3위 경계 동점은 유일한 순위 정답으로 판정하지 않았다.

선택 Intent는 최종 행동과 별개다. 두 SourceJson의 IntentAttack은 지연·거리·CanActivate 조건을 확인하며, IntentAttackApproach는 거리가 optimalCombatDistance보다 크면 Chase를 요청한다. ExecuteEnemyAttackNode는 거리·공격 가용성·슬롯·트리거 수락을 다시 확인한다. BT의 우선순위·다른 분기·시간·난수·외부 서비스는 실행하지 않았다.

## 신규 선택 결과

| 요청 | 선택 Intent | 추론 ms |
|---|---|---:|
| Golem_Normal-near | Attack | 185.829 |
| Golem_Normal-far | Attack | 124.324 |
| Golem_Normal-player-recovery | Attack | 125.206 |
| Golem_Normal-player-pressure | KeepDistance | 125.303 |
| Golem_Normal-waiting | KeepDistance | 125.386 |
| Golem_Normal-low-health | Retreat | 124.041 |
| Ent_Normal-near | Pressure | 156.108 |
| Ent_Normal-far | Attack | 134.712 |
| Ent_Normal-player-recovery | Attack | 142.726 |
| Ent_Normal-player-pressure | KeepDistance | 166.26 |
| Ent_Normal-waiting | KeepDistance | 194.377 |
| Ent_Normal-low-health | Pressure | 221.713 |

최고 점수 선택은 참고상 1/12이며 합격률이 아니다. 호출자가 이미 상위 후보를 제한하므로 후보 안 선택 역시 독립적인 전투 판단 정확도의 증거가 아니다.

## 판단 우려 조사

Golem_Normal-far는 Distance=6이고 현재 eligible 공격이 없는데 Attack을 선택했다(확률 0.5373). 후보 설명은 available attack에 대한 commitment이므로 현재 공격 자격과 의미상 긴장이 있다. Ent_Normal-far도 Attack을 선택했지만(0.4411), Basic/Heavy는 불가이고 Skill.1.05는 range 1~20에서 eligible=true다. 따라서 엔트에 공격 자격이 전혀 없다고 판단하면 안 된다. 다만 실제 두 BT에 Attack→Chase 접근 경로가 있어 즉시 제자리 공격·명백한 BT 모순으로 판정하지 않는다. 이 사례는 원시 Intent 선택의 의미와 실제 행동 해석을 분리해야 함을 보여준다.

waiting 두 건은 ActionDelayElapsed=false에서 KeepDistance를 골랐다. player-pressure 두 건도 KeepDistance, low-health는 골렘 Retreat·엔트 Pressure다. 공격 강제·없는 방어 모션 선택은 관찰되지 않았다. 단일 소표본이며 반복·순서·언어 안정성은 이번에 측정하지 않았다.

첫 추론 185.829ms, 이후 11건 중앙값 134.712ms, 전체 최대 221.713ms. inference_ms는 모델 로드와 분리된 처리자 계측이며 프로세스/로드 시간과 혼합하지 않았다. 사용 토큰은 각 응답 usage에 보존했다.

## 결론과 미검증

이번 새 데이터의 합성 요청 처리 계약은 확인했다. 주 판단기 채택이나 기존 확률 정책의 대체를 뒷받침할 평가는 아니다. 실제 런타임 관측이 없으므로 태그·슬롯·쿨다운·공격 자격 등의 가정을 유지한다. Unity 실행·전투 감각·BT 실행·실측 CanActivate·Windows 연결·새 학습은 미수행이며 이번 범위 밖이다. 기본 프로필과 원본 에셋, 기존 평가 결과는 변경하지 않았다.
