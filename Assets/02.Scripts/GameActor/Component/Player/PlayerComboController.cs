using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>캐릭터별 콤보 진행도 보관과 콤보 입력 창 상태의 단일 소유자.</summary>
    public sealed class PlayerComboController
    {
        public readonly struct Snapshot
        {
            public readonly int CurrentIndex;
            public readonly int NormalIndex;
            public readonly int HeavyIndex;
            public readonly float LastAttackTime;
            public readonly bool CanCombo;
            public readonly int AttackState;
            public readonly bool HadAttackMotion;

            public Snapshot(int currentIndex, int normalIndex, int heavyIndex, float lastAttackTime,
                bool canCombo, int attackState, bool hadAttackMotion)
            {
                CurrentIndex = currentIndex;
                NormalIndex = normalIndex;
                HeavyIndex = heavyIndex;
                LastAttackTime = lastAttackTime;
                CanCombo = canCombo;
                AttackState = attackState;
                HadAttackMotion = hadAttackMotion;
            }
        }

        private readonly Dictionary<CharacterActorType, Snapshot> _snapshots = new();

        public bool IsWindowOpen { get; private set; }

        public void OpenWindow() => IsWindowOpen = true;
        public void CloseWindow() => IsWindowOpen = false;
        public void ResetWindow() => IsWindowOpen = false;

        public void Save(CharacterActorType characterType, in Snapshot snapshot)
        {
            if (characterType != CharacterActorType.None)
                _snapshots[characterType] = snapshot;
        }

        public bool TryRestore(CharacterActorType characterType, float maxCarryTime, out Snapshot snapshot)
        {
            snapshot = default;
            if (characterType == CharacterActorType.None || !_snapshots.TryGetValue(characterType, out snapshot))
                return false;

            if (maxCarryTime > 0f && Time.time - snapshot.LastAttackTime > maxCarryTime)
            {
                _snapshots.Remove(characterType);
                snapshot = default;
                return false;
            }

            IsWindowOpen = snapshot.CanCombo;
            return true;
        }
    }
}
