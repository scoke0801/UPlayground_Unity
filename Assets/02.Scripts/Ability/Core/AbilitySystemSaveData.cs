using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    [Serializable]
    public sealed class AbilitySystemSaveData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public List<AttributeSaveEntry> attributes = new();
        public List<GasCooldownSaveEntry> cooldowns = new();
        public List<ActiveEffectSaveEntry> activeEffects = new();
    }

    [Serializable]
    public sealed class AttributeSaveEntry
    {
        public string attributeId;
        public float baseValue;

        public AttributeSaveEntry() { }
        public AttributeSaveEntry(string attributeId, float baseValue)
        {
            this.attributeId = attributeId;
            this.baseValue = baseValue;
        }
    }

    [Serializable]
    public sealed class GasCooldownSaveEntry
    {
        public string groupId;
        public float remainingSeconds;
    }

    [Serializable]
    public sealed class ActiveEffectSaveEntry
    {
        public string effectId;
        public float remainingSeconds;
        public int stackCount;
        public List<SetByCallerSaveEntry> setByCaller = new();
    }

    [Serializable]
    public sealed class SetByCallerSaveEntry
    {
        public string key;
        public float value;
    }
}
