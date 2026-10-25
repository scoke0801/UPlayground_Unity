#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.Manager
{
    /// <summary>
    /// CheatManager — 지역 이동 치트. 본편 진행 조건으로 닫힌 길을 거치지 않고 등록된 지역으로 바로 간다. 개발 빌드 전용.
    /// 진행 플래그를 바꾸지 않고 씬만 옮기므로 이동 뒤에도 퀘스트·길 해금 상태는 그대로 남는다.
    /// </summary>
    public partial class CheatManager
    {
        private const string TravelMapDatabaseKey = "MapConfigDatabase";

        private MapConfigDatabaseSO _travelMapDatabase;

        /// <summary>이동 목록의 원본인 지도 데이터베이스를 반환한다. 처음 호출할 때만 Addressables에서 불러온다.</summary>
        public async UniTask<MapConfigDatabaseSO> LoadTravelMapDatabaseAsync()
        {
            if (_travelMapDatabase != null)
                return _travelMapDatabase;

            AssetManager assets = AssetManager.Instance;
            if (assets == null)
                return null;

            try
            {
                _travelMapDatabase = await assets.TryLoadGlobalAsync<MapConfigDatabaseSO>(
                    TravelMapDatabaseKey, nameof(CheatManager), destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            return _travelMapDatabase;
        }

        /// <summary>빌드에 포함된 지역인지 확인한다. 지도 데이터에만 남은 옛 씬은 이동 대상에서 뺀다.</summary>
        public static bool CanTravelToMap(string mapId) =>
            !string.IsNullOrWhiteSpace(mapId) && Application.CanStreamedLevelBeLoaded(mapId);

        /// <summary>mapId 씬의 기본 시작 위치로 바로 이동한다. 진행 조건으로 닫힌 출구를 거치지 않는다.</summary>
        public bool TravelToMap(string mapId, string displayName = null)
        {
            string label = string.IsNullOrEmpty(displayName) ? mapId : displayName;

            SceneManager scenes = SceneManager.Instance;
            if (scenes == null)
            {
                Log(CheatCategory.Travel, $"이동 실패: 씬 관리자 없음 ({label})");
                return false;
            }

            if (scenes.IsLoading)
            {
                Log(CheatCategory.Travel, $"이동 실패: 다른 지역을 불러오는 중 ({label})");
                return false;
            }

            if (!CanTravelToMap(mapId))
            {
                Log(CheatCategory.Travel, $"이동 실패: 빌드에 없는 지역 ({label})");
                return false;
            }

            // 출구별 도착점은 그 출구의 연결 상태를 전제하므로, 치트는 대상 씬의 기본 시작 위치를 쓴다.
            bool accepted = scenes.TryLoadScene(mapId, null);
            Log(CheatCategory.Travel, accepted
                ? $"지역 이동: {label}"
                : $"이동 실패: {label} ({scenes.LastLoadFailure})");
            return accepted;
        }
    }
}
#endif
