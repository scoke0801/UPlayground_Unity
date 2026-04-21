using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Blackboard", fileName = "BTBlackboard")]
    public class BTBlackboardSO : ScriptableObject
    {
        public List<BlackboardKeyDefinition> keys = new();

        public void InitializeBlackboard(RuntimeBlackboard bb)
        {
            foreach (var key in keys)
            {
                switch (key.keyType)
                {
                    case BlackboardKeyType.Bool:   bb.Set(key.keyName, key.defaultBool);   break;
                    case BlackboardKeyType.Float:  bb.Set(key.keyName, key.defaultFloat);  break;
                    case BlackboardKeyType.Int:    bb.Set(key.keyName, key.defaultInt);    break;
                    case BlackboardKeyType.String: bb.Set(key.keyName, key.defaultString); break;
                }
            }
        }

        public BlackboardKeyDefinition GetKey(string keyName)
            => keys.Find(k => k.keyName == keyName);
    }
}
