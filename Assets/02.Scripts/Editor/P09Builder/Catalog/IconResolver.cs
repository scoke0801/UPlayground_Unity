using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using P09.Modular.Humanoid.Data;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class IconResolver
    {
        public const string ICON_ROOT_256 = PathConfig.IconRoot256;

        private readonly Dictionary<string, Texture2D> _cache = new();
        private readonly Dictionary<string, string> _iconPathByKey = new();
        private bool _indexBuilt;
        private static readonly Regex _trailingNumber = new Regex(@"(\d+)\s*$", RegexOptions.Compiled);

        public Texture2D GetIcon(ScriptableObject so, BuilderSex? preferredSex = null)
        {
            if (so == null) return null;

            var key = ResolveIconKey(so, preferredSex);
            if (string.IsNullOrEmpty(key)) return null;

            if (_cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var icon = LoadIconForKey(key);
            if (icon != null) _cache[key] = icon;
            return icon;
        }

        public void ClearCache()
        {
            _cache.Clear();
            _iconPathByKey.Clear();
            _indexBuilt = false;
        }

        private static int ResolveContentId(ScriptableObject so)
        {
            if (so is IEditPartData iface)
            {
                try
                {
                    var id = iface.ContentId;
                    if (id > 0) return id;
                }
                catch { }
            }

            // SO 이름 끝의 숫자 추출
            var match = _trailingNumber.Match(so.name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
                return idx;

            return -1;
        }

        private string ResolveIconKey(ScriptableObject so, BuilderSex? preferredSex)
        {
            var assetPath = AssetDatabase.GetAssetPath(so).Replace('\\', '/');
            if (string.IsNullOrEmpty(assetPath))
                return null;

            int contentId = ResolveContentId(so);
            if (contentId <= 0)
                return null;

            var sex = preferredSex ?? BuilderSex.Female;
            var sexKey = sex == BuilderSex.Male ? "Male" : "Female";

            if (assetPath.Contains("/ScriptableObject/Head/"))
                return MakeKey("Armor", sexKey, "Head", contentId);
            if (assetPath.Contains("/ScriptableObject/Chest/"))
                return MakeKey("Armor", sexKey, "Chest", contentId);
            if (assetPath.Contains("/ScriptableObject/Arm/"))
                return MakeKey("Armor", sexKey, "Arm", contentId);
            if (assetPath.Contains("/ScriptableObject/Waist/"))
                return MakeKey("Armor", sexKey, "Waist", contentId);
            if (assetPath.Contains("/ScriptableObject/Leg/"))
                return MakeKey("Armor", sexKey, "Leg", contentId);

            if (assetPath.Contains("/ScriptableObject/Shield/"))
                return MakeKey("Weapon", "Shield", contentId);
            if (assetPath.Contains("/ScriptableObject/Bow/"))
                return MakeKey("Weapon", "Bow", contentId);
            if (assetPath.Contains("/ScriptableObject/Staff/"))
                return MakeKey("Weapon", "Staff", contentId);
            if (assetPath.Contains("/ScriptableObject/Weapon/Shield/"))
                return MakeKey("Weapon", "Shield", contentId);
            if (assetPath.Contains("/ScriptableObject/Weapon/Bow/"))
                return MakeKey("Weapon", "Bow", contentId);
            if (assetPath.Contains("/ScriptableObject/Weapon/Staff/"))
                return MakeKey("Weapon", "Staff", contentId);
            if (assetPath.Contains("/ScriptableObject/Weapon/"))
                return MakeKey("Weapon", "Sword", contentId);

            return null;
        }

        private Texture2D LoadIconForKey(string key)
        {
            EnsureIndex();
            return _iconPathByKey.TryGetValue(key, out var path)
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(path)
                : null;
        }

        private void EnsureIndex()
        {
            if (_indexBuilt)
                return;

            _indexBuilt = true;
            if (!AssetDatabase.IsValidFolder(ICON_ROOT_256))
                return;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ICON_ROOT_256 });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = ResolveIconPathKey(path);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (!_iconPathByKey.ContainsKey(key))
                    _iconPathByKey.Add(key, path);
            }
        }

        public void Warmup()
        {
            EnsureIndex();
        }

        private static string ResolveIconPathKey(string path)
        {
            var normalized = path.Replace('\\', '/');
            var normalizedNoExtension = normalized.Substring(0, normalized.Length - System.IO.Path.GetExtension(normalized).Length);
            var fileName = System.IO.Path.GetFileNameWithoutExtension(normalized);

            var armorMatch = Regex.Match(
                normalizedNoExtension,
                @"/Armor/(?<sex>Female|Male)_Armor_\d+/icon_P09_(?:Fem|Male)_Armor_(?<id>\d+)_(?<slot>Head|Chest|Arm|Waist|Leg)$",
                RegexOptions.IgnoreCase);
            if (armorMatch.Success)
            {
                var sex = NormalizeSexKey(armorMatch.Groups["sex"].Value);
                var slot = NormalizeSlotKey(armorMatch.Groups["slot"].Value);
                var id = int.Parse(armorMatch.Groups["id"].Value);
                return MakeKey("Armor", sex, slot, id);
            }

            var weaponMatch = Regex.Match(
                fileName,
                @"^icon_P09_Weapon_(?<kind>Sword|Shield|Bow|Staff)_(?<id>\d+)$",
                RegexOptions.IgnoreCase);
            if (weaponMatch.Success)
            {
                var kind = NormalizeSlotKey(weaponMatch.Groups["kind"].Value);
                var id = int.Parse(weaponMatch.Groups["id"].Value);
                return MakeKey("Weapon", kind, id);
            }

            return null;
        }

        private static string NormalizeSexKey(string sex)
        {
            return string.Equals(sex, "Male", System.StringComparison.OrdinalIgnoreCase)
                ? "Male"
                : "Female";
        }

        private static string NormalizeSlotKey(string slot)
        {
            if (string.IsNullOrEmpty(slot))
                return string.Empty;
            return char.ToUpperInvariant(slot[0]) + slot.Substring(1).ToLowerInvariant();
        }

        private static string MakeKey(string category, string sex, string slot, int contentId)
        {
            return $"{category}:{sex}:{slot}:{contentId:000}";
        }

        private static string MakeKey(string category, string kind, int contentId)
        {
            return $"{category}:{kind}:{contentId:000}";
        }
    }
}
