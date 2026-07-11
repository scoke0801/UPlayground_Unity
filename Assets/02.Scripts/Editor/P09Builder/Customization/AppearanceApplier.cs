using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using P09.Modular.Humanoid.Data;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
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
        private static readonly Regex FacialHairNamePattern =
            new Regex(@"^(?:Male|Female|Fem)_FacialHair_(\d+)$", RegexOptions.Compiled);

        public static void Apply(GameObject prefabRoot, CharacterBuildConfig config, P09AssetCatalog catalog)
        {
            if (prefabRoot == null || config == null || catalog == null) return;

            int sexId = config.Sex == BuilderSex.Male ? MaleSexId : FemaleSexId;
            var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            var rootTransform = prefabRoot.transform;

            // 베이스 프리팹에는 성별 모드별 그룹("Male" / "Female")이 함께 들어있다.
            // 선택된 성별 그룹만 활성화하고 반대편은 비활성화. (Variant 프리팹의 경우 한쪽이 RemovedGameObjects 로 빠져 있어도 안전)
            ApplyGenderGroup(allTransforms, config.Sex, rootTransform);

            // FaceType
            if (config.FaceTypeSo != null)
                ApplyRenderer(allTransforms, ToData(catalog.FaceTypes), GetId(config.FaceTypeSo), sexId, rootTransform);

            // HairStyle
            if (config.HairStyleSo != null)
                ApplyRenderer(allTransforms, ToData(catalog.HairStyles), GetId(config.HairStyleSo), sexId, rootTransform);

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
            int facialHairId = config.FacialHairId > 0 ? config.FacialHairId : GetId(config.FacialHairSo);
            ApplyAttachedFacialHair(allTransforms, config.Sex == BuilderSex.Male ? facialHairId : 0, rootTransform);

            // BustSize (Female only)
            if (config.Sex == BuilderSex.Female && config.BustSizeSo != null)
                ApplyBustSize(allTransforms, ToData(catalog.BustSizes), GetId(config.BustSizeSo));

            // Armor slots
            // so == null (None) 일 때도 슬롯에 속한 모든 메시를 꺼야 하므로 selectedId=0 으로 일괄 적용.
            foreach (var slot in System.Enum.GetValues(typeof(BuilderArmorSlot)).Cast<BuilderArmorSlot>())
            {
                var so = config.ArmorSelections?.Get(slot);
                var items = GetCatalogForSlot(slot, catalog);
                ApplyRenderer(allTransforms, ToData(items), GetId(so), sexId, rootTransform);
            }
        }

        // 성별 그룹 토글: 베이스 프리팹 안의 "Male" / "Female" 최상위 그룹을 선택된 성별에 맞게 활성화한다.
        // - Male 선택: "Male" 활성, "Female" 비활성. (반대편 그룹 안의 모든 자식 메시도 함께 꺼짐.)
        // - 그룹이 없으면(이미 변형 프리팹에서 RemovedGameObjects 처리된 경우) 조용히 패스.
        // - 양쪽 그룹이 모두 활성이거나 모두 비활성인 베이스 프리팹의 비정상 상태를 강제 정상화하는 역할도 겸한다.
        private static void ApplyGenderGroup(Transform[] allTransforms, BuilderSex sex, Transform root)
        {
            if (allTransforms == null) return;
            bool isMale = sex == BuilderSex.Male;
            foreach (var t in allTransforms)
            {
                if (t == null || t == root) continue;
                if (t.name == "Male")
                {
                    t.gameObject.SetActive(isMale);
                    if (isMale) EnsureAncestorsActive(t, root);
                }
                else if (t.name == "Female" || t.name == "Fem")
                {
                    t.gameObject.SetActive(!isMale);
                    if (!isMale) EnsureAncestorsActive(t, root);
                }
            }
        }

        // 활성화하려는 트랜스폼의 조상 체인을 root 직하까지 self-active 로 끌어올린다.
        // P09 베이스 프리팹은 Armor_XXX 같은 상위 그룹이 m_IsActive=0 이라
        // 자식만 SetActive(true) 해도 보이지 않는 문제를 보정.
        private static void EnsureAncestorsActive(Transform t, Transform root)
        {
            if (t == null) return;
            var p = t.parent;
            while (p != null && p != root)
            {
                if (!p.gameObject.activeSelf)
                    p.gameObject.SetActive(true);
                p = p.parent;
            }
        }

        private static List<IEditPartData> ToData(List<ScriptableObject> list)
            => list?.OfType<IEditPartData>().ToList() ?? new List<IEditPartData>();

        private static int GetId(ScriptableObject so)
            => (so as IEditPartData)?.ContentId ?? 0;

        // AvatarView.UpdateRenderer 에디터 버전.
        // 정확 일치뿐 아니라 "{베이스이름}_*" 형태의 보조 메시(예: *_Chest_Cloak)도 함께 토글한다.
        // 활성화되는 메시의 부모 체인은 EnsureAncestorsActive 로 함께 켜준다.
        private static void ApplyRenderer(Transform[] allTransforms, List<IEditPartData> dataList, int selectedId, int sexId, Transform root)
        {
            if (dataList == null || dataList.Count == 0) return;
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                foreach (var data in dataList)
                {
                    if (data == null || string.IsNullOrEmpty(data.MeshName)) continue;

                    if (NameMatches(t.name, data.MeshName))
                    {
                        bool active = data.ContentId == selectedId;
                        t.gameObject.SetActive(active);
                        if (active) EnsureAncestorsActive(t, root);
                        continue;
                    }

                    string maleName = TryFormat(data.MeshName, "Male");
                    if (maleName != null && NameMatches(t.name, maleName))
                    {
                        bool active = sexId == MaleSexId && data.ContentId == selectedId;
                        t.gameObject.SetActive(active);
                        if (active) EnsureAncestorsActive(t, root);
                        continue;
                    }

                    string femaleName = TryFormat(data.MeshName, "Female");
                    string femName    = TryFormat(data.MeshName, "Fem");
                    if ((femaleName != null && NameMatches(t.name, femaleName)) ||
                        (femName != null    && NameMatches(t.name, femName)))
                    {
                        bool active = sexId == FemaleSexId && data.ContentId == selectedId;
                        t.gameObject.SetActive(active);
                        if (active) EnsureAncestorsActive(t, root);
                    }
                }
            }
        }

        private static bool NameMatches(string transformName, string baseName)
        {
            if (string.IsNullOrEmpty(transformName) || string.IsNullOrEmpty(baseName)) return false;
            if (transformName == baseName) return true;
            // Cloak/Cape 등 보조 파츠: "{base}_..." 접두 매칭
            return transformName.Length > baseName.Length
                && transformName[baseName.Length] == '_'
                && transformName.StartsWith(baseName, System.StringComparison.Ordinal);
        }

        private static string TryFormat(string format, string arg)
        {
            try { return string.Format(format, arg); }
            catch (System.FormatException) { return null; }
        }

        private static void ApplyAttachedFacialHair(Transform[] allTransforms, int selectedId, Transform root)
        {
            if (allTransforms == null) return;

            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                var match = FacialHairNamePattern.Match(t.name);
                if (!match.Success) continue;

                int id = 0;
                int.TryParse(match.Groups[1].Value, out id);
                bool active = selectedId > 0 && id == selectedId;
                t.gameObject.SetActive(active);
                if (active) EnsureAncestorsActive(t, root);
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
