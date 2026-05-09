using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    public sealed class P09AssetCatalog
    {
        public List<ScriptableObject> Heads { get; } = new();
        public List<ScriptableObject> Arms { get; } = new();
        public List<ScriptableObject> Chests { get; } = new();
        public List<ScriptableObject> Waists { get; } = new();
        public List<ScriptableObject> Legs { get; } = new();
        public List<ScriptableObject> HairStyles { get; } = new();
        public List<ScriptableObject> HairColors { get; } = new();
        public List<ScriptableObject> FaceTypes { get; } = new();
        public List<ScriptableObject> Emotions { get; } = new();
        public List<ScriptableObject> FacialHairs { get; } = new();
        public List<ScriptableObject> EyeColors { get; } = new();
        public List<ScriptableObject> SkinColorsAll { get; } = new();
        public List<ScriptableObject> SkinColorsMale { get; } = new();
        public List<ScriptableObject> SkinColorsFemale { get; } = new();
        public List<ScriptableObject> BustSizes { get; } = new();
        public List<ScriptableObject> Swords { get; } = new();
        public List<ScriptableObject> SubSwords { get; } = new();
        public List<ScriptableObject> GreatSwords { get; } = new();
        public List<ScriptableObject> Shields { get; } = new();
        public List<ScriptableObject> Bows { get; } = new();
        public List<ScriptableObject> Staves { get; } = new();
        public List<ScriptableObject> Spears { get; } = new();
        public List<ScriptableObject> DualAxes { get; } = new();
        public List<ScriptableObject> Whips { get; } = new();
        public List<ScriptableObject> WeaponGroups { get; } = new();

        public bool IsLoaded { get; private set; }

        public void Refresh()
        {
            ClearAll();

            if (!AssetDatabase.IsValidFolder(PathConfig.CatalogRoot))
            {
                Debug.LogWarning($"[P09Builder] Catalog root not found: {PathConfig.CatalogRoot}");
                IsLoaded = true;
                return;
            }

            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { PathConfig.CatalogRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;

                Categorize(so, path);
            }

            SortAll();
            IsLoaded = true;
        }

        private void Categorize(ScriptableObject so, string path)
        {
            // path를 분해해서 카탈로그 루트 이후 폴더 세그먼트 검사
            var rel = path.Replace('\\', '/');
            if (!rel.StartsWith(PathConfig.CatalogRoot)) return;
            var sub = rel.Substring(PathConfig.CatalogRoot.Length).TrimStart('/');
            var segs = sub.Split('/');

            // 무기 분류 (서브폴더에 Sword/Shield/Bow/Staff 명시될 수도, 아닐 수도)
            bool inWeapon = ContainsSegment(segs, "Weapon");
            bool inShield = ContainsSegment(segs, "Shield");

            if (inWeapon)
            {
                if (ContainsSegment(segs, "SubSword"))   { SubSwords.Add(so); return; }
                if (ContainsSegment(segs, "GreatSword")) { GreatSwords.Add(so); return; }
                if (ContainsSegment(segs, "Sword"))      { Swords.Add(so); return; }
                if (ContainsSegment(segs, "Shield"))     { Shields.Add(so); return; }
                if (ContainsSegment(segs, "Bow"))        { Bows.Add(so); return; }
                if (ContainsSegment(segs, "Staff"))      { Staves.Add(so); return; }
                if (ContainsSegment(segs, "Spear"))      { Spears.Add(so); return; }
                if (ContainsSegment(segs, "DualAxe"))    { DualAxes.Add(so); return; }
                if (ContainsSegment(segs, "DoubleAxe"))  { DualAxes.Add(so); return; }
                if (ContainsSegment(segs, "Whip"))       { Whips.Add(so); return; }
                if (ContainsSegment(segs, "WeaponGroup")) { WeaponGroups.Add(so); return; }

                CategorizeWeaponByMeshName(so);
                return;
            }
            if (inShield) { Shields.Add(so); return; }
            if (ContainsSegment(segs, "WeaponGroup")) { WeaponGroups.Add(so); return; }

            if (ContainsSegment(segs, "Head"))         { Heads.Add(so); return; }
            if (ContainsSegment(segs, "Arm"))          { Arms.Add(so); return; }
            if (ContainsSegment(segs, "Chest"))        { Chests.Add(so); return; }
            if (ContainsSegment(segs, "Waist"))        { Waists.Add(so); return; }
            if (ContainsSegment(segs, "Leg"))          { Legs.Add(so); return; }
            if (ContainsSegment(segs, "HairStyle"))    { HairStyles.Add(so); return; }
            if (ContainsSegment(segs, "HairColor"))    { HairColors.Add(so); return; }
            if (ContainsSegment(segs, "FaceType"))     { FaceTypes.Add(so); return; }
            if (ContainsSegment(segs, "FaceEmotion"))  { Emotions.Add(so); return; }
            if (ContainsSegment(segs, "FacialHair"))   { FacialHairs.Add(so); return; }
            if (ContainsSegment(segs, "EyeColor"))     { EyeColors.Add(so); return; }
            if (ContainsSegment(segs, "BustSize"))     { BustSizes.Add(so); return; }
            if (ContainsSegment(segs, "SkinColor"))
            {
                SkinColorsAll.Add(so);
                int sexId = ReadSexId(so);
                if (sexId == 1) SkinColorsMale.Add(so);
                else if (sexId == 2) SkinColorsFemale.Add(so);
                else
                {
                    SkinColorsMale.Add(so);
                    SkinColorsFemale.Add(so);
                }
                return;
            }
        }

        private static bool ContainsSegment(string[] segs, string name)
        {
            for (int i = 0; i < segs.Length; i++)
                if (string.Equals(segs[i], name, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void CategorizeWeaponByMeshName(ScriptableObject so)
        {
            string meshName = ReadString(so, "_meshName", "meshName");

            if (StartsWith(meshName, "SubSword"))   { SubSwords.Add(so); return; }
            if (StartsWith(meshName, "GreatSword")) { GreatSwords.Add(so); return; }
            if (StartsWith(meshName, "Shield"))     { Shields.Add(so); return; }
            if (StartsWith(meshName, "Bow"))        { Bows.Add(so); return; }
            if (StartsWith(meshName, "Staff"))      { Staves.Add(so); return; }
            if (StartsWith(meshName, "Spear"))      { Spears.Add(so); return; }
            if (StartsWith(meshName, "DualAxe"))    { DualAxes.Add(so); return; }
            if (StartsWith(meshName, "DoubleAxe"))  { DualAxes.Add(so); return; }
            if (StartsWith(meshName, "Whip"))       { Whips.Add(so); return; }

            // 기존 P09 데이터처럼 Weapon/ 직속에 검 데이터가 놓이는 경우.
            Swords.Add(so);
        }

        private static bool StartsWith(string value, string prefix)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadString(ScriptableObject so, params string[] propertyNames)
        {
            try
            {
                var sObj = new SerializedObject(so);
                foreach (var propertyName in propertyNames)
                {
                    var prop = sObj.FindProperty(propertyName);
                    if (prop != null && prop.propertyType == SerializedPropertyType.String)
                        return prop.stringValue;
                }
            }
            catch { }

            return string.Empty;
        }

        private static int ReadSexId(ScriptableObject so)
        {
            try
            {
                var sObj = new SerializedObject(so);
                var prop = sObj.FindProperty("_sexId");
                if (prop == null) prop = sObj.FindProperty("sexId");
                if (prop == null) return 0;
                if (prop.propertyType == SerializedPropertyType.Integer) return prop.intValue;
                if (prop.propertyType == SerializedPropertyType.Enum) return prop.enumValueIndex;
            }
            catch { }
            return 0;
        }

        public string GetDisplayName(ScriptableObject so)
        {
            return so == null ? "(none)" : so.name;
        }

        private void ClearAll()
        {
            Heads.Clear(); Arms.Clear(); Chests.Clear(); Waists.Clear(); Legs.Clear();
            HairStyles.Clear(); HairColors.Clear(); FaceTypes.Clear(); Emotions.Clear();
            FacialHairs.Clear(); EyeColors.Clear();
            SkinColorsAll.Clear(); SkinColorsMale.Clear(); SkinColorsFemale.Clear();
            BustSizes.Clear();
            Swords.Clear(); SubSwords.Clear(); GreatSwords.Clear();
            Shields.Clear(); Bows.Clear(); Staves.Clear();
            Spears.Clear(); DualAxes.Clear(); Whips.Clear();
            WeaponGroups.Clear();
        }

        private void SortAll()
        {
            Heads.Sort(NameCompare);
            Arms.Sort(NameCompare);
            Chests.Sort(NameCompare);
            Waists.Sort(NameCompare);
            Legs.Sort(NameCompare);
            HairStyles.Sort(NameCompare);
            HairColors.Sort(NameCompare);
            FaceTypes.Sort(NameCompare);
            Emotions.Sort(NameCompare);
            FacialHairs.Sort(NameCompare);
            EyeColors.Sort(NameCompare);
            SkinColorsAll.Sort(NameCompare);
            SkinColorsMale.Sort(NameCompare);
            SkinColorsFemale.Sort(NameCompare);
            BustSizes.Sort(NameCompare);
            Swords.Sort(NameCompare);
            SubSwords.Sort(NameCompare);
            GreatSwords.Sort(NameCompare);
            Shields.Sort(NameCompare);
            Bows.Sort(NameCompare);
            Staves.Sort(NameCompare);
            Spears.Sort(NameCompare);
            DualAxes.Sort(NameCompare);
            Whips.Sort(NameCompare);
            WeaponGroups.Sort(NameCompare);
        }

        private static int NameCompare(ScriptableObject a, ScriptableObject b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
