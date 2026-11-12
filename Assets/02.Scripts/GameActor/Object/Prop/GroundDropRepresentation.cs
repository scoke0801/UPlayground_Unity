using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 미획득 바닥 아이템의 월드 외형만 표시하며 지급 상태는 소유하지 않는다.
    /// 희귀도색 바닥 링과 빛기둥, 위에서 회전하며 떠 있는 아이콘 메달로 구성하고
    /// 원천 위치에서 튀어나와 착지하는 등장 연출을 재생한다.
    /// ItemSO.worldPrefab이 있으면 메달 자리에 그 모델을 띄우고, 없으면 메달로 폴백한다.
    /// 3D 모델이 없는 아이템도 같은 형태로 읽히도록 메달 방식을 택했다(아이콘만 카메라를 보게 하면 평면 스티커처럼 보인다).
    /// </summary>
    public sealed class GroundDropRepresentation : MonoBehaviour
    {
        private enum Phase { Toss, Landing, Idle }

        // 빛기둥 스프라이트 폭(0.25m) 대비 배율. 기둥 굵기는 형태 계약이라 데이터로 빼지 않는다.
        private const float BeamWidthScale = 2f;

        // 프리팹에 이미 연결된 아이콘 렌더러. 메달 앞면으로 재사용한다.
        [SerializeField] private SpriteRenderer _icon;

        private GroundDropSettingsSO _settings;
        private GroundDropRarityStyle _style;
        private Color _rarityColor;
        private Transform _visual;
        private Transform _token;
        private Transform _ring;
        private Transform _beam;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer[] _beamRenderers;
        private SpriteRenderer _iconBack;
        private float _iconScale;
        private Vector3 _launchOffset;
        private Phase _phase;
        private float _phaseTime;
        private float _landedTime;
        private float _spinAngle;
        private float _bobPhase;

        /// <summary>착지를 마쳐 수동 획득이 가능한 상태.</summary>
        public bool IsSettled => _phase != Phase.Toss;

        /// <summary>착지 직후 연출이 잠깐 지난 뒤 이름표를 띄워도 되는 상태.</summary>
        public bool IsLabelReady => IsSettled && _settings != null && _landedTime >= _settings.labelRevealDelay;

        /// <param name="launchWorldPosition">튀어나오는 시작 위치. null이면 투척 없이 제자리에서 나타난다.</param>
        public void Initialize(ItemSO item, GroundDropSettingsSO settings, Vector3? launchWorldPosition = null)
        {
            _settings = settings;
            if (item == null || settings == null) return;
            _style = settings.GetRarityStyle(item.itemRarity);
            _rarityColor = item.itemRarity.ToColor();
            if (_rarityColor.a <= 0f) _rarityColor = Color.white;
            _bobPhase = Random.value * Mathf.PI * 2f;
            _spinAngle = Random.value * 360f;

            BuildVisual(item.icon, item.worldPrefab);

            if (launchWorldPosition.HasValue)
            {
                _launchOffset = transform.InverseTransformPoint(launchWorldPosition.Value) + Vector3.up * settings.launchHeight;
                _phase = Phase.Toss;
            }
            else
            {
                _phase = Phase.Landing;
            }
            _phaseTime = 0f;
            _landedTime = 0f;
            ApplyPose(0f);
            GroundDropVisualDriver.Register(this);
        }

        private void OnDestroy() => GroundDropVisualDriver.Unregister(this);

        private void BuildVisual(Sprite icon, GameObject worldPrefab)
        {
            _visual = NewChild("Visual", transform).transform;
            _token = NewChild("Token", _visual).transform;
            _token.localPosition = Vector3.up * _settings.tokenHoverHeight;

            if (worldPrefab != null && TryBuildWorldModel(worldPrefab))
            {
                if (_icon != null) _icon.enabled = false;
            }
            else
            {
                BuildMedal(icon);
            }
            BuildGroundGlow();
        }

        /// <summary>
        /// 월드 모델을 메달 자리에 넣는다. 긴 변을 tokenSize에 맞추고 바운드 중심을 회전축에 둬서
        /// 모델마다 피벗이 달라도 같은 크기·같은 축으로 돈다. 렌더러가 없으면 실패로 보고 메달로 폴백한다.
        /// </summary>
        private bool TryBuildWorldModel(GameObject worldPrefab)
        {
            var model = Instantiate(worldPrefab, _token, false);
            model.name = "WorldModel";
            StripNonVisualComponents(model);

            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            bool hasBounds = false;
            var bounds = new Bounds();
            for (int i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null) continue;
                var local = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var p = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    var inToken = _token.InverseTransformPoint(filters[i].transform.TransformPoint(p));
                    if (!hasBounds) { bounds = new Bounds(inToken, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(inToken);
                }
            }
            if (!hasBounds)
            {
                Destroy(model);
                return false;
            }

            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = _settings.tokenSize / Mathf.Max(0.0001f, longest);
            model.transform.localScale *= scale;
            model.transform.localPosition -= bounds.center * scale;
            return true;
        }

        // 획득 판정은 루트 프리팹이 소유한다. 모델에 딸린 물리·로직이 판정이나 상태를 바꾸지 않게 표시 전용으로 만든다.
        private static void StripNonVisualComponents(GameObject model)
        {
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(behaviour);
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) Destroy(animator);
            foreach (var joint in model.GetComponentsInChildren<Joint>(true)) Destroy(joint);
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Destroy(collider);
            }
        }

        private void BuildMedal(Sprite icon)
        {
            float diameter = _settings.tokenSize * 1.15f;
            Color plate = Color.Lerp(new Color(0.05f, 0.07f, 0.1f), _rarityColor, 0.18f);
            plate.a = 0.92f;
            AddSprite("Plate", _token, GroundDropArt.Disc, plate, 0, Vector3.one * diameter);
            AddSprite("Rim", _token, GroundDropArt.Ring, _rarityColor, 1, Vector3.one * diameter);

            if (icon != null)
            {
                Vector2 size = icon.bounds.size;
                _iconScale = _settings.tokenSize * 0.72f / Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
                if (_icon == null) _icon = AddSprite("IconFront", _token, icon, Color.white, 2, Vector3.one * _iconScale);
                else
                {
                    _icon.transform.SetParent(_token, false);
                    _icon.transform.localPosition = Vector3.zero;
                    _icon.transform.localRotation = Quaternion.identity;
                    _icon.transform.localScale = Vector3.one * _iconScale;
                    _icon.sprite = icon; _icon.sortingOrder = 2; _icon.enabled = true;
                }
                // 회전해 뒤에서 볼 때 아이콘이 좌우 반전되지 않도록 뒷면을 따로 둔다.
                _iconBack = AddSprite("IconBack", _token, icon, Color.white, 2, Vector3.one * _iconScale);
                _iconBack.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else if (_icon != null) _icon.enabled = false;
        }

        private void BuildGroundGlow()
        {
            _ring = AddSprite("GroundRing", transform, GroundDropArt.GlowRing,
                WithAlpha(_rarityColor, _style.glowAlpha), -1, Vector3.zero).transform;
            _ringRenderer = _ring.GetComponent<SpriteRenderer>();
            _ring.localRotation = Quaternion.Euler(90f, 0f, 0f);

            if (_style.beamHeight > 0.01f)
            {
                _beam = NewChild("Beam", transform).transform;
                _beamRenderers = new SpriteRenderer[2];
                for (int i = 0; i < 2; i++)
                {
                    var renderer = AddSprite("Beam" + i, _beam, GroundDropArt.Beam,
                        WithAlpha(_rarityColor, _style.glowAlpha * 0.8f), -1, new Vector3(BeamWidthScale, 1f, 1f));
                    renderer.transform.localRotation = Quaternion.Euler(0f, 90f * i, 0f);
                    _beamRenderers[i] = renderer;
                }
                _beam.localScale = new Vector3(1f, 0f, 1f);
            }
        }

        /// <summary>드라이버가 매 프레임 호출한다. 개별 액터가 Update를 갖지 않도록 한 곳에서 모아 돌린다.</summary>
        internal void Tick(float deltaTime, Camera camera)
        {
            if (_settings == null) return;
            _phaseTime += deltaTime;
            _spinAngle += _settings.tokenSpinSpeed * deltaTime;

            if (_phase == Phase.Toss && _phaseTime >= _settings.tossDuration)
            {
                _phase = Phase.Landing; _phaseTime = 0f;
            }
            else if (_phase == Phase.Landing && _phaseTime >= _settings.landingPulseDuration)
            {
                _phase = Phase.Idle; _phaseTime = 0f;
            }
            if (_phase != Phase.Toss) _landedTime += deltaTime;

            ApplyPose(Time.time);
            FaceIconToCamera(camera);
        }

        private void ApplyPose(float time)
        {
            float bob = Mathf.Sin(time * _settings.tokenBobSpeed + _bobPhase) * _settings.tokenBobAmplitude;
            switch (_phase)
            {
                case Phase.Toss:
                {
                    float u = Mathf.Clamp01(_phaseTime / _settings.tossDuration);
                    _visual.localPosition = Vector3.Lerp(_launchOffset, Vector3.zero, u) + Vector3.up * (4f * u * (1f - u) * _settings.tossArcHeight);
                    _token.localScale = Vector3.one * Mathf.Clamp01(u * 3f);
                    _token.localPosition = Vector3.up * _settings.tokenHoverHeight;
                    SetGroundGlow(0f, 0f);
                    break;
                }
                case Phase.Landing:
                {
                    float p = Mathf.Clamp01(_phaseTime / _settings.landingPulseDuration);
                    _visual.localPosition = Vector3.zero;
                    _token.localScale = Vector3.one;
                    // 착지 반동: 한 번 가라앉았다가 떠오르며 부유 상태로 이어진다.
                    float bounce = Mathf.Sin(p * Mathf.PI) * (1f - p) * 0.18f;
                    _token.localPosition = Vector3.up * (_settings.tokenHoverHeight - bounce + bob * p);
                    SetGroundGlow(EaseOutBack(p), EaseOutCubic(p));
                    break;
                }
                default:
                {
                    _visual.localPosition = Vector3.zero;
                    _token.localScale = Vector3.one;
                    _token.localPosition = Vector3.up * (_settings.tokenHoverHeight + bob);
                    SetGroundGlow(1f, 1f);
                    float breathe = 0.88f + 0.12f * Mathf.Sin(time * 2f + _bobPhase);
                    if (_ringRenderer != null) _ringRenderer.color = WithAlpha(_rarityColor, _style.glowAlpha * breathe);
                    break;
                }
            }
            _token.localRotation = Quaternion.Euler(0f, _spinAngle, 0f);
        }

        private void SetGroundGlow(float ringProgress, float beamProgress)
        {
            if (_ring != null)
                _ring.localScale = Vector3.one * (_settings.tokenSize * 2.6f * _style.ringScale * Mathf.Max(0f, ringProgress));
            if (_beam != null)
                _beam.localScale = new Vector3(1f, Mathf.Max(0.0001f, beamProgress) * _style.beamHeight, 1f);
        }

        private void FaceIconToCamera(Camera camera)
        {
            if (camera == null || _iconBack == null || _icon == null) return;
            Vector3 toCamera = camera.transform.position - _token.position;
            bool frontVisible = Vector3.Dot(-_token.forward, toCamera) >= 0f;
            _icon.enabled = frontVisible;
            _iconBack.enabled = !frontVisible;
        }

        private static GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Color color, int order, Vector3 scale)
        {
            var go = NewChild(name, parent);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        private static float EaseOutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t -= 1f; return 1f + c3 * t * t * t + c1 * t * t;
        }
    }

    /// <summary>모든 바닥 드롭 외형의 회전·부유·등장 연출을 한 번의 Update로 갱신한다.</summary>
    internal sealed class GroundDropVisualDriver : MonoBehaviour
    {
        private static readonly List<GroundDropRepresentation> s_active = new();
        private static GroundDropVisualDriver s_instance;
        private Camera _camera;
        private float _nextCameraRefresh;

        public static void Register(GroundDropRepresentation view)
        {
            if (s_instance == null)
            {
                var go = new GameObject("GroundDropVisualDriver") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                s_instance = go.AddComponent<GroundDropVisualDriver>();
            }
            if (!s_active.Contains(view)) s_active.Add(view);
        }

        public static void Unregister(GroundDropRepresentation view) => s_active.Remove(view);

        private void Update()
        {
            if (_camera == null || Time.unscaledTime >= _nextCameraRefresh)
            {
                _nextCameraRefresh = Time.unscaledTime + 1f;
                if (Services.TryGet<ICameraViewService>(out var cameras)) _camera = cameras.GetMainCamera();
            }
            float deltaTime = Time.deltaTime;
            for (int i = s_active.Count - 1; i >= 0; i--)
            {
                var view = s_active[i];
                if (view == null) { s_active.RemoveAt(i); continue; }
                view.Tick(deltaTime, _camera);
            }
        }
    }

    /// <summary>바닥 드롭 연출에 쓰는 절차 생성 스프라이트. 외부 에셋 없이 어떤 아이템에도 같은 형태를 준다.</summary>
    internal static class GroundDropArt
    {
        private static Sprite s_disc, s_ring, s_glowRing, s_beam;
        public static Sprite Disc => s_disc != null ? s_disc : s_disc = Build(64, 64, 64f, new Vector2(0.5f, 0.5f), (x, y) =>
        { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01((1f - d) * 32f / 1.5f); });
        public static Sprite Ring => s_ring != null ? s_ring : s_ring = Build(64, 64, 64f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            float outer = Mathf.Clamp01((1f - d) * 32f / 1.5f);
            float inner = Mathf.Clamp01((d - 0.8f) * 32f / 1.5f);
            return outer * inner;
        });
        public static Sprite GlowRing => s_glowRing != null ? s_glowRing : s_glowRing = Build(128, 128, 128f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d >= 1f) return 0f;
            float band = Mathf.Exp(-Mathf.Pow((d - 0.82f) / 0.1f, 2f));
            float fill = 0.22f * (1f - d);
            return Mathf.Clamp01(Mathf.Max(band, fill)) * Mathf.Clamp01((1f - d) * 8f);
        });
        // 피벗을 바닥 중앙에 둔다. 폭 0.25, 높이 1 단위.
        public static Sprite Beam => s_beam != null ? s_beam : s_beam = Build(32, 128, 128f, new Vector2(0.5f, 0f), (x, y) =>
        {
            float v = (y + 1f) * 0.5f;
            return Mathf.Pow(1f - Mathf.Abs(x), 2f) * Mathf.Pow(1f - v, 1.3f);
        });

        // 좌표는 중심 기준 -1~1로 정규화해 넘긴다.
        private static Sprite Build(int width, int height, float pixelsPerUnit, Vector2 pivot, System.Func<float, float, float> alphaAt)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[width * height];
            for (int py = 0; py < height; py++)
                for (int px = 0; px < width; px++)
                {
                    float x = (px + 0.5f) / width * 2f - 1f;
                    float y = (py + 0.5f) / height * 2f - 1f;
                    byte alpha = (byte)(Mathf.Clamp01(alphaAt(x, y)) * 255f);
                    pixels[py * width + px] = new Color32(255, 255, 255, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, pixelsPerUnit);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
