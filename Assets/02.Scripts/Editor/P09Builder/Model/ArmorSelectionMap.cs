using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using P09.Modular.Humanoid.Data;
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

    internal sealed class ArmorIndexPreset
    {
        public int Index { get; }
        public ScriptableObject Head { get; set; }
        public ScriptableObject Chest { get; set; }
        public ScriptableObject Arm { get; set; }
        public ScriptableObject Waist { get; set; }
        public ScriptableObject Leg { get; set; }

        public string DisplayName => $"Armor {Index:00}";

        public ArmorIndexPreset(int index)
        {
            Index = index;
        }

        public ScriptableObject Get(BuilderArmorSlot slot)
        {
            switch (slot)
            {
                case BuilderArmorSlot.Head:  return Head;
                case BuilderArmorSlot.Chest: return Chest;
                case BuilderArmorSlot.Arm:   return Arm;
                case BuilderArmorSlot.Waist: return Waist;
                case BuilderArmorSlot.Leg:   return Leg;
                default: return null;
            }
        }

        public bool HasAnySlot()
        {
            return Head != null || Chest != null || Arm != null || Waist != null || Leg != null;
        }
    }

    internal static class ArmorIndexPresetUtility
    {
        public static List<ArmorIndexPreset> Build(P09AssetCatalog catalog)
        {
            var map = new Dictionary<int, ArmorIndexPreset>();
            if (catalog == null) return new List<ArmorIndexPreset>();

            AddSlot(map, BuilderArmorSlot.Head, catalog.Heads);
            AddSlot(map, BuilderArmorSlot.Chest, catalog.Chests);
            AddSlot(map, BuilderArmorSlot.Arm, catalog.Arms);
            AddSlot(map, BuilderArmorSlot.Waist, catalog.Waists);
            AddSlot(map, BuilderArmorSlot.Leg, catalog.Legs);

            var result = new List<ArmorIndexPreset>();
            foreach (var pair in map)
            {
                if (pair.Value.HasAnySlot())
                    result.Add(pair.Value);
            }

            result.Sort((a, b) => a.Index.CompareTo(b.Index));
            return result;
        }

        public static void Apply(ArmorSelectionMap selections, ArmorIndexPreset preset)
        {
            if (selections == null || preset == null) return;

            foreach (var slot in BuilderArmorSlotExtensions.All)
                selections.Set(slot, preset.Get(slot));
        }

        public static int GetCurrentPresetIndex(ArmorSelectionMap selections)
        {
            if (selections == null) return -1;

            int? index = null;
            bool hasAny = false;

            foreach (var slot in BuilderArmorSlotExtensions.All)
            {
                var so = selections.Get(slot);
                if (so == null) continue;

                if (!TryGetIndex(so, out int slotIndex))
                    return -1;

                hasAny = true;
                if (!index.HasValue)
                    index = slotIndex;
                else if (index.Value != slotIndex)
                    return -1;
            }

            return hasAny && index.HasValue ? index.Value : -1;
        }

        public static bool TryGetIndex(ScriptableObject so, out int index)
        {
            index = 0;
            if (so == null) return false;

            if (so is IEditPartData data && data.ContentId >= 0)
            {
                index = data.ContentId;
                return true;
            }

            return ArmorSelectionMap.TryParseIndexFromName(so.name, out index);
        }

        private static void AddSlot(Dictionary<int, ArmorIndexPreset> map, BuilderArmorSlot slot, List<ScriptableObject> items)
        {
            if (items == null) return;

            foreach (var so in items)
            {
                if (!TryGetIndex(so, out int index)) continue;

                if (!map.TryGetValue(index, out var preset))
                {
                    preset = new ArmorIndexPreset(index);
                    map.Add(index, preset);
                }

                switch (slot)
                {
                    case BuilderArmorSlot.Head:
                        preset.Head = so;
                        break;
                    case BuilderArmorSlot.Chest:
                        preset.Chest = so;
                        break;
                    case BuilderArmorSlot.Arm:
                        preset.Arm = so;
                        break;
                    case BuilderArmorSlot.Waist:
                        preset.Waist = so;
                        break;
                    case BuilderArmorSlot.Leg:
                        preset.Leg = so;
                        break;
                }
            }
        }
    }
}
