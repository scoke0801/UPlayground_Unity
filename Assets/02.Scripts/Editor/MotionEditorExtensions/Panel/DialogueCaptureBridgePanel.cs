using UPlayGround.Data.Editor;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed class DialogueCaptureBridgePanel : IMotionEditorPanel
    {
        public string Title => "카메라 촬영";
        public int Order => 500;

        public bool IsAvailable(IMotionEditorContext context) =>
            context?.Asset != null;

        public void OnGUI(IMotionEditorContext context)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("카메라 동기 촬영", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"MotionSet: {context.Asset.name}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"대상: {(context.Subject?.Root != null ? context.Subject.Root.name : "-")}",
                    EditorStyles.miniLabel);

                if (GUILayout.Button(
                        "현재 모션으로 카메라 녹화 열기",
                        GUILayout.Height(24f)))
                {
                    Transform anchor = context.Subject?.Root != null
                        ? context.Subject.Root.transform
                        : null;
                    DialogueCameraRecorderWindow.OpenForMotion(
                        context.Asset,
                        anchor);
                }

                EditorGUILayout.HelpBox(
                    "녹화 창의 동기 촬영은 현재 제네릭 MotionSet 에디터의 재생 시간과 종료 구간을 사용합니다.",
                    MessageType.None);
            }
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
        }

        public void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state)
        {
        }
    }
}
