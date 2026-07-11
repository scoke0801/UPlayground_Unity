#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UPlayGround.Editor
{
    public class UPlaygroundToolsLauncher : EditorWindow
    {
        private const string FavoritesPrefsKey = "UPlayground.ToolsLauncher.Favorites";
        private const string RecentPrefsKey = "UPlayground.ToolsLauncher.Recent";
        private const char PrefsSeparator = '\u001F';
        private const int MaxRecentTools = 12;

        private enum ToolImpact
        {
            Normal,
            DataWrite,
            BulkWrite,
            Destructive
        }

        private readonly struct ToolEntry
        {
            public readonly string Name;
            public readonly string MenuPath;
            public readonly string Summary;
            public readonly string Detail;

            public ToolEntry(string name, string menuPath, string summary, string detail)
            {
                Name     = name;
                MenuPath = menuPath;
                Summary  = summary;
                Detail   = detail;
            }
        }

        private static readonly (string Category, ToolEntry[] Tools)[] s_categories =
        {
            ("생성 도구", new[]
            {
                Tool("ID Enum 생성기",               "UPlayGround/생성 도구/ID Enum 생성기", "프로젝트 ID enum을 데이터 에셋 기준으로 생성/갱신합니다.", "FX, UI, Actor, Quest 등 문자열 ID를 코드에서 안전하게 참조할 수 있도록 enum 파일을 다시 만듭니다. 데이터 추가 후 enum 누락이 의심될 때 사용합니다."),
                Tool("Enemy Blackboard Keys 생성",           "UPlayGround/생성 도구/Enemy Blackboard Keys 생성", "EnemyBlackboardKeys 생성 코드를 갱신합니다.", "Behavior Tree JSON/레지스트리 기준 blackboard key 식별자를 C# 상수로 생성합니다. AI 데이터 키를 추가하거나 이름을 변경한 뒤 사용합니다."),
                Tool("아이템 데이터 생성기",             "UPlayGround/생성 도구/아이템 데이터 생성기", "아이템 데이터 에셋과 ID 구성을 생성합니다.", "ItemSO, EquipmentSO, ItemDatabase 계열 데이터를 일괄 생성하거나 누락 항목을 채우는 생성기입니다. 아이템 테이블 확장 후 데이터 정합성을 맞출 때 사용합니다."),
                Tool("레시피 데이터 생성기",           "UPlayGround/생성 도구/레시피 데이터 생성기", "제작 레시피 데이터를 생성합니다.", "제작 시스템에서 사용할 Recipe 데이터와 관련 DB를 생성/갱신합니다. 신규 제작 항목을 대량으로 넣거나 테이블 기반 데이터를 반영할 때 사용합니다."),
                Tool("스탯 데이터 생성기",             "UPlayGround/생성 도구/스탯 데이터 생성기", "액터 스탯 SO를 생성/갱신합니다.", "CharacterActorType, Monster ActorDefinition 등 액터 정의에 맞춰 ActorStatSO 누락분을 만들고 기본 스탯 프리셋을 적용하는 용도입니다."),
                Tool("스탯 커버리지 검증",          "UPlayGround/생성 도구/스탯 데이터 커버리지 검증", "액터별 스탯 데이터 연결 누락을 검사합니다.", "ActorDefinitionSO와 ActorStatSO 연결 상태를 확인해 런타임 스탯 초기화가 빠지는 문제를 사전에 찾습니다."),
                Tool("파티 성장 에디터",             "UPlayGround/생성 도구/파티 성장 에디터", "파티 캐릭터 성장 데이터를 편집합니다.", "CharacterActorType별 성장 곡선, 기본 스탯, 전투력 미리보기를 스프레드시트 형태로 확인하고 조정합니다."),
                Tool("NPC 데이터 생성기",              "UPlayGround/생성 도구/NPC 데이터 생성기", "NPC용 ActorDefinition과 대화 데이터를 생성합니다.", "NPC Actor 정의, Talkable 플래그, NpcActorSO 연결을 빠르게 구성하는 생성기입니다."),
                Tool("메인 스토리 생성기",            "UPlayGround/생성 도구/메인 스토리 생성기", "메인 스토리 데이터 묶음을 생성합니다.", "메인 스토리용 Quest, Dialogue, StoryEntry 데이터를 한 흐름으로 만들 때 사용합니다."),
                Tool("서브 스토리 생성기",             "UPlayGround/생성 도구/서브 스토리 생성기", "서브 스토리 데이터 묶음을 생성합니다.", "서브 퀘스트와 관련 Dialogue/StoryEntry를 생성하고 기본 연결 구조를 잡습니다."),
                Tool("로코모션 모션 설정",         "UPlayGround/생성 도구/로코모션 모션 설정", "이동 애니메이션 MotionSet을 일괄 구성합니다.", "FBX 클립을 기반으로 8방향/속도별 로코모션 MotionSetAsset을 생성하거나 등록합니다."),
                Tool("카메라 흔들림 프리셋",            "UPlayGround/생성 도구/카메라 흔들림 프리셋", "기본 카메라 쉐이크 프리셋을 생성합니다.", "CameraShakeData 또는 카메라 효과 프리셋이 누락됐을 때 표준 전투 피드백 프리셋을 채웁니다."),
                Tool("입력 글리프 데이터 생성·동기화",     "UPlayGround/입력/글리프 데이터 생성·동기화", "InputGlyphDataSO를 입력 액션 기준으로 생성/동기화합니다.", "PlayerInputActions 에셋의 controlPath 목록을 자동 추출해 InputGlyphDataSO를 만들고 키캡 글리프 항목을 동기화합니다. 입력 바인딩을 추가/변경한 뒤 프롬프트 UI 글리프 누락을 맞출 때 사용합니다."),
                Tool("P09 Weapon EditPartData",         "Tools/P09 Builder/Generate Weapon EditPartData", "P09 무기 EditPartData를 생성/갱신합니다.", "P09 기본 프리팹의 무기 메시를 스캔해 WeaponEditPartData 카탈로그 에셋을 생성하거나 갱신합니다."),
            }),
            ("캐릭터 / 액터", new[]
            {
                Tool("P09 캐릭터 프리팹 빌더",    "Tools/P09 Builder/Character Prefab Builder", "P09 모듈러 캐릭터 프리팹을 빌드합니다.", "성별, 외형 파츠, 무기, 스탯을 탭으로 구성하고 프리셋 저장/불러오기와 라이브 프리뷰를 통해 캐릭터 프리팹을 생성합니다."),
                Tool("액터 데이터베이스 에디터",           "UPlayGround/캐릭터/액터/액터 데이터베이스 에디터", "ActorDefinitionSO 데이터베이스를 관리합니다.", "Actor ID, 표시 이름, 타입, 프리팹, 스탯/드랍/NPC 데이터 연결을 검색하고 편집합니다. 런타임 스폰 기준 데이터의 중심 편집기입니다."),
                Tool("액터 런타임 모니터",           "UPlayGround/캐릭터/액터/액터 런타임 모니터", "현재 씬의 액터 등록 상태를 확인합니다.", "GameObjectManager/ActorSpawnManager에 등록된 액터, ActorType 필터, 런타임 상태를 점검하는 모니터입니다."),
                Tool("Lossy Scale 검사기",           "UPlayGround/캐릭터/액터/Lossy Scale 검사기", "선택 오브젝트 계층의 스케일 문제를 검사합니다.", "캐릭터/무기/이펙트 하위 Transform의 lossyScale을 확인해 비정상 스케일 전파를 찾습니다."),
                Tool("애니메이션 에디터",                 "UPlayGround/캐릭터/액터/애니메이션 에디터", "MotionSet 타임라인을 편집하고 테스트합니다.", "Animancer 기반 MotionSet, MotionEvent, 캐릭터 프리뷰를 다루는 핵심 애니메이션 편집기입니다."),
                Tool("모션셋 복제기",                    "Tools/UPlayGround/Animation/모션셋 복제기 (참조 포함)", "MotionSet과 참조 에셋을 함께 복제합니다.", "ActorAnimationMotionSet 계열 에셋을 새 캐릭터/무기용으로 복제할 때 참조 관계까지 함께 정리하는 보조 도구입니다."),
                Tool("몬스터 데이터 내보내기",             "UPlayGround/캐릭터/액터/데이터/몬스터 데이터 내보내기", "몬스터 데이터를 외부 파일로 내보냅니다.", "몬스터 ActorDefinition/스탯/행동 데이터 점검 또는 백업용으로 데이터를 export합니다."),
                Tool("몬스터 데이터 가져오기",             "UPlayGround/캐릭터/액터/데이터/몬스터 데이터 가져오기", "외부 몬스터 데이터를 프로젝트 에셋으로 반영합니다.", "테이블이나 JSON 등으로 정리한 몬스터 데이터를 ActorDefinition/관련 SO에 다시 적용할 때 사용합니다."),
            }),
            ("캐릭터 / AI", new[]
            {
                Tool("비헤이비어 트리 에디터",            "UPlayGround/비헤이비어 트리/에디터", "몬스터 AI Behavior Tree를 편집합니다.", "BehaviorTreeAsset 노드 그래프를 만들고 조건/액션/데코레이터/서비스 노드를 구성하는 AI 편집기입니다."),
                Tool("AI JSON 가져오기",                  "UPlayGround/비헤이비어 트리/JSON/AI JSON 가져오기 (자동 감지)", "AI JSON 포맷을 자동 판별해 BT 에셋으로 가져옵니다.", "BT Node JSON과 Monster Rules JSON을 자동 감지해 알맞은 importer로 라우팅합니다. 포맷을 확신하지 못할 때 기본으로 사용합니다."),
                Tool("BT JSON 내보내기",                  "UPlayGround/비헤이비어 트리/JSON/BT 노드/선택 항목 내보내기", "선택한 BT 에셋을 JSON으로 내보냅니다.", "BT 구조를 외부 검토, 백업, 비교용 JSON으로 저장합니다."),
                Tool("BT JSON 가져오기",                  "UPlayGround/비헤이비어 트리/JSON/BT 노드/JSON 가져오기", "BT Node JSON에서 BT 에셋을 가져옵니다.", "BehaviorTreeAsset을 그대로 표현한 JSON을 프로젝트 에셋으로 복원합니다. Rules JSON은 Import AI Json 또는 Monster Rules Import를 사용합니다."),
                Tool("몬스터 Rules 내보내기",            "UPlayGround/비헤이비어 트리/JSON/선택 BehaviorSO에서 내보내기", "선택한 EnemyBehaviorSO에서 Rules JSON 초안을 내보냅니다.", "기존 EnemyBehaviorSO 값을 기반으로 Monster Behavior Rules JSON의 기본 blackboard/rule 구조를 생성합니다."),
                Tool("몬스터 Rules 가져오기",            "UPlayGround/비헤이비어 트리/JSON/선택 JSON 가져오기", "Monster Rules JSON을 BT 에셋으로 변환합니다.", "id/groups/rules 기반의 몬스터 행동 규칙 JSON을 Generated 폴더의 BehaviorTreeAsset으로 변환합니다."),
                Tool("몬스터 Rules 폴더 가져오기",     "UPlayGround/비헤이비어 트리/JSON/폴더 가져오기", "폴더 단위 Monster Rules JSON을 가져옵니다.", "선택한 폴더 안의 몬스터 행동 규칙 JSON들을 일괄 변환해 Generated BehaviorTreeAsset으로 생성합니다."),
                Tool("Project 선택 Rules 가져오기", "UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기", "Project 선택 JSON들을 가져옵니다.", "Project 창에서 선택한 Monster Rules JSON들을 한 번에 BT 에셋으로 변환합니다."),
                Tool("SourceJson 전체 가져오기", "UPlayGround/비헤이비어 트리/JSON/SourceJson 전체 가져오기", "SourceJson 전체를 가져옵니다.", "BehaviorTree SourceJson 폴더의 모든 몬스터 행동 규칙 JSON을 일괄 재생성합니다."),
            }),
            ("캐릭터 / 궁극기", new[]
            {
                Tool("궁극기 시퀀스 에디터",              "UPlayGround/캐릭터/궁극기/궁극기 시퀀스 에디터", "UltimateSequenceAsset 연출 타임라인을 편집합니다.", "궁극기 시퀀스의 VFX/SFX/TimeScale/카메라 이펙트/카메라 쉐이크/데미지 윈도우 이벤트를 타임라인으로 구성하는 궁극기 연출 편집기입니다."),
            }),
            ("적 / 의도 가중치", new[]
            {
                Tool("기본 프로필 전체 생성",       "UPlayGround/적/의도 가중치/기본 프로필 전체 생성", "기본 Intent Weight 프로필을 생성합니다.", "EnemyIntentWeights 기본 프로필 세트를 프로젝트 데이터로 생성합니다."),
                Tool("기본 근접 프로필 재생성",        "UPlayGround/적/의도 가중치/기본 근접 프로필 재생성 (레거시 동등)", "기본 근접 프로필을 재생성합니다.", "레거시 근접 AI와 동등한 기본 melee intent weight 프로필을 다시 만듭니다."),
                Tool("레거시 동등성 검사",        "UPlayGround/적/의도 가중치/레거시 동등성 검사 (IW_Default_Melee)", "레거시 근접 AI 등가성을 검사합니다.", "IW_Default_Melee 프로필이 기존 하드코딩/레거시 평가와 동일한 결정을 내리는지 검증합니다."),
            }),
            ("게임플레이 / 전투", new[]
            {
                Tool("공격 데이터 에디터",                "UPlayGround/게임플레이/전투/공격 데이터 에디터", "플레이어 공격 데이터를 편집합니다.", "PlayerAttackDataSO의 콤보, 강공격, 점프/대시/스킬/차지 공격 데이터를 조정합니다."),
                Tool("MotionSet 기반 공격 데이터 생성기", "UPlayGround/게임플레이/전투/MotionSet 기반 공격 데이터 생성기", "MotionSet 이벤트에서 공격 데이터 초안을 생성합니다.", "BeginCollisionEvent의 hitPhaseIndex와 타이밍을 분석해 AttackDataSO/HitPhase 구성을 만드는 보조 도구입니다."),
                Tool("전투 데이터 검증기",           "UPlayGround/게임플레이/전투/도구/데이터 검증기", "전투 데이터 정합성을 검사합니다.", "공격 데이터, 충돌 이벤트, 전투 정책 등 전투 관련 에셋 연결 누락과 위험값을 검증합니다."),
                Tool("전투 로그 기록기",             "UPlayGround/게임플레이/전투/도구/전투 로그 기록기", "전투 로그를 기록/확인합니다.", "플레이 중 전투 판정과 의사결정 로그를 수집해 밸런스와 버그 재현에 사용합니다."),
                Tool("프레임 데이터 테이블",          "UPlayGround/게임플레이/전투/도구/프레임 데이터 테이블", "전 공격의 선딜/액티브/후딜·데미지를 한 테이블로 봅니다.", "MotionSet의 Collision/ComboWindow 이벤트와 AttackDataSO를 합산해 격투게임식 프레임 데이터를 만듭니다. 정렬/CSV 내보내기와 페이즈 불일치 하이라이트를 지원합니다."),
                Tool("HitBox 셋업",                  "UPlayGround/게임플레이/전투/도구/HitBox 셋업", "부착형 Combat HitBox를 자동 생성하고 검증합니다.", "무기/캐릭터 계층을 분석해 HitBox를 생성하고, 통합 검증으로 HitBox 그룹과 AttackData/MotionSet 이벤트 연결 상태를 확인합니다."),
                Tool("HitBox 그룹 ID 동기화",         "UPlayGround/게임플레이/전투/도구/HitBox 그룹 ID 동기화", "HitBox와 공격 데이터의 그룹 ID를 함께 변경합니다.", "CombatHitbox.groupId, HitPhaseData.hitboxGroupId, BeginCollisionEvent.hitboxGroupId를 같은 매핑으로 정리해 판정 누락을 방지합니다."),
                Tool("기본 정책 에셋 생성",  "UPlayGround/게임플레이/전투/정책/기본 정책 에셋 생성", "기본 전투 정책 에셋을 생성합니다.", "CombatPolicy 계열 기본 에셋이 누락됐을 때 표준 설정으로 생성합니다."),
                Tool("입장 변형 슬롯 채우기",          "UPlayGround/게임플레이/전투/입장 변형 슬롯 채우기 (모든 무기)", "모든 무기의 입장 변형(Entry Variant) 슬롯을 일괄 채웁니다.", "PlayerAttackDataSO의 기본 entryAttack을 깊은 복사해 그로기/공중 타깃용 입장 공격 슬롯을 채우고 토글을 켭니다. 이미 켜진 슬롯은 건너뛰어 재실행이 멱등합니다."),
            }),
            ("게임플레이 / 밸런스", new[]
            {
                Tool("밸런스 디자이너",                "UPlayGround/게임플레이/밸런스/밸런스 디자이너", "전투 밸런스 값을 설계합니다.", "몬스터/플레이어 스탯, 전투력, 성장 수치 등을 비교하며 밸런스 기준값을 조정하는 도구입니다."),
                Tool("밸런스 데이터 추출기",          "UPlayGround/게임플레이/밸런스/밸런스 데이터 추출기", "밸런스 데이터를 추출합니다.", "프로젝트의 스탯/전투 데이터를 분석 가능한 형태로 모아 밸런스 점검에 사용합니다."),
                Tool("몬스터 스탯 생성기",          "UPlayGround/게임플레이/밸런스/몬스터 스탯 생성기", "몬스터 스탯을 생성합니다.", "밸런스 기준과 몬스터 정의를 기반으로 EnemyStatsSO 또는 관련 스탯 데이터를 생성합니다."),
                Tool("밸런스 점검 (스냅샷·검증)",     "UPlayGround/게임플레이/밸런스/밸런스 점검 (스냅샷·검증)", "스냅샷 diff와 일괄 검증으로 밸런스 변경을 점검합니다.", "베이스라인 JSON과 현재 에셋 수치를 비교해 의도치 않은 변경을 표시하고, 전체 몬스터 ActorDefinitionSO에 검증/추정을 실행합니다. 생성기 실행 전후 비교가 권장 워크플로입니다."),
                Tool("밸런스 CSV 편집",              "UPlayGround/게임플레이/밸런스/밸런스 CSV 편집", "밸런스 수치를 CSV로 왕복 편집합니다.", "몬스터 스탯/적 스킬 데이터를 CSV로 내보내 외부 시트에서 일괄 수정한 뒤 다시 적용합니다. 가져오기 전 밸런스 점검에서 베이스라인을 저장하면 diff 검증이 가능합니다."),
                Tool("리스크·리워드 산점도",          "UPlayGround/게임플레이/밸런스/리스크·리워드 산점도", "공격별 리스크 대비 리워드를 산점도로 봅니다.", "쿨다운/선후딜 리스크와 데미지/경직/브레이크 리워드를 사분면 산점도로 그려 '저리스크·고리워드' 지배적 공격을 시각적으로 드러냅니다."),
                Tool("몬스터 경험치 발급기",          "UPlayGround/게임플레이/밸런스/몬스터 경험치 발급기", "몬스터 EXP 보상을 일괄 발급합니다.", "ActorDefinitionSO의 level/grade와 기준 플레이어 레벨 차이를 사용해 성장 설계 기반 expReward를 일괄 계산/적용합니다."),
            }),
            ("게임플레이 / 아이템", new[]
            {
                Tool("아이템 에디터",                     "UPlayGround/게임플레이/아이템/아이템 에디터", "아이템 데이터를 검색/편집합니다.", "ItemSO와 장비 데이터의 기본 정보, 아이콘, 분류, 수치 연결을 관리합니다."),
                Tool("드랍 테이블 에디터",               "UPlayGround/게임플레이/아이템/드랍 테이블 에디터", "드랍 테이블을 편집합니다.", "몬스터/상호작용 오브젝트가 사망 또는 채집 시 떨어뜨릴 아이템과 확률을 구성합니다."),
                Tool("무기 정의 누락분 생성", "UPlayGround/게임플레이/아이템/무기 정의/누락 정의 생성", "누락된 무기 정의만 생성합니다.", "EquipmentSO 등에 존재하지만 WeaponDefinitionSO가 없는 항목만 추가합니다."),
                Tool("무기 정의 전체 재생성",   "UPlayGround/게임플레이/아이템/무기 정의/전체 정의 재생성", "전체 무기 정의를 재생성합니다.", "현재 무기 데이터 기준으로 WeaponDefinitionSO 세트를 다시 만듭니다. 기존 수정값 덮어쓰기 가능성이 있어 사용 전 확인이 필요합니다."),
            }),
            ("게임플레이 / 제작", new[]
            {
                Tool("레시피 에디터",                   "UPlayGround/게임플레이/제작/레시피 에디터", "제작 레시피를 편집합니다.", "재료, 결과 아이템, 언락 조건 등 제작 시스템의 실제 레시피 데이터를 관리합니다."),
                Tool("레시피 데이터 가져오기",              "UPlayGround/게임플레이/제작/레시피 데이터 가져오기", "외부 레시피 데이터를 가져옵니다.", "테이블 기반 레시피 입력을 프로젝트 Recipe 데이터로 반영합니다."),
            }),
            ("게임플레이 / 스탯", new[]
            {
                Tool("스탯 데이터베이스 에디터",            "UPlayGround/게임플레이/스탯/스탯 데이터베이스 에디터", "스탯 데이터베이스를 편집합니다.", "ActorStatSO, 스탯 타입, 성장/기본 수치 연결을 검색하고 관리합니다."),
                Tool("스탯 런타임 모니터",            "UPlayGround/게임플레이/스탯/스탯 런타임 모니터", "런타임 스탯 값을 모니터링합니다.", "현재 액터의 ActorStatContainer 값과 modifier 적용 상태를 확인합니다."),
            }),
            ("게임플레이 / 퀘스트", new[]
            {
                Tool("퀘스트 에디터",                    "UPlayGround/게임플레이/퀘스트/퀘스트 에디터", "퀘스트 데이터를 편집합니다.", "QuestSO, 목표, 보상, 진행 조건, ID enum 생성을 관리하는 퀘스트 편집기입니다."),
            }),
            ("게임플레이 / 게임플레이 태그", new[]
            {
                Tool("태그 레지스트리 에디터",             "UPlayGround/게임플레이/게임플레이 태그/태그 레지스트리 에디터", "GameplayTag 레지스트리를 관리합니다.", "상태/전투/AI에서 공유하는 계층형 태그를 등록하고 enum 생성을 위한 기준 데이터를 편집합니다."),
            }),
            ("월드 / 맵", new[]
            {
                Tool("월드 배치 도구",              "UPlayGround/월드/맵/월드 배치 도구", "씬에 액터/상호작용/드랍 아이템을 배치합니다.", "ActorDefinition 기반 프리팹, 직접 프리팹, InteractableActorSO, ItemSO를 한 창에서 선택하고 씬 클릭으로 배치합니다. Interaction 탭은 프리팹이 없으면 기본 GameObject를 만들고 상호작용 데이터와 SceneEntityId를 자동 주입합니다."),
                Tool("SceneEntityId 일괄 부여",      "UPlayGround/World/월드 상태 SceneEntityId 일괄 부여", "월드 상태 대상에 SceneEntityId/GUID를 일괄 부여합니다.", "열린 씬의 MonsterActor와 GatheringActor에 SceneEntityId를 부착하고 비었거나 중복된 GUID를 보정합니다. 몬스터 처치/채집 오브젝트 소모 영속화의 안정적 식별자를 발급하는 용도입니다."),
            }),
            ("월드 / 미니맵", new[]
            {
                Tool("미니맵 캡처 에디터",          "UPlayGround/월드/미니맵/미니맵 캡처 에디터", "미니맵 배경 이미지를 캡처합니다.", "씬을 탑다운 카메라로 촬영하고 PNG 저장 및 Minimap 설정 연결을 보조합니다."),
            }),
            ("월드 / 카메라", new[]
            {
                Tool("카메라 스냅샷 에디터",           "UPlayGround/월드/카메라/카메라 스냅샷 에디터", "카메라 스냅샷을 저장/관리합니다.", "씬 카메라 구도나 연출용 카메라 상태를 스냅샷으로 기록하고 재사용하는 도구입니다."),
                Tool("대화 카메라 녹화",              "UPlayGround/월드/카메라/대화 카메라 녹화", "대화 카메라 연출을 사전 녹화합니다.", "PlayMode에서 프리카메라로 카메라를 직접 몰며 연기/녹화한 뒤 DialogueCameraRecordingSO로 베이크하는 저작 전용 도구입니다. 재생은 런타임이 담당합니다."),
                Tool("대화 카메라 설정 생성", "UPlayGround/월드/카메라/대화 카메라 설정 생성", "대화용 카메라 설정 에셋을 생성합니다.", "Dialogue 카메라 모드에서 사용할 기본 설정 데이터가 없을 때 생성합니다."),
                Tool("전투 카메라 프로필 DB 생성", "UPlayGround/월드/카메라/전투 카메라 프로필 DB 생성", "전투 카메라 프로필 DB를 생성합니다.", "Combat Camera Profile Database 에셋이 없을 때 기본 데이터베이스를 생성합니다."),
                Tool("전투 카메라 프로필 DB 검증", "UPlayGround/월드/카메라/전투 카메라 프로필 DB 검증", "전투 카메라 프로필 DB를 검증합니다.", "Combat Camera Profile Database의 누락 프로필, 중복, 참조 상태를 검사합니다."),
            }),
            ("UI", new[]
            {
                Tool("가이드 팝업 데이터 편집기", "UPlayGround/UI/가이드 팝업 데이터 편집기", "GuidePopupDataSO를 생성하고 페이지 내용을 편집합니다.", "가이드 팝업의 이미지/동영상 페이지, 제목, 본문, 반복 재생 여부를 한 창에서 설정합니다. 페이지 추가, 복제, 삭제, 순서 변경과 미디어 누락 검사를 지원합니다."),
            }),
            ("내러티브 / 대화", new[]
            {
                Tool("대화 그래프 에디터",           "UPlayGround/내러티브/대화/대화 그래프 에디터", "대화 그래프를 편집합니다.", "DialogueGraphSO와 노드 기반 대화 흐름을 편집하는 스토리/대화 도구입니다."),
                Tool("화자 액터 바인딩 생성기",           "UPlayGround/내러티브/대화/화자 액터 바인딩 생성기", "대화 화자와 액터 바인딩 테이블을 생성합니다.", "DialogueGraph의 speaker 정보를 씬/Actor 데이터와 연결하기 위한 바인딩 데이터를 만듭니다."),
            }),
            ("VFX", new[]
            {
                Tool("무기 슬래시 셋업",               "UPlayGround/VFX/Weapon Slash Setup", "무기 슬래시 VFX 스포너와 모션 이벤트를 셋업합니다.", "무기 칼날(Blade) 트랜스폼을 분석해 WeaponSlashVfxSpawner를 구성하고, MotionSet의 슬래시 스폰 이벤트와 프리뷰를 연결하는 전투 VFX 저작 도구입니다."),
            }),
            ("디버그", new[]
            {
                Tool("디버그 기즈모 창",               "UPlayGround/Debug/Debug Gizmo Window", "런타임 디버그 기즈모 표시를 토글합니다.", "DebugGizmo 시스템의 카테고리별 기즈모 표시 여부를 제어하는 개발용 창입니다."),
            }),
            ("유틸", new[]
            {
                Tool("치트 콘솔",                        "UPlayGround/유틸/치트 콘솔", "개발용 치트 명령을 실행합니다.", "테스트 중 상태 변경, 아이템 지급, 플래그 조작 등 개발 편의 명령을 실행하는 콘솔입니다."),
                Tool("아바타 Armature 베이크",            "UPlayGround/유틸/아바타 Armature 베이크 도구", "아바타/본 구조 베이크를 보조합니다.", "캐릭터 모델의 Armature 구조를 프로젝트 표준 구조에 맞추거나 검사할 때 사용합니다."),
                Tool("무기 모션 설정",             "UPlayGround/유틸/무기 모션 설정", "무기별 MotionSet 구성을 보조합니다.", "무기 타입별 공격 모션, 애니메이션 클립, MotionSet 연결을 일괄 설정합니다."),
                Tool("Root Motion 임포트 일괄 변경",        "UPlayGround/유틸/Root Motion 임포트 설정 일괄 변경", "루트 모션 임포트 설정을 일괄 변경합니다.", "선택 폴더 하위 모델 에셋의 AnimationClip Root Transform 옵션을 스캔하고 일괄 적용합니다."),
                Tool("액터 스크린샷 도구",           "UPlayGround/유틸/액터 스크린샷 도구", "액터 스크린샷을 촬영합니다.", "캐릭터/몬스터 프리뷰, 문서, UI 아이콘용 이미지를 캡처하는 보조 도구입니다."),
                Tool("데이터 검증 허브",             "UPlayGround/유틸/데이터 검증 허브", "프로젝트 데이터 정합성을 통합 검증합니다.", "ActorDefinitionSO 중심 참조, ActorDatabase 등록 상태, 전투 데이터 검증 결과를 한 화면에서 확인하고 리포트를 저장합니다."),
                Tool("PlayMode 변경값 프리팹 적용",   "UPlayGround/유틸/PlayMode 변경값 프리팹 적용", "PlayMode에서 조정한 값을 원본 프리팹에 저장합니다.", "Hierarchy에서 선택한 프리팹 인스턴스의 현재 PlayMode 직렬화 값을 프리팹 override로 기록한 뒤 원본 프리팹 에셋에 즉시 적용합니다."),
                Tool("URP 머티리얼 변환기",               "UPlayGround/유틸/변환기/URP 머티리얼 변환기", "머티리얼을 URP 호환으로 변환합니다.", "레거시/외부 에셋 머티리얼을 URP 프로젝트에서 사용할 수 있도록 변환합니다."),
                Tool("SO 스프레드시트",               "UPlayGround/SO 스프레드시트", "ScriptableObject 에셋을 타입별 스프레드시트로 조회/편집합니다.", "프로젝트의 ScriptableObject 에셋을 타입별로 모아 행/열 테이블 형태로 확인하고 직렬화 필드를 직접 편집합니다. 대량 데이터 검토나 SO 값 비교가 필요할 때 사용합니다."),
                Tool("JSON 테이블 뷰어",               "UPlayGround/유틸/뷰어/JSON 테이블 뷰어", "JSON 테이블 파일을 확인합니다.", "외부 데이터 테이블 내용을 Unity 에디터 안에서 빠르게 열람합니다."),
                Tool("Missing Script 정리",             "UPlayGround/유틸/Missing Script 정리/선택 오브젝트 하위 전체", "선택 오브젝트 하위 Missing Script를 제거합니다.", "프리팹/씬 오브젝트에 남은 깨진 MonoBehaviour 참조를 정리합니다. 선택 대상 전체에 적용되므로 실행 전 범위를 확인해야 합니다."),
            }),
        };

        private SearchField _searchField;
        private string _searchQuery = "";
        private Vector2 _scroll;
        private Vector2 _detailScroll;
        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly HashSet<string> _favorites = new();
        private readonly List<string> _recentMenuPaths = new();
        private ToolEntry? _selectedTool;
        private string _selectedCategory;
        private bool _favoritesOnly;
        private bool _recentOnly;
        // 캐시된 GUIStyle은 EditorStyles에서 복사한 내장 텍스처(HideFlags.DontSaveInEditor)를 참조한다.
        // 직렬화되어 도메인 리로드(재컴파일)를 넘어가면 텍스처가 무효화되어
        // GUI.Label 호출 시 "kDontSaveInEditor" Assertion으로 UI가 깨진다.
        // [NonSerialized]로 리로드 후 null이 되게 해 EnsureStyles에서 매번 새로 빌드한다.
        [System.NonSerialized] private GUIStyle _selectedToolStyle;
        [System.NonSerialized] private GUIStyle _toolRowStyle;
        [System.NonSerialized] private GUIStyle _summaryStyle;
        [System.NonSerialized] private GUIStyle _mutedWrapStyle;
        [System.NonSerialized] private GUIStyle _sectionTitleStyle;
        [System.NonSerialized] private GUIStyle _detailBoxStyle;
        [System.NonSerialized] private GUIStyle _badgeStyle;
        [System.NonSerialized] private GUIStyle _favoriteButtonStyle;

        private static ToolEntry Tool(string name, string menuPath, string summary, string detail) =>
            new ToolEntry(name, menuPath, summary, detail);

        [MenuItem("UPlayGround/툴 런처", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.Launcher)]
        public static void Open()
        {
            var win = GetWindow<UPlaygroundToolsLauncher>("툴 런처");
            win.minSize = new Vector2(420f, 400f);
            win.Show();
        }

        private void OnEnable()
        {
            _searchField = new SearchField();
            LoadUserState();
            foreach (var (cat, _) in s_categories)
                if (!_foldouts.ContainsKey(cat)) _foldouts[cat] = true;

            SelectDefaultToolIfNeeded();
        }

        private void OnGUI()
        {
            // 컴파일/도메인 리로드 진행 중에는 EditorStyles 텍스처가 무효화될 수 있어
            // 그리기를 건너뛴다(재컴파일 중 UI 깨짐 방지). 리로드 후 다시 정상 렌더한다.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("스크립트 컴파일 중...", EditorStyles.centeredGreyMiniLabel);
                Repaint();
                return;
            }

            EnsureStyles();
            DrawToolbar();
            DrawSearchBar();

            if (position.width >= 720f)
            {
                EditorGUILayout.BeginHorizontal();
                float listWidth = Mathf.Clamp(position.width * 0.44f, 340f, 520f);
                DrawToolList(GUILayout.Width(listWidth));
                DrawDetailPanel(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawToolList();
                DrawDetailPanel();
            }
        }

        private void EnsureStyles()
        {
            if (_selectedToolStyle != null) return;

            _selectedToolStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            _toolRowStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(12, 4, 2, 2)
            };

            _summaryStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            _mutedWrapStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                wordWrap = true
            };

            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                margin = new RectOffset(0, 0, 8, 2)
            };

            _detailBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(8, 8, 4, 8)
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                padding = new RectOffset(4, 4, 1, 1)
            };

            _favoriteButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(1, 1, 0, 1)
            };
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("UPlayGround Tools", EditorStyles.boldLabel);
            GUILayout.Space(8f);
            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;
            if (_favoritesOnly)
            {
                GUI.backgroundColor = new Color(0.95f, 0.68f, 0.18f);
                GUI.contentColor = Color.white;
            }
            bool favoritesOnly = GUILayout.Toggle(
                _favoritesOnly,
                $"★ 즐겨찾기 {_favorites.Count}",
                EditorStyles.toolbarButton,
                GUILayout.Width(104f));
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
            if (favoritesOnly != _favoritesOnly)
            {
                _favoritesOnly = favoritesOnly;
                if (_favoritesOnly)
                    _recentOnly = false;
            }

            bool recentOnly = GUILayout.Toggle(
                _recentOnly,
                $"최근 {_recentMenuPaths.Count}",
                EditorStyles.toolbarButton,
                GUILayout.Width(72f));
            if (recentOnly != _recentOnly)
            {
                _recentOnly = recentOnly;
                if (_recentOnly)
                    _favoritesOnly = false;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("모두 열기",  EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(true);
            if (GUILayout.Button("모두 닫기", EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(false);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.Space(3);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            rect.x     += 4;
            rect.width -= 8;
            _searchQuery = _searchField.OnGUI(rect, _searchQuery);
            EditorGUILayout.Space(3);
        }

        private void DrawToolList(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCategories();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCategories()
        {
            bool filtering = !string.IsNullOrWhiteSpace(_searchQuery);
            string q = _searchQuery.ToLower();

            foreach (var (category, tools) in s_categories)
            {
                ToolEntry[] matches = System.Array.FindAll(
                    tools,
                    tool => ShouldShowTool(category, tool, filtering, q));

                if (matches.Length == 0) continue;

                if (filtering)
                {
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                }
                else
                {
                    if (!_foldouts.TryGetValue(category, out bool open)) open = true;
                    _foldouts[category] = EditorGUILayout.Foldout(open, category, true, EditorStyles.foldoutHeader);
                    if (!_foldouts[category]) continue;
                }

                EditorGUI.indentLevel++;
                foreach (var tool in matches)
                {
                    DrawToolRow(category, tool);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        private void DrawToolRow(string category, ToolEntry tool)
        {
            bool selected = _selectedTool.HasValue && _selectedTool.Value.MenuPath == tool.MenuPath;

            EditorGUILayout.BeginVertical(_toolRowStyle);
            Rect rowRect = GUILayoutUtility.GetRect(0f, selected ? 42f : 26f, GUILayout.ExpandWidth(true));
            rowRect.x += EditorGUI.indentLevel * 8f;
            rowRect.width -= EditorGUI.indentLevel * 8f;

            if (selected)
                EditorGUI.DrawRect(new Rect(rowRect.x - 4f, rowRect.y - 2f, rowRect.width + 8f, rowRect.height + 4f), new Color(0.25f, 0.36f, 0.52f, 0.35f));

            bool favorite = _favorites.Contains(tool.MenuPath);
            if (favorite)
            {
                EditorGUI.DrawRect(
                    new Rect(rowRect.x - 5f, rowRect.y - 2f, 4f, rowRect.height + 4f),
                    new Color(0.96f, 0.68f, 0.16f));
            }

            Rect favoriteRect = new Rect(rowRect.xMax - 30f, rowRect.y, 28f, 22f);
            Rect clickRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 34f, rowRect.height);
            // GUI.Button은 MouseUp에 true를 반환하지만 clickCount는 MouseDown에서만 채워진다.
            // 따라서 더블클릭은 raw MouseDown 이벤트에서 직접 감지해야 한다(MouseUp 기준 검사는 항상 실패).
            Event evt = Event.current;
            bool doubleClick = evt.type == EventType.MouseDown
                               && evt.button == 0
                               && evt.clickCount == 2
                               && clickRect.Contains(evt.mousePosition);
            if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none) || doubleClick)
            {
                SelectTool(category, tool);

                if (doubleClick)
                {
                    OpenSelectedTool();
                    evt.Use();
                }
            }

            ToolImpact impact = GetToolImpact(tool);
            string badge = GetImpactLabel(impact);
            float badgeWidth = impact == ToolImpact.Normal ? 0f : 58f;

            Rect titleRect = new Rect(rowRect.x, rowRect.y, rowRect.width - badgeWidth - 38f, 18f);
            GUI.Label(titleRect, tool.Name, selected ? EditorStyles.boldLabel : EditorStyles.label);

            if (impact != ToolImpact.Normal)
            {
                Rect badgeRect = new Rect(rowRect.xMax - badgeWidth - 34f, rowRect.y + 1f, badgeWidth, 17f);
                var prevColor = GUI.color;
                GUI.color = GetImpactColor(impact);
                GUI.Label(badgeRect, badge, _badgeStyle);
                GUI.color = prevColor;
            }

            Color oldBackground = GUI.backgroundColor;
            Color oldContent = GUI.contentColor;
            GUI.backgroundColor = favorite
                ? new Color(1f, 0.66f, 0.08f)
                : new Color(0.52f, 0.52f, 0.52f);
            GUI.contentColor = favorite
                ? new Color(1f, 0.95f, 0.65f)
                : new Color(0.76f, 0.76f, 0.76f);
            if (GUI.Button(
                    favoriteRect,
                    new GUIContent(favorite ? "★" : "☆", favorite ? "즐겨찾기 해제" : "즐겨찾기에 추가"),
                    _favoriteButtonStyle))
                SetFavorite(tool.MenuPath, !favorite);
            GUI.backgroundColor = oldBackground;
            GUI.contentColor = oldContent;

            if (selected)
            {
                Rect summaryRect = new Rect(rowRect.x, rowRect.y + 19f, rowRect.width, 18f);
                GUI.Label(summaryRect, tool.Summary, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void SelectTool(string category, ToolEntry tool)
        {
            _selectedCategory = category;
            _selectedTool = tool;
            _detailScroll = Vector2.zero;
        }

        private void DrawDetailPanel(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(_detailBoxStyle, options);

            if (!_selectedTool.HasValue)
            {
                GUILayout.Label("툴을 선택하세요", EditorStyles.boldLabel);
                GUILayout.Label("왼쪽 목록에서 툴을 클릭하면 기능 요약과 사용 상황을 확인할 수 있습니다.", _mutedWrapStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            ToolEntry tool = _selectedTool.Value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label(tool.Name, EditorStyles.boldLabel);
            GUILayout.Label(_selectedCategory, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            bool favorite = _favorites.Contains(tool.MenuPath);
            Color oldBackground = GUI.backgroundColor;
            Color oldContent = GUI.contentColor;
            GUI.backgroundColor = favorite
                ? new Color(1f, 0.66f, 0.08f)
                : new Color(0.52f, 0.52f, 0.52f);
            GUI.contentColor = favorite
                ? new Color(1f, 0.95f, 0.65f)
                : new Color(0.76f, 0.76f, 0.76f);
            if (GUILayout.Button(
                    new GUIContent(favorite ? "★ 즐겨찾기" : "☆ 즐겨찾기", favorite ? "즐겨찾기 해제" : "즐겨찾기에 추가"),
                    _favoriteButtonStyle,
                    GUILayout.Width(84f),
                    GUILayout.Height(24f)))
                SetFavorite(tool.MenuPath, !favorite);
            GUI.backgroundColor = oldBackground;
            GUI.contentColor = oldContent;
            if (GUILayout.Button("경로 복사", GUILayout.Width(82f), GUILayout.Height(24f)))
            {
                EditorGUIUtility.systemCopyBuffer = tool.MenuPath;
                ShowNotification(new GUIContent("메뉴 경로를 복사했습니다."));
            }
            if (GUILayout.Button("열기", GUILayout.Width(88f), GUILayout.Height(24f)))
                OpenSelectedTool();
            EditorGUILayout.EndHorizontal();

            ToolImpact impact = GetToolImpact(tool);
            if (impact != ToolImpact.Normal)
            {
                GUILayout.Space(6);
                EditorGUILayout.HelpBox(GetImpactMessage(impact), GetImpactMessageType(impact));
            }

            GUILayout.Space(8);
            GUILayout.Label("요약", _sectionTitleStyle);
            GUILayout.Label(tool.Summary, _summaryStyle);
            GUILayout.Space(4);
            GUILayout.Label("상세", _sectionTitleStyle);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.MinHeight(96f));
            GUILayout.Label(tool.Detail, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("메뉴 경로", _sectionTitleStyle);
            EditorGUILayout.TextField(tool.MenuPath);
            GUILayout.Label("목록 더블클릭으로도 바로 열 수 있습니다.", _mutedWrapStyle);

            EditorGUILayout.EndVertical();
        }

        private void OpenSelectedTool()
        {
            if (!_selectedTool.HasValue) return;

            ToolEntry tool = _selectedTool.Value;
            bool opened = EditorApplication.ExecuteMenuItem(tool.MenuPath);
            if (opened)
            {
                AddRecent(tool.MenuPath);
                ShowNotification(new GUIContent($"{tool.Name} 열기"));
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "툴 실행 실패",
                    $"메뉴 경로를 실행하지 못했습니다.\n\n{tool.MenuPath}\n\nMenuItem 경로가 변경됐거나 조건부 메뉴 검증에 실패했을 수 있습니다.",
                    "확인");
            }
        }

        private static bool MatchesSearch(string category, ToolEntry tool, string normalizedQuery)
        {
            return category.ToLowerInvariant().Contains(normalizedQuery)
                   || tool.Name.ToLowerInvariant().Contains(normalizedQuery)
                   || tool.MenuPath.ToLowerInvariant().Contains(normalizedQuery)
                   || tool.Summary.ToLowerInvariant().Contains(normalizedQuery)
                   || tool.Detail.ToLowerInvariant().Contains(normalizedQuery)
                   || GetImpactLabel(GetToolImpact(tool)).ToLowerInvariant().Contains(normalizedQuery);
        }

        private bool ShouldShowTool(string category, ToolEntry tool, bool filtering, string query)
        {
            if (_favoritesOnly && !_favorites.Contains(tool.MenuPath))
                return false;
            if (_recentOnly && !_recentMenuPaths.Contains(tool.MenuPath))
                return false;
            return !filtering || MatchesSearch(category, tool, query);
        }

        private static ToolImpact GetToolImpact(ToolEntry tool)
        {
            string text = $"{tool.Name} {tool.MenuPath} {tool.Summary} {tool.Detail}".ToLowerInvariant();

            if (text.Contains("missing script 제거")
                || text.Contains("regenerate all")
                || text.Contains("프로젝트 전체")
                || text.Contains("덮어쓰기"))
            {
                return ToolImpact.Destructive;
            }

            if (text.Contains("import all")
                || text.Contains("import folder")
                || text.Contains("import selected project")
                || text.Contains("generate all")
                || text.Contains("batch")
                || text.Contains("일괄")
                || text.Contains("재생성"))
            {
                return ToolImpact.BulkWrite;
            }

            if (text.Contains("generator")
                || text.Contains("generate")
                || text.Contains("import")
                || text.Contains("create")
                || text.Contains("export")
                || text.Contains("생성")
                || text.Contains("가져옵니다")
                || text.Contains("내보냅니다"))
            {
                return ToolImpact.DataWrite;
            }

            return ToolImpact.Normal;
        }

        private static string GetImpactLabel(ToolImpact impact)
        {
            return impact switch
            {
                ToolImpact.DataWrite => "쓰기",
                ToolImpact.BulkWrite => "일괄",
                ToolImpact.Destructive => "주의",
                _ => "일반"
            };
        }

        private static string GetImpactMessage(ToolImpact impact)
        {
            return impact switch
            {
                ToolImpact.DataWrite => "프로젝트 에셋을 생성, 가져오기, 내보내기 또는 갱신할 수 있는 툴입니다. 실행 전 선택 대상과 저장 경로를 확인하세요.",
                ToolImpact.BulkWrite => "여러 에셋이나 폴더 단위 데이터를 일괄 변경할 수 있는 툴입니다. 실행 전 대상 범위와 결과 프리뷰/로그를 확인하세요.",
                ToolImpact.Destructive => "기존 데이터 삭제, 전체 재생성, 참조 제거처럼 되돌리기 어려운 변경을 만들 수 있는 툴입니다. 실행 전 버전 관리 상태와 선택 범위를 확인하세요.",
                _ => string.Empty
            };
        }

        private static MessageType GetImpactMessageType(ToolImpact impact)
        {
            return impact == ToolImpact.Destructive ? MessageType.Warning : MessageType.Info;
        }

        private static Color GetImpactColor(ToolImpact impact)
        {
            return impact switch
            {
                ToolImpact.DataWrite => new Color(0.78f, 0.9f, 1f, 1f),
                ToolImpact.BulkWrite => new Color(1f, 0.86f, 0.54f, 1f),
                ToolImpact.Destructive => new Color(1f, 0.62f, 0.55f, 1f),
                _ => Color.white
            };
        }

        private bool TryFindTool(string menuPath, out string category, out ToolEntry tool)
        {
            foreach (var (cat, tools) in s_categories)
            {
                foreach (var candidate in tools)
                {
                    if (candidate.MenuPath != menuPath) continue;
                    category = cat;
                    tool = candidate;
                    return true;
                }
            }

            category = null;
            tool = default;
            return false;
        }

        private void SelectDefaultToolIfNeeded()
        {
            if (_selectedTool.HasValue
                && TryFindTool(_selectedTool.Value.MenuPath, out _, out _))
            {
                return;
            }

            foreach (var (category, tools) in s_categories)
            {
                if (tools.Length == 0) continue;
                SelectTool(category, tools[0]);
                return;
            }
        }

        private void OnFocus()
        {
            SelectDefaultToolIfNeeded();
        }

        private void SetAllFoldouts(bool value)
        {
            foreach (var (cat, _) in s_categories)
                _foldouts[cat] = value;
        }

        private void SetFavorite(string menuPath, bool favorite)
        {
            if (favorite)
                _favorites.Add(menuPath);
            else
                _favorites.Remove(menuPath);

            EditorPrefs.SetString(FavoritesPrefsKey, string.Join(PrefsSeparator.ToString(), _favorites));
            Repaint();
        }

        private void AddRecent(string menuPath)
        {
            _recentMenuPaths.Remove(menuPath);
            _recentMenuPaths.Insert(0, menuPath);
            if (_recentMenuPaths.Count > MaxRecentTools)
                _recentMenuPaths.RemoveRange(MaxRecentTools, _recentMenuPaths.Count - MaxRecentTools);

            EditorPrefs.SetString(RecentPrefsKey, string.Join(PrefsSeparator.ToString(), _recentMenuPaths));
            Repaint();
        }

        private void LoadUserState()
        {
            _favorites.Clear();
            _recentMenuPaths.Clear();

            foreach (string path in SplitPrefs(EditorPrefs.GetString(FavoritesPrefsKey, "")))
            {
                if (TryFindTool(path, out _, out _))
                    _favorites.Add(path);
            }

            foreach (string path in SplitPrefs(EditorPrefs.GetString(RecentPrefsKey, "")))
            {
                if (TryFindTool(path, out _, out _) && !_recentMenuPaths.Contains(path))
                    _recentMenuPaths.Add(path);
            }
        }

        private static IEnumerable<string> SplitPrefs(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? System.Array.Empty<string>()
                : value.Split(PrefsSeparator, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
#endif
