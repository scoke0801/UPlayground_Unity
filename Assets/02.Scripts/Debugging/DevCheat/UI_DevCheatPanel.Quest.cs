#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 퀘스트 탭(수락/완료/실패/추적).</summary>
    public partial class UI_DevCheatPanel
    {
        private TMP_InputField _questSearch;
        private RectTransform  _questListContent;
        private string         _questSelectedId;
        private string         _questSelectedName;
        private TextMeshProUGUI _questName, _questIdText, _questStatusText, _questDesc;

        private void BuildQuestTab(RectTransform panel)
        {
            AddHLG(panel.gameObject, 12, 12);

            // 좌: 검색 + 리스트
            var center = NewRect("QuestCenter", panel);
            SetSize(center.gameObject, flexW: 1);
            AddImage(center.gameObject, PanelBg);
            var cv = AddVLG(center.gameObject, 8, 8);
            cv.childForceExpandHeight = false;

            _questSearch = MakeInput(center, "QuestId 또는 이름 검색", _ => RefreshQuestList());
            SetSize(_questSearch.gameObject, minH: 40, prefH: 40);

            var listScroll = MakeScroll(center, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _questListContent = listScroll;

            // 우: 상세 + 액션
            var right = NewRect("QuestDetail", panel);
            SetSize(right.gameObject, minW: 360, prefW: 360);
            AddImage(right.gameObject, PanelBg);
            var rv = AddVLG(right.gameObject, 8, 12);
            rv.childForceExpandHeight = false;

            _questName      = MakeText(right, "-", 22, TextMain, TextAlignmentOptions.Center);
            SetSize(_questName.gameObject, minH: 34, prefH: 34);
            _questIdText    = MakeText(right, "QuestId  -", 15, TextSub, TextAlignmentOptions.Center);
            _questStatusText= MakeText(right, "상태  -", 16, Accent, TextAlignmentOptions.Center);
            _questDesc      = MakeText(right, "-", 15, TextSub, TextAlignmentOptions.Left);
            SetSize(_questDesc.gameObject, minH: 80, prefH: 80);

            var accept = MakeButton(right, "수락", AccentBtn, () => QuestAction(0), 20);
            SetSize(accept.gameObject, minH: 46, prefH: 46);
            var complete = MakeButton(right, "완료", new Color(0.16f, 0.42f, 0.30f), () => QuestAction(1), 20);
            SetSize(complete.gameObject, minH: 46, prefH: 46);
            var fail = MakeButton(right, "실패", DangerBtn, () => QuestAction(2), 20);
            SetSize(fail.gameObject, minH: 46, prefH: 46);
            var track = MakeButton(right, "추적", BtnBg, () => QuestAction(3), 18);
            SetSize(track.gameObject, minH: 42, prefH: 42);
        }

        private void RefreshQuestList()
        {
            if (_questListContent == null) return;
            ClearChildren(_questListContent);

            var qm = QuestManager.Instance;
            if (qm == null || !qm.IsDBLoaded)
            {
                MakeText(_questListContent, "QuestDatabase 로드 대기 중…", 16, TextSub);
                RefreshQuestDetail();
                return;
            }

            string search = _questSearch != null ? _questSearch.text : string.Empty;
            int rowIndex = 0;
            foreach (var quest in qm.GetAllQuestDefinitions())
            {
                if (quest == null || string.IsNullOrEmpty(quest.questId)) continue;
                if (!MatchesQuestSearch(quest, search)) continue;

                string id = quest.questId;
                string name = string.IsNullOrEmpty(quest.questName) ? id : quest.questName;
                QuestStatus status = qm.GetQuestStatus(id);

                var row = NewRect("Row", _questListContent);
                SetSize(row.gameObject, minH: 44, prefH: 44);
                var bg = AddImage(row.gameObject, id == _questSelectedId ? AccentBtn : (rowIndex++ % 2 == 0 ? RowBg : RowBgAlt));
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SelectQuest(id, name));

                var rh = AddHLG(row.gameObject, 8, 8);
                rh.childForceExpandWidth = false;
                var nameT = MakeText(row, name, 16, TextMain); SetSize(nameT.gameObject, flexW: 1);
                MakeText(row, StatusLabel(status), 14, StatusColor(status));
            }

            if (_questListContent.childCount == 0)
                MakeText(_questListContent, "검색 결과가 없습니다.", 16, TextSub);
        }

        private static bool MatchesQuestSearch(QuestSO quest, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            search = search.Trim();
            if (quest.questId.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return !string.IsNullOrEmpty(quest.questName) &&
                   quest.questName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectQuest(string id, string name)
        {
            _questSelectedId = id;
            _questSelectedName = name;
            RefreshQuestList();
            RefreshQuestDetail();
        }

        private void RefreshQuestDetail()
        {
            var qm = QuestManager.Instance;
            QuestSO quest = qm != null ? qm.GetQuestData(_questSelectedId) : null;
            if (quest == null)
            {
                if (_questName != null) _questName.text = "-";
                if (_questIdText != null) _questIdText.text = "QuestId  -";
                if (_questStatusText != null) _questStatusText.text = "상태  -";
                if (_questDesc != null) _questDesc.text = "-";
                return;
            }

            QuestStatus status = qm.GetQuestStatus(quest.questId);
            if (_questName != null) _questName.text = string.IsNullOrEmpty(quest.questName) ? quest.questId : quest.questName;
            if (_questIdText != null) _questIdText.text = $"QuestId  {quest.questId}";
            if (_questStatusText != null)
            {
                _questStatusText.text = $"상태  {StatusLabel(status)}";
                _questStatusText.color = StatusColor(status);
            }
            if (_questDesc != null)
                _questDesc.text = string.IsNullOrEmpty(quest.questDescription) ? "설명 없음" : quest.questDescription;
        }

        private void QuestAction(int action)
        {
            if (string.IsNullOrEmpty(_questSelectedId)) return;
            var cheat = CheatManager.Instance;
            if (cheat == null) return;

            switch (action)
            {
                case 0: cheat.AcceptQuest(_questSelectedId, _questSelectedName);   break;
                case 1: cheat.CompleteQuest(_questSelectedId, _questSelectedName); break;
                case 2: cheat.FailQuest(_questSelectedId, _questSelectedName);     break;
                case 3: cheat.TrackQuest(_questSelectedId, _questSelectedName);    break;
            }
            RefreshQuestList();
            RefreshQuestDetail();
        }

        private static string StatusLabel(QuestStatus s) => s switch
        {
            QuestStatus.Locked => "잠김",
            QuestStatus.Available => "가능",
            QuestStatus.Active => "진행중",
            QuestStatus.Completed => "완료",
            QuestStatus.Failed => "실패",
            _ => "-",
        };

        private static Color StatusColor(QuestStatus s) => s switch
        {
            QuestStatus.Active => Accent,
            QuestStatus.Completed => Positive,
            QuestStatus.Failed => new Color(0.85f, 0.4f, 0.4f),
            _ => TextSub,
        };
    }
}
#endif
