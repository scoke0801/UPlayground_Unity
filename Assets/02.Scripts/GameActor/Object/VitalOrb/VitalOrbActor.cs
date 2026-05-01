using System;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 회복 구슬 오브젝트 런타임 동작.
    /// Idle(부유) → Attract(흡입) → Collect(습득) / Expire(소멸) 의 단순 상태 전이.
    /// </summary>
    public class VitalOrbActor : MonoBehaviour
    {
        // VitalOrbHandler가 카운트 관리에 사용
        public event Action OnExpired;

        private const float AttractDelay = 1.5f; // 1.5초 동안은 흡입되지 않음
        
        private VitalOrbDataSO _data;
        private Action         _onCollect;   // 습득 시 콜백 (VitalOrbHandler의 카운트 감소)

        private Transform _playerTransform;
        private State     _state = State.Idle;

        private float _lifetimeTimer;
        private float _spawnY;     // 부유 기준 Y 위치

        // Expire 페이드 아웃
        private const float FadeOutDuration = 0.5f;
        private float _fadeTimer;
        private Renderer[] _renderers;

        private enum State { Idle, Attract, Collect, Expire }
        // -----------------------------------------------------------
        // 초기화
        // -----------------------------------------------------------
        public void Initialize(VitalOrbDataSO data, Action onCollect)
        {
            _data      = data;
            _onCollect = onCollect;
            _spawnY    = transform.position.y;
            _state     = State.Idle;
            _lifetimeTimer = 0f;
            _fadeTimer     = 0f;

            _renderers = GetComponentsInChildren<Renderer>();
            
            // 플레이어 캐싱
            var playerObj = GameObjectManager.Instance.Player;
            if (playerObj != null)
                _playerTransform = playerObj.GetSocket(ActorSocketType.Center);
        }

        // -----------------------------------------------------------
        // Update
        // -----------------------------------------------------------
        private void Update()
        {
            if (_data == null || _playerTransform == null) return;

            switch (_state)
            {
                case State.Idle:    UpdateIdle();    break;
                case State.Attract: UpdateAttract(); break;
                case State.Expire:  UpdateExpire();  break;
                // Collect는 즉시 처리 후 Destroy되므로 Update 불필요
            }
        }

        private void UpdateIdle()
        {
            ApplyFloatAnimation();

            _lifetimeTimer += Time.deltaTime;
            if (_lifetimeTimer >= _data.lifetime)
            {
                EnterExpire();
                return;
            }
            
            // 딜레이 시간이 지난 이후에만 플레이어와의 거리를 체크하여 Attract 상태로 전환
            float dist = HorizontalDistance(transform.position, _playerTransform.position);
            if (_lifetimeTimer >= AttractDelay)
            {
                if (dist <= _data.collectRadius)
                {
                    _state = State.Attract;
                }
            }
        }

        private void UpdateAttract()
        {
            Vector3 toPlayer = _playerTransform.position - transform.position;
            float dist = toPlayer.magnitude;

            if (dist <= _data.collectDistance)
            {
                EnterCollect();
                return;
            }

            // EaseIn 가속: 처음엔 느리게 출발, 가까워질수록 빠르게
            float t = 1f - Mathf.Clamp01(dist / _data.collectRadius);
            float speed = Mathf.Lerp(_data.attractSpeed * 0.3f, _data.attractSpeed, t);

            transform.position += toPlayer.normalized * (speed * Time.deltaTime);
        }

        private void UpdateExpire()
        {
            _fadeTimer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(_fadeTimer / FadeOutDuration);
            // SetAlpha(alpha);

            if (_fadeTimer >= FadeOutDuration)
            {
                OnExpired?.Invoke();
                Destroy(gameObject);
            }
        }

        // -----------------------------------------------------------
        // 상태 전이
        // -----------------------------------------------------------
        private void EnterCollect()
        {
            _state = State.Collect;

            // 플레이어 HP / 게이지 회복
            ApplyRewards();

            GameObjectManager.Instance.ShowFX(_data.collectParticleName, transform.position);
            // TODO: 사운드 재생 - AudioManager 구현 후 연결
            // AudioManager.Instance.Play(_data.collectSoundName);

            _onCollect?.Invoke();
            Destroy(gameObject);
        }

        private void EnterExpire()
        {
            _state     = State.Expire;
            _fadeTimer = 0f;
        }

        // -----------------------------------------------------------
        // 보상 적용
        // -----------------------------------------------------------
        private void ApplyRewards()
        {
            var player = GameObjectManager.Instance.Player;
            if (player == null) return;

            if (_data.healAmount > 0f)
                player.HealPercent(_data.healAmount);

            // 스킬 게이지 - 플레이어 액터에서 직접 접근
            if (player != null && player.SkillGauge != null)
                player.SkillGauge.AddGauge(_data.gaugeAmount);
        }

        // -----------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------
        private void ApplyFloatAnimation()
        {
            float offsetY = Mathf.Sin(Time.time * _data.floatSpeed * Mathf.PI * 2f) * _data.floatAmplitude;
            var pos = transform.position;
            pos.y = _spawnY + offsetY;
            transform.position = pos;
        }

        private void SetAlpha(float alpha)
        {
            foreach (var r in _renderers)
            {
                foreach (var mat in r.materials)
                {
                    // URP Lit 셰이더 기준 알파 채널 적용
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var color = mat.GetColor("_BaseColor");
                        color.a = alpha;
                        mat.SetColor("_BaseColor", color);
                    }
                }
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
