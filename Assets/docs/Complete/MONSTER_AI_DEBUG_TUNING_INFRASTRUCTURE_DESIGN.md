# 몬스터 AI 디버깅·튜닝 인프라 구현 완료 문서

> 작성일: 2026-05-23  
> 구현 완료일: 2026-05-23  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 관련 시스템: `BehaviorTreeRunner.DebugTrace`, `BehaviorTreeEditorWindow`, `EnemyCombatDecisionEvaluator`, `EvaluateEnemyCombatIntentService`

---

## 0. 완료 요약

몬스터 BT 기반 전투 Intent 디버깅을 위해 다음 인프라를 구현했다.

1. **Intent Score Timeline**
   - `EvaluateEnemyCombatIntentService`가 매 평가 결과를 런타임 링 버퍼에 기록한다.
   - BT Editor의 `Timeline` 탭에서 9개 Intent 점수 곡선을 확인할 수 있다.

2. **Encounter Replay Dump**
   - 적별 `EncounterReplayRecorder`가 선택적으로 Intent 평가 프레임과 입력 컨텍스트를 JSON으로 저장한다.
   - 기본값은 비활성이다.

3. **Replay JSON Load**
   - BT Editor 상단 `Load Replay` 버튼으로 저장된 JSON을 불러와 Timeline 패널에 표시한다.

4. **Resolver Failure Reason**
   - Intent 점수는 높지만 실제 State 전환이 실패하는 문제를 분리하기 위해 마지막 실패 사유를 Blackboard에 기록한다.

5. **BT Editor 프레임 드랍 완화**
   - Debug 갱신 주기를 낮추고, Blackboard/Minimap/Timeline repaint를 열린 탭과 토글 상태 기준으로 제한했다.

---

## 1. 구현 파일

### 1.1 런타임 디버깅 데이터

설계 문서의 `Assets/02.Scripts/AI/Debug/` 경로는 `.gitignore`의 `[Dd]ebug/` 규칙에 걸려 Git 추적 대상에서 제외된다. 따라서 실제 구현 파일은 `Assets/02.Scripts/AI/Debugging/`에 배치했다.

| 위치 | 역할 |
|------|------|
| `Assets/02.Scripts/AI/Debugging/IntentScoreTimeline.cs` | Intent 점수 링 버퍼 컴포넌트 |
| `Assets/02.Scripts/AI/Debugging/IntentScoreSnapshot.cs` | Timeline 단일 샘플 구조체 |
| `Assets/02.Scripts/AI/Debugging/EncounterReplay.cs` | Replay 직렬화 데이터 |
| `Assets/02.Scripts/AI/Debugging/EncounterReplayRecorder.cs` | 적별 Replay 기록기 |

### 1.2 BT Editor

