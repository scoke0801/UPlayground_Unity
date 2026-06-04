using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.Data.Editor
{
    public static class CombatCameraProfileDatabaseEditorUtility
    {
        private const string DefaultRootPath = "Assets/10.Datas/Camera/CombatCamera";
        private const string DefaultEffectPath = DefaultRootPath + "/Effects";
        private const string DefaultDatabasePath = DefaultRootPath + "/CombatCameraProfileDatabase.asset";

        private struct ProfileDef
        {
            public CombatCameraIntentType intentType;
            public CameraShakeIdType shakeKey;
            public int priority;
            public bool requireAttackerGrade;
            public MonsterActorGrade attackerGrade;
            public bool requireVictimGrade;
            public MonsterActorGrade victimGrade;
            public float triggerChance;
            public bool usePunch;
            public float punchStrength;
            public float punchDuration;
            public bool softTargetAssist;
            public float fovDelta;
            public float zoomDistanceDelta;
            public string suffix;
        }

        private static readonly ProfileDef[] DefaultProfiles =
        {
            new() { intentType = CombatCameraIntentType.LightHit, shakeKey = CameraShakeIdType.LiteHit, triggerChance = 1f, usePunch = true, punchStrength = 0.08f, punchDuration = 0.07f, softTargetAssist = true, fovDelta = 0.75f },
            new() { intentType = CombatCameraIntentType.HeavyHit, shakeKey = CameraShakeIdType.HeavyHit, triggerChance = 1f, usePunch = true, punchStrength = 0.18f, punchDuration = 0.12f, softTargetAssist = true, fovDelta = 1.5f, zoomDistanceDelta = -0.08f },
            new() { intentType = CombatCameraIntentType.SkillHit, shakeKey = CameraShakeIdType.CriticalHit, triggerChance = 1f, usePunch = true, punchStrength = 0.24f, punchDuration = 0.15f, softTargetAssist = true, fovDelta = 2.0f, zoomDistanceDelta = -0.12f },
            new() { intentType = CombatCameraIntentType.ChargeHit, shakeKey = CameraShakeIdType.HeavyHit, triggerChance = 1f, usePunch = true, punchStrength = 0.22f, punchDuration = 0.15f, softTargetAssist = true, fovDelta = 2.0f, zoomDistanceDelta = -0.1f },
            new() { intentType = CombatCameraIntentType.DashHit, shakeKey = CameraShakeIdType.MediumHit, triggerChance = 1f, usePunch = true, punchStrength = 0.14f, punchDuration = 0.1f, softTargetAssist = true, fovDelta = 1.25f },
            new() { intentType = CombatCameraIntentType.PlayerDamaged, shakeKey = CameraShakeIdType.PlayerHit, triggerChance = 1f, usePunch = false, fovDelta = 0.75f },
            new() { intentType = CombatCameraIntentType.PlayerHeavyDamaged, shakeKey = CameraShakeIdType.PlayerHeavyHit, triggerChance = 1f, usePunch = false, fovDelta = 1.5f },
            new() { intentType = CombatCameraIntentType.PlayerDeath, shakeKey = CameraShakeIdType.PlayerDeath, triggerChance = 1f, usePunch = false, fovDelta = 2.25f },
            new() { intentType = CombatCameraIntentType.PerfectGuard, shakeKey = CameraShakeIdType.CriticalHit, triggerChance = 1f, usePunch = true, punchStrength = 0.14f, punchDuration = 0.14f, softTargetAssist = true, fovDelta = 1.75f, zoomDistanceDelta = -0.08f },
            new() { intentType = CombatCameraIntentType.PerfectDodge, shakeKey = CameraShakeIdType.LiteHit, triggerChance = 1f, usePunch = true, punchStrength = 0.06f, punchDuration = 0.09f, softTargetAssist = true, fovDelta = 1.25f },
            new() { intentType = CombatCameraIntentType.DodgeCounter, shakeKey = CameraShakeIdType.MediumHit, triggerChance = 1f, usePunch = true, punchStrength = 0.14f, punchDuration = 0.1f, softTargetAssist = true, fovDelta = 1.5f, zoomDistanceDelta = -0.08f },
            new() { intentType = CombatCameraIntentType.Kill, shakeKey = CameraShakeIdType.KillCam, priority = 1, requireVictimGrade = true, victimGrade = MonsterActorGrade.Weak, triggerChance = 0.15f, usePunch = false, fovDelta = 1.25f, zoomDistanceDelta = -0.08f, suffix = "Kill_Weak" },
            new() { intentType = CombatCameraIntentType.Kill, shakeKey = CameraShakeIdType.KillCam, priority = 2, requireVictimGrade = true, victimGrade = MonsterActorGrade.Normal, triggerChance = 0.25f, usePunch = false, fovDelta = 1.5f, zoomDistanceDelta = -0.12f, suffix = "Kill_Normal" },
            new() { intentType = CombatCameraIntentType.Kill, shakeKey = CameraShakeIdType.KillCam, priority = 3, requireVictimGrade = true, victimGrade = MonsterActorGrade.Elite, triggerChance = 0.60f, usePunch = false, fovDelta = 2.0f, zoomDistanceDelta = -0.18f, suffix = "Kill_Elite" },
            new() { intentType = CombatCameraIntentType.Kill, shakeKey = CameraShakeIdType.KillCam, priority = 4, requireVictimGrade = true, victimGrade = MonsterActorGrade.Boss, triggerChance = 1.00f, usePunch = false, fovDelta = 2.5f, zoomDistanceDelta = -0.25f, suffix = "Kill_Boss" },
        };

        [MenuItem("UPlayGround/월드/카메라/전투 카메라 프로필 DB 생성", false, UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera + 2)]
        public static void CreateOrUpdateDefaultDatabase()
        {
            EnsureDirectory(DefaultRootPath);
            EnsureDirectory(DefaultEffectPath);

            CombatCameraProfileDatabaseSO database = AssetDatabase.LoadAssetAtPath<CombatCameraProfileDatabaseSO>(DefaultDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<CombatCameraProfileDatabaseSO>();
                AssetDatabase.CreateAsset(database, DefaultDatabasePath);
            }

            Undo.RecordObject(database, "Update Combat Camera Profile Database");
            if (database.profiles == null)
                database.profiles = new List<CombatCameraProfileSO>();

            int created = 0;
            int reused = 0;
            foreach (ProfileDef def in DefaultProfiles)
            {
                CombatCameraProfileSO profile = FindProfile(database, def);
                if (profile == null)
                {
                    profile = CreateProfile(def);
                    database.profiles.Add(profile);
                    created++;
                }
                else
                {
                    EnsureDefaultEffects(profile, def);
                    reused++;
                }
            }

            EditorUtility.SetDirty(database);
            EnsureAddressable(database, CombatCameraProfileDatabaseSO.AddressableKey);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            EditorUtility.DisplayDialog(
                "Combat Camera Profile Database",
                $"생성: {created}개 / 기존 유지: {reused}개\nAddressables Key: {CombatCameraProfileDatabaseSO.AddressableKey}",
                "확인");
        }

        [MenuItem("UPlayGround/월드/카메라/전투 카메라 프로필 DB 검증", false, UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera + 3)]
        public static void ValidateDefaultDatabase()
        {
            CombatCameraProfileDatabaseSO database = AssetDatabase.LoadAssetAtPath<CombatCameraProfileDatabaseSO>(DefaultDatabasePath);
            if (database == null)
            {
                EditorUtility.DisplayDialog("Combat Camera Profile Database", "기본 경로에 데이터베이스가 없습니다.", "확인");
                return;
            }

            ValidateDatabase(database, true);
        }

        public static bool ValidateDatabase(CombatCameraProfileDatabaseSO database, bool showDialog)
        {
            List<string> messages = new List<string>();
            if (database == null)
            {
                messages.Add("데이터베이스 참조가 없습니다.");
                return Report(messages, showDialog);
            }

            if (database.profiles == null || database.profiles.Count == 0)
            {
                messages.Add("등록된 프로필이 없습니다.");
                return Report(messages, showDialog);
            }

            HashSet<CombatCameraIntentType> covered = new HashSet<CombatCameraIntentType>();
            Dictionary<string, CombatCameraProfileSO> selectedByIntentPriority = new Dictionary<string, CombatCameraProfileSO>();

            for (int i = 0; i < database.profiles.Count; i++)
            {
                CombatCameraProfileSO profile = database.profiles[i];
                if (profile == null)
                {
                    messages.Add($"profiles[{i}]가 비어 있습니다.");
                    continue;
                }

                covered.Add(profile.intentType);
                string key = $"{profile.intentType}:{profile.priority}:{profile.requireAttackerMonsterGrade}:{profile.attackerMonsterGrade}:{profile.requireVictimMonsterGrade}:{profile.victimMonsterGrade}";
                if (selectedByIntentPriority.ContainsKey(key))
                    messages.Add($"{profile.intentType} priority {profile.priority} 프로필이 중복됩니다.");
                else
                    selectedByIntentPriority.Add(key, profile);

                if (!profile.HasPlayableContent())
                    messages.Add($"{profile.name}: 재생 가능한 내용이 없습니다.");

                if (profile.effects != null && profile.effects.Exists(effect => effect == null))
                    messages.Add($"{profile.name}: effects 목록에 null 항목이 있습니다.");

            }

            foreach (CombatCameraIntentType type in System.Enum.GetValues(typeof(CombatCameraIntentType)))
            {
                if (type == CombatCameraIntentType.None)
                    continue;

                if (!covered.Contains(type))
                    messages.Add($"{type} 프로필이 없습니다.");
            }

            if (!IsAddressable(database, CombatCameraProfileDatabaseSO.AddressableKey))
                messages.Add($"Addressables Key '{CombatCameraProfileDatabaseSO.AddressableKey}' 등록이 필요합니다.");

            return Report(messages, showDialog);
        }

        public static void EnsureAddressable(UnityEngine.Object asset, string address)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[CombatCameraProfileDatabaseEditorUtility] Addressables Settings를 찾지 못해 주소 등록을 건너뜁니다.");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            EditorUtility.SetDirty(settings);
        }

        private static CombatCameraProfileSO CreateProfile(ProfileDef def)
        {
            CombatCameraProfileSO profile = ScriptableObject.CreateInstance<CombatCameraProfileSO>();
            ApplyDefaultValues(profile, def);

            string path = $"{DefaultRootPath}/CCP_{(string.IsNullOrWhiteSpace(def.suffix) ? def.intentType.ToString() : def.suffix)}.asset";
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static void ApplyDefaultValues(CombatCameraProfileSO profile, ProfileDef def)
        {
            Undo.RecordObject(profile, "Update Combat Camera Profile");
            profile.intentType = def.intentType;
            profile.priority = def.priority;
            profile.requireAttackerMonsterGrade = def.requireAttackerGrade;
            profile.attackerMonsterGrade = def.attackerGrade;
            profile.requireVictimMonsterGrade = def.requireVictimGrade;
            profile.victimMonsterGrade = def.victimGrade;
            profile.triggerChance = def.triggerChance <= 0f ? 1f : Mathf.Clamp01(def.triggerChance);
            profile.shakeKey = def.shakeKey;
            profile.usePunch = def.usePunch;
            profile.punchStrength = def.punchStrength;
            profile.punchDuration = def.punchDuration;
            profile.enableSoftTargetAssist = def.softTargetAssist;
            profile.softTargetYawDuration = 0.12f;
            profile.manualInputSuppressDuration = 0.45f;

            if (profile.effects == null)
                profile.effects = new List<CameraEffectData>();

            AddEffectIfMissing(profile, CreateOrUpdateFovEffect(def, true));
            AddEffectIfMissing(profile, CreateOrUpdateZoomEffect(def, true));
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureDefaultEffects(CombatCameraProfileSO profile, ProfileDef def)
        {
            if (profile == null)
                return;

            Undo.RecordObject(profile, "Ensure Combat Camera Profile Effects");
            if (profile.effects == null)
                profile.effects = new List<CameraEffectData>();

            AddEffectIfMissing(profile, CreateOrUpdateFovEffect(def, false));
            AddEffectIfMissing(profile, CreateOrUpdateZoomEffect(def, false));
            EditorUtility.SetDirty(profile);
        }

        private static FOVCameraEffectData CreateOrUpdateFovEffect(ProfileDef def, bool overwriteExisting)
        {
            if (Mathf.Approximately(def.fovDelta, 0f))
                return null;

            string effectName = string.IsNullOrWhiteSpace(def.suffix) ? def.intentType.ToString() : def.suffix;
            string path = $"{DefaultEffectPath}/CCE_{effectName}_FOV.asset";
            FOVCameraEffectData effect = AssetDatabase.LoadAssetAtPath<FOVCameraEffectData>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<FOVCameraEffectData>();
                AssetDatabase.CreateAsset(effect, path);
            }
            else if (!overwriteExisting)
            {
                return effect;
            }

            effect.effectKey = $"CombatCamera.{def.intentType}.FOV";
            effect.priority = 10;
            effect.duration = 0.16f;
            effect.blendInDuration = 0.02f;
            effect.blendOutDuration = 0.12f;
            effect.fovDelta = def.fovDelta;
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static ZoomCameraEffectData CreateOrUpdateZoomEffect(ProfileDef def, bool overwriteExisting)
        {
            if (Mathf.Approximately(def.zoomDistanceDelta, 0f))
                return null;

            string effectName = string.IsNullOrWhiteSpace(def.suffix) ? def.intentType.ToString() : def.suffix;
            string path = $"{DefaultEffectPath}/CCE_{effectName}_Zoom.asset";
            ZoomCameraEffectData effect = AssetDatabase.LoadAssetAtPath<ZoomCameraEffectData>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<ZoomCameraEffectData>();
                AssetDatabase.CreateAsset(effect, path);
            }
            else if (!overwriteExisting)
            {
                return effect;
            }

            effect.effectKey = $"CombatCamera.{def.intentType}.Zoom";
            effect.priority = 10;
            effect.duration = 0.18f;
            effect.blendInDuration = 0.03f;
            effect.blendOutDuration = 0.12f;
            effect.distanceDelta = def.zoomDistanceDelta;
            effect.offsetDelta = Vector3.zero;
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static void AddEffectIfMissing(CombatCameraProfileSO profile, CameraEffectData effect)
        {
            if (profile == null || effect == null)
                return;

            if (!profile.effects.Contains(effect))
                profile.effects.Add(effect);
        }

        private static CombatCameraProfileSO FindProfile(CombatCameraProfileDatabaseSO database, ProfileDef def)
        {
            if (database == null || database.profiles == null)
                return null;

            for (int i = 0; i < database.profiles.Count; i++)
            {
                CombatCameraProfileSO profile = database.profiles[i];
                if (profile != null
                    && profile.intentType == def.intentType
                    && profile.priority == def.priority
                    && profile.requireAttackerMonsterGrade == def.requireAttackerGrade
                    && profile.attackerMonsterGrade == def.attackerGrade
                    && profile.requireVictimMonsterGrade == def.requireVictimGrade
                    && profile.victimMonsterGrade == def.victimGrade)
                    return profile;
            }

            return null;
        }

        private static bool IsAddressable(UnityEngine.Object asset, string address)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings != null && !string.IsNullOrEmpty(guid)
                ? settings.FindAssetEntry(guid)
                : null;

            return entry != null && entry.address == address;
        }

        private static bool Report(List<string> messages, bool showDialog)
        {
            bool hasIssue = messages != null && messages.Count > 0;
            string report = hasIssue ? string.Join("\n", messages) : "문제가 없습니다.";
            if (hasIssue)
                Debug.LogWarning("[CombatCameraProfileDatabase] 검증 결과\n" + report);
            else
                Debug.Log("[CombatCameraProfileDatabase] 검증 통과");

            if (showDialog)
                EditorUtility.DisplayDialog("Combat Camera Profile Database", report, "확인");

            return !hasIssue;
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folder))
                return;

            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    [CustomEditor(typeof(CombatCameraProfileDatabaseSO))]
    public class CombatCameraProfileDatabaseSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Addressables 키 등록"))
            {
                CombatCameraProfileDatabaseEditorUtility.EnsureAddressable(
                    target,
                    CombatCameraProfileDatabaseSO.AddressableKey);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("프로필 검증"))
                CombatCameraProfileDatabaseEditorUtility.ValidateDatabase((CombatCameraProfileDatabaseSO)target, true);
        }
    }
}
