# A 현장 기준

2026-11-07 Unity 6000.3.21f1의 열린 LakeVillageWorld에서 읽기 전용 조사했다. 씬은 조사 시작 시 dirty=false, Play=false였다. 기존 작업은 `output/NpcLifeAI/worktree-before.txt`, 일과 직렬화는 `routines-before.txt`, Git 제외 씬 원본은 `LakeVillageWorld.before.unity`에 보존한다. 이전 LivingWorld 인계를 덮어쓰지 않는다.

## 현재 사실

- 통합 월드의 일과는 Joy/Hazel/Morgan/Yeonmok 4개다. 모두 placementReady=true이고 SerializedObject로 확인한 actor/작업점 연결은 정상이다. EditorJsonUtility가 씬 참조를 instanceID=0으로 출력해 최초 조사에서 누락으로 오판했으나 직접 참조 검사로 정정했다. 실제 NpcWorkPoint는 12개다. 준비 플래그만으로 접점 승인을 판정할 수 없다.
- 12개 작업점은 논리 mapId=LakeOfLifeScenario, shelter=false다. 통합 월드 파일명으로 ID를 바꾸지 않는다. 원본 시나리오 meta 삭제는 시작 전 사용자 변경이며 복구하지 않는다.
- 생성 주민은 전용 배치 데이터 8개 레코드다. 288개 외형은 활성 인구가 아니다. SceneEntity GUID는 원본/통합 표현에서 보존한다.
- NpcRoutine은 서비스 준비 코루틴→시간/플래그 구독→Idle.TryStart→NpcRoutineState/KCC→작업 체류→취소 경로다. 작업점의 소유자+세대는 늦은 해제를 차단한다.
- NpcReactState는 PauseRoutine으로 전투 우선권을 확보하고 Return 이후 Idle로 돌아간다. 대화/서사도 같은 중단 통지를 쓴다. 환경은 EnvironmentController→NpcRoutineEnvironmentBinding의 producer/map/revision 검증과 진입/해제 히스테리시스를 사용한다.
- 현재 일과는 전 구간 simulation lease를 가진다. 작업 완료는 체류 타이머이며 종료 MotionSet은 미연결이다. 경유점은 모두 비어 있다. 원거리 중단과 중간 합류를 개선해야 한다.

## 대표 대상과 실측 위치

|대상|현재 위치 XYZ|후보|준비 제한|
|---|---|---|---|
|NPC_Joy|1033,62.11,406|조리/배식, lake.meal.*|용기와 접점 미승인|
|Npc_Yeonmok|987,63.05,431|공구/수선, lake.repair.*|타격 작업물 미승인|
|NPC_Gen_S01_M_01|975,61.108685,414|GUID 9b15226e9d9f910a2feb8cb55a36e641, 방문 주민|실제 이동/체류 경로 검수 필요|
|NPC_Gen_S01_F_04|986,61.192944,417|GUID 3a977e6398fb451241196bfb96002711, 방문 주민|실제 이동/체류 경로 검수 필요|

작업자 선정은 기존 모션/손 도구가 있는 두 명으로 제한한다. 준비됐다고 표시하지 않는다. Hazel(1005.23,62.56,407.46), Morgan(1088,56.13,410)은 후속 확대 후보다. 조리 용기/수선 대상/끈/분류 대상/지붕이 없다는 E_MOTION_HANDOFF의 보류는 현재 승인 근거가 없으므로 유지한다. 교류는 일반 주민 두 명 생활권의 넓은 공간을 후보로 하되 통로·가림·충돌·접지 검사가 필요하다.

## B 연결 지점

Data/Actor/NPC에 프로필, Actor/Component/NPC/LivingWorld에 선택기와 바인딩을 추가한다. Routine.TryStart의 소유자를 한 곳으로 모으고 기존 프로필 없는 경로는 유지한다. 기존 씬 참조를 보존하고 신규 콘텐츠는 검수 전 준비 게이트를 닫는다. RuntimePlacementLoader의 정상 생성 이후 GUID 바인딩을 사용한다. 생성 몸을 추가로 만들지 않는다.

## 검증 기준선

NpcRoutineContractTests의 Unity EditMode 실제 16/16 통과(job 534b84a9ffda455ba7e4d81d3e104ab8). 짧은 이름으로 함께 요청한 Idle 테스트는 실행되지 않았으므로 별도 재실행한다. 이전 대기/제스처 수정의 Unity·화면 검수는 아직 대기다. CPU p50/p95·GC·16/32/64명 부하는 미측정이다. 기존 사용자 데이터 변경과 자동 재직렬화를 구분한다.
