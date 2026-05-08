using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// 하나의 Actor 종류를 정의하는 ScriptableObject.
    /// ActorID를 키로 ActorDatabase에 등록해 런타임 스폰 및 스탯 조회에 활용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorDef_", menuName = "UPlayGround/Actor/Actor Definition")]
    public class ActorDefinitionSO : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("런타임에서 사용하는 고유 문자열 ID (중복 불가)")]
        public string actorId = "";

        [Tooltip("에디터/UI에 표시할 이름")]
        public string displayName = "";

        [TextArea(2, 4)]
        public string description = "";

        [Header("Actor 기본 정보")]
        public ActorType actorType = ActorType.Monster;
        public CharacterActorType characterType = CharacterActorType.None;
        [Tooltip("이 ActorID가 공격 판정을 켤 때 대상으로 삼을 레이어. 비워두면 ActorType 기본 규칙을 사용한다.")]
        public LayerMask targetLayerMask = 0;

        [Header("프리팹")]
        [Tooltip("런타임 스폰에 사용할 프리팹. GameActor 컴포넌트를 포함해야 함.")]
        public GameObject prefab;

        [Header("스탯 데이터")]
        [Tooltip("레거시 몬스터 튜닝 데이터. 체력은 런타임에 사용하지 않고 ActorStatSO 생성 입력으로만 사용됨.")]
        public EnemyStatsSO stats;

        [Tooltip("통합 스탯 SO. 자동 생성기로 모든 ActorDefinitionSO에 연결되어 있어야 한다.")]
        public ActorStatSO statData;

        [Tooltip("Poise 데이터. null이면 프리팹에 설정된 값 사용.")]
        public PoiseSO poiseData;

        [Header("NPC 데이터")]
        [Tooltip("NpcActor에 주입할 NPC 전용 대화/상호작용 데이터. NPC가 아니면 비워둔다.")]
        public NpcActorSO npcData;

        [Header("드랍 데이터")]
        [Tooltip("사망 시 드랍 테이블. null이면 프리팹에 설정된 값 사용.")]
        public EnemyDropTableSO dropTable;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(actorId))
                actorId = name;
        }
#endif
    }
}
