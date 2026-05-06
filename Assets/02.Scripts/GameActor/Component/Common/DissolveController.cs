
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UPlayGround.Component
{
    public class DissolveController : MonoBehaviour
    {
        private struct RendererInfo
        {
            public Renderer renderer;
            public Texture baseMap;
            public Material[] originalSharedMaterials; // ResetDissolve 복원용
        }
        
        private const string DissolveMaterialAddress = "DissolveMaterial";
        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");

        // 셰이더만 공유, 머티리얼 인스턴스는 액터마다 생성
        private static AsyncOperationHandle<Material> _loadHandle;
        private static Material _dissolveSourceMaterial;

        private List<RendererInfo> _rendererInfos = new List<RendererInfo>();
        private List<Material> _instancedMaterials = new List<Material>(); // 해제용
        private MaterialPropertyBlock _mpb;
        
        private float _dissolveDuration = 2f;
        
        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            InitializeRendererData();

            if (!_loadHandle.IsValid())
            {
                _loadHandle = Addressables.LoadAssetAsync<Material>(DissolveMaterialAddress);
                _loadHandle.Completed += OnDissolveMaterialLoaded;
            }
            else if (_loadHandle.IsDone)
            {
                OnDissolveMaterialLoaded(_loadHandle);
            }
            else
            {
                _loadHandle.Completed -= OnDissolveMaterialLoaded;
                _loadHandle.Completed += OnDissolveMaterialLoaded;
            }
        }

        private void OnDestroy()
        {
            // 인스턴스 머티리얼 해제
            foreach (var mat in _instancedMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
        
        private IEnumerator PreloadDissolveMaterial()
        {
            _loadHandle = Addressables.LoadAssetAsync<Material>(DissolveMaterialAddress);
            yield return _loadHandle;

            OnDissolveMaterialLoaded(_loadHandle);
        }

        private static void OnDissolveMaterialLoaded(AsyncOperationHandle<Material> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DissolveController] DissolveMaterial 로드 실패: {DissolveMaterialAddress}");
                return;
            }

            _dissolveSourceMaterial = handle.Result;
        }
        
        public void InitializeRendererData()
        {
            _rendererInfos.Clear();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterial == null) continue;
                
                // 파티클 셰이더 제외
                if (r.sharedMaterial.shader.name.Contains("Particle")) continue;

                var baseMap = r.sharedMaterial.HasProperty(BaseMapID)
                    ? r.sharedMaterial.GetTexture(BaseMapID)
                    : null;

                _rendererInfos.Add(new RendererInfo
                {
                    renderer = r,
                    baseMap = baseMap,
                    originalSharedMaterials = r.sharedMaterials
                });
            }
        }
        
        /// <summary>
        /// 내장 무기 복원 시 호출. 디졸브 머티리얼을 제거하고 원본 머티리얼과 렌더러 상태를 복원한다.
        /// </summary>
        public void ResetDissolve()
        {
            StopAllCoroutines();

            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null) continue;
                info.renderer.enabled = true;
                if (info.originalSharedMaterials != null)
                    info.renderer.sharedMaterials = info.originalSharedMaterials;
            }

            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();
        }

        /// <summary>
        /// 모델 교체 시 호출. 기존 인스턴스 머티리얼을 해제하고 새 Model의 렌더러로 재초기화한다.
        /// </summary>
        public void RefreshRenderers()
        {
            StopAllCoroutines();

            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();

            InitializeRendererData();
        }
        
        public void StartDissolve(float duration, bool destroyOnComplete = true, System.Action onComplete = null)
        {
            if (_rendererInfos.Count == 0)
            {
                onComplete?.Invoke();
                if (destroyOnComplete) Destroy(gameObject);
                return;
            }

            _dissolveDuration = duration;

            StopAllCoroutines();
            StartCoroutine(DissolveRoutine(destroyOnComplete, onComplete));
        }

        public void CompleteDissolve(bool destroyOnComplete = true, System.Action onComplete = null)
        {
            StopAllCoroutines();

            if (_rendererInfos.Count > 0 && _dissolveSourceMaterial != null)
            {
                SwapToDissolveMaterials();
                SetDissolveAmount(1f);
            }

            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
        }

        private IEnumerator DissolveRoutine(bool destroyOnComplete, System.Action onComplete)
        {
            float waitTime = 0f;
            while (_dissolveSourceMaterial == null)
            {
                waitTime += Time.unscaledDeltaTime;
                if (waitTime > 1.5f)
                {
                    Debug.LogWarning("[DissolveController] DissolveMaterial 로드 지연/실패 — 즉시 파괴 처리.");
                    onComplete?.Invoke();
                    if (destroyOnComplete) Destroy(gameObject);
                    yield break;
                }
                yield return null;
            }

            SwapToDissolveMaterials();

            float elapsed = 0f;
            while (elapsed < _dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolveAmount(Mathf.Clamp01(elapsed / _dissolveDuration));
                yield return null;
            }

            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
        }

        private void SwapToDissolveMaterials()
        { 
            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();
            
            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null) continue;

                // 슬롯 수만큼 인스턴스 생성, BaseMap 복사
                var mats = new Material[info.renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var instance = new Material(_dissolveSourceMaterial);
                    if (info.baseMap != null)
                        instance.SetTexture(BaseMapID, info.baseMap);

                    mats[i] = instance;
                    _instancedMaterials.Add(instance);
                }

                info.renderer.materials = mats;
            }
        }

        private void SetDissolveAmount(float amount)
        {   
            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null) continue;

                info.renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(DissolveAmountID, amount);
                info.renderer.SetPropertyBlock(_mpb);
            }
        }
        
    }
}
