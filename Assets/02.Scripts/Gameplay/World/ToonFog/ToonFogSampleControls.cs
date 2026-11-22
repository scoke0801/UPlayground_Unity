using UnityEngine;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Gameplay.World.ToonFog
{
    /// <summary>샘플 씬의 네 비교 구역과 테스트 입력을 제공한다.</summary>
    public sealed class ToonFogSampleControls : MonoBehaviour, IToonFogEnvironmentSource
    {
        public ToonFogWorld world;
        public Camera sampleCamera;
        public Transform[] viewpoints;
        public ToonFogProfile[] profiles;
        public bool showControls = true;
        [Range(0, 2)] public float density = 1;
        [Range(0, 1)] public float combat;
        public Vector2 wind;
        private static readonly string[] ViewLabels = { "거리", "계곡과 부피", "재질과 물", "지역 경계", "계곡 위" };
        private int _viewIndex, _profileIndex;

        public bool TryGetInput(Camera camera, out ToonFogEnvironmentInput input)
        {
            input = ToonFogEnvironmentInput.Identity;
            input.DensityMultiplier = density;
            input.CombatWeight = combat;
            input.WindVelocityXZ = wind;
            return true;
        }

        /// <summary>이전 구역의 안개 전환을 초기화하고 정해진 카메라 구도로 이동한다.</summary>
        public void SelectView(int index)
        {
            if (index < 0 || index >= viewpoints.Length) return;
            _viewIndex = index;
            sampleCamera.transform.SetPositionAndRotation(viewpoints[index].position, viewpoints[index].rotation);
            world.captureLegacyRefraction = index == 2;
            world.ResetTransition();
        }

        private void OnGUI()
        {
            if (!showControls || world == null) return;
            GUILayout.BeginArea(new Rect(16, 16, 396, 232), GUI.skin.box);
            GUILayout.Label("툰 안개 비교");
            world.fogEnabled = GUILayout.Toggle(world.fogEnabled, "안개 사용");
            int view = GUILayout.SelectionGrid(_viewIndex, ViewLabels, 3);
            if (view != _viewIndex) SelectView(view);
            if (profiles != null && profiles.Length > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("이전 지역색")) SelectProfile((_profileIndex + profiles.Length - 1) % profiles.Length);
                GUILayout.Label(world.profile != null ? world.profile.name : "프로필 없음");
                if (GUILayout.Button("다음 지역색")) SelectProfile((_profileIndex + 1) % profiles.Length);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label($"날씨 밀도 {density:F2}");
            density = GUILayout.HorizontalSlider(density, 0, 2);
            GUILayout.Label($"전투 가중치 {combat:F2}");
            combat = GUILayout.HorizontalSlider(combat, 0, 1);
            GUILayout.EndArea();
        }

        private void SelectProfile(int index)
        {
            _profileIndex = index;
            world.profile = profiles[index];
        }
    }
}
