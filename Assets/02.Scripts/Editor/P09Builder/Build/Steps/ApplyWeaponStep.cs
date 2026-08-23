using System.Collections.Generic;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// 무기 슬롯에 해당하는 메쉬를 SetActive 토글로 활성화한다.
    /// WeaponEditPartData.MeshName 기반.
    /// </summary>
    public sealed class ApplyWeaponStep : IBuildStep
    {
        private const int MaleSexId = 1;
        private const int FemaleSexId = 2;

        private static readonly string[] SwordRoots = { "Sword" };
        private static readonly string[] SubSwordRoots = { "SubSword" };
        private static readonly string[] GreatSwordRoots = { "GreatSword" };
        private static readonly string[] ShieldRoots = { "Shield" };
        private static readonly string[] BowRoots = { "Bow" };
        private static readonly string[] StaffRoots = { "Staff" };
        private static readonly string[] SpearRoots = { "Spear" };
        private static readonly string[] DualAxeRoots = { "DualAxe", "DoubleAxe" };
        private static readonly string[] WhipRoots = { "Whip" };

        private const string HumanoidMotionSetRoot =
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid";

        private static readonly Dictionary<WeaponType, string> HumanoidMotionSetPaths = new()
        {
            { WeaponType.Sword,       HumanoidMotionSetRoot + "/Humanoid_KatanaAnimationSet.asset" },
            { WeaponType.SwordShield, HumanoidMotionSetRoot + "/Humanoid_SwordShieldAnimationSet.asset" },
            { WeaponType.GreatSword,  HumanoidMotionSetRoot + "/Humanoid_GreatSwordAnimationSet.asset" },
            { WeaponType.Staff,       HumanoidMotionSetRoot + "/Humanoid_StaffAnimationSet.asset" },
            { WeaponType.Bow,         HumanoidMotionSetRoot + "/Humanoid_BowAnimationSet.asset" },
            { WeaponType.Katana,      HumanoidMotionSetRoot + "/Humanoid_KatanaAnimationSet.asset" },
            { WeaponType.DoubleAxe,   HumanoidMotionSetRoot + "/Humanoid_DoubleAxeAnimationSet.asset" },
            { WeaponType.Whip,        HumanoidMotionSetRoot + "/Humanoid_WhipAnimationSet.asset" },
            { WeaponType.Spear,       HumanoidMotionSetRoot + "/Humanoid_SpearAnimationSet.asset" },
            { WeaponType.DualBlade,   HumanoidMotionSetRoot + "/Humanoid_DualBladeAnimationSet.asset" },
        };

        public void Execute(BuildContext ctx)
        {
            if (ctx == null || ctx.RootInstance == null)
            {
                Debug.LogWarning("[P09Builder] ApplyWeaponStep: ctx or RootInstance is null");
                return;
            }

            Apply(ctx.RootInstance, ctx.Config, logResult: true, ctx.PrefabName);
            ApplyEnemyDrawnWeaponState(ctx.RootInstance, ctx.Config);
            ApplyMotionSet(ctx);
        }

        public static void Apply(GameObject root, CharacterBuildConfig config, bool logResult = false, string contextName = null)
        {
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            Apply(root, config, catalog, logResult, contextName);
        }

        public static void Apply(GameObject root, CharacterBuildConfig config, P09AssetCatalog catalog,
            bool logResult = false, string contextName = null)
        {
            if (root == null || config == null || catalog == null) return;

            var allTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            var rootTransform = root.transform;
            int sexId = config.Sex == BuilderSex.Male ? MaleSexId : FemaleSexId;

            if (config.UseWeaponGroup && config.WeaponGroupSo != null)
            {
                ClearWeaponList(allTransforms, catalog.Swords, SwordRoots);
                ClearWeaponList(allTransforms, catalog.SubSwords, SubSwordRoots);
                ClearWeaponList(allTransforms, catalog.GreatSwords, GreatSwordRoots);
                ClearWeaponList(allTransforms, catalog.Shields, ShieldRoots);
                ClearWeaponList(allTransforms, catalog.Bows, BowRoots);
                ClearWeaponList(allTransforms, catalog.Staves, StaffRoots);
                ClearWeaponList(allTransforms, catalog.Spears, SpearRoots);
                ClearWeaponList(allTransforms, catalog.DualAxes, DualAxeRoots);
                ClearWeaponList(allTransforms, catalog.Whips, WhipRoots);

                // WeaponGroup: 같은 WeaponGroupId를 가진 무기 모두 활성화
                var groupData = config.WeaponGroupSo as WeaponGroupData;
                if (groupData != null)
                {
                    int groupId = groupData.WeaponGroupId;

                    ApplyWeaponGroupCandidates(allTransforms, catalog.Swords, groupId, sexId, rootTransform, SwordRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.SubSwords, groupId, sexId, rootTransform, SubSwordRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.GreatSwords, groupId, sexId, rootTransform, GreatSwordRoots);

                    if (!groupData.IsUnEquippedShield)
                        ApplyWeaponGroupCandidates(allTransforms, catalog.Shields, groupId, sexId, rootTransform, ShieldRoots);

                    ApplyWeaponGroupCandidates(allTransforms, catalog.Bows, groupId, sexId, rootTransform, BowRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.Staves, groupId, sexId, rootTransform, StaffRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.Spears, groupId, sexId, rootTransform, SpearRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.DualAxes, groupId, sexId, rootTransform, DualAxeRoots);
                    ApplyWeaponGroupCandidates(allTransforms, catalog.Whips, groupId, sexId, rootTransform, WhipRoots);
                }
            }
            else
            {
                // 개별 무기 적용
                ApplyWeaponList(allTransforms, catalog.Swords, GetId(config.SwordSo), sexId, rootTransform, SwordRoots);
                ApplyWeaponList(allTransforms, catalog.SubSwords, GetId(config.SubSwordSo), sexId, rootTransform, SubSwordRoots);
                ApplyWeaponList(allTransforms, catalog.GreatSwords, GetId(config.GreatSwordSo), sexId, rootTransform, GreatSwordRoots);
                ApplyWeaponList(allTransforms, catalog.Shields, GetId(config.ShieldSo), sexId, rootTransform, ShieldRoots);
                ApplyWeaponList(allTransforms, catalog.Bows, GetId(config.BowSo), sexId, rootTransform, BowRoots);
                ApplyWeaponList(allTransforms, catalog.Staves, GetId(config.StaffSo), sexId, rootTransform, StaffRoots);
                ApplyWeaponList(allTransforms, catalog.Spears, GetId(config.SpearSo), sexId, rootTransform, SpearRoots);
                ApplyWeaponList(allTransforms, catalog.DualAxes, GetId(config.DualAxeSo), sexId, rootTransform, DualAxeRoots);
                ApplyWeaponList(allTransforms, catalog.Whips, GetId(config.WhipSo), sexId, rootTransform, WhipRoots);
            }

            if (logResult)
                Debug.Log($"[P09Builder] WeaponApplier 적용 완료: {contextName}");
        }

        private static int GetId(ScriptableObject so)
        {
            return (so as IEditPartData)?.ContentId ?? 0;
        }

        private static void ClearWeaponList(Transform[] allTransforms, List<ScriptableObject> catalogList, string[] rootNames)
        {
            ApplyWeaponList(allTransforms, catalogList, 0, 0, null, rootNames);
        }

        private static void ApplyWeaponGroupCandidates(Transform[] allTransforms, List<ScriptableObject> catalogList,
            int groupId, int sexId, Transform root, string[] rootNames)
        {
            var candidates = catalogList?.OfType<WeaponEditPartData>()
                .Where(w => w.WeaponGroupId == groupId)
                .ToList();
            if (candidates == null || candidates.Count == 0) return;

            ApplyWeaponList(allTransforms, catalogList, candidates[0].ContentId, sexId, root, rootNames);
        }

        private static void ApplyWeaponList(Transform[] allTransforms, List<ScriptableObject> catalogList, int selectedId, int sexId, Transform root, string[] rootNames)
        {
            if (catalogList == null || catalogList.Count == 0) return;

            foreach (var so in catalogList)
            {
                var data = so as IEditPartData;
                if (data == null || string.IsNullOrEmpty(data.MeshName)) continue;

                foreach (var t in allTransforms)
                {
                    if (t == null) continue;
                    if (!IsUnderAnyRoot(t, rootNames)) continue;

                    if (t.name == data.MeshName)
                    {
                        bool active = data.ContentId == selectedId;
                        t.gameObject.SetActive(active);
                        if (active) EnsureAncestorsActive(t, root);
                    }
                    else
                    {
                        try
                        {
                            var maleName = string.Format(data.MeshName, "Male");
                            if (t.name == maleName)
                            {
                                bool active = sexId == MaleSexId && data.ContentId == selectedId;
                                t.gameObject.SetActive(active);
                                if (active) EnsureAncestorsActive(t, root);
                                continue;
                            }
                            var femaleName = string.Format(data.MeshName, "Female");
                            var femName = string.Format(data.MeshName, "Fem");
                            if (t.name == femaleName || t.name == femName)
                            {
                                bool active = sexId == FemaleSexId && data.ContentId == selectedId;
                                t.gameObject.SetActive(active);
                                if (active) EnsureAncestorsActive(t, root);
                            }
                        }
                        catch (System.FormatException)
                        {
                            // MeshName에 placeholder 없는 경우 무시
                        }
                    }
                }
            }
        }

        private static bool IsUnderAnyRoot(Transform t, string[] rootNames)
        {
            if (rootNames == null || rootNames.Length == 0)
                return true;

            var current = t.parent;
            while (current != null)
            {
                for (int i = 0; i < rootNames.Length; i++)
                {
                    if (current.name == rootNames[i])
                        return true;
                }

                current = current.parent;
            }

            return false;
        }

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

        private static void ApplyMotionSet(BuildContext ctx)
        {
            if (ctx == null || ctx.RootInstance == null || ctx.Config == null)
                return;

            var selectedWeaponType = ResolveSelectedWeaponType(ctx.Config);
            ApplyDefaultWeaponType(ctx.RootInstance, ctx.Config, selectedWeaponType);

            if (ctx.Config.ActorKind == BuilderActorKind.Player)
            {
                ApplyPlayerMotionSet(ctx);
                return;
            }

            ApplyActorMotionSet(ctx.RootInstance, selectedWeaponType);
        }

        private static void ApplyEnemyDrawnWeaponState(GameObject root, CharacterBuildConfig config)
        {
            if (root == null || config == null || config.ActorKind != BuilderActorKind.Enemy)
                return;

            var weaponRoot = WeaponAttachmentResolver.FindWeaponRoot(root.transform);
            if (weaponRoot == null)
            {
                Debug.LogWarning($"[P09Builder] Enemy 무기 발도 상태 적용 실패: 'Weapon' 루트를 찾지 못했습니다. ({root.name})");
                return;
            }

            var applied = 0;
            var constraints = weaponRoot.GetComponentsInChildren<ParentConstraint>(includeInactive: true);
            foreach (var constraint in constraints)
            {
                if (constraint == null || constraint.sourceCount < 2)
                    continue;

                if (!constraint.gameObject.activeInHierarchy)
                    continue;

                Undo.RecordObject(constraint, "Apply Enemy Drawn Weapon State");
                SetSourceWeight(constraint, 0, 1f);
                SetSourceWeight(constraint, 1, 0f);
                constraint.constraintActive = true;
                EditorUtility.SetDirty(constraint);
                applied++;
            }

            if (applied == 0)
                Debug.LogWarning($"[P09Builder] Enemy 무기 발도 상태 적용 대상 ParentConstraint를 찾지 못했습니다. ({root.name})");
        }

        private static void SetSourceWeight(ParentConstraint constraint, int index, float weight)
        {
            var source = constraint.GetSource(index);
            source.weight = weight;
            constraint.SetSource(index, source);
        }

        private static void ApplyPlayerMotionSet(BuildContext ctx)
        {
            var animator = ctx.RootInstance.GetComponentInChildren<PlayerActorAnimator>(includeInactive: true);
            if (animator == null)
                animator = Undo.AddComponent<PlayerActorAnimator>(ctx.RootInstance);

            var playerMotionSet = ScriptableObject.CreateInstance<PlayerActorAnimationMotionSet>();
            playerMotionSet.motionSets = new AYellowpaper.SerializedCollections.SerializedDictionary<WeaponType, ActorAnimationMotionSet>();

            foreach (var pair in HumanoidMotionSetPaths)
            {
                var motionSet = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(pair.Value);
                if (motionSet == null)
                {
                    Debug.LogWarning($"[P09Builder] Humanoid MotionSet을 찾을 수 없습니다: {pair.Key} ({pair.Value})");
                    continue;
                }

                playerMotionSet.motionSets[pair.Key] = motionSet;
            }

            if (playerMotionSet.motionSets.Count == 0)
            {
                Object.DestroyImmediate(playerMotionSet);
                return;
            }

            var dataFolder = PathConfig.GetGeneratedDataFolder(typeof(PlayerActorAnimationMotionSet));
            // 기존 세트는 제자리 갱신해 프리팹과 외부 에셋이 가진 GUID 참조를 보존한다.
            playerMotionSet = PathConfig.CreateOrUpdateAsset(
                playerMotionSet,
                dataFolder,
                $"{ctx.PrefabName}_PlayerWeaponAnimationSet",
                out string assetPath,
                out bool created,
                ctx);
            ctx.GeneratedDescs.Add(playerMotionSet);
            if (created)
                ctx.GeneratedAssetPaths.Add(assetPath);

            ReflectionUtil.SetField(animator, "_playerActorAnimationMotionSet", playerMotionSet);
            EditorUtility.SetDirty(animator);
            Debug.Log($"[P09Builder] PlayerActorAnimationMotionSet 연결 완료: {assetPath}");
        }

        private static void ApplyActorMotionSet(GameObject root, WeaponType selectedWeaponType)
        {
            var animator = root.GetComponentInChildren<ActorAnimator>(includeInactive: true);
            if (animator == null)
                animator = Undo.AddComponent<ActorAnimator>(root);

            var motionSet = LoadHumanoidMotionSet(selectedWeaponType);
            if (motionSet == null)
            {
                Debug.LogWarning($"[P09Builder] {selectedWeaponType}용 Humanoid MotionSet을 찾지 못해 ActorAnimator MotionSet을 설정하지 못했습니다.");
                return;
            }

            ReflectionUtil.SetField(animator, "_motionSet", motionSet);
            EditorUtility.SetDirty(animator);
            Debug.Log($"[P09Builder] ActorAnimator MotionSet 연결 완료: {selectedWeaponType} -> {motionSet.name}");
        }

        private static ActorAnimationMotionSet LoadHumanoidMotionSet(WeaponType weaponType)
        {
            if (!HumanoidMotionSetPaths.TryGetValue(weaponType, out var path))
                return null;

            return AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(path);
        }

        private static void ApplyDefaultWeaponType(GameObject root, CharacterBuildConfig config, WeaponType selectedWeaponType)
        {
            if (root == null || config == null) return;

            var modelData = root.GetComponentInChildren<CharacterModelData>(includeInactive: true);
            if (modelData == null)
            {
                Debug.LogWarning("[P09Builder] CharacterModelData를 찾지 못해 defaultWeaponType을 설정하지 못했습니다.");
                return;
            }

            var definition = modelData.Definition;
            if (definition == null
                && config.PlayerCharacterType != CharacterActorType.None)
            {
                string definitionPath =
                    "Assets/10.Datas/Party/PlayerCharacters/" +
                    $"PlayerCharacterDefinition_{config.PlayerCharacterType}.asset";
                definition = AssetDatabase.LoadAssetAtPath<
                    UPlayGround.Data.Party.PlayerCharacterDefinitionSO>(
                    definitionPath);
                if (definition != null)
                    modelData.AssignDefinition(definition);
            }

            if (definition == null)
            {
                Debug.LogWarning(
                    "[P09Builder] PlayerCharacterDefinitionSO가 없어 " +
                    "기본 무기 타입을 설정하지 못했습니다.");
                return;
            }

            definition.defaultWeaponType = selectedWeaponType;
            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(modelData);
        }

        private static WeaponType ResolveSelectedWeaponType(CharacterBuildConfig config)
        {
            if (config == null)
                return WeaponType.NoWeapon;

            if (config.GreatSwordSo != null) return WeaponType.GreatSword;
            if (config.BowSo != null)        return WeaponType.Bow;
            if (config.StaffSo != null)      return WeaponType.Staff;
            if (config.SpearSo != null)      return WeaponType.Spear;
            if (config.DualAxeSo != null)    return WeaponType.DoubleAxe;
            if (config.WhipSo != null)       return WeaponType.Whip;
            if (config.SwordSo != null && config.ShieldSo != null)
                return WeaponType.SwordShield;
            if (config.SwordSo != null && config.SubSwordSo != null)
                return WeaponType.DualBlade;
            if (config.SwordSo != null)      return WeaponType.Katana;
            if (config.ShieldSo != null)     return WeaponType.SwordShield;
            if (config.SubSwordSo != null)   return WeaponType.DualBlade;

            return WeaponType.NoWeapon;
        }
    }
}
