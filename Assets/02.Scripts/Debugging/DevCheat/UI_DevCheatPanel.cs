#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>
    /// 개발 치트 패널. 개발 빌드(및 에디터)에서만 컴파일·표시된다.
    ///
    /// 프리팹은 골격(헤더/좌측 탭 레일/탭 콘텐츠 컨테이너 7개/하단 로그 바)만 갖고,
    /// 각 탭의 실제 콘텐츠(아이템 리스트/스탯 필드/파티 행 등)는 런타임에 코드로 생성한다.
    /// 이렇게 하면 프리팹 빌더와 런타임 스크립트의 결합이 최소화되고 유지보수가 쉬워진다.
    ///
    /// 10개 탭: 기즈모 / 아이템 / 퀘스트 / 플레이어 스텟 / 파티원 / 시간 / 전투 / 버프·디버프 / 도감 / 스폰.
    /// 모든 조작은 <see cref="CheatManager"/> 파사드를 통해 실행되어 하단 실행 로그에 기록된다.
    /// </summary>
    public partial class UI_DevCheatPanel : UI_Base
    {
        public enum CheatTab { Gizmo, Item, Quest, Stat, Party, Time, Combat, Effect, Codex, Spawn }

        [Header("Dev Cheat — 구조 참조 (프리팹 빌더가 연결)")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _tabButtons;          // 순서: Gizmo, Item, Quest, Stat, Party, Time, Combat, Effect
        [SerializeField] private RectTransform[] _tabPanels;    // _tabButtons 와 1:1
        [SerializeField] private TextMeshProUGUI _tabPreviewText;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private Button _clearLogButton;

        private CheatTab _currentTab = CheatTab.Gizmo;
        private readonly bool[] _tabBuilt = new bool[Enum.GetValues(typeof(CheatTab)).Length];

        // ── 팔레트 ────────────────────────────────────────────────
        protected static readonly Color PanelBg   = new(0.10f, 0.13f, 0.17f, 1f);
        protected static readonly Color RowBg      = new(0.14f, 0.17f, 0.22f, 1f);
        protected static readonly Color RowBgAlt   = new(0.16f, 0.20f, 0.26f, 1f);
        protected static readonly Color BtnBg      = new(0.20f, 0.28f, 0.34f, 1f);
        protected static readonly Color AccentBtn  = new(0.16f, 0.42f, 0.52f, 1f);
        protected static readonly Color DangerBtn  = new(0.42f, 0.16f, 0.18f, 1f);
        protected static readonly Color TextMain   = new(0.90f, 0.92f, 0.95f, 1f);
        protected static readonly Color TextSub    = new(0.62f, 0.68f, 0.74f, 1f);
        protected static readonly Color Accent     = new(0.35f, 0.80f, 0.90f, 1f);
        protected static readonly Color Positive   = new(0.35f, 0.85f, 0.45f, 1f);

        protected override bool BlocksLowerInput => true;

        #region 생명주기

        protected override void OnInit()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            if (_clearLogButton != null)
                _clearLogButton.onClick.AddListener(() => CheatManager.Instance?.ClearLog());

            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int idx = i;
                    if (_tabButtons[i] != null)
                        _tabButtons[i].onClick.AddListener(() => SelectTab((CheatTab)idx));
                }
            }
        }

        protected override void OnShow()
        {
            // 핵심 수정: 루트 ScreenSpaceOverlay Canvas가 프리팹 저장 시 구동 스케일 0으로 직렬화되고,
            // 중첩 Canvas는 스케일이 재구동되지 않아 월드 스케일 0(=화면상 0 크기)으로 렌더된다.
            // 표시할 때마다 스케일을 1로 강제 복구한다.
            if (_rectTransform != null) _rectTransform.localScale = Vector3.one;

            // 안전장치: 페이드/애니메이터 잔상으로 알파가 0이거나 캔버스가 꺼진 경우 강제 복구.
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_canvas != null) _canvas.enabled = true;

            if (CheatManager.Instance != null)
                CheatManager.Instance.OnLogChanged += RefreshLog;

            BindEffectCheatEvents();
            SelectTab(_currentTab);
            RefreshLog();
        }

        protected override void OnHide()
        {
            if (CheatManager.Instance != null)
                CheatManager.Instance.OnLogChanged -= RefreshLog;
            UnbindEffectCheatEvents();
        }

        #endregion

        #region 탭 전환

        private void SelectTab(CheatTab tab)
        {
            _currentTab = tab;

            if (_tabPanels != null)
            {
                for (int i = 0; i < _tabPanels.Length; i++)
                    if (_tabPanels[i] != null)
                        _tabPanels[i].gameObject.SetActive(i == (int)tab);
            }

            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                    HighlightTabButton(_tabButtons[i], i == (int)tab);
            }

            EnsureTabBuilt(tab);
            RefreshTab(tab);
            UpdatePreviewText(tab);
            RebuildLayout();
        }

        // 프리팹은 레이아웃이 계산되지 않은(0 크기) 상태로 저장되고, 탭 콘텐츠도 런타임에 생성되므로
        // 표시/탭 전환 시 레이아웃을 즉시 재계산한다. (미호출 시 요소가 0 크기로 겹쳐 보이지 않는다.)
        private void RebuildLayout()
        {
            if (_rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }

        private void EnsureTabBuilt(CheatTab tab)
        {
            int idx = (int)tab;
            if (_tabBuilt[idx] || _tabPanels == null || idx >= _tabPanels.Length || _tabPanels[idx] == null)
                return;

            RectTransform panel = _tabPanels[idx];
            switch (tab)
            {
                case CheatTab.Gizmo: BuildGizmoTab(panel); break;
                case CheatTab.Item:  BuildItemTab(panel);  break;
                case CheatTab.Quest: BuildQuestTab(panel); break;
                case CheatTab.Stat:  BuildStatTab(panel);  break;
                case CheatTab.Party: BuildPartyTab(panel); break;
                case CheatTab.Time:  BuildTimeTab(panel);  break;
                case CheatTab.Combat: BuildCombatTab(panel); break;
                case CheatTab.Effect: BuildEffectTab(panel); break;
                case CheatTab.Codex: BuildCodexTab(panel); break;
                case CheatTab.Spawn: BuildSpawnTab(panel); break;
            }
            _tabBuilt[idx] = true;
        }

        // 탭이 표시될 때마다 최신 상태로 갱신(데이터가 런타임에 바뀔 수 있으므로).
        private void RefreshTab(CheatTab tab)
        {
            switch (tab)
            {
                case CheatTab.Item:  RefreshItemList();  break;
                case CheatTab.Quest: RefreshQuestList(); break;
                case CheatTab.Stat:  RefreshStatValues(); break;
                case CheatTab.Party: RefreshPartyList(); break;
                case CheatTab.Time:  RefreshTimeInfo();  break;
                case CheatTab.Combat: RefreshCombatTab(); break;
                case CheatTab.Effect: RefreshEffectTab(); break;
                case CheatTab.Codex: RefreshCodexList(); break;
                case CheatTab.Spawn: RefreshSpawnList(); break;
            }
        }

        private static void HighlightTabButton(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = selected ? AccentBtn : PanelBg;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = selected ? Color.white : TextSub;
        }

        private void UpdatePreviewText(CheatTab tab)
        {
            if (_tabPreviewText == null) return;
            _tabPreviewText.text = tab switch
            {
                CheatTab.Gizmo => "<b>기즈모</b>\nHitbox 런타임 렌더(개발 빌드 포함),\nDetection/AI/Nav 토글(에디터 씬뷰).",
                CheatTab.Item  => "<b>아이템</b>\nID/이름 검색, 카테고리 필터,\n수량 지정 생성/삭제/최대치 지급.",
                CheatTab.Quest => "<b>퀘스트</b>\nQuestId 선택 후 수락/완료/실패/추적.",
                CheatTab.Stat  => "<b>플레이어 스텟</b>\nMaxHealth/AttackPower/Defense 등\nbase 스탯 즉시 변경.",
                CheatTab.Party => "<b>파티원</b>\n캐릭터 해금, 레벨 설정, 경험치 지급,\n파티 회복, 스왑 쿨 초기화.",
                CheatTab.Time  => "<b>시간</b>\n인게임 시간 스킵(+10분~+1일),\n특정 시각 이동, 시계 배속.",
                CheatTab.Combat => "<b>전투</b>\n항상 패리 토글,\n주변 몬스터 즉시 처치(반경 지정).",
                CheatTab.Effect => "<b>버프 / 디버프</b>\n활성 캐릭터에게 Effect 발급,\n활성 Effect 개별 또는 전체 제거.",
                CheatTab.Codex => "<b>도감</b>\n몬스터 선택 후 도감 대상 등록(100% 기록)\n또는 기록 제거.",
                CheatTab.Spawn => "<b>스폰</b>\nActorDatabase 액터를 검색·선택해\n플레이어 전방에 마리 수/거리 지정 소환.",
                _ => string.Empty,
            };
        }

        #endregion

        #region 로그

        private readonly StringBuilder _logBuilder = new(1024);

        private void RefreshLog()
        {
            if (_logText == null) return;

            var cheat = CheatManager.Instance;
            _logBuilder.Clear();
            if (cheat != null)
            {
                IReadOnlyList<CheatLogEntry> logs = cheat.RecentLogs;
                for (int i = 0; i < logs.Count; i++)
                {
                    CheatLogEntry e = logs[i];
                    _logBuilder.Append("<color=#8FA6B5>[")
                               .Append(e.Time.ToString("HH:mm:ss"))
                               .Append("]</color> <color=#5FD0E0>")
                               .Append(e.Category)
                               .Append("</color>  ")
                               .Append(e.Message)
                               .Append('\n');
                }
            }
            _logText.text = _logBuilder.ToString();
        }

        #endregion
    }
}
#endif
