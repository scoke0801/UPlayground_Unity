#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 지역 이동 탭(진행 조건을 무시하고 지도에 등록된 지역으로 이동).</summary>
    public partial class UI_System_DevCheatPanel
    {
        private RectTransform _travelListContent;
        private bool _isTravelListLoading;

        private void BuildTravelTab(RectTransform panel)
        {
            AddImage(panel.gameObject, PanelBg);
            var v = AddVLG(panel.gameObject, 10, 12);
            v.childForceExpandHeight = false;

            var title = MakeText(panel, "지역 이동", 22, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 34, prefH: 34);

            var note = MakeText(panel,
                "본편 진행 조건을 무시하고 지역의 기본 시작 위치로 이동합니다. 퀘스트와 길 해금 상태는 바꾸지 않습니다.",
                15, TextSub, TextAlignmentOptions.Left);
            SetSize(note.gameObject, minH: 44, prefH: 44);

            var listScroll = MakeScroll(panel, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _travelListContent = listScroll;
        }

        private void RefreshTravelList() => RefreshTravelListAsync().Forget();

        private async UniTaskVoid RefreshTravelListAsync()
        {
            if (_travelListContent == null || _isTravelListLoading)
                return;

            CheatManager cheat = CheatManager.Instance;
            if (cheat == null)
            {
                ShowTravelMessage("CheatManager 준비 대기 중…");
                return;
            }

            _isTravelListLoading = true;
            ShowTravelMessage("지도 데이터 불러오는 중…");
            MapConfigDatabaseSO database;
            try
            {
                database = await cheat.LoadTravelMapDatabaseAsync();
            }
            finally
            {
                _isTravelListLoading = false;
            }

            // 불러오는 동안 패널이 파괴될 수 있다.
            if (_travelListContent == null)
                return;

            if (database == null)
            {
                ShowTravelMessage("MapConfigDatabase를 불러오지 못했습니다.");
                return;
            }

            ClearChildren(_travelListContent);
            string currentMapId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int rowIndex = 0;
            foreach (MapConfigDatabaseSO.Entry entry in database.Entries)
            {
                if (!CheatManager.CanTravelToMap(entry.mapId))
                    continue;

                AddTravelRow(entry, entry.mapId == currentMapId, rowIndex++);
            }

            if (rowIndex == 0)
                MakeText(_travelListContent, "이동할 수 있는 지역이 없습니다.", 16, TextSub);

            RebuildLayout();
        }

        private void AddTravelRow(MapConfigDatabaseSO.Entry entry, bool isCurrentMap, int rowIndex)
        {
            string mapId = entry.mapId;
            string displayName = entry.regionInfo != null && !string.IsNullOrWhiteSpace(entry.regionInfo.continentName)
                ? entry.regionInfo.continentName
                : mapId;

            var row = NewRect("Row", _travelListContent);
            SetSize(row.gameObject, minH: 48, prefH: 48);
            AddImage(row.gameObject, rowIndex % 2 == 0 ? RowBg : RowBgAlt);
            AddHLG(row.gameObject, 10, 8);

            var nameText = MakeText(row, isCurrentMap ? $"{displayName}  <color=#5FD0E0>(현재 지역)</color>" : displayName,
                18, TextMain);
            SetSize(nameText.gameObject, flexW: 1);

            var idText = MakeText(row, mapId, 14, TextSub);
            SetSize(idText.gameObject, minW: 220, prefW: 220);

            var travelButton = MakeButton(row, "이동", AccentBtn, () => TravelToMap(mapId, displayName), 16);
            SetSize(travelButton.gameObject, minW: 110, prefW: 110);
        }

        private void TravelToMap(string mapId, string displayName)
        {
            // 패널을 연 채로 씬이 바뀌면 다음 씬 입력을 패널이 계속 막는다.
            if (CheatManager.Instance?.TravelToMap(mapId, displayName) == true)
                Hide();
        }

        private void ShowTravelMessage(string message)
        {
            if (_travelListContent == null)
                return;

            ClearChildren(_travelListContent);
            MakeText(_travelListContent, message, 16, TextSub);
        }
    }
}
#endif
