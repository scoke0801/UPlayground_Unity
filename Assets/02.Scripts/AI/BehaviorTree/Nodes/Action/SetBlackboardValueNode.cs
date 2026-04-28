using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class SetBlackboardValueNode : BTActionNode
    {
        [SerializeField] private string _key;
        [SerializeField] private BlackboardValueType _valueType;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private string _stringValue;
        [SerializeField] private Vector3 _vector3Value;
        [SerializeField] private UnityEngine.Object _objectValue;

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null || string.IsNullOrWhiteSpace(_key))
                return BTStatus.Failure;

            switch (_valueType)
            {
                case BlackboardValueType.Bool:
                    Context.Blackboard.SetBool(_key, _boolValue);
                    break;
                case BlackboardValueType.Int:
                    Context.Blackboard.SetInt(_key, _intValue);
                    break;
                case BlackboardValueType.Float:
                    Context.Blackboard.SetFloat(_key, _floatValue);
                    break;
                case BlackboardValueType.String:
                    Context.Blackboard.SetString(_key, _stringValue);
                    break;
                case BlackboardValueType.Vector3:
                    Context.Blackboard.SetVector3(_key, _vector3Value);
                    break;
                case BlackboardValueType.Object:
                    Context.Blackboard.SetObject(_key, _objectValue);
                    break;
                default:
                    return BTStatus.Failure;
            }

            return BTStatus.Success;
        }
    }
}
