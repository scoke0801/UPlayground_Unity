#if UNITY_EDITOR
using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 애니메이션 에디터 SlashVFX Scene Tune 값을 Game View Gizmos에도 표시한다.
    /// Game View 우상단 Gizmos 토글이 켜져 있어야 보인다.
    /// 순수 에디터 툴링 컴포넌트이므로 빌드에 포함되지 않도록 UNITY_EDITOR로 격리한다.
    /// </summary>
    public sealed class SlashVfxSceneTuneGizmoOverlay : MonoBehaviour
    {
        static GameObject _target;
        static bool _visible;
        static bool _showBlade;
        static bool _showWorld;
        static Vector3 _bladeBase;
        static Vector3 _bladeTip;
        static Vector3 _center;
        static Vector3 _spawnPosition;
        static Vector3 _bladeOffsetPosition;
        static Vector3 _worldOffsetPosition;
        static Quaternion _bladeRotation;
        static Quaternion _actorRootRotation;
        static Quaternion _rotationBase;
        static Quaternion _vfxRotation;
        static string _rotationMode;
        static readonly Vector2 ScreenOffset = new Vector2(16f, 140f);
        static readonly Color BladeColor = new Color(1f, 0.82f, 0.18f, 0.95f);
        static readonly Color WorldColor = new Color(1f, 0.25f, 0.9f, 0.9f);
        static readonly Color SpawnColor = Color.cyan;

        public static void Publish(
            GameObject target,
            bool visible,
            bool showBlade,
            bool showWorld,
            Vector3 bladeBase,
            Vector3 bladeTip,
            Vector3 center,
            Vector3 spawnPosition,
            Vector3 bladeOffsetPosition,
            Vector3 worldOffsetPosition,
            Quaternion bladeRotation,
            Quaternion actorRootRotation,
            Quaternion rotationBase,
            Quaternion vfxRotation,
            string rotationMode)
        {
            _target = target;
            _visible = visible;
            _showBlade = showBlade;
            _showWorld = showWorld;
            _bladeBase = bladeBase;
            _bladeTip = bladeTip;
            _center = center;
            _spawnPosition = spawnPosition;
            _bladeOffsetPosition = bladeOffsetPosition;
            _worldOffsetPosition = worldOffsetPosition;
            _bladeRotation = bladeRotation;
            _actorRootRotation = actorRootRotation;
            _rotationBase = rotationBase;
            _vfxRotation = vfxRotation;
            _rotationMode = rotationMode;
        }

        public static void Clear()
        {
            _target = null;
            _visible = false;
        }

        void OnDrawGizmos()
        {
            if (!_visible || _target == null || _target != gameObject)
                return;

            Gizmos.color = BladeColor;
            Gizmos.DrawLine(_bladeBase, _bladeTip);
            Gizmos.DrawWireSphere(_bladeBase, 0.04f);
            Gizmos.DrawWireSphere(_bladeTip, 0.04f);

            if (_showBlade)
                DrawOffsetSpace(_center, _bladeOffsetPosition, _bladeRotation, BladeColor, 0.42f);

            if (_showWorld)
                DrawOffsetSpace(_center, _worldOffsetPosition, _actorRootRotation, WorldColor, 0.50f);

            Gizmos.color = SpawnColor;
            Gizmos.DrawLine(_center, _spawnPosition);
            Gizmos.DrawWireSphere(_spawnPosition, 0.06f);

            DrawBasis(_spawnPosition, _rotationBase, 0.34f);
            DrawBasis(_spawnPosition + Vector3.up * 0.1f, _vfxRotation, 0.28f);
        }

        void OnGUI()
        {
            if (!_visible || _target == null || _target != gameObject)
                return;

            Rect rect = new Rect(ScreenOffset.x, ScreenOffset.y, 320f, 174f);
            GUILayout.BeginArea(rect, GUI.skin.box);
            DrawLabel("SlashVFX Scene Tune Gizmo", Color.white);
            DrawLabel("Blade Base/Tip + Blade Offset", BladeColor);
            DrawLabel("World / Actor Offset", WorldColor);
            DrawLabel("Runtime Spawn Position", SpawnColor);
            DrawLabel("X Axis", Color.red);
            DrawLabel("Y Axis", Color.green);
            DrawLabel("Z Axis", Color.blue);
            DrawLabel($"Rotation: {_rotationMode}", Color.white);
            GUILayout.EndArea();
        }

        static void DrawLabel(string text, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            GUILayout.Label(text, style);
        }

        static void DrawOffsetSpace(Vector3 origin, Vector3 offsetPosition, Quaternion basis, Color color, float size)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(origin, offsetPosition);
            Gizmos.DrawWireSphere(offsetPosition, 0.055f);
            DrawBasis(origin, basis, size);
        }

        static void DrawBasis(Vector3 origin, Quaternion rotation, float size)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + rotation * Vector3.right * size);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + rotation * Vector3.up * size);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origin, origin + rotation * Vector3.forward * size);
        }
    }
}
#endif
