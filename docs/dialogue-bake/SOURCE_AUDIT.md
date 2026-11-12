# 프로젝트 연결 조사

2026-11-11 파일 조사. Unity 6000.3.21f1, Addressables 2.9.1, AI Navigation 2.0.14가 프로젝트 설정에 선언되어 있다. 실행 검증 결과는 아니다.

| 설계 책임 | 기존 연결 지점 | 적용 판단 |
|---|---|---|
| 시간 | `Assets/02.Scripts/Manager/GameTimeManager.cs` | 별도 Clock 매니저를 만들기 전에 기존 시간 계약 재사용 |
| 월드 상태·저장 | `Manager/Save/WorldStateManager.cs`, `SaveManager.cs`, `SaveManager.LivingWorld.cs` | 기존 저장·마이그레이션 경로 보존. 원본 슬롯 자동 변경 금지 |
| NPC 판단 | `GameActor/Component/NPC/NpcBrain.cs` | 기존 판단·틱과 통합. 새 BT/Brain을 병렬 소유자로 만들지 않음 |
| 생활·행동 | `GameActor/Component/NPC/LivingWorld/NpcRoutine.cs`, `Data/Actor/NPC/NpcLifeProfileSO.cs` | 기존 일과와 프로필 우선. 설계의 스케줄/Goal 모델과 매핑 후 확장 |
| 시뮬레이션 | `Manager/Actor/ActorSimulationManager.cs` | 기존 LOD/갱신 정책 조사 후 통합. 독립 LOD 서비스 중복 도입 금지 |
| 이동 | `GameActor/MovementController/NpcMovementController.cs` | 실제 KCC 기반 이동 소유권과 연결. 설계 NavMesh 예시를 그대로 복제하지 않음 |
| 짧은 일상 대사 | `Data/Actor/NPC/NpcAmbientSpeechSO.cs` | Voice/Binding/Rule/Line이 이미 존재. Line ID·활동·시간 조건, Flag/Quest Rule, 보류 정책을 보존 |
| 구조 대화 | `Data/Dialogue/DialogueGraphSO.cs` | graphId/nodeId와 기존 노드·중단·채널 검증 보존 |
| 퀘스트 | `Data/Quest/QuestSO.cs` | questId·목표·완료·활성 콘텐츠 경계 유지. 상태 enum과 서비스에서 허용 값 추출 |

표에서 축약한 경로는 `Assets/02.Scripts/` 기준이다. 실제 파일의 현재 해시는 `Tools/NpcWorldHarness/sources.json`으로 discover하여 얻는다.

## 아직 확정하지 않은 연결

- NPC별 사실 지식·소문·비밀 공개 권한의 정규 데이터와 실제 인물 이미지/Voice Profile 매핑.
- 월드 Fact/Command/Role/SmartObject의 프로젝트 전체 계약과 저장 단일 소스. 클래스명 검색 부재만으로 기능 미구현이라고 단정하지 않는다.
- 퀘스트/사건/지역 에셋의 canonical 권위 목록과 정규화 필드 매핑. 코드 파일은 스키마 근거이며 세계관 사실의 증거가 아니다.
- AST 조건 → 기존 Ambient Rule/DialogueNode 변환에서 표현 불가능한 조건 처리, 승인 카탈로그 import, 런타임 우선권/중단 정책.

따라서 discover는 정상 실행되어도 `BLOCKED_MISSING_SOURCE`를 반환한다. 정식 NPC 대사를 임의 생성하지 않는다. 이 상태는 하네스 실패가 아니라 실제 콘텐츠 Bake의 선행 자료 미확정 상태다.