| 위치 | 역할 |
|------|------|
| `Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineView.cs` | Timeline 탭 View |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/IntentScoreTimelineRenderer.cs` | IMGUI 점수 곡선 렌더링 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/EncounterReplayLoader.cs` | Replay JSON 파일 로더 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.Layout.cs` | `Timeline` 탭 및 `Load Replay` 버튼 통합 |
| `Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeEditorWindow.Debug.cs` | 디버그 repaint 빈도/범위 최적화 |

### 1.3 연결 코드

| 위치 | 변경 |
|------|------|
| `EvaluateEnemyCombatIntentService.cs` | Intent 평가 후 Timeline/Replay 기록 |
| `EnemyAIController.cs` | `IntentScoreTimeline`, `EncounterReplayRecorder` 자동 부착 |
| `EnemyDetection.cs` | `OnTargetLost` 이벤트 추가 |
| `MonsterActor.cs` | 사망 시 Replay 저장 종료 |
| `RequestEnemyActionNode.cs` | `Decision.ResolverFailureReason` 기록 |
| `EnemyBlackboardKeys.cs` | `ResolverFailureReason` 수동 키 추가 |

---

## 2. 동작 방식

### 2.1 Intent Score Timeline

`EnemyAIController.Awake`에서 `IntentScoreTimeline`이 없으면 자동으로 추가한다. 이후 `EvaluateEnemyCombatIntentService.OnServiceTick`에서 `EnemyCombatDecisionEvaluator.TryEvaluate` 성공 시 평가 결과를 기록한다.

기록 필드:

- `SelectedIntent`
- `LastIntent`
- `ConsecutiveIntentCount`
- `Attack/Punish/Counter/Pressure/Chase/Retreat/KeepDistance/Defend/Recover` 점수
- `RhythmPhase`
- `Reason`

BT Editor의 `Timeline` 탭은 현재 지정된 `Debug Runner`의 `IntentScoreTimeline`을 읽어 점수 곡선과 선택 Intent 색띠를 표시한다.

### 2.2 Encounter Replay Dump

`EnemyAIController.Awake`에서 `EncounterReplayRecorder`가 없으면 자동으로 추가한다.

기본값:

```csharp
[SerializeField] private bool _enableReplayRecording;
```

기록 시작/종료:

- 시작: `EnemyDetection.AcquireTarget`로 타겟을 새로 획득했을 때
- 종료: `EnemyDetection.OnTargetLost` 후 지연 저장 또는 `MonsterActor.OnDeath`
- 저장 위치: `Application.persistentDataPath/EncounterReplays/{timestamp}_{actorId}.json`

Replay에는 Intent 점수 외에 거리, HP%, 예측 액션, 예측 신뢰도, 공격 슬롯 여부, Resolver 실패 사유가 포함된다.

### 2.3 Replay Load

BT Editor Debug Toolbar의 `Load Replay` 버튼으로 JSON을 선택하면 `IntentScoreTimelineView`가 Replay 프레임을 `IntentScoreSnapshot`으로 변환해 동일한 Timeline 렌더러로 표시한다.

현재 구현은 “정적 Replay 표시”까지다. 시간 슬라이더, Play/Pause/Step 재생, 이벤트 마커 클릭 UI는 후속 선택사항으로 남긴다.

### 2.4 Resolver Failure Reason

`RequestEnemyActionNode`에서 전환 실패 시 `Decision.ResolverFailureReason`에 마지막 실패 사유를 쓴다.

기록 예:

- 쿨다운 미준비
- 필수 컴포넌트 또는 타겟 없음
- 현재 거리에서 사용 가능한 공격 없음
- 공격 슬롯 확보 실패
- 보호 액션/회피 액션/하드락 상태로 전환 차단

성공 시에는 빈 문자열로 초기화한다.

---

## 3. 성능 조치

BT Editor가 열린 상태에서 프레임이 60fps에서 10fps 수준으로 떨어지는 문제를 완화하기 위해 다음을 적용했다.

| 항목 | 변경 |
|------|------|
| Debug refresh interval | `0.05s` → `0.15s` |
| Blackboard repaint | `Variables` 탭이 열려 있을 때만 수행 |
| Minimap repaint | Minimap 토글이 켜져 있을 때만 수행 |
| Timeline repaint | `Timeline` 탭이 열려 있을 때만 수행 |

이 변경은 GraphView 전체 디버그 스타일 갱신 비용과 IMGUI Blackboard repaint 비용을 줄이는 목적이다.

---

## 4. 설계 대비 차이

| 설계 항목 | 구현 상태 | 비고 |
|----------|----------|------|
| `Assets/02.Scripts/AI/Debug/` 경로 | `AI/Debugging/`으로 변경 | `.gitignore` 충돌 회피 |
| Stacked Area Chart | 9개 Intent별 라인 차트로 구현 | 점수 추세 판독을 우선 |
| Blackboard 스냅샷 툴팁 | Intent 점수/Reason 중심 툴팁 구현 | 전체 Blackboard 덤프는 미구현 |
| `EncounterReplayManager` | 미구현 | 적별 `EncounterReplayRecorder` 방식으로 대체 |
| Replay 메뉴 `Replay → Load JSON…` | Toolbar `Load Replay` 버튼으로 구현 | 기존 UI 흐름에 맞춤 |
| Replay 시간 슬라이더/Play/Step | 미구현 | 정적 Timeline 표시까지 완료 |
| Replay 이벤트 마커 | 데이터 구조만 준비 | UI 표시는 후속 선택사항 |
| 빌드 오버헤드 0 | 부분 충족 | Replay는 토글 OFF 시 기록하지 않음. Timeline은 컴포넌트/링 버퍼가 존재하므로 완전 0은 아님 |

---

## 5. 검증 결과

### 5.1 빌드

검증 명령:

```powershell
dotnet build UPlayground.sln --no-restore
```

결과:

- 오류 0개
- 경고 23개

경고는 기존 Unity 패키지 참조 충돌 및 외부 에셋 경고이며, 본 구현으로 인한 컴파일 오류는 없다.

### 5.2 수동 검증 필요 항목

Unity Editor Play Mode에서 다음을 확인한다.

| ID | 확인 항목 |
|----|----------|
| 1 | BT Editor에서 `Debug Runner` 지정 후 `Timeline` 탭에 9개 Intent 점수 곡선 표시 |
| 2 | Timeline 위에 마우스 hover 시 해당 시점 점수/Reason 표시 |
| 3 | `EncounterReplayRecorder._enableReplayRecording = true`인 적이 교전 종료 시 JSON 저장 |
| 4 | `Load Replay`로 저장 JSON을 불러와 Timeline 표시 |
| 5 | BT Editor를 열어 둔 상태에서 기존 대비 프레임 드랍 완화 확인 |

---

## 6. 후속 선택사항

아래 항목은 현재 구현의 필수 동작에는 포함하지 않았다.

- Replay 시간 슬라이더, Play/Pause/Step 컨트롤
- ReplayEvent 타임라인 마커 및 클릭 상세 표시
- 두 Replay 비교 모드
- 전체 Blackboard 스냅샷 저장/툴팁 표시
- 전역 `EncounterReplayManager`로 여러 적 Replay를 한 파일로 묶는 방식
- Player Build에서 Timeline 컴포넌트까지 완전히 제거하는 컴파일 분기

---

## 7. 완료 판정

본 문서의 핵심 목표였던 **Intent 점수 시간축 가시화**, **전투 Intent Replay Dump**, **Replay JSON 로드**, **Resolver 실패 사유 기록**, **BT Editor 프레임 드랍 완화**는 구현 완료했다.

일부 UI 세부 기능은 후속 선택사항으로 분리했으며, 현재 구현은 플레이테스트 중 “왜 이 Intent가 선택됐는가”와 “시간축에서 점수가 어떻게 변했는가”를 확인하는 실사용 가능한 1차 버전이다.
