using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public static class AbilitySystemDebugRegistry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly Dictionary<ulong, WeakReference<IAbilitySystemDebugSource>> Sources = new();

        public static void Register(AbilitySystemHandle handle, IAbilitySystemDebugSource source)
        {
            if (!handle.IsValid || source == null) return;
            Sources[handle.Value] = new WeakReference<IAbilitySystemDebugSource>(source);
        }

        public static void Unregister(AbilitySystemHandle handle)
        {
            if (handle.IsValid) Sources.Remove(handle.Value);
        }

        public static void CopyAlive(ICollection<IAbilitySystemDebugSource> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            var dead = new List<ulong>();
            foreach (KeyValuePair<ulong, WeakReference<IAbilitySystemDebugSource>> pair in Sources)
            {
                if (pair.Value.TryGetTarget(out IAbilitySystemDebugSource source) && source != null)
                    destination.Add(source);
                else
                    dead.Add(pair.Key);
            }
            for (int i = 0; i < dead.Count; i++) Sources.Remove(dead[i]);
        }
#else
        public static void Register(AbilitySystemHandle handle, IAbilitySystemDebugSource source) { }
        public static void Unregister(AbilitySystemHandle handle) { }
        public static void CopyAlive(ICollection<IAbilitySystemDebugSource> destination) => destination?.Clear();
#endif
    }
}
