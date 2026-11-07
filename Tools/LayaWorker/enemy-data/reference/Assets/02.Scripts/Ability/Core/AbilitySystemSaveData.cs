using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    [Serializable]
    public sealed class AbilitySystemSaveData
    {
        public const int CurrentVersion = 5;
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
        public int availableCharges;
        public int maxCharges = 1;
        public float rechargeDurationSeconds;
    }

    [Serializable]
    public sealed class ActiveEffectSaveEntry
    {
        public string effectId;
        public string sourceActorId;
        public float remainingSeconds;
        public int stackCount;
        public int hudVisibility;
        public float specLevel;
        public List<SetByCallerSaveEntry> setByCaller = new();
    }

    [Serializable]
    public sealed class SetByCallerSaveEntry
    {
        public string key;
        public float value;
    }
}
