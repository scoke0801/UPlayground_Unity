#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.AI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.UI.Debugging.Laya
{
    /// <summary>에디터와 Player의 실험 명령을 연결하는 화면 소비자 계약.</summary>
    public sealed class LayaDebuggerCommands
    {
        public Action<int, LayaTrainingMode, string> Start;
        public Action<string, int> Check;
        public Action Stop, Restart, Export, Close;
        public Action<bool> Pause;
    }

    /// <summary>Editor API 없이 같은 진단 DTO를 표시하는 가상화된 한글 디버거.</summary>
    public sealed class LayaDebuggerView : VisualElement, IDisposable
    {
        private readonly Func<LayaDiagnostics> _source;
        private readonly List<LayaDecisionRecord> _rows = new();
        private readonly ListView _list;
        private readonly Label _connection, _target, _footer, _empty, _selection;
        private readonly TextField _detail, _search, _endpoint;
        private readonly DropdownField _profile, _mode, _filter;
        private readonly Toggle _freeze, _follow;
        private readonly VisualElement _history, _inspector;
        private readonly Button _start;
        private readonly List<Button> _tabs = new();
        private LayaDecisionRecord _selected;
        private int _tab, _revision = -1;
        private LayaDiagnostics _store;
        private Tween _tween;
        private readonly IVisualElementScheduledItem _refresh;
        private bool _compact;
        public Action CancelRequested;

        public LayaDebuggerView(LayaDebuggerAssets assets, Func<LayaDiagnostics> source, LayaDebuggerCommands commands)
        {
            _source = source;
            AddToClassList("laya-root");
            assets.layout.CloneTree(this);
            this[0].style.flexGrow = 1; this[0].style.minHeight = 0;
            styleSheets.Add(assets.style);
            style.unityFont = assets.koreanFont;
            _connection = this.Q<Label>("connection"); _target = this.Q<Label>("target");
            _footer = this.Q<Label>("footer"); _empty = this.Q<Label>("empty"); _selection = this.Q<Label>("selection");
            _detail = this.Q<TextField>("detail"); _detail.isReadOnly = true; _detail.multiline = true;
            _history = this.Q("history"); _inspector = this.Q("inspector");
            _search = this.Q<TextField>("search"); _endpoint = this.Q<TextField>("endpoint");
            _endpoint.value = PlayerPrefs.GetString("Laya.Endpoint", "http://127.0.0.1:18765");
            _profile = this.Q<DropdownField>("profile");
            var profiles = new List<string>(); foreach (var p in assets.profiles) profiles.Add(p.definition.displayName + " · " + p.definition.actorId);
            _profile.choices = profiles; _profile.index = source()?.ProfileIndex ?? 0;
            _mode = this.Q<DropdownField>("mode");
            _mode.choices = new List<string> { "기존 AI", "관찰 비교", "Mac 의도 적용" }; _mode.index = source()?.ModeIndex ?? 0;
            _filter = this.Q<DropdownField>("filter");
            _filter.choices = new List<string> { "전체", "문제/폐기", "실제 실행", "지연 500ms 이상" }; _filter.index = 0;
            _freeze = this.Q<Toggle>("freeze"); _follow = this.Q<Toggle>("follow"); _follow.value = true;
            this.Q<Button>("check").clicked += () => commands.Check?.Invoke(_endpoint.value, _profile.index);
            _start = this.Q<Button>("start");
            _start.SetEnabled(commands.Start != null);
            _start.clicked += () => commands.Start?.Invoke(_profile.index, (LayaTrainingMode)_mode.index, _endpoint.value);
            this.Q<Button>("stop").clicked += () => commands.Stop?.Invoke();
            this.Q<Button>("restart").clicked += () => commands.Restart?.Invoke();
            this.Q<Button>("export").clicked += () => commands.Export?.Invoke();
            this.Q<Button>("close").clicked += () => commands.Close?.Invoke();
            this.Q<Toggle>("pause").RegisterValueChangedCallback(e => commands.Pause?.Invoke(e.newValue));
            this.Q<Button>("copy").clicked += () => GUIUtility.systemCopyBuffer = LayaProtocol.Redact(_detail.value);
            this.Q<Button>("history-tab").clicked += () => ShowCompact(false);
            this.Q<Button>("detail-tab").clicked += () => ShowCompact(true);
            _list = this.Q<ListView>("requests");
            _list.itemsSource = _rows; _list.fixedItemHeight = 60; _list.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _list.makeItem = () => { var row = new VisualElement(); row.AddToClassList("request-row"); row.Add(new Label { name = "primary" }); row.Add(new Label { name = "secondary" }); return row; };
            _list.bindItem = (element, index) => {
                var r = _rows[index];
                element.Q<Label>("primary").text = $"{r.sequence:000}  {r.selectedIntent ?? r.localIntent ?? "미수신"}  ·  {r.status}";
                element.Q<Label>("secondary").text = $"{r.actor}  |  왕복 {Ms(r.RoundTripMs)}  |  {r.reason}";
                element.tooltip = r.requestId;
            };
            _list.selectionChanged += items => { foreach (var item in items) { _selected = (LayaDecisionRecord)item; RenderDetail(); break; } };
            string[] names = { "요약·시간", "관측", "후보", "패킷", "실행" };
            for (int i = 0; i < names.Length; i++) { int tab = i; var button = new Button(() => SelectTab(tab)) { text = names[i] }; _tabs.Add(button); this.Q("tabs").Add(button); }
            _search.RegisterValueChangedCallback(_ => { _revision = -1; Refresh(); });
            _filter.RegisterValueChangedCallback(_ => { _revision = -1; Refresh(); });
            RegisterCallback<NavigationCancelEvent>(e => { CancelRequested?.Invoke(); e.StopPropagation(); });
            RegisterCallback<GeometryChangedEvent>(e => { bool compact = e.newRect.width < 1000; if (compact == _compact) return;
                _compact = compact; EnableInClassList("compact", compact); ShowCompact(false); });
            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());
            _refresh = schedule.Execute(Refresh).Every(200);
            style.opacity = 0;
            _tween = DOTween.To(() => resolvedStyle.opacity, v => style.opacity = v, 1, 0.22f).SetUpdate(true);
            SelectTab(0); Refresh();
        }

        public void FocusDefault() => _start.Focus();
        public void StepTab(int delta) => SelectTab((_tab + delta + _tabs.Count) % _tabs.Count);
        public void MoveRequest(int direction)
        {
            if (_rows.Count == 0) return;
            int next = (_list.selectedIndex + direction + _rows.Count) % _rows.Count;
            _list.SetSelection(next); _list.ScrollToItem(next); _list.Focus();
        }
        private void ShowCompact(bool details)
        {
            _history.style.display = !_compact || !details ? DisplayStyle.Flex : DisplayStyle.None;
            _inspector.style.display = !_compact || details ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void SelectTab(int index)
        {
            _tab = index;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].EnableInClassList("selected-tab", i == index);
            RenderDetail();
        }
        private void Refresh()
        {
            if (_freeze.value) return;
            var store = _source();
            if (store == null) { _connection.text = "훈련장을 준비한 뒤 실험을 시작하세요."; return; }
            if (_store == store && _revision == store.Revision) return;
            _store = store; _revision = store.Revision;
            _connection.text = store.Connection + "  |  세션 " + store.Session;
            _connection.tooltip = store.Metadata;
            _target.text = store.TargetSummary;
            _rows.Clear();
            int received = 0, applied = 0, executed = 0, discarded = 0, hits = 0, fallback = 0;
            var delays = new List<double>();
            foreach (var r in store.Records)
            {
                if (r.httpSuccess) received++; if (r.intentApplied) { applied++; delays.Add(r.TotalMs); }
                if (r.executed && r.intentApplied && !r.preempted) executed++;
                if (r.status == "폐기" || r.status == "거절") discarded++;
                if (r.status == "로컬 전환") fallback++;
                hits += r.hits;
                if (!string.IsNullOrWhiteSpace(_search.value) && !(r.requestId + r.actor + r.reason + r.selectedIntent + r.status).Contains(_search.value)) continue;
                if (_filter.index == 1 && r.status != "폐기" && r.status != "거절" && r.status != "로컬 전환") continue;
                if (_filter.index == 2 && !r.executed) continue;
                if (_filter.index == 3 && r.RoundTripMs < 500) continue;
                _rows.Add(r);
            }
            _list.RefreshItems();
            _empty.style.display = _rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_selected != null && !_rows.Contains(_selected)) _selected = null;
            if (_follow.value && _rows.Count > 0) { _selected = _rows[_rows.Count - 1]; _list.SetSelectionWithoutNotify(new[] { _rows.Count - 1 }); _list.ScrollToItem(_rows.Count - 1); }
            delays.Sort();
            string percentiles = delays.Count == 0 ? "미측정" : $"P50 {Ms(delays[(delays.Count - 1) / 2])} / P95 {Ms(delays[(int)((delays.Count - 1) * .95)])}";
            _footer.text = $"보관 {store.Records.Count}/{store.Capacity}  HTTP {received} · 적용 {applied} · Mac 행동 {executed} · 타격 {hits} · 폐기 {discarded} · 로컬 전환 {fallback}\n적용 지연 {percentiles} (보관 중 적용 {applied}건)  |  이력 퇴출 {store.Evicted} · 본문 만료 {store.BodyEvicted}\n{store.Recording}";
            RenderDetail();
        }
        private void RenderDetail()
        {
            var r = _selected;
            _selection.text = r == null ? "요청을 선택하세요" : r.actor + " → " + r.target + "\n" + r.requestId;
            _selection.tooltip = r?.requestId;
            if (r == null) { _detail.value = "실험 전에는 요청이 없습니다. 기존 AI 모드는 Mac에 요청하지 않습니다.\n실험 종료 후에도 기록을 확인하거나 내보낼 수 있습니다."; return; }
            string body = _tab switch {
                0 => $"{r.status}\n{r.reason}\n\n당시 상태: {r.state}\n로컬 선택: {r.localIntent}\nMac 선택: {r.selectedIntent ?? "미수신"}\nHTTP 성공: {Yes(r.httpSuccess)} / 응답 검증: {Yes(r.validated)}\n의도 적용: {Yes(r.intentApplied)} / 실제 실행: {Yes(r.executed)}\n우선 분기 개입: {Yes(r.preempted)} / 확인된 타격: {r.hits}\n\n왕복: {Ms(r.RoundTripMs)}\n서버 추론: {Ms(r.inferenceMs)}\n적용 대기: {Ms(r.ApplyWaitMs)}\n관측→적용: {Ms(r.TotalMs)}\n실제 동작: {Ms(r.ActionMs)}\n\n한 PC의 단조 시간만 사용. 편도 지연은 계산하지 않습니다.\n\n" + string.Join("\n", r.events),
                1 => r.observationJson,
                2 => r.candidatesJson + "\n\n모델 확률\n" + Probabilities(r.responseJson),
                3 => "송신 JSON\n" + r.requestJson + "\n\n응답 JSON\n" + (r.responseJson ?? "미수신"),
                _ => $"분기: {r.branch}\nAbility: {r.ability}\nVariant: {r.variant}\nMotionKey: {r.motionKey}\n시작: {Stamp(r.started)} / 종료: {Stamp(r.ended)}\n타격: {r.hits} (귀속 불확실한 반복 투사체는 집계하지 않음)\n\n" + string.Join("\n", r.events)
            };
            if (r.bodyExpired && _tab >= 1 && _tab <= 3) body = "본문 보관 만료 · 메모리 상한에 따라 원문을 퇴출했습니다. 메타데이터와 실행 기록은 남아 있습니다.";
            _detail.value = LayaProtocol.Redact(body ?? "미확인");
        }
        private static string Yes(bool value) => value ? "예" : "아니오";
        private static string Probabilities(string json)
        {
            if (string.IsNullOrEmpty(json)) return "미수신";
            try { return Newtonsoft.Json.Linq.JObject.Parse(json)["probabilities"]?.ToString() ?? "미확인"; }
            catch { return "응답 JSON 오류"; }
        }
        private static string Ms(double value) => value < 0 ? "미측정" : value.ToString("0.0") + "ms";
        private static string Stamp(double value) => value < 0 ? "미확인" : value.ToString("0.000") + "s";
        public void Dispose() { _refresh?.Pause(); _tween?.Kill(); _tween = null; }
    }
}
#endif
