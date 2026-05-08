using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    [Serializable]
    public class ArmorSelectionMap
    {
        [SerializeField] private ScriptableObject[] _slots = new ScriptableObject[5];

        private static readonly Regex _trailingNumber = new Regex(@"(\d+)\s*$", RegexOptions.Compiled);

        public ScriptableObject Get(BuilderArmorSlot slot)
        {
            EnsureCapacity();
            return _slots[(int)slot];
        }

        public void Set(BuilderArmorSlot slot, ScriptableObject so)
        {
            EnsureCapacity();
            _slots[(int)slot] = so;
        }

        public bool TryGetArmorIndex(BuilderArmorSlot slot, out int index)
        {
            index = 0;
            var so = Get(slot);
            if (so == null) return false;
            return TryParseIndexFromName(so.name, out index);
        }

        public int TryGetArmorIndex(BuilderArmorSlot slot)
        {
            return TryGetArmorIndex(slot, out var idx) ? idx : 0;
        }

        public static bool TryParseIndexFromName(string name, out int index)
        {
            index = 0;
            if (string.IsNullOrEmpty(name)) return false;
            var match = _trailingNumber.Match(name);
            if (!match.Success) return false;
            return int.TryParse(match.Groups[1].Value, out index);
        }

        private void EnsureCapacity()
        {
            if (_slots == null || _slots.Length < 5)
            {
                var arr = new ScriptableObject[5];
                if (_slots != null)
                {
                    for (int i = 0; i < _slots.Length && i < 5; i++)
                        arr[i] = _slots[i];
                }
                _slots = arr;
            }
        }
    }
}
