
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
            public Texture baseMap; // 기존 머티리얼의 BaseMap
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
                StartCoroutine(PreloadDissolveMaterial());
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

            if (_loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DissolveController] DissolveMaterial 로드 실패: {DissolveMaterialAddress}");
                yield break;
            }

            _dissolveSourceMaterial = _loadHandle.Result;
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
                    baseMap = baseMap
                });
            }
        }
        
        public void RefreshRenderers() => InitializeRendererData();
        
        public void StartDissolve(float duration)
        {
            if (_rendererInfos.Count == 0) return;

            _dissolveDuration = duration;
            
            StopAllCoroutines();
            StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveRoutine()
        {
            // 로드 대기
            while (_dissolveSourceMaterial == null)
                yield return null;
            
            SwapToDissolveMaterials();

            float elapsed = 0f;
            while (elapsed < _dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolveAmount(Mathf.Clamp01(elapsed / _dissolveDuration));
                yield return null;
            }
            
            Destroy(gameObject);
        }

        private void SwapToDissolveMaterials()
        { 
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