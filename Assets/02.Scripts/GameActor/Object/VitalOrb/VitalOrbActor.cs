using System;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 회복 구슬 오브젝트 런타임 동작.
    /// Idle(부유) → Attract(흡입) → Collect(습득) / Expire(소멸) 의 단순 상태 전이.
    /// </summary>
    public class VitalOrbActor : MonoBehaviour
    {
        private VitalOrbDataSO _data;
        private float _healScale = 1f;
        private Action<VitalOrbActor, FinishReason> _onFinished;

        private State _state = State.Idle;
        private float _lifetimeTimer;
        private float _spawnY;
        private bool _finished;

        private const float FadeOutDuration = 0.5f;
        private float _fadeTimer;
        private Renderer[] _renderers;
        private MaterialPropertyBlock[] _propertyBlocks;
        private Color[] _baseColors;
        private bool[] _supportsBaseColor;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private enum State { Idle, Attract, Collect, Expire }
        public enum FinishReason { Collected, Expired }

        public void Initialize(VitalOrbDataSO data, Action<VitalOrbActor, FinishReason> onFinished, float healScale = 1f)
        {
            _data = data;
            _healScale = Mathf.Max(0f, healScale);
            _onFinished = onFinished;
            _spawnY = transform.position.y;
            _state = State.Idle;
            _lifetimeTimer = 0f;
            _fadeTimer = 0f;
            _finished = false;

            CacheRendererData();
            SetAlpha(1f);
        }

        public void ResetForPool()
        {
            _data = null;
            _healScale = 1f;
            _onFinished = null;
            _state = State.Idle;
            _lifetimeTimer = 0f;
            _fadeTimer = 0f;
            _finished = false;
            SetAlpha(1f);
        }

        public void Tick(float deltaTime, Vector3 playerPosition, bool hasPlayer)
        {
            if (_data == null || _finished)
                return;

            switch (_state)
            {
                case State.Idle:
                    UpdateIdle(deltaTime, playerPosition, hasPlayer);
                    break;
                case State.Attract:
                    UpdateAttract(deltaTime, playerPosition, hasPlayer);
                    break;
                case State.Expire:
                    UpdateExpire(deltaTime);
                    break;
            }
        }

        private void UpdateIdle(float deltaTime, Vector3 playerPosition, bool hasPlayer)
        {
            ApplyFloatAnimation();

            _lifetimeTimer += deltaTime;
            if (_lifetimeTimer >= _data.lifetime)
            {
                EnterExpire();
                return;
            }

            if (!hasPlayer || _lifetimeTimer < _data.attractDelay)
                return;

            float dist = HorizontalDistance(transform.position, playerPosition);
            if (dist <= _data.collectRadius)
                _state = State.Attract;
        }

        private void UpdateAttract(float deltaTime, Vector3 playerPosition, bool hasPlayer)
        {
            if (!hasPlayer)
            {
                _state = State.Idle;
                return;
            }

            Vector3 toPlayer = playerPosition - transform.position;
            float dist = toPlayer.magnitude;

            if (dist <= _data.collectDistance)
            {
                EnterCollect();
                return;
            }

            float t = 1f - Mathf.Clamp01(dist / _data.collectRadius);
            float minSpeed = _data.minAttractSpeed > 0f ? _data.minAttractSpeed : _data.attractSpeed * 0.3f;
            float maxSpeed = _data.maxAttractSpeed > 0f ? _data.maxAttractSpeed : _data.attractSpeed;
            float speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.SmoothStep(0f, 1f, t));

            transform.position += toPlayer.normalized * (speed * deltaTime);
        }

        private void UpdateExpire(float deltaTime)
        {
            _fadeTimer += deltaTime;
            float alpha = 1f - Mathf.Clamp01(_fadeTimer / FadeOutDuration);
            SetAlpha(alpha);

            if (_fadeTimer >= FadeOutDuration)
                Finish(FinishReason.Expired);
        }

        private void EnterCollect()
        {
            _state = State.Collect;

            ApplyRewards();

            ActorSvc.Objects.ShowFX(_data.collectParticleName, transform.position);

            if (!string.IsNullOrWhiteSpace(_data.collectSoundName))
                Svc.Sound?.PlayUi(_data.collectSoundName);

            Finish(FinishReason.Collected);
        }

        private void EnterExpire()
        {
            _state = State.Expire;
            _fadeTimer = 0f;
        }

        private void Finish(FinishReason reason)
        {
            if (_finished)
                return;

            _finished = true;
            _onFinished?.Invoke(this, reason);
        }

        private void ApplyRewards()
        {
            var player = ActorSvc.Objects.Player;
            if (player == null) return;

            if (_data.healAmount > 0f)
            player.ApplyPercentHealingEffect(_data.healAmount * _healScale);

            if (player.SkillGauge != null)
                player.SkillGauge.AddGauge(_data.gaugeAmount);
        }

        private void ApplyFloatAnimation()
        {
            float offsetY = Mathf.Sin(Time.time * _data.floatSpeed * Mathf.PI * 2f) * _data.floatAmplitude;
            var pos = transform.position;
            pos.y = _spawnY + offsetY;
            transform.position = pos;
        }

        private void SetAlpha(float alpha)
        {
            if (_renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (!_supportsBaseColor[i])
                    continue;

                var color = _baseColors[i];
                color.a = alpha;
                _propertyBlocks[i].SetColor(BaseColorId, color);
                _renderers[i].SetPropertyBlock(_propertyBlocks[i]);
            }
        }

        private void CacheRendererData()
        {
            if (_renderers != null)
                return;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _propertyBlocks = new MaterialPropertyBlock[_renderers.Length];
            _baseColors = new Color[_renderers.Length];
            _supportsBaseColor = new bool[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _propertyBlocks[i] = new MaterialPropertyBlock();

                var material = _renderers[i].sharedMaterial;
                _supportsBaseColor[i] = material != null && material.HasProperty(BaseColorId);
                _baseColors[i] = _supportsBaseColor[i] ? material.GetColor(BaseColorId) : Color.white;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
