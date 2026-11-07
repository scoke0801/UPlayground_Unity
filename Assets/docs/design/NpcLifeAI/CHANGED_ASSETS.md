# 변경 및 전달 목록

## 재개 반영

현재 에셋 해시는 `output/NpcLifeAI/changed-assets-resume.json`을 따른다. 아래 원본 diff 수치는 재개 전 기록이다. 현재 씬에는 다른 세션의 풀/환경 변경도 있어 전체 diff를 생활 AI 변경으로 간주하거나 통째로 원복하지 않는다.

- 씬: `NpcLifeAI_Review`의 WorkContacts에 기존 프리팹 조리대/용기·수선대/목재, Shelters에 지붕 4개/기둥 16개, Routes에 접근 경유점을 추가했다. 작업자 두 몸의 캡슐만 보정하고 조리/방문/교류 위치·종료 참조를 정합했다. 배식·별도 공구 준비 두 Step은 참조를 보존한 채 보류했다.
- 일반 주민 둘과 교류의 준비 게이트는 사용자 승인 후 true로 저장했다. 기존 작업자 둘도 true를 유지했다. 이전 자동 승인 차단은 사용자 명시 승인으로 해소했으며 정확한 세 필드와 저장 증거는 STATUS에 있다.
- 신규 코드/meta: `Assets/02.Scripts/Editor/NpcLifeAIAuthoring.Layout.cs`, `Assets/Tests/EditMode/Content/NpcLifePlacementBindingTests.cs`. 기존 신규 파일들은 안전 합류·공지 중복 방지·좌석 배정/모션 전체 길이·로더 충돌 복구를 보완했다.
- 프로필 SO 4개와 씬 meta의 이전 SHA256 일치를 확인했다. 기존 Prefab/MotionSet 원본·식별자·보상/저장 슬롯은 변경하지 않았다. 새 음향 에셋은 없으며 발소리/작업 접촉음은 남았다.
- 원본 `LakeVillageWorld.before.unity`(148,360,153바이트) 외에 재개 시점 `LakeVillageWorld.resume-before.unity`(148,382,901바이트)를 output에 보존한다. 사용자 배치가 들어 있으므로 삭제하거나 현재 씬에 덮어쓰지 않는다.
- 정적 경로/지붕 증거: `representative-layout-paths.txt`, 게이트 값: `final-gates.txt`, 수정 접점 화면: `cook-contact-refined.png`. Edit 화면의 정지 포즈는 실제 모션 접점 검수를 대신하지 않는다.

## 최초 반영 기록

## 실제 에셋

|경로|GUID|상태|
|---|---|---|
|Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity|1e4e737871acd4a468c5917796b2a958|Git 제외. 대표 바인딩·새 작업점·채집 사건 소스 추가|
|Assets/10.Datas/NpcLife/Profiles/WorkerCook.asset|55bc7f7a626defc42b7c3ae3f1d623b0|신규 프로필|
|Assets/10.Datas/NpcLife/Profiles/WorkerRepair.asset|205b310346bd5904f8a72a1a9317bf39|신규 프로필|
|Assets/10.Datas/NpcLife/Profiles/ResidentFarm.asset|a35b0d7abc5e927429a9c0be8c6fea98|신규 프로필|
|Assets/10.Datas/NpcLife/Profiles/ResidentMarket.asset|94ff7dd67084ef7418d488875d13ca99|신규 프로필|

각 파일의 .meta와 NpcLife.meta/Profiles.meta를 함께 전달한다. 전체 경로·크기·SHA256은 `output/NpcLifeAI/changed-assets.json`이다. 씬 원본 대비 diff는 725줄 추가/2줄 교체이며 교체는 `m_AddedComponents: []` 두 곳을 실제 추가 컴포넌트 목록으로 바꾼 것이다. 기존 배치 삭제·지형/건물 재생성은 없다. 생성 주민 배치 SO의 기존 GUID는 원본/통합 데이터 모두 그대로다.

원본 시나리오의 시작 전 삭제된 meta, 몬스터/GAS/BT/셰이더 등 사용자 변경을 유지한다. 기존 Prefab·MotionSet 원본은 수정하지 않았으며 씬에서 종료 모션 참조만 추가했다. 새 작업 대상물·지붕·접촉음 에셋을 생성했다고 표시하지 않는다.

## 코드

신규(각 meta 포함):

- Data/Actor/NPC/NpcLifeProfileSO.cs
- GameActor/Component/NPC/LivingWorld/NpcLifeController.cs, NpcSocialPoint.cs, NpcLifeStimulusSource.cs
- Gameplay/Interaction/LivingWorld/NpcLifePlacementBinding.cs
- Editor/NpcLifeAIAuthoring.cs
- Tests/EditMode/Combat/NpcLifeControllerTests.cs

기존 수정:

- NpcRoutine.cs: 선택기 위임·대체 예약·종료 MotionSet·공통 취소·시뮬레이션 중단·몸 해제.
- NpcIdleState.cs: 일과 소유 중 고정 작업 폴백 중복 방지. 시작 전 사용자 변경도 포함된 파일이므로 파일 전체 원복 금지.
- RuntimePlacementLoader.cs: 생성/정리 통지와 파괴 전 비활성화. 시작 전 근접 스폰 변경을 보존했으며 전체 diff가 이번 작업만인 것은 아니다.
- GatheringActor.cs: 실제 채집 타격 통지.

## 연결·복구

통합 월드의 `NpcLifeAI_Review`를 통해 생성 주민 GUID 두 개를 기존 로더에 연결한다. 작업자 LifeController는 기존 JoyRoutine/YeonmokRoutine에 있다. 신규 주민/교류는 준비 게이트 false다. 환경 바인딩은 기존 EnvironmentController를 소비한다. 실제 자극 소스는 기존 채집물 `Carrot_2_2_0`에 추가했다. 씬 외형 이름을 런타임 분기에 사용하지 않는다.

`NpcLifeAI.PrepareRepresentativeBindings`는 툴 런처에 등록하지 않은 일회성 저작 도구다. 이미 연결된 씬에서 다시 실행하면 실패하며 기존 데이터를 교체하지 않는다. 다른 씬 전체에 자동 적용하지 않는다. 프로필 생성 뒤 씬 저작이 실패한 경우 생성된 유효 SO는 남을 수 있으므로 이를 Undo 전체 transaction으로 간주하지 않는다.

Git으로 복구할 수 없는 원본 씬은 `output/NpcLifeAI/LakeVillageWorld.before.unity`(148,360,153바이트, 약 141.5MiB)에 보관한다. 미커밋 사용자 배치를 보호하기 위한 사본이므로 사용자 확인 없이 삭제하지 않는다. 전달 시 원본 사본으로 현재 씬을 덮어쓰지 말고 이번 루트/컴포넌트 추가를 대조한다. 새 문서 폴더와 모든 문서 .meta도 함께 전달한다. 커밋·스테이징·ignore 변경은 하지 않았다.
