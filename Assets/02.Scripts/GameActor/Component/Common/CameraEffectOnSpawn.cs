using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.Components
{
    /// <summary>착탄 FX 생성 시 효과 SO를 실행해 공격 속도와 무관하게 실제 착탄에 연출을 맞춘다.</summary>
    [DisallowMultipleComponent]
    public sealed class CameraEffectOnSpawn : MonoBehaviour
    {
        public List<CameraEffectData> effects = new();
        private readonly List<ICameraEffect> _handles = new(4);
        private CameraManager _camera;
        private bool _played;

        private void OnEnable()
        {
            if (!Application.isPlaying || _played || !gameObject.activeInHierarchy) return;
            if (!Services.TryGet<ICameraViewService>(out var view) || view is not CameraManager camera) return;
            _camera = camera;
            _played = true;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null) _handles.Add(camera.PlayEffect(effects[i]));
        }

        private void OnDisable()
        {
            if (_camera != null)
                for (int i = 0; i < _handles.Count; i++)
                    if (_handles[i] != null && !_handles[i].IsFinished) _camera.StopEffect(_handles[i], immediate: true);
            _handles.Clear();
            _camera = null;
            _played = false;
        }
    }
}
