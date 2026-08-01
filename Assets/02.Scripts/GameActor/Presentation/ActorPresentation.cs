using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>
    /// 액터의 시뮬레이션 오브젝트를 비활성화하지 않고 표현 렌더러만 숨긴다.
    /// 중첩 요청은 참조 카운트로 관리하고 최초 요청 전 상태를 마지막 해제 때 복구한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorPresentation : MonoBehaviour
    {
        private readonly Dictionary<Renderer, bool> _rendererStates = new();
        private int _hideRequestCount;

        public bool IsHidden => _hideRequestCount > 0;

        public bool TryGetOriginalForceRenderingOff(Renderer renderer, out bool value)
            => _rendererStates.TryGetValue(renderer, out value);

        public void Hide()
        {
            if (_hideRequestCount++ > 0)
                return;

            _rendererStates.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                HideRenderer(renderer);
            }
        }

        public void Show()
        {
            if (_hideRequestCount <= 0)
                return;

            _hideRequestCount--;
            if (_hideRequestCount > 0)
                return;

            foreach (KeyValuePair<Renderer, bool> pair in _rendererStates)
            {
                if (pair.Key != null)
                    pair.Key.forceRenderingOff = pair.Value;
            }

            _rendererStates.Clear();
            RefreshCameraProximityVisibility();
        }

        private void OnDisable()
        {
            ForceRestore();
        }

        private void OnDestroy()
        {
            ForceRestore();
        }

        private void ForceRestore()
        {
            foreach (KeyValuePair<Renderer, bool> pair in _rendererStates)
            {
                if (pair.Key != null)
                    pair.Key.forceRenderingOff = pair.Value;
            }

            _rendererStates.Clear();
            _hideRequestCount = 0;
            RefreshCameraProximityVisibility();
        }

        private void OnTransformChildrenChanged()
        {
            if (!IsHidden)
                return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                HideRenderer(renderers[i]);
        }

        private void HideRenderer(Renderer renderer)
        {
            if (renderer == null || _rendererStates.ContainsKey(renderer))
                return;

            _rendererStates.Add(renderer, renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }

        private void RefreshCameraProximityVisibility()
        {
            ActorCameraProximityDither proximityDither =
                GetComponent<ActorCameraProximityDither>();
            proximityDither?.RefreshVisibilitySuppression();
        }
    }
}
