using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Manager;
using Random = UnityEngine.Random;

namespace UPlayGround
{
    /// <summary>확정된 드랍 보상을 낮게 분출한 뒤 플레이어에게 가속 흡수시키는 표현 전용 액터.</summary>
    public class ItemActor : GameActor
    {
        private const string LootFlightFxKey = "LootFlight";
        private const string LootArrivalFxKey = "LootArrival";
        private static readonly AnimationCurve TrailWidthCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));

        protected override bool RequiresCombatVisuals => false;

        [Header("출현 리듬")]
        [SerializeField] private Vector2 _releaseDelayRange = new(0.10f, 0.16f);
        [Min(0f)] [SerializeField] private float _launchStagger = 0.055f;

        [Header("낮은 분출")]
        [SerializeField] private Vector2 _spreadRadiusRange = new(0.45f, 0.95f);
        [SerializeField] private Vector2 _spreadPeakHeightRange = new(0.65f, 1.15f);
        [SerializeField] private Vector2 _spreadEndHeightRange = new(0.25f, 0.45f);
        [SerializeField] private Vector2 _spreadDurationRange = new(0.14f, 0.22f);
        [SerializeField] private Vector2 _hoverDurationRange = new(0.06f, 0.11f);

        [Header("가속 흡수")]
        [SerializeField] private Vector2 _flightDurationRange = new(0.28f, 0.60f);
        [Min(0f)] [SerializeField] private float _flightSecondsPerMeter = 0.025f;
        [SerializeField] private Vector2 _lateralCurveRange = new(0.12f, 0.35f);
        [SerializeField] private Vector2 _departureLiftRange = new(0.20f, 0.45f);
        [Min(0f)] [SerializeField] private float _arrivalLeadDistance = 0.80f;
        [Min(0f)] [SerializeField] private float _arrivalControlHeight = 0.25f;
        [Min(1f)] [SerializeField] private float _homingAccelerationPower = 2.4f;
        [SerializeField] private Vector3 _fallbackCollectionOffset = new(0f, 1.05f, 0f);
        [SerializeField] private float _collectionVerticalOffset = 0.08f;

        [Header("시각 표현")]
        [SerializeField] private TrailRenderer _trailRenderer;
        [Min(0.01f)] [SerializeField] private float _baseTrailWidth = 0.12f;
        [Min(0.01f)] [SerializeField] private float _trailLifetime = 0.28f;
        [Min(0f)] [SerializeField] private float _rarityScaleStep = 0.12f;
        [Range(0.01f, 1f)] [SerializeField] private float _arrivalScale = 0.18f;
        [SerializeField] private ItemRarity _enhancedFlightFxMinimumRarity = ItemRarity.RARE;
        [Min(0.1f)] [SerializeField] private float _flightFxLifetime = 2f;
        [Min(0.1f)] [SerializeField] private float _arrivalFxLifetime = 1.2f;

        private PlayerActor _player;
        private Collider _playerCollider;
        private ItemInstance _itemInstance;
        private int _launchOrder;
        private bool _playsArrivalAccent;
        private bool _isCompleted;
        private float _rarityScale = 1f;

        protected override void Awake()
        {
            base.Awake();

            _trailRenderer ??= GetComponentInChildren<TrailRenderer>(true);
            if (_trailRenderer == null)
                return;

            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
        }

        protected override void Start()
        {
            base.Start();

            if (_itemInstance?.data == null)
            {
                Destroy(gameObject);
                return;
            }

            _player = ActorSvc.Objects?.Player;
            if (_player == null)
            {
                CompletePresentation();
                return;
            }

            _playerCollider = _player.ActorController?.Motor?.Capsule;
            ConfigureVisuals();
            StartCoroutine(PlayAcquisitionPresentation());
        }

        /// <summary>이미 지급된 아이템과 한 드랍 묶음 안의 발사 순서를 설정한다.</summary>
        public void Init(ItemInstance itemInstance, int launchOrder, bool playsArrivalAccent)
        {
            _itemInstance = itemInstance;
            _launchOrder = Mathf.Max(0, launchOrder);
            _playsArrivalAccent = playsArrivalAccent;
        }

        private IEnumerator PlayAcquisitionPresentation()
        {
            float releaseDelay = RandomRange(_releaseDelayRange) + _launchOrder * _launchStagger;
            if (releaseDelay > 0f)
                yield return new WaitForSeconds(releaseDelay);

            BeginFlightVisuals();
            yield return PlaySpread();

            float hoverDuration = RandomRange(_hoverDurationRange);
            if (hoverDuration > 0f)
                yield return new WaitForSeconds(hoverDuration);

            yield return PlayHomingFlight();
            CompletePresentation();
        }

        private IEnumerator PlaySpread()
        {
            Vector3 startPosition = transform.position;
            Vector2 horizontal = Random.insideUnitCircle;
            if (horizontal.sqrMagnitude < 0.001f)
                horizontal = Vector2.right;

            horizontal.Normalize();
            horizontal *= RandomRange(_spreadRadiusRange);

            Vector3 endPosition = startPosition
                                  + new Vector3(horizontal.x, RandomRange(_spreadEndHeightRange), horizontal.y);
            Vector3 controlPosition = Vector3.Lerp(startPosition, endPosition, 0.5f);
            controlPosition.y = startPosition.y + RandomRange(_spreadPeakHeightRange);

            float duration = Mathf.Max(0.01f, RandomRange(_spreadDurationRange));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = 1f - (1f - normalizedTime) * (1f - normalizedTime);
                transform.position = EvaluateQuadraticBezier(startPosition, controlPosition, endPosition, progress);
                yield return null;
            }

            transform.position = endPosition;
        }

        private IEnumerator PlayHomingFlight()
        {
            if (_player == null)
                yield break;

            Vector3 startPosition = transform.position;
            Vector3 initialTarget = GetCollectionPosition();
            Vector3 flatDirection = initialTarget - startPosition;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.001f)
                flatDirection = _player.transform.forward;
            flatDirection.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, flatDirection);
            float sideSign = (_launchOrder & 1) == 0 ? 1f : -1f;
            float lateralCurve = RandomRange(_lateralCurveRange) * sideSign;
            Vector3 departureControl = startPosition
                                       + flatDirection * (_arrivalLeadDistance * 0.35f)
                                       + side * lateralCurve
                                       + Vector3.up * RandomRange(_departureLiftRange);

            float distance = Vector3.Distance(startPosition, initialTarget);
            float minimumFlightDuration = Mathf.Min(_flightDurationRange.x, _flightDurationRange.y);
            float maximumFlightDuration = Mathf.Max(_flightDurationRange.x, _flightDurationRange.y);
            float duration = Mathf.Clamp(
                minimumFlightDuration + distance * _flightSecondsPerMeter,
                minimumFlightDuration,
                maximumFlightDuration);

            float elapsed = 0f;
            while (elapsed < duration && _player != null)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = Mathf.Pow(normalizedTime, _homingAccelerationPower);

                Vector3 targetPosition = GetCollectionPosition();
                Vector3 arrivalControl = targetPosition
                                         - flatDirection * _arrivalLeadDistance
                                         + Vector3.up * _arrivalControlHeight;
                transform.position = EvaluateCubicBezier(
                    startPosition,
                    departureControl,
                    arrivalControl,
                    targetPosition,
                    progress);

                float visualScale = Mathf.Lerp(1f, _arrivalScale, progress);
                transform.localScale = Vector3.one * visualScale;
                yield return null;
            }

            if (_player != null)
                transform.position = GetCollectionPosition();
        }

        private void ConfigureVisuals()
        {
            ItemRarity rarity = _itemInstance.data.itemRarity;
            int raritySteps = Mathf.Max(0, (int)rarity - (int)ItemRarity.COMMON);
            _rarityScale = 1f + raritySteps * _rarityScaleStep;
            transform.localScale = Vector3.one;

            if (_trailRenderer == null)
                return;

            Color rarityColor = rarity.ToColor();
            if (rarityColor.a <= 0f)
                rarityColor = Color.white;

            Color brightColor = rarityColor * 1.6f;
            brightColor.a = 1f;
            _trailRenderer.widthMultiplier = _baseTrailWidth * _rarityScale;
            _trailRenderer.time = _trailLifetime;
            _trailRenderer.widthCurve = TrailWidthCurve;
            _trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _trailRenderer.receiveShadows = false;
            _trailRenderer.generateLightingData = false;
            _trailRenderer.numCornerVertices = 2;
            _trailRenderer.numCapVertices = 2;
            _trailRenderer.minVertexDistance = 0.05f;
            _trailRenderer.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(brightColor, 0f),
                    new GradientColorKey(rarityColor, 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f),
                },
            };
        }

        private void BeginFlightVisuals()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }

            if (_itemInstance.data.itemRarity < _enhancedFlightFxMinimumRarity)
                return;

            GameObject flightFx = ActorSvc.Objects?.ShowFX(
                LootFlightFxKey,
                transform.position,
                parent: transform,
                duration: _flightFxLifetime);
            if (flightFx != null)
                flightFx.transform.localScale *= _rarityScale;
        }

        private Vector3 GetCollectionPosition()
        {
            if (_playerCollider != null)
                return _playerCollider.bounds.center + Vector3.up * _collectionVerticalOffset;

            return _player.transform.TransformPoint(_fallbackCollectionOffset);
        }

        private void CompletePresentation()
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            Vector3 arrivalPosition = _player != null ? GetCollectionPosition() : transform.position;
            if (_playsArrivalAccent && _player != null)
            {
                GameObject arrivalFx = ActorSvc.Objects?.ShowFX(
                    LootArrivalFxKey,
                    arrivalPosition,
                    duration: _arrivalFxLifetime);
                if (arrivalFx != null)
                    arrivalFx.transform.localScale *= _rarityScale;
            }

            ActorSvc.UI?.ShowItemAcquisition(_itemInstance.data, _itemInstance.count);
            Destroy(gameObject);
        }

        private static Vector3 EvaluateQuadraticBezier(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * start
                   + 2f * inverse * progress * control
                   + progress * progress * end;
        }

        private static Vector3 EvaluateCubicBezier(
            Vector3 start,
            Vector3 firstControl,
            Vector3 secondControl,
            Vector3 end,
            float progress)
        {
            float inverse = 1f - progress;
            float inverseSquared = inverse * inverse;
            float progressSquared = progress * progress;
            return inverseSquared * inverse * start
                   + 3f * inverseSquared * progress * firstControl
                   + 3f * inverse * progressSquared * secondControl
                   + progressSquared * progress * end;
        }

        private static float RandomRange(Vector2 range)
        {
            return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _launchStagger = Mathf.Max(0f, _launchStagger);
            _flightSecondsPerMeter = Mathf.Max(0f, _flightSecondsPerMeter);
            _arrivalLeadDistance = Mathf.Max(0f, _arrivalLeadDistance);
            _arrivalControlHeight = Mathf.Max(0f, _arrivalControlHeight);
            _homingAccelerationPower = Mathf.Max(1f, _homingAccelerationPower);
            _baseTrailWidth = Mathf.Max(0.01f, _baseTrailWidth);
            _trailLifetime = Mathf.Max(0.01f, _trailLifetime);
            _rarityScaleStep = Mathf.Max(0f, _rarityScaleStep);
            _arrivalScale = Mathf.Clamp(_arrivalScale, 0.01f, 1f);
            _flightFxLifetime = Mathf.Max(0.1f, _flightFxLifetime);
            _arrivalFxLifetime = Mathf.Max(0.1f, _arrivalFxLifetime);
        }
#endif
    }
}
