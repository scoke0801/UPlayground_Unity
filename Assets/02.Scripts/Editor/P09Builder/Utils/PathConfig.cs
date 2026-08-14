using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Editor.P09Builder
{
    public static class PathConfig
    {
        public const string BasePrefabMaleMagica =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/P09_Human_Variant_Male.prefab";

        public const string BasePrefabFemaleMagica =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/P09_Human_Variant_Female.prefab";

        public const string BasePrefabMaleNoPhysics =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/No_MagicaCloth/P09_Human_No_Physics_Male Variant.prefab";

        public const string BasePrefabFemaleNoPhysics =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/No_MagicaCloth/P09_Human_No_Physics_Female Variant.prefab";

        public const string CatalogRoot =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Scenes/DemoScene_Data/ScriptableObject";

        public const string IconRoot256 =
            "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Scenes/DemoScene_Data/Icons_Equipment/256";

        public const string CharactersRoot = "Assets/03.Prefabs/Characters";
        public const string ActorDefinitionRoot = "Assets/10.Datas/Actor/DataBase";
        public const string EnemyBehaviorDataRoot = "Assets/10.Datas/Actor/Enemy/BehaviorData";
        public const string EnemyPoiseDataRoot = "Assets/10.Datas/Actor/Enemy/PoiseData";
        public const string NpcDataRoot = "Assets/10.Datas/Actor/Npc";
        public const string PlayerGeneratedMotionSetRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Generated";
        public const string GeneratedDataRoot = "Assets/10.Datas/Actor/Generated";

        public const string SequenceFilePath = "Library/P09Builder/sequence.json";

        public static string GetBasePrefabPath(BuilderSex sex, bool useMagicaCloth)
        {
            if (useMagicaCloth)
                return sex == BuilderSex.Male ? BasePrefabMaleMagica : BasePrefabFemaleMagica;
            return sex == BuilderSex.Male ? BasePrefabMaleNoPhysics : BasePrefabFemaleNoPhysics;
        }

        public static string GetPrefabFolder(string kind, string name)
        {
            return $"{CharactersRoot}/{kind}/{name}";
        }

        public static string GetPrefabFolder(string baseFolder, string kind, string name)
        {
            string root = string.IsNullOrWhiteSpace(baseFolder)
                ? CharactersRoot
                : baseFolder.TrimEnd('/', '\\').Replace('\\', '/');
            return $"{root}/{kind}/{name}";
        }

        public static string GetDescFolder(string kind, string name)
        {
            return $"{CharactersRoot}/{kind}/{name}/Descs";
        }

        public static string GetGeneratedDataFolder(System.Type assetType)
        {
            if (assetType == typeof(ActorDefinitionSO))
                return ActorDefinitionRoot;
            if (assetType == typeof(EnemyBehaviorSO))
                return EnemyBehaviorDataRoot;
            if (assetType == typeof(PoiseSO))
                return EnemyPoiseDataRoot;
            if (assetType == typeof(NpcActorSO))
                return NpcDataRoot;
            if (assetType == typeof(PlayerActorAnimationMotionSet))
                return PlayerGeneratedMotionSetRoot;

            // 그 외 ScriptableObject 및 미지정 타입은 공용 Generated 폴더로 폴백.
            return GeneratedDataRoot;
        }

        /// <summary>
        /// 결정적(고정) 경로로 에셋을 생성하거나 같은 타입의 기존 에셋을 제자리 갱신한다.
        /// 기존 객체를 삭제하지 않으므로 GUID와 외부 참조가 유지된다.
        /// </summary>
        public static T CreateOrUpdateAsset<T>(
            T asset,
            string folder,
            string fileName,
            out string path,
            out bool created,
            BuildContext context = null)
            where T : UnityEngine.Object
        {
            if (asset == null)
                throw new BuildException("생성 또는 갱신할 에셋이 null입니다.");

            EnsureFolderExists(folder);

            path = fileName.EndsWith(".asset") ? $"{folder}/{fileName}" : $"{folder}/{fileName}.asset";
            UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (existing != null)
            {
                if (existing.GetType() != asset.GetType())
                {
                    string requestedType = asset.GetType().Name;
                    UnityEngine.Object.DestroyImmediate(asset);
                    throw new BuildException(
                        $"고정 경로에 다른 타입의 에셋이 있습니다: {path} " +
                        $"({existing.GetType().Name} != {requestedType})");
                }

                string existingName = existing.name;
                context?.StageAssetForUpdate(existing);
                Undo.RecordObject(existing, $"P09 Builder: Update {existingName}");
                EditorUtility.CopySerialized(asset, existing);
                existing.name = existingName;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(asset);
                created = false;
                return (T)existing;
            }

            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        public static void EnsureFolderExists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        public static void EnsureSystemFolderExists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }
}
