#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 도감 탭(도감 대상 등록/제거).</summary>
    public partial class UI_DevCheatPanel
    {
        private TMP_InputField _codexSearch;
        private RectTransform  _codexListContent;
        private string         _codexSelectedId;
        private string         _codexSelectedName;
        private TextMeshProUGUI _codexName, _codexIdText, _codexStatusText, _codexBonusText;

        private void BuildCodexTab(RectTransform panel)
        {
            AddHLG(panel.gameObject, 12, 12);

            // 좌: 검색 + 리스트
            var center = NewRect("CodexCenter", panel);
            SetSize(center.gameObject, flexW: 1);
            AddImage(center.gameObject, PanelBg);
            var cv = AddVLG(center.gameObject, 8, 8);
            cv.childForceExpandHeight = false;

            _codexSearch = MakeInput(center, "ActorId 또는 이름 검색", _ => RefreshCodexList());
            SetSize(_codexSearch.gameObject, minH: 40, prefH: 40);

            var listScroll = MakeScroll(center, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _codexListContent = listScroll;

            // 우: 상세 + 액션
            var right = NewRect("CodexDetail", panel);
            SetSize(right.gameObject, minW: 360, prefW: 360);
            AddImage(right.gameObject, PanelBg);
            var rv = AddVLG(right.gameObject, 8, 12);
            rv.childForceExpandHeight = false;

            _codexName       = MakeText(right, "-", 22, TextMain, TextAlignmentOptions.Center);
            SetSize(_codexName.gameObject, minH: 34, prefH: 34);
            _codexIdText     = MakeText(right, "ActorId  -", 15, TextSub, TextAlignmentOptions.Center);
            _codexStatusText = MakeText(right, "기록  -", 16, Accent, TextAlignmentOptions.Center);
            _codexBonusText  = MakeText(right, "-", 15, TextSub, TextAlignmentOptions.Left);
            SetSize(_codexBonusText.gameObject, minH: 90, prefH: 90);

            var register = MakeButton(right, "도감 등록 (100% 기록)", AccentBtn, () => CodexAction(true), 18);
            SetSize(register.gameObject, minH: 48, prefH: 48);
            var remove = MakeButton(right, "도감 제거", DangerBtn, () => CodexAction(false), 18);
            SetSize(remove.gameObject, minH: 48, prefH: 48);
        }

        private void RefreshCodexList()
        {
            if (_codexListContent == null) return;
            ClearChildren(_codexListContent);

            var mgr = MonsterCodexManager.Instance;
            if (mgr == null || !mgr.IsDatabaseLoaded)
            {
                MakeText(_codexListContent, "MonsterCodexDatabase 로드 대기 중…", 16, TextSub);
                RefreshCodexDetail();
                return;
            }

            string search = _codexSearch != null ? _codexSearch.text : string.Empty;
            int rowIndex = 0;
            foreach (MonsterCodexEntryView view in mgr.GetAllEntries())
            {
                if (view == null || string.IsNullOrEmpty(view.actorId)) continue;
                if (!MatchesCodexSearch(view, search)) continue;

                string id = view.actorId;
                string name = string.IsNullOrEmpty(view.displayName) ? id : view.displayName;
                int percent = Mathf.RoundToInt(view.recordRatio * 100f);
                string elementLabel = CodexElementLabel(view);

                var row = NewRect("Row", _codexListContent);
                SetSize(row.gameObject, minH: 44, prefH: 44);
                var bg = AddImage(row.gameObject, id == _codexSelectedId ? AccentBtn : (rowIndex++ % 2 == 0 ? RowBg : RowBgAlt));
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SelectCodex(id, name));

                var rh = AddHLG(row.gameObject, 8, 8);
                rh.childForceExpandWidth = false;
                var nameT = MakeText(row, name, 16, view.discovered ? TextMain : TextSub);
                SetSize(nameT.gameObject, flexW: 1);
                MakeText(row, elementLabel, 14, TextSub, TextAlignmentOptions.Center);
                var pctT = MakeText(row, $"{percent}%", 15, view.discovered ? Positive : TextSub, TextAlignmentOptions.Right);
                SetSize(pctT.gameObject, minW: 56, prefW: 56);
            }

            if (_codexListContent.childCount == 0)
                MakeText(_codexListContent, "검색 결과가 없습니다.", 16, TextSub);

            RefreshCodexDetail();
        }

        private static bool MatchesCodexSearch(MonsterCodexEntryView view, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            search = search.Trim();
            if (view.actorId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return !string.IsNullOrEmpty(view.displayName) &&
                   view.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectCodex(string id, string name)
        {
            _codexSelectedId = id;
            _codexSelectedName = name;
            RefreshCodexList();
        }

        private void RefreshCodexDetail()
        {
            MonsterCodexEntryView view = FindCodexView(_codexSelectedId);
            if (view == null)
            {
                if (_codexName != null) _codexName.text = "-";
                if (_codexIdText != null) _codexIdText.text = "ActorId  -";
                if (_codexStatusText != null) _codexStatusText.text = "기록  -";
                if (_codexBonusText != null) _codexBonusText.text = "-";
                return;
            }

            int percent = Mathf.RoundToInt(view.recordRatio * 100f);
            if (_codexName != null)
                _codexName.text = string.IsNullOrEmpty(view.displayName) ? view.actorId : view.displayName;
            if (_codexIdText != null)
                _codexIdText.text = $"ActorId  {view.actorId}   [{view.grade}]  {CodexElementLabel(view)}";
            if (_codexStatusText != null)
                _codexStatusText.text = view.discovered
                    ? $"기록  {percent}%  ({view.killCount}/{view.fullRecordKillCount})"
                    : "기록  미발견";
            if (_codexBonusText != null)
                _codexBonusText.text =
                    $"경험치 획득  x{view.ExpMultiplier:0.00}\n" +
                    $"가하는 피해  x{view.DamageDealtMultiplier:0.00}\n" +
                    $"입는 피해     x{view.DamageTakenMultiplier:0.00}";
        }

        private void CodexAction(bool register)
        {
            if (string.IsNullOrEmpty(_codexSelectedId)) return;
            var cheat = CheatManager.Instance;
            if (cheat == null) return;

            if (register)
                cheat.RegisterCodexTarget(_codexSelectedId, _codexSelectedName);
            else
                cheat.RemoveCodexTarget(_codexSelectedId, _codexSelectedName);

            RefreshCodexList();
        }

        private static MonsterCodexEntryView FindCodexView(string actorId)
        {
            if (string.IsNullOrEmpty(actorId)) return null;
            var mgr = MonsterCodexManager.Instance;
            if (mgr == null) return null;

            foreach (MonsterCodexEntryView view in mgr.GetAllEntries())
                if (view != null && view.actorId == actorId)
                    return view;
            return null;
        }

        // 랜덤 속성 몬스터가 아직 미발견이면 '?'로 표시한다(도감 표시 규칙과 동일).
        private static string CodexElementLabel(MonsterCodexEntryView view)
        {
            if (view.elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame && !view.discovered)
                return "?";
            return ElementLabel(view.element);
        }

        private static string ElementLabel(CombatElement element) => element switch
        {
            CombatElement.Fire => "불",
            CombatElement.Water => "물",
            CombatElement.Nature => "자연",
            CombatElement.Light => "빛",
            CombatElement.Dark => "어둠",
            _ => "무속성",
        };
    }
}
#endif
