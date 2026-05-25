using UnityEngine;
using UPlayGround;
using UPlayGround.Group;

namespace UPlayGround.TriggerSystem
{
    /// <summary>
    /// ScriptableObject 액션/소스가 직접 들기 어려운 씬 오브젝트 참조를 TriggerComposer 옆에서 제공한다.
    /// SO 에셋은 씬 객체 참조를 직렬화하지 못하므로(리로드 시 null로 드롭), 씬 단위 참조는 여기에 연결한다.
    /// </summary>
    [AddComponentMenu("UPlayGround/Trigger/Trigger Scene References")]
    public sealed class TriggerSceneReferences : MonoBehaviour
    {
        [SerializeField] private MonsterGroupController _group;
        [SerializeField] private MonsterActor _actor;

        public MonsterGroupController Group => _group;
        public MonsterActor Actor => _actor;
    }
}
