using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>모션 시계에 맞춰 독립 형상을 이동시켜 공격 속도가 바뀌어도 접지와 판정을 맞춘다.</summary>
    [Serializable]
    [MotionEventDescriptor("TimedProp", "VFX / SFX", 0, "모션 구간에 맞춰 형상의 위치와 크기를 보간합니다.", "prop", "형상", "소품")]
    public sealed class TimedPropEvent : MotionEventBase, IMotionEventTick
    {
        public GameObject prefab;
        public Vector3 startOffset;
        public Vector3 endOffset;
        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;
        public Vector3 rotationOffset;
        public AnimationCurve positionProgress = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("모션 정규화 시간에 따른 소멸량. 비워 두면 재질 속성을 변경하지 않습니다.")]
        public AnimationCurve dissolveAmount;
        public string dissolveProperty = "_DissolveAmount";

        private struct ActiveProp
        {
            public Transform instance;
            public Renderer[] renderers;
            public MaterialPropertyBlock properties;
            public int dissolvePropertyId;
        }

        [NonSerialized] private readonly Dictionary<int, ActiveProp> _instances = new(2);

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "모션 동기 형상";

        public override void Execute(GameObject target)
        {
            if (target == null || prefab == null || SignatureMotionEventTarget.ResolvePlayer(target) == null) return;
            OnCompleteEvent(target);
            Transform instance = UnityEngine.Object.Instantiate(prefab, target.transform).transform;
            instance.localRotation = Quaternion.Euler(rotationOffset);
            var active = new ActiveProp { instance = instance };
            if (dissolveAmount != null && dissolveAmount.length > 0 && !string.IsNullOrEmpty(dissolveProperty))
            {
                active.renderers = instance.GetComponentsInChildren<Renderer>(true);
                active.properties = new MaterialPropertyBlock();
                active.dissolvePropertyId = Shader.PropertyToID(dissolveProperty);
            }
            _instances[target.GetInstanceID()] = active;
            Tick(target, 0, 0);
        }

        public void Tick(GameObject target, float normalizedTime, float deltaTime)
        {
            if (target == null || !_instances.TryGetValue(target.GetInstanceID(), out ActiveProp active) || active.instance == null) return;
            Transform instance = active.instance;
            float normalized = Mathf.Clamp01(normalizedTime);
            float progress = Mathf.Clamp01(positionProgress.Evaluate(normalized));
            instance.localPosition = Vector3.Lerp(startOffset, endOffset, progress);
            instance.localScale = Vector3.Lerp(startScale, endScale, progress);
            if (active.properties == null || dissolveAmount == null) return;
            float amount = Mathf.Clamp01(dissolveAmount.Evaluate(normalized));
            for (int i = 0; i < active.renderers.Length; i++)
            {
                Renderer renderer = active.renderers[i];
                if (renderer == null) continue;
                // 다른 인스턴스 속성은 유지하고 공유 재질에는 쓰지 않는다.
                renderer.GetPropertyBlock(active.properties);
                active.properties.SetFloat(active.dissolvePropertyId, amount);
                renderer.SetPropertyBlock(active.properties);
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (target == null || !_instances.TryGetValue(target.GetInstanceID(), out ActiveProp active)) return;
            _instances.Remove(target.GetInstanceID());
            if (active.instance != null) UnityEngine.Object.Destroy(active.instance.gameObject);
        }
    }
}
