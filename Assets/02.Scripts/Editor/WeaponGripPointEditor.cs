using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// WeaponGripPoint 세팅 보조 에디터 (Phase 0 마찰 완화 — 설계서 §12).
    ///
    /// 그립 마커를 "눈대중"이 아니라 보조손 본 기준으로 정렬하도록 돕는다.
    ///   - 씬뷰: 그립 자세 축 기즈모 + 보조손 본까지의 시안 점선/거리 표시.
    ///   - 인스펙터: "보조손 본 위치로 스냅" 버튼으로 시작 자세를 본에 맞춘 뒤 미세조정.
    /// 본 탐지는 무기의 부모 계층 Animator 우선, 없으면 씬의 첫 휴머노이드 Animator 폴백.
    /// (Handles/Undo 패턴은 WeaponSlashVfxSpawnerEditor를 미러링)
    /// </summary>
    [CustomEditor(typeof(WeaponGripPoint))]
    public sealed class WeaponGripPointEditor : UnityEditor.Editor
    {
        private Animator _animator;

        private void OnEnable() => ResolveAnimator();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var grip = (WeaponGripPoint)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("세팅 보조", EditorStyles.boldLabel);

            Transform bone = ResolveBone(grip, out string boneInfo);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(bone == null))
                {
                    if (GUILayout.Button("보조손 본 위치로 스냅", GUILayout.Height(24)))
                        SnapToBone(grip, bone);
                }

                if (GUILayout.Button("본 재탐색", GUILayout.Width(80), GUILayout.Height(24)))
                    ResolveAnimator();
            }

            if (bone == null)
            {
                EditorGUILayout.HelpBox(
                    "보조손 본을 찾지 못했습니다. 씬에 휴머노이드 플레이어가 있고 무기가 그 자식 계층에 있거나, " +
                    "씬에 휴머노이드 Animator가 존재할 때 자동 탐지됩니다. 본 없이도 씬뷰 이동/회전 도구로 직접 배치할 수 있습니다.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"대상 보조손 본: {boneInfo}\n씬뷰 시안 점선 = 그립↔본 거리. 스냅 후 무기 메시에 맞춰 미세조정하세요.",
                    MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            var grip = (WeaponGripPoint)target;
            Transform t = grip.transform;
            float h = HandleUtility.GetHandleSize(t.position);

            // 그립 자세 축 기즈모 + 라벨
            DrawBasis(t.position, t.rotation, 0.4f);
            Handles.color = Color.white;
            Handles.Label(t.position + t.rotation * Vector3.up * h * 0.4f, "Off-hand Grip");

            // 보조손 본 참조선 + 거리(cm)
            Transform bone = ResolveBone(grip, out _);
            if (bone != null)
            {
                Handles.color = Color.cyan;
                Handles.DrawDottedLine(t.position, bone.position, 4f);
                Handles.SphereHandleCap(0, bone.position, Quaternion.identity,
                    HandleUtility.GetHandleSize(bone.position) * 0.05f, EventType.Repaint);
                Handles.Label(Vector3.Lerp(t.position, bone.position, 0.5f),
                    $"{Vector3.Distance(t.position, bone.position) * 100f:0.0} cm");
            }
        }

        private Transform ResolveBone(WeaponGripPoint grip, out string info)
        {
            info = string.Empty;
            if (_animator == null || !_animator.isHuman)
                return null;

            bool left = grip.gripHand != EquipPosition.RightHand; // 기본 LeftHand
            Transform bone = _animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (bone != null)
                info = $"{(left ? "LeftHand" : "RightHand")} ({_animator.name})";
            return bone;
        }

        private void ResolveAnimator()
        {
            var grip = target as WeaponGripPoint;
            if (grip == null) return;

            // 무기가 플레이어에 부착된 상태면 부모 계층에서 바로 찾음.
            _animator = grip.GetComponentInParent<Animator>();
            if (_animator != null && _animator.isHuman)
                return;

            // 프리팹/미부착 편집 상태 폴백: 씬의 첫 휴머노이드.
            _animator = null;
            foreach (var a in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (a.isHuman) { _animator = a; break; }
            }
        }

        private static void SnapToBone(WeaponGripPoint grip, Transform bone)
        {
            Undo.RecordObject(grip.transform, "Snap Weapon Grip To Bone");
            grip.transform.SetPositionAndRotation(bone.position, bone.rotation);
            EditorUtility.SetDirty(grip.transform);
        }

        private static void DrawBasis(Vector3 origin, Quaternion rotation, float size)
        {
            float h = HandleUtility.GetHandleSize(origin) * size;
            Handles.color = Color.red;
            Handles.DrawLine(origin, origin + rotation * Vector3.right * h);
            Handles.color = Color.green;
            Handles.DrawLine(origin, origin + rotation * Vector3.up * h);
            Handles.color = Color.blue;
            Handles.DrawLine(origin, origin + rotation * Vector3.forward * h);
        }
    }
}
