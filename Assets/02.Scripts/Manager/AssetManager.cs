using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace UPlayGround.Manager
{
    public class AssetManager : BaseManager<AssetManager>, IManager
    {
        private SpriteAtlas _itemAtlas;

        public void Init()
        {
            LoadItemAtlas();
        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public Sprite GetAtlas(string key)
        {
            return _itemAtlas.GetSprite(key);
        }

        private async void LoadItemAtlas()
        {
            const string path = "ItemAtlas";

            var handle = Addressables.LoadAssetAsync<SpriteAtlas>(path);
            try
            {
                _itemAtlas = await handle.Task;

                if (_itemAtlas == null)
                {
                    Debug.LogError($"[AssetManager] '{path}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                Debug.Log($"[AssetManager] path - 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AssetManager] path 로드 실패: {e.Message}");
            }
        }
    }
}