# 실제 몬스터 판단 데이터

골렘 일반형과 엔트 일반형의 **실제 정적 판단 의존성**을 Unity Editor에서 읽어 내보낸 패키지다. 기존 합성 플레이어 문제와 달리 몬스터의 유효 Definition/Profile, BehaviorSO, Intent 가중치, 생성된 BT 노드, AbilitySet 상속 결과, 공격 Payload와 Motion 매핑이 연결되어 있다. 원본 게임 에셋·씬·컴포넌트를 저장하거나 전투를 실행하지 않았다.

## 무엇부터 읽는가

1. `actors.json`: 두 몬스터의 역할·행동 설정과 AI 선택 공격 요약. 골렘 4개, 엔트 5개다.
2. `unity-export.json`: GUID와 local file ID로 연결된 486개 실제 데이터 객체. `objects[참조.id].properties`에서 세부 조건·계산값·모션 이벤트를 찾는다.
3. `reference/Assets/10.Datas/AI/BehaviorTree/SourceJson/`: 사람이 저작한 두 Rules JSON.
4. `reference/Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs`: 전체 판단 순서와 보정·상위 후보 선택.
5. `reference/Assets/02.Scripts/GameActor/AI/CombatDecision/`: 조건 해석과 점수 계산 코어.
6. `observation-contract.json`: 실제 C# Context 필드 31개와 추가 런타임 입력 계약.

`properties`는 Unity의 직렬화 경로다. `list.Array.data[0]`는 배열 원소, `.$count`는 길이, `.$type`는 managed reference의 실제 타입을 뜻한다. enum에는 정수와 이름을 함께 넣었다. 같은 에셋 안의 여러 Payload·BT 노드는 local file ID가 달라 별도 객체로 보존된다.

## 포함된 것과 제외한 것

- 유효 Actor 설정, 실제 BehaviorSO·가중치·레벨 성향·스탯·Poise/Break·정책, 생성 BT 노드 그래프와 원본 Rules를 포함한다.
- 유효 AbilitySet은 Unity의 `EnumerateAll()`로 상속·교체·제거 결과를 해석했다. 공격 외 태그 트리거·리액션 Ability도 원시 그래프에 있다.
- AI 공격은 MotionKey를 실제 프리팹이 참조하는 ActorAnimationMotionSet과 fallback에서 해석했다. 선택 가능한 9개 공격 모두 대응 Motion이 하나씩 확인됐다. 골렘과 엔트가 같은 Motion 매핑 SO를 공유하는 현재 구조도 그대로 기록했다.
- 실제 판정·전조·캔슬·워프 이벤트와 HitPhase·비용·쿨다운·조건을 원시 데이터에 포함했다. 요약의 이벤트 시간은 해당 직렬화 타임라인의 값이며, 클립 재생 속도·섹션 체이닝을 무시한 실시간 공격 길이로 해석하지 않는다.
- 클립은 이름·경로·길이·프레임 정보만 제공한다. 원본 애니메이션·메시·텍스처·음원·VFX 프리팹·네이티브 바이너리·씬은 보내지 않는다. `metadata_only`에 제외 범위를 명시했다.
- 판단 관련 C#을 읽기용으로 복사했다. 이 소스는 Unity·KCC·Animancer 의존성을 제거한 독립 실행 프로그램이 아니다.

## 현재 AI의 판단 순서

거리·행동 상태·전술 기억·그룹 기억·Blackboard를 Context로 구성하고 레벨 성향을 반영한다. 가중치 SO를 `페이즈 전략 → 페이즈 가중치 → 기본 전략 → 기본 가중치` 순서로 선택한다. 점수에 예측·페이즈·역할·그룹 보정과 직전 Intent 반복 페널티를 적용한 뒤 0~1로 제한한다.

**가장 높은 점수 하나를 확정 선택하는 구조가 아니다.** 상위 3개에서 `max(0.01, score)` 비례로 난수 선택한다. 선택된 Intent가 BT 조건·행동 요청·Ability 선택을 거쳐야 실제 동작이 시작된다. 정답 하나로 모델과 일치율을 계산하거나 Intent 선택 성공을 공격 실행 성공으로 취급하지 않는다.

`native-score-fixtures.json`에는 명시적 합성 Context 12개를 **현재 Unity의 IntentScoreComputer**로 계산한 원시·clamp 점수와 상위 후보를 넣었다. 이 사례들은 Melee 역할, 페이즈 없음, 예측 신뢰도 0, 중립 그룹 보정, 이전 Intent 없음 조건이다. 따라서 해당 범위에서는 추가 보정이 중립이다. BT 실행·난수 선택·실제 전투는 수행하지 않았다. `boundary_tie`가 참이면 3위 경계 순서를 유일한 정답으로 취급하지 않는다.

