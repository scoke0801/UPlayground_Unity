#if UNITY_EDITOR
using UnityEditor;
using UPlayGround.Animation.Editor;
using UPlayGround.Tool.Editor.Party;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 여러 상위 카테고리에 흩어진 자동 생성 도구를 Generator Tool 메뉴에 모아 노출한다.
    /// 통합 데이터 허브에 포함되지 않은 독립 생성 도구의 접근성용 메뉴를 제공한다.
    /// </summary>
    public static class GeneratorToolMenu
    {
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/ID Enum 생성기", false, 10)]
        private static void OpenIdEnumGenerator()
            => IdEnumGeneratorWindow.Open();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/파티 성장 에디터", false, 32)]
        private static void OpenPartyGrowthEditor()
            => PartyGrowthEditorWindow.Open();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/메인 스토리 생성기", false, 40)]
        private static void OpenMainStoryGenerator()
            => MainStoryGeneratorWindow.ShowWindow();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/서브 스토리 생성기", false, 41)]
        private static void OpenSubStoryGenerator()
            => SubStoryGeneratorWindow.ShowWindow();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/로코모션 모션 설정", false, 50)]
        private static void OpenLocomotionMotionSetup()
            => LocoMotionSetupWindow.Open();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/생성 도구/카메라 흔들림 프리셋", false, 60)]
        private static void GenerateCameraShakePresets()
            => CameraShakePresetGenerator.Generate();
    }
}
#endif
