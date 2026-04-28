#if UNITY_EDITOR
using UnityEditor;
using UPlayGround.Animation.Editor;
using UPlayGround.Tool.Editor.Stat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 여러 상위 카테고리에 흩어진 자동 생성 도구를 Generator Tool 메뉴에 모아 노출한다.
    /// 기존 메뉴 경로는 유지하고, 접근성용 alias만 제공한다.
    /// </summary>
    public static class GeneratorToolMenu
    {
        [MenuItem("UPlayGround/Generator Tool/ID Enum Generator", false, 10)]
        private static void OpenIdEnumGenerator()
            => IdEnumGeneratorWindow.Open();

        [MenuItem("UPlayGround/Generator Tool/Item Data Generator", false, 15)]
        private static void OpenItemDataGenerator()
            => EditorApplication.ExecuteMenuItem("UPlayGround/Item/Item Data Generator");

        [MenuItem("UPlayGround/Generator Tool/Recipe Data Generator", false, 20)]
        private static void OpenRecipeDataGenerator()
            => RecipeDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/Generator Tool/Stat Data Generator", false, 30)]
        private static void OpenStatDataGenerator()
            => StatDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/Generator Tool/Validate Stat Data Coverage", false, 31)]
        private static void ValidateStatDataCoverage()
            => StatDataGeneratorWindow.ValidateStatDataCoverageMenu();

        [MenuItem("UPlayGround/Generator Tool/NPC Data Generator", false, 35)]
        private static void OpenNpcDataGenerator()
            => EditorApplication.ExecuteMenuItem("UPlayGround/NPC/NPC Data Generator");

        [MenuItem("UPlayGround/Generator Tool/Main Story Generator", false, 40)]
        private static void OpenMainStoryGenerator()
            => MainStoryGeneratorWindow.ShowWindow();

        [MenuItem("UPlayGround/Generator Tool/Sub Story Generator", false, 41)]
        private static void OpenSubStoryGenerator()
            => SubStoryGeneratorWindow.ShowWindow();

        [MenuItem("UPlayGround/Generator Tool/Locomotion Motion Setup", false, 50)]
        private static void OpenLocomotionMotionSetup()
            => LocoMotionSetupWindow.Open();

        [MenuItem("UPlayGround/Generator Tool/Camera Shake Presets", false, 60)]
        private static void GenerateCameraShakePresets()
            => CameraShakePresetGenerator.Generate();
    }
}
#endif