## 런타임 관측은 정적 에셋으로 대체하지 않는다

다음은 상황마다 달라지므로 캡처한 값이 필요하다.

- 현재 레벨·HP·Poise·활성 페이즈·자신/대상 태그와 행동 상태.
- 타깃 거리·현재 Blackboard 및 동적 `Cooldown.*` 값·그룹 슬롯·대기 시간.
- 플레이어의 현재 행동, 최근 행동 빈도, 피격 기억, 행동 예측과 신뢰도.
- 각 AI 공격 Variant의 실제 GAS 활성화 결과·선택 자격·거절 사유·남은 쿨다운.
- 그룹 Intent 보정, 직전 Intent·반복 횟수, 호출자가 만든 의도 후보.

전체 BT를 정확하게 재생하려면 Blackboard 외에도 노드가 읽는 감지·경로·상태 서비스 결과와 시간·난수 상태가 필요하다. 현재 패키지는 정적 데이터와 판단 입력 계약을 제공하며, 실측 스냅샷·완전한 Unity BT 실행기를 생성했다고 주장하지 않는다. 기존 EncounterReplay도 모든 Context 필드를 기록하지 않으므로 빈 값을 0으로 채워 변환하지 않는다.

`templates/*.json`은 실측 캡처 양식이다. null이 남아 있으면 요청 변환기가 거절한다. `examples/*.json` 12개는 실행 가능한 **합성 입력**이다. 행동 설정·거리 기준·레벨 배율은 실제 데이터에서 가져왔지만, 관측 상태·태그 없음·슬롯 있음·쿨다운 준비·시야와 외부 조건은 가정이다. 공격 자격도 명목 거리/레벨 기준으로 구성했다. 런타임 CanActivate를 실측한 결과가 아니다. 이 가정을 실제 전투 성공으로 보고하지 않는다.

## Mac에서 사용

폴더는 `Tools/LayaWorker`다. 실제 원본 `Assets` 폴더 없이 사용할 수 있다.

```bash
python enemy_decision_request.py --verify
python -m unittest discover -s . -p 'test_*.py' -v
python enemy_decision_request.py \
  --snapshot enemy-data/examples/Golem_Normal-near.json \
  --output results/golem-request.jsonl
python laya_worker.py infer --offline \
  --config enemy-data/worker-config.json \
  --input enemy-data/examples/requests.jsonl \
  --output results/enemy-intents-01.jsonl
```

모델에는 몬스터 관측 Context·현재 공격 자격·후보만 전달한다. 486개 객체 전체를 짧은 모델 문맥에 억지로 넣지 않는다. 일반 AI 작업 에이전트는 이 README·실제 소스·가중치·BT를 파일로 읽어 상세 검토할 수 있다. Laya 호출은 전체 소스를 실행하거나 BT를 재현하는 것이 아니다. 토큰 손실이 나면 기존 처리자가 거절하며 예산 검사를 제거하지 않는다.

입력이 없거나 일부 공격 상태가 빠지면 추측하지 않고 실패한다. 선택한 Intent는 최종 BT/Ability 실행 성공 보장이 아니며 실행 직전에 다시 확인해야 한다.

## 현재 PC에서 갱신

1. Unity가 Edit Mode이고 컴파일 중이 아닐 때 `export_enemy_decision.csx`의 본문을 MCP `execute_code`로 실행한다. 상단 메뉴나 런처에 등록하지 않는다.
2. `python package_enemy_data.py`로 복사본과 양식을 만든다. 이전 native score fixture의 Context가 바뀌면 오래된 파일을 별도 보관한 뒤 재생성한다.
3. 같은 Edit Mode에서 `export_enemy_scores.csx`를 실행한다. Play Mode나 난수 선택 없이 합성 Context의 점수 코어만 읽는다.
4. `python package_enemy_data.py`를 다시 실행해 네이티브 점수 후보를 예제에 연결하고 manifest를 갱신한다.
5. `python enemy_decision_request.py --verify`와 관련 검사를 실행한다.

두 `.csx`는 스크립트 파일을 Assets에 만들지 않고 메모리에서 컴파일하는 도구용 본문이다. 데이터와 판단 로직을 수정하거나 재직렬화하지 않는다. 원본 에셋은 해시를 보존한 바이트 복사이며 Git 줄바꿈 변환을 끈다.
