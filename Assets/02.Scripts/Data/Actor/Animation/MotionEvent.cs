using System;
using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 모션 이벤트 기본 추상 클래스
    /// 모든 이벤트는 이 클래스를 상속받아야 함
    /// </summary>
    [Serializable]
    public abstract class MotionEventBase
    {
        public float startTime;
        public float endTime;

        /// <summary>
        /// 이벤트가 특정 시간에 활성화되는지 확인
        /// </summary>
        public bool IsActiveAt(float time) => time >= startTime && time <= endTime;

        /// <summary>
        /// 이벤트의 표시 이름 (에디터용)
        /// </summary>
        public abstract string GetDisplayName();

        /// <summary>
        /// 이벤트의 짧은 설명 (타임라인 바에 표시)
        /// </summary>
        public virtual string GetShortLabel() => GetDisplayName();

        /// <summary>
        /// 이벤트 실행 (런타임)
        /// </summary>
        public abstract void Execute(GameObject target);
        public abstract void OnCompleteEvent(GameObject target);
    }

    // ====================================================================
    //  구체적인 이벤트 타입들
    // ====================================================================

    /// <summary>
    /// 파티클 이펙트 재생 이벤트
    /// </summary>
    [Serializable]
    public class BeginParticleEvent : MotionEventBase
    {
        public GameObject particlePrefab;
        public Vector3 offset;
        public bool attachToTarget = true;

        private GameObject _instance;
        public override string GetDisplayName() => "Particle";

        public override string GetShortLabel()
        {
            if (particlePrefab != null)
                return $"Particle: {particlePrefab.name}";
            return "Particle: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (particlePrefab == null) return;

            if (attachToTarget)
            {
                _instance = GameObject.Instantiate(particlePrefab, target.transform);
                _instance.transform.localPosition = offset;
            }
            else
            {
                _instance = GameObject.Instantiate(particlePrefab);
                _instance.transform.position = target.transform.position + offset;
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (_instance != null)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }
    }

    /// <summary>
    /// 카메라 쉐이크 이벤트
    /// </summary>
    [Serializable]
    public class BeginCameraShakeEvent : MotionEventBase
    {
        public float intensity = 1f;
        public float frequency = 10f;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        public override string GetDisplayName() => "Camera Shake";

        public override string GetShortLabel() => $"Shake: {intensity:F1}";

        public override void Execute(GameObject target)
        {
            // 실제 구현은 카메라 매니저 연동 필요
            Debug.Log($"Camera Shake: Intensity={intensity}, Frequency={frequency}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 충돌 판정 활성화 이벤트
    /// </summary>
    [Serializable]
    public class BeginCollisionEvent : MotionEventBase
    {
        public string colliderName;
        public float damage = 10f;
        public LayerMask targetLayers = -1;

        public override string GetDisplayName() => "Collision";

        public override string GetShortLabel()
        {
            if (!string.IsNullOrEmpty(colliderName))
                return $"Collision: {colliderName}";
            return "Collision";
        }

        public override void Execute(GameObject target)
        {
            // 충돌 판정 로직 구현
            Debug.Log($"Collision Active: {colliderName}, Damage={damage}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 사운드 재생 이벤트
    /// </summary>
    [Serializable]
    public class PlaySoundEvent : MotionEventBase
    {
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool is3D = true;

        public override string GetDisplayName() => "Sound";

        public override string GetShortLabel()
        {
            if (audioClip != null)
                return $"Sound: {audioClip.name}";
            return "Sound: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (audioClip == null) return;

            if (is3D)
                AudioSource.PlayClipAtPoint(audioClip, target.transform.position, volume);
            else
                Debug.Log($"Play 2D Sound: {audioClip.name}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 애니메이션 속도 변경 이벤트
    /// </summary>
    [Serializable]
    public class AnimationSpeedEvent : MotionEventBase
    {
        public float speedMultiplier = 1f;
        public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public override string GetDisplayName() => "Anim Speed";

        public override string GetShortLabel() => $"Speed: {speedMultiplier:F2}x";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Animation Speed: {speedMultiplier}x");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 무적 상태 이벤트
    /// </summary>
    [Serializable]
    public class InvincibilityEvent : MotionEventBase
    {
        public bool canCancelByInput = false;

        public override string GetDisplayName() => "Invincibility";

        public override string GetShortLabel() => "Invincible";

        public override void Execute(GameObject target)
        {
            Debug.Log("Invincibility Active");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 발자국 이벤트 (지형별 사운드)
    /// </summary>
    [Serializable]
    public class FootstepEvent : MotionEventBase
    {
        public enum Foot { Left, Right }
        public Foot foot;
        [Range(0f, 1f)] public float volume = 0.5f;

        public override string GetDisplayName() => "Footstep";

        public override string GetShortLabel() => $"Foot: {foot}";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Footstep: {foot}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    public class SpawnProjectileEvent : MotionEventBase
    {
        public GameObject projectilePrefab;
        public Transform spawnPoint;
        public Vector3 direction = Vector3.forward;
        public float speed = 10f;

        public override string GetDisplayName() => "Projectile";

        public override string GetShortLabel()
        {
            if (projectilePrefab != null)
                return $"Spawn: {projectilePrefab.name}";
            return "Projectile: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (projectilePrefab == null) return;

            var pos = spawnPoint != null ? spawnPoint.position : target.transform.position;
            var rot = target.transform.rotation;
            var instance = GameObject.Instantiate(projectilePrefab, pos, rot);

            var rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = target.transform.TransformDirection(direction) * speed;
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 타임스케일 조작 이벤트 (슬로우 모션 등)
    /// </summary>
    [Serializable]
    public class TimeScaleEvent : MotionEventBase
    {
        [Range(0.01f, 2f)] public float timeScale = 0.5f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public override string GetDisplayName() => "Time Scale";

        public override string GetShortLabel() => $"Time: {timeScale:F2}x";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Time Scale: {timeScale}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

    /// <summary>
    /// 커스텀 콜백 이벤트
    /// </summary>
    [Serializable]
    public class CustomCallbackEvent : MotionEventBase
    {
        public string callbackName;
        public string[] parameters;

        public override string GetDisplayName() => "Callback";

        public override string GetShortLabel()
        {
            if (!string.IsNullOrEmpty(callbackName))
                return $"Call: {callbackName}";
            return "Callback";
        }

        public override void Execute(GameObject target)
        {
            Debug.Log($"Callback: {callbackName}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}