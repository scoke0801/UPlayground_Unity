using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    [CustomEditor(typeof(MotionSetAsset))]
    public class MotionSetEditor : UnityEditor.Editor
    {
        MotionSetDrawer _drawer;

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(() => target, Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var asset = (MotionSetAsset)target;
            _drawer.DrawFullGUI(asset.motionSet);

            if (GUI.changed)
                EditorUtility.SetDirty(target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}