using System;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    [Serializable]
    public class BlackboardEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private BlackboardValueType _valueType;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private string _stringValue;
        [SerializeField] private Vector3 _vector3Value;
        [SerializeField] private UnityEngine.Object _objectValue;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public BlackboardValueType ValueType
        {
            get => _valueType;
            set => _valueType = value;
        }

        public bool BoolValue
        {
            get => _boolValue;
            set => _boolValue = value;
        }

        public int IntValue
        {
            get => _intValue;
            set => _intValue = value;
        }

        public float FloatValue
        {
            get => _floatValue;
            set => _floatValue = value;
        }

        public string StringValue
        {
            get => _stringValue;
            set => _stringValue = value;
        }

        public Vector3 Vector3Value
        {
            get => _vector3Value;
            set => _vector3Value = value;
        }

        public UnityEngine.Object ObjectValue
        {
            get => _objectValue;
            set => _objectValue = value;
        }

        public BlackboardEntry Clone()
        {
            return new BlackboardEntry
            {
                _key = _key,
                _valueType = _valueType,
                _boolValue = _boolValue,
                _intValue = _intValue,
                _floatValue = _floatValue,
                _stringValue = _stringValue,
                _vector3Value = _vector3Value,
                _objectValue = _objectValue
            };
        }
    }
}
