using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;

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

        [Header("프리팹")]
        [Tooltip("런타임 스폰에 사용할 프리팹. GameActor 컴포넌트를 포함해야 함.")]
        public GameObject prefab;

        [Header("스탯 데이터")]
        [Tooltip("몬스터 스탯 (체력, 이동속도 등). MonsterActor에만 적용됨.")]
        public EnemyStatsSO stats;

        [Tooltip("Poise 데이터. null이면 프리팹에 설정된 값 사용.")]
        public PoiseSO poiseData;

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
