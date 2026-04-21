using System;
using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    public enum BlackboardKeyType { Bool, Float, Int, String }

    [Serializable]
    public class BlackboardKeyDefinition
    {
        public string            keyName;
        public BlackboardKeyType keyType;
        [TextArea(1, 2)]
        public string            description;

        public bool   defaultBool;
        public float  defaultFloat;
        public int    defaultInt;
        public string defaultString;
    }
}
