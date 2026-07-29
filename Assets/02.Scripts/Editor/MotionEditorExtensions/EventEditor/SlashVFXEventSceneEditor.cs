using System;
using UPlayGround.Data.Event;
using UPlayGround.Particle;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// SlashVFX 이벤트의 발화 포즈를 Scene View에서 직접 조정한다.
    /// </summary>
    public sealed class SlashVFXEventSceneEditor : IMotionEventSceneEditor
    {
        private bool _enabled;

        public Type EventType => typeof(SlashVFXEvent);

        public void OnInspectorGUI(
            MotionEventBase motionEvent,
            IMotionEditorContext context)
        {
            if (motionEvent is not SlashVFXEvent slash)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Slash VFX Scene Tune",
                    EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _enabled = GUILayout.Toggle(
                        _enabled,
                        "Scene Tune",
                        EditorStyles.miniButton,
                        GUILayout.Width(92f));

                    using (new EditorGUI.DisabledScope(
                               context.Subject?.Root == null ||
                               !Application.isPlaying))
                    {
                        if (GUILayout.Button("이벤트 프레임으로"))
                            context.SetPlaybackTime(
                                FindGlobalStart(
                                    context.CurrentSet,
                                    slash));
                        if (GUILayout.Button("VFX 미리보기"))
                            slash.Execute(context.Subject.Root);
                    }
                }

                EditorGUILayout.HelpBox(
                    "Scene Tune을 켜면 청록색 위치 핸들과 회전 핸들로 이벤트 오프셋을 편집할 수 있습니다.",
                    MessageType.None);
            }
        }

        public bool OnSceneGUI(
            MotionEventBase motionEvent,
            IMotionEditorContext context)
        {
            if (!_enabled ||
                motionEvent is not SlashVFXEvent slash ||
                context.Subject?.Root == null)
                return false;

            GameObject root = context.Subject.Root;
            WeaponSlashVfxSpawner spawner = ResolveSpawner(root, slash);
            if (spawner == null ||
                spawner.BladeBase == null ||
                spawner.BladeTip == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(12f, 52f, 390f, 42f), EditorStyles.helpBox);
                GUILayout.Label("SlashVFX Scene Tune: Blade Base/Tip을 찾지 못했습니다.");
                GUILayout.EndArea();
                Handles.EndGUI();
                return false;
            }

            bool worldPosition =
                slash.positionSpace == SlashVFXPositionSpace.World;
            bool worldRotation =
                slash.rotationSpace == SlashVFXRotationSpace.World;
            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(
                    spawner.BladeBase,
                    spawner.BladeTip,
                    root.transform,
                    slash.positionOffset,
                    worldPosition,
                    slash.rotationOffset,
                    worldRotation,
                    root.transform.rotation,
                    out Vector3 position,
                    out Quaternion rotation))
                return false;

            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(
                    spawner.BladeBase,
                    spawner.BladeTip,
                    root.transform,
                    Vector3.zero,
                    worldPosition,
                    Vector3.zero,
                    worldRotation,
                    root.transform.rotation,
                    out Vector3 origin,
                    out Quaternion basisRotation))
                return false;

            bool changed = false;
            using (new Handles.DrawingScope(Color.cyan))
            {
                Handles.DrawDottedLine(origin, position, 3f);
                EditorGUI.BeginChangeCheck();
                Vector3 nextPosition = Handles.PositionHandle(
                    position,
                    worldPosition ? root.transform.rotation : basisRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    context.RecordUndo("SlashVFX 위치 오프셋 편집");
                    Quaternion basis = worldPosition
                        ? root.transform.rotation
                        : basisRotation;
                    slash.positionOffset =
                        Quaternion.Inverse(basis) * (nextPosition - origin);
                    changed = true;
                }

                EditorGUI.BeginChangeCheck();
                Quaternion nextRotation = Handles.RotationHandle(
                    rotation,
                    position);
                if (EditorGUI.EndChangeCheck())
                {
                    context.RecordUndo("SlashVFX 회전 오프셋 편집");
                    Quaternion basis = worldRotation
                        ? root.transform.rotation
                        : basisRotation;
                    slash.rotationOffset =
                        (Quaternion.Inverse(basis) * nextRotation).eulerAngles;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(context.Asset);
                context.Repaint();
            }
            return changed;
        }

        private static WeaponSlashVfxSpawner ResolveSpawner(
            GameObject root,
            SlashVFXEvent slash)
        {
            WeaponSlashVfxSpawner[] spawners =
                root.GetComponentsInChildren<WeaponSlashVfxSpawner>(true);
            if (string.IsNullOrEmpty(slash.spawnerObjectName))
                return spawners.Length > 0 ? spawners[0] : null;

            foreach (WeaponSlashVfxSpawner spawner in spawners)
            {
                if (spawner.name == slash.spawnerObjectName ||
                    spawner.transform.parent != null &&
                    spawner.transform.parent.name == slash.spawnerObjectName)
                    return spawner;
            }
            return null;
        }

        private static float FindGlobalStart(
            MotionSet set,
            SlashVFXEvent target)
        {
            if (set == null)
                return target.startTime;
            if (set.globalEvents != null && set.globalEvents.Contains(target))
                return target.startTime;

            float offset = 0f;
            if (set.motions != null)
            {
                foreach (Motion motion in set.motions)
                {
                    if (motion?.events != null && motion.events.Contains(target))
                        return offset + target.startTime;
                    offset += motion?.Duration ?? 0f;
                }
            }
            return target.startTime;
        }
    }
}
