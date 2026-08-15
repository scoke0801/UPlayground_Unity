using System.Collections.Generic;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 월드 액터의 머리 위 HP Bar + 데미지 플로터를
    /// Screen Space Canvas 위에서 관리합니다.
    /// Screen Space - Overlay Canvas에 부착합니다.
    /// </summary>
    public class UIWorldSpaceHudLayer : MonoBehaviour
    {
        private Canvas _parentCanvas;
        private RectTransform _parentCanvasRect;
        private Transform _floaterRoot;
        private RectTransform _floaterCanvasRect;
        private Camera _mainCamera;

        private GameObject _hpBarPrefab;
        private GameObject _dangerRingPrefab;
        private GameObject _breakInteractionPrefab;

        private GameObject              _floaterPrefab;
        private DamageFloaterConfigSO   _floaterConfig;
        private readonly Queue<UIDamageFloater> _floaterPool = new();
        private readonly Queue<UIActorHpBar> _hpBarPool = new();
        private readonly Queue<UIBreakInteraction> _breakInteractionPool = new();
        private readonly Dictionary<GameObject, Queue<UIDangerRing>> _dangerRingPools = new();

        private readonly List<UIDamageFloater> _activeFloaters = new();
        private readonly List<UIActorHpBar> _activeHpBars = new();
        private readonly List<UIDangerRing> _activeDangerRings = new();
        private readonly List<UIBreakInteraction> _activeBreakInteractions = new();

        // 풀 준비 완료 여부 — 미준비 상태에서 호출 시 조용히 스킵
        private bool _isPoolReady = false;

        public void Init(Canvas parentCanvas, Canvas floaterCanvas = null)
        {
            _parentCanvas = parentCanvas;
            _parentCanvasRect = parentCanvas != null
                ? parentCanvas.GetComponent<RectTransform>()
                : null;
            _floaterRoot = floaterCanvas != null ? floaterCanvas.transform : transform;
            _floaterCanvasRect = floaterCanvas != null
                ? floaterCanvas.GetComponent<RectTransform>()
                : _parentCanvasRect;
            _mainCamera   = Camera.main;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            float unscaledTime = Time.unscaledTime;

            TickActive(_activeHpBars, deltaTime, unscaledTime);
            TickActive(_activeDangerRings, deltaTime, unscaledTime);
            TickActive(_activeBreakInteractions, deltaTime, unscaledTime);
            TickActive(_activeFloaters, deltaTime, unscaledTime);
        }

        // ── HP Bar ────────────────────────────────────────────────────────

        public void SetHpBarPrefab(GameObject hpBarPrefab) => _hpBarPrefab = hpBarPrefab;

        public UIActorHpBar CreateHpBar(GameActor actor)
        {
            if (_hpBarPrefab == null) return null;
            if (_mainCamera  == null) _mainCamera = Camera.main;

            UIActorHpBar hpBar;
            if (_hpBarPool.Count > 0)
            {
                hpBar = _hpBarPool.Dequeue();
            }
            else
            {
                GameObject instance = Instantiate(_hpBarPrefab, transform);
                hpBar = instance.GetComponent<UIActorHpBar>();
                if (hpBar == null)
                    Destroy(instance);
            }

            if (hpBar == null)
                return null;

            hpBar.gameObject.SetActive(true);
            hpBar.Init(actor, _mainCamera, _parentCanvasRect, this);
            _activeHpBars.Add(hpBar);
            return hpBar;
        }

        public void ReturnHpBarToPool(UIActorHpBar hpBar)
        {
            if (hpBar == null)
                return;

            _activeHpBars.Remove(hpBar);
            hpBar.gameObject.SetActive(false);
            _hpBarPool.Enqueue(hpBar);
        }

        // ── Danger Ring ───────────────────────────────────────────────────

        public void SetDangerRingPrefab(GameObject dangerRingPrefab) => _dangerRingPrefab = dangerRingPrefab;

        /// <param name="prefabOverride">스킬별 프리팹 키로 해석된 프리팹. null이면 기본 프리팹 사용.</param>
        public UIDangerRing CreateDangerRing(GameActor actor, float duration, AttackDefenseType defenseType, GameObject prefabOverride = null)
        {
            GameObject prefab = prefabOverride != null ? prefabOverride : _dangerRingPrefab;
            if (prefab == null) return null;
            if (_mainCamera == null) _mainCamera = Camera.main;

            Queue<UIDangerRing> pool = GetDangerRingPool(prefab);
            UIDangerRing ring;
            if (pool.Count > 0)
            {
                ring = pool.Dequeue();
            }
            else
            {
                GameObject instance = Instantiate(prefab, transform);
                ring = instance.GetComponent<UIDangerRing>();
                if (ring == null)
                    Destroy(instance);
            }

            if (ring == null)
                return null;

            ring.gameObject.SetActive(true);
            ring.Init(actor, _mainCamera, _parentCanvasRect, duration, defenseType, this, prefab);
            _activeDangerRings.Add(ring);
            return ring;
        }

        public void ReturnDangerRingToPool(UIDangerRing ring, GameObject poolKey)
        {
            if (ring == null)
                return;

            _activeDangerRings.Remove(ring);
            ring.gameObject.SetActive(false);

            if (poolKey != null)
                GetDangerRingPool(poolKey).Enqueue(ring);
            else
                Destroy(ring.gameObject);
        }

        // ── Break Interaction ─────────────────────────────────────────────

        public void SetBreakInteractionPrefab(GameObject breakInteractionPrefab) => _breakInteractionPrefab = breakInteractionPrefab;

        /// <summary>
        /// 브레이크 공격 가능(노출) 표시 상호작용 UI 생성. 프리팹이 없으면 조용히 null 반환.
        /// </summary>
        public UIBreakInteraction CreateBreakInteraction(GameActor actor)
        {
            if (_breakInteractionPrefab == null) return null;
            if (_mainCamera == null) _mainCamera = Camera.main;

            UIBreakInteraction interaction;
            if (_breakInteractionPool.Count > 0)
            {
                interaction = _breakInteractionPool.Dequeue();
            }
            else
            {
                GameObject instance = Instantiate(_breakInteractionPrefab, transform);
                interaction = instance.GetComponent<UIBreakInteraction>();
                if (interaction == null)
                    Destroy(instance);
            }

            if (interaction == null)
                return null;

            interaction.gameObject.SetActive(true);
            interaction.Init(actor, _mainCamera, _parentCanvasRect, this);
            _activeBreakInteractions.Add(interaction);
            return interaction;
        }

        public void ReturnBreakInteractionToPool(UIBreakInteraction interaction)
        {
            if (interaction == null)
                return;

            _activeBreakInteractions.Remove(interaction);
            interaction.gameObject.SetActive(false);
            _breakInteractionPool.Enqueue(interaction);
        }

        // ── 데미지 플로터 ─────────────────────────────────────────────────

        public void SetupFloaterPool(GameObject floaterPrefab, DamageFloaterConfigSO config)
        {
            _floaterPrefab = floaterPrefab;
            _floaterConfig = config;

            if (_mainCamera == null) _mainCamera = Camera.main;

            for (int i = 0; i < config.initialPoolSize; i++)
                _floaterPool.Enqueue(CreateFloater());

            _isPoolReady = true;
        }

        public void ShowFloater(Vector3 worldPos, float damage, FloatStyle style)
        {
            if (!_isPoolReady) return;
            GetFloaterFromPool().Play(worldPos, Mathf.RoundToInt(damage).ToString(), style);
        }

        public void ShowFloaterLabel(Vector3 worldPos, string label, FloatStyle style)
        {
            if (!_isPoolReady || string.IsNullOrWhiteSpace(label)) return;
            GetFloaterFromPool().Play(worldPos, label, style);
        }

        public void ShowFloaterMiss(Vector3 worldPos)
        {
            if (!_isPoolReady) return;
            GetFloaterFromPool().Play(worldPos, "MISS", FloatStyle.Miss);
        }

        /// <param name="style">Heal 또는 MonsterHeal — 호출자가 구분해서 전달</param>
        public void ShowFloaterHeal(Vector3 worldPos, float amount, FloatStyle style = FloatStyle.Heal)
        {
            if (!_isPoolReady) return;
            GetFloaterFromPool().Play(worldPos, $"+{Mathf.RoundToInt(amount)}", style);
        }

        public void ReturnFloaterToPool(UIDamageFloater floater)
        {
            if (floater == null)
                return;

            _activeFloaters.Remove(floater);
            _floaterPool.Enqueue(floater);
        }

        private UIDamageFloater GetFloaterFromPool()
        {
            // 카메라가 null이면 여기서 재취득 (씬 전환 후 복구)
            if (_mainCamera == null) _mainCamera = Camera.main;

            var floater = _floaterPool.Count > 0 ? _floaterPool.Dequeue() : CreateFloater();
            floater.UpdateCamera(_mainCamera);
            _activeFloaters.Add(floater);
            return floater;
        }

        private UIDamageFloater CreateFloater()
        {
            var go      = Instantiate(_floaterPrefab, _floaterRoot != null ? _floaterRoot : transform);
            var floater = go.GetComponent<UIDamageFloater>();
            floater.Init(_mainCamera, _floaterCanvasRect, _floaterConfig, this);
            go.SetActive(false);
            return floater;
        }

        private Queue<UIDangerRing> GetDangerRingPool(GameObject prefab)
        {
            if (!_dangerRingPools.TryGetValue(prefab, out Queue<UIDangerRing> pool))
            {
                pool = new Queue<UIDangerRing>();
                _dangerRingPools.Add(prefab, pool);
            }

            return pool;
        }

        private static void TickActive(
            List<UIActorHpBar> items,
            float deltaTime,
            float unscaledTime)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                UIActorHpBar item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                item.ManagedLateTick(deltaTime, unscaledTime);
            }
        }

        private static void TickActive(
            List<UIDangerRing> items,
            float deltaTime,
            float unscaledTime)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                UIDangerRing item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                item.ManagedLateTick(deltaTime, unscaledTime);
            }
        }

        private static void TickActive(
            List<UIBreakInteraction> items,
            float deltaTime,
            float unscaledTime)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                UIBreakInteraction item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                item.ManagedLateTick(deltaTime, unscaledTime);
            }
        }

        private static void TickActive(
            List<UIDamageFloater> items,
            float deltaTime,
            float unscaledTime)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                UIDamageFloater item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                item.ManagedLateTick(deltaTime, unscaledTime);
            }
        }
    }
}
