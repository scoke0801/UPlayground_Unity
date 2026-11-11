using UnityEngine;
using UPlayGround.Data.Pet;

namespace UPlayGround.Components.Pet
{
    /// <summary>공유 머티리얼을 교환하며 숨김과 기본 표정을 복원한다.</summary>
    public sealed class PetPresentation : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer _body;
        [SerializeField] private GameObject _visualRoot;
        [Min(0.01f)]
        [SerializeField] private float _modelScale = 0.25f;

        private void Awake() => ApplyModelScale();

        /// <summary>이동 루트와 독립적으로 펫 모델의 표시 크기를 적용한다.</summary>
        public void ApplyModelScale()
        {
            if (_visualRoot != null) _visualRoot.transform.localScale = Vector3.one * Mathf.Max(0.01f, _modelScale);
        }

        private void OnValidate() => ApplyModelScale();
        /// <summary>서비스나 게임 액터 없이 프리뷰 모델의 대기 모션만 재생한다.</summary>
        public static void PlayPreviewIdle(GameObject model, AnimationClip clip)
        {
            var animancer = model.GetComponentInChildren<Animancer.AnimancerComponent>(true);
            if (animancer != null && clip != null)
            { animancer.Play(clip); animancer.Evaluate(0f); }
        }
        public void ApplyAppearance(PetAppearance appearance)
        {
            if (_body == null || appearance == null) return;
            Material[] materials = _body.sharedMaterials;
            if (materials.Length > 0 && appearance.body != null) materials[0] = appearance.body;
            if (materials.Length > 1 && appearance.face != null) materials[1] = appearance.face;
            _body.sharedMaterials = materials;
        }
        public void SetVisible(bool visible)
        {
            if (_visualRoot != null && _visualRoot.activeSelf != visible) _visualRoot.SetActive(visible);
        }
    }
}
