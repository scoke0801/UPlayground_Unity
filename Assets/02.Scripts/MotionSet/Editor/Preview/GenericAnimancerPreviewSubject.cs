using Animancer;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    internal sealed class GenericAnimancerPreviewSubject : IMotionPreviewSubject
    {
        private readonly GameObject _root;
        private AnimancerComponent _animancer;

        private GenericAnimancerPreviewSubject(GameObject root)
        {
            _root = root;
            Refresh();
        }

        public GameObject Root => _root;
        public AnimancerComponent Animancer => _animancer;
        public IMotionSetCatalog Catalog => null;

        public AvatarMask GetLayerMask(int layerIndex) => null;

        public void Refresh()
        {
            _animancer = _root != null
                ? _root.GetComponentInChildren<AnimancerComponent>(true)
                : null;
        }

        public static IMotionPreviewSubject TryCreate(GameObject root)
        {
            GenericAnimancerPreviewSubject subject = new(root);
            return subject.Animancer != null ? subject : null;
        }
    }
}
