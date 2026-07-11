#if UNITY_EDITOR
using UnityEditor;
using UPlayGround.Animation.Editor;
using UPlayGround.Data.Item.Editor;
using UPlayGround.Data.Crafting.Editor;
using UPlayGround.Tool.Editor.Actor;
using UPlayGround.Tool.Editor.Party;
using UPlayGround.Tool.Editor.Stat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 여러 상위 카테고리에 흩어진 자동 생성 도구를 Generator Tool 메뉴에 모아 노출한다.
    /// 기존 메뉴 경로는 유지하고, 접근성용 alias만 제공한다.
    /// </summary>
    public static class GeneratorToolMenu
    {
        [MenuItem("UPlayGround/생성 도구/ID Enum 생성기", false, 10)]
        private static void OpenIdEnumGenerator()
            => IdEnumGeneratorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/아이템 데이터 생성기", false, 15)]
        private static void OpenItemDataGenerator()
            => ItemDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/레시피 데이터 생성기", false, 20)]
        private static void OpenRecipeDataGenerator()
            => RecipeDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/스탯 데이터 생성기", false, 30)]
        private static void OpenStatDataGenerator()
            => StatDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/스탯 데이터 커버리지 검증", false, 31)]
        private static void ValidateStatDataCoverage()
            => StatDataGeneratorWindow.ValidateStatDataCoverageMenu();

        [MenuItem("UPlayGround/생성 도구/파티 성장 에디터", false, 32)]
        private static void OpenPartyGrowthEditor()
            => PartyGrowthEditorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/NPC 데이터 생성기", false, 35)]
        private static void OpenNpcDataGenerator()
            => NpcDataGeneratorWindow.Open();

        [MenuItem("UPlayGround/생성 도구/메인 스토리 생성기", false, 40)]
        private static void OpenMainStoryGenerator()
            => MainStoryGeneratorWindow.ShowWindow();

        [MenuItem("UPlayGround/생성 도구/서브 스토리 생성기", false, 41)]
        private static void OpenSubStoryGenerator()
            => SubStoryGeneratorWindow.ShowWindow();

        [MenuItem("UPlayGround/생성 도구/로코모션 모션 설정", false, 50)]
        private static void OpenLocomotionMotionSetup()
            => LocoMotionSetupWindow.Open();

        [MenuItem("UPlayGround/생성 도구/카메라 흔들림 프리셋", false, 60)]
        private static void GenerateCameraShakePresets()
            => CameraShakePresetGenerator.Generate();
    }
}
#endif
