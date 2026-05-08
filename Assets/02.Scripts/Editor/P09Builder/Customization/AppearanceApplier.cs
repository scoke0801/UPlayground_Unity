using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using P09.Modular.Humanoid.Data;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// AvatarView의 SetActive 토글 / 머티리얼 교체 로직을 에디터에서 재현.
    /// 런타임 AvatarView는 Awake/Start 시점에 동작하므로 에디터에서는 우리가 직접 적용해야 한다.
    /// 머티리얼 교체는 sharedMaterial / sharedMaterials 사용 (에디터에서 material 사용 시 인스턴스 생성됨).
    /// </summary>
    internal static class AppearanceApplier
    {
        private const int MaleSexId = 1;
        private const int FemaleSexId = 2;
        private const string SkinMaterialPattern = @"^P09_.*_Skin.*$";
        private const string EyeMaterialPattern = @"^P09_Eye.*$";

        public static void Apply(GameObject prefabRoot, CharacterBuildConfig config, P09AssetCatalog catalog)
        {
            if (prefabRoot == null || config == null || catalog == null) return;

            int sexId = config.Sex == BuilderSex.Male ? MaleSexId : FemaleSexId;
            var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(includeInactive: true);

            // FaceType
            if (config.FaceTypeSo != null)
                ApplyRenderer(allTransforms, ToData(catalog.FaceTypes), GetId(config.FaceTypeSo), sexId);

            // HairStyle
            if (config.HairStyleSo != null)
                ApplyRenderer(allTransforms, ToData(catalog.HairStyles), GetId(config.HairStyleSo), sexId);

            // HairColor (sharedMaterial 사용)
            if (config.HairColorSo != null && config.HairStyleSo != null)
            {
                int hairStyleId = GetId(config.HairStyleSo);
                int hairColorId = GetId(config.HairColorSo);
                ApplyHairColor(allTransforms, ToData(catalog.HairColors), hairColorId, hairStyleId);
            }

            // SkinColor
            if (config.SkinColorSo != null)
            {
                var skinList = config.Sex == BuilderSex.Male
                    ? ToData(catalog.SkinColorsMale)
                    : ToData(catalog.SkinColorsFemale);
                ApplySkinColor(allTransforms, skinList, GetId(config.SkinColorSo));
            }

            // EyeColor
            if (config.EyeColorSo != null)
                ApplyEyeColor(allTransforms, ToData(catalog.EyeColors), GetId(config.EyeColorSo));

            // FacialHair (Male only)
            if (config.Sex == BuilderSex.Male && config.FacialHairSo != null)
                ApplyRenderer(allTransforms, ToData(catalog.FacialHairs), GetId(config.FacialHairSo), sexId);

            // BustSize (Female only)
            if (config.Sex == BuilderSex.Female && config.BustSizeSo != null)
                ApplyBustSize(allTransforms, ToData(catalog.BustSizes), GetId(config.BustSizeSo));

            // Armor slots
            foreach (var slot in System.Enum.GetValues(typeof(BuilderArmorSlot)).Cast<BuilderArmorSlot>())
            {
                var so = config.ArmorSelections?.Get(slot);
                if (so == null) continue;
                var items = GetCatalogForSlot(slot, catalog);
                ApplyRenderer(allTransforms, ToData(items), GetId(so), sexId);
            }
        }

        private static List<IEditPartData> ToData(List<ScriptableObject> list)
            => list?.OfType<IEditPartData>().ToList() ?? new List<IEditPartData>();

        private static int GetId(ScriptableObject so)
            => (so as IEditPartData)?.ContentId ?? 0;

        // AvatarView.UpdateRenderer 에디터 버전
        private static void ApplyRenderer(Transform[] allTransforms, List<IEditPartData> dataList, int selectedId, int sexId)
        {
            if (dataList == null || dataList.Count == 0) return;
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                foreach (var data in dataList)
                {
                    if (data == null || string.IsNullOrEmpty(data.MeshName)) continue;
                    if (t.name == data.MeshName)
                    {
                        t.gameObject.SetActive(data.ContentId == selectedId);
                    }
                    else
                    {
                        try
                        {
                            var maleName = string.Format(data.MeshName, "Male");
                            if (t.name == maleName)
                            {
                                t.gameObject.SetActive(sexId == MaleSexId && data.ContentId == selectedId);
                                continue;
                            }
                            var femaleName = string.Format(data.MeshName, "Female");
                            var femName = string.Format(data.MeshName, "Fem");
                            if (t.name == femaleName || t.name == femName)
                            {
                                t.gameObject.SetActive(sexId == FemaleSexId && data.ContentId == selectedId);
                            }
                        }
                        catch (System.FormatException)
                        {
                            // MeshName에 {0} placeholder가 없는 경우 무시
                        }
                    }
                }
            }
        }

        // AvatarView.UpdateHairColor 에디터 버전 (sharedMaterial 사용)
        private static void ApplyHairColor(Transform[] allTransforms, List<IEditPartData> dataList, int selectedColorId, int hairStyleId)
        {
            if (dataList == null || dataList.Count == 0) return;
            var currentData = dataList.FirstOrDefault(d => d.ContentId == selectedColorId);
            if (currentData == null) return;

            foreach (var data in dataList)
            {
                if (data == null || string.IsNullOrEmpty(data.MeshName)) continue;
                string targetName;
                try { targetName = string.Format(data.MeshName, hairStyleId); }
                catch { continue; }

                foreach (var t in allTransforms)
                {
                    if (t == null || t.name != targetName) continue;
                    var renderer = t.GetComponent<Renderer>();
                    if (renderer == null) continue;
                    var mat = (currentData as HairColorEditPartData)?.GetMaterial(hairStyleId);
                    if (mat != null) renderer.sharedMaterial = mat;
                }
            }
        }

        // AvatarView.UpdateSkinColor 에디터 버전 (sharedMaterials 사용)
        private static void ApplySkinColor(Transform[] allTransforms, List<IEditPartData> dataList, int selectedId)
        {
            if (dataList == null || dataList.Count == 0) return;
            var currentData = dataList.FirstOrDefault(d => d.ContentId == selectedId);
            var mat = (currentData as ColorEditPartData)?.Material;
            if (mat == null) return;

            var rx = new Regex(SkinMaterialPattern);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                var renderer = t.GetComponent<Renderer>();
                if (renderer == null) continue;
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && rx.IsMatch(mats[i].name))
                    {
                        mats[i] = mat;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        // AvatarView.UpdateEyeColor 에디터 버전
        private static void ApplyEyeColor(Transform[] allTransforms, List<IEditPartData> dataList, int selectedId)
        {
            if (dataList == null || dataList.Count == 0) return;
            var currentData = dataList.FirstOrDefault(d => d.ContentId == selectedId);
            var mat = (currentData as ColorEditPartData)?.Material;
            if (mat == null) return;
            if (string.IsNullOrEmpty(currentData?.MeshName)) return;

            var rx = new Regex(EyeMaterialPattern);
            foreach (var t in allTransforms)
            {
                if (t == null || !t.name.Contains(currentData.MeshName)) continue;
                foreach (var renderer in t.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null && rx.IsMatch(mats[i].name))
                        {
                            mats[i] = mat;
                            changed = true;
                        }
                    }
                    if (changed) renderer.sharedMaterials = mats;
                }
            }
        }

        // AvatarView.UpdateBustSize 에디터 버전
        private static void ApplyBustSize(Transform[] allTransforms, List<IEditPartData> dataList, int selectedId)
        {
            if (dataList == null || dataList.Count == 0) return;
            var currentData = dataList.FirstOrDefault(d => d.ContentId == selectedId) as BustSizeEditPartData;
            if (currentData == null) return;

            string rName, lName;
            try
            {
                rName = string.Format(currentData.MeshName, "R");
                lName = string.Format(currentData.MeshName, "L");
            }
            catch { return; }

            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (t.name == rName || t.name == lName)
                    t.localScale = currentData.Size;
            }
        }

        private static List<ScriptableObject> GetCatalogForSlot(BuilderArmorSlot slot, P09AssetCatalog catalog)
        {
            switch (slot)
            {
                case BuilderArmorSlot.Head:  return catalog.Heads;
                case BuilderArmorSlot.Chest: return catalog.Chests;
                case BuilderArmorSlot.Arm:   return catalog.Arms;
                case BuilderArmorSlot.Waist: return catalog.Waists;
                case BuilderArmorSlot.Leg:   return catalog.Legs;
                default: return new List<ScriptableObject>();
            }
        }
    }
}
