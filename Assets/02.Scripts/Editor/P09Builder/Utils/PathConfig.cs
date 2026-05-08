using System.IO;
using UnityEditor;

namespace Game.Editor.P09Builder
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

        public static string GetDescFolder(string kind, string name)
        {
            return $"{CharactersRoot}/{kind}/{name}/Descs";
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
