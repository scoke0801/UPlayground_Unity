#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.AI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Training;
using UPlayGround.UI;
using UPlayGround.UI.Debugging.Laya;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UPlayGround.Debugging.Laya
{
    /// <summary>훈련장 씬 수명이 소유하는 Player 디버거와 실험 조립부.</summary>
    public sealed class LayaTrainingHost : MonoBehaviour
    {
        public static LayaTrainingHost Current { get; private set; }
        public static LayaDiagnostics LastDiagnostics { get; private set; }
        public LayaDiagnostics Diagnostics { get; private set; } = new();
        public LayaDebuggerAssets Assets { get; private set; }
        private TrainingGroundSession _training;
        private EnemyRemoteIntentController _remote;
        private UIDocument _document;
        private LayaDebuggerView _view;
        private Label _summary;
        private bool _detail, _previousSuppression, _checking, _paused;
        private int _pauseRequest = -1;
        private GameObject _previousFocus;
        private string _verifiedEndpoint;
        private int _verifiedProfile = -1;
        private readonly CancellationTokenSource _lifetime = new();
        private double _nextSummary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Current = null; LastDiagnostics = null; }

        public void Configure(TrainingGroundSession training)
        {
            _training = training; Current = this; LastDiagnostics = Diagnostics;
            Assets = Resources.Load<LayaDebuggerAssets>("Laya/DebuggerAssets");
            if (Assets == null) { Diagnostics.Connection = "디버거 리소스 누락"; return; }
            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = Assets.panel; _document.sortingOrder = 10000;
            ShowSummary(); StartCoroutine(MonitorControls());
        }

        public LayaDebuggerCommands CreateCommands() => new() {
            Start = StartExperiment, Check = (url, profile) => CheckConnection(url, profile).Forget(),
            Stop = StopExperiment, Restart = () => { SetPaused(false); _training.Restart(); },
            Export = Export, Close = ShowSummary, Pause = SetPaused };

        public void OpenDetails()
        {
            if (_detail || Assets == null) return;
            _detail = true;
            _previousFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            EventSystem.current?.SetSelectedGameObject(null);
            _previousSuppression = Svc.Input.IsPlayerActionInputSuppressed;
            Svc.Input.SetPlayerActionInputSuppressed(true); Svc.Input.ShowCursor(true);
            BindTabs(true); _document.rootVisualElement.Clear();
            _view = new LayaDebuggerView(Assets, () => Diagnostics, CreateCommands());
            _view.CancelRequested = ShowSummary;
            _document.rootVisualElement.Add(_view);
            _view.schedule.Execute(_view.FocusDefault).StartingIn(50);
        }

        public void ShowSummary()
        {
            if (_document == null) return;
            if (_detail)
            {
                _detail = false; BindTabs(false);
                Svc.Input?.SetPlayerActionInputSuppressed(_previousSuppression);
                Svc.Input?.SuppressPlayerActionInputBriefly(); Svc.Input?.ShowCursor(false);
                if (_previousFocus != null) EventSystem.current?.SetSelectedGameObject(_previousFocus);
            }
            _view?.Dispose(); _view = null; _document.rootVisualElement.Clear();
            _document.rootVisualElement.pickingMode = PickingMode.Ignore;
            _summary = new Label("Laya · 실험 대기") { pickingMode = PickingMode.Ignore };
            _summary.style.unityFont = Assets.koreanFont;
            _summary.style.position = Position.Absolute; _summary.style.right = 12; _summary.style.top = 12;
            _summary.style.width = 330; _summary.style.whiteSpace = WhiteSpace.Normal;
            _summary.style.fontSize = 14; _summary.style.color = new Color(.88f,.93f,.96f);
            _summary.style.backgroundColor = new Color(.08f,.1f,.13f,.85f);
            _summary.style.paddingLeft = _summary.style.paddingRight = 10;
            _summary.style.paddingTop = _summary.style.paddingBottom = 8;
            _document.rootVisualElement.Add(_summary);
        }

        private void BindTabs(bool bind)
        {
            Bind(UIAction.MainTabPrevious, PreviousTab, bind); Bind(UIAction.SubTabPrevious, PreviousTab, bind);
            Bind(UIAction.MainTabNext, NextTab, bind); Bind(UIAction.SubTabNext, NextTab, bind);
            Bind(UIAction.Cancel, Cancel, bind);
        }
        private void Bind(string action, Action<InputAction.CallbackContext> handler, bool bind)
        {
            if (bind) Svc.Input.RegisterInputEvent(InputMapNames.UI, action, null, handler, null, () => _detail, null, InputLayer.Level_Top);
            else Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, action, null, handler, null);
        }
        private void PreviousTab(InputAction.CallbackContext _) => _view?.StepTab(-1);
        private void NextTab(InputAction.CallbackContext _) => _view?.StepTab(1);
        private void Cancel(InputAction.CallbackContext _) => ShowSummary();

        private IEnumerator MonitorControls()
        {
            while (true)
            {
                if (Keyboard.current?.f8Key.wasPressedThisFrame == true)
                { if (_detail) ShowSummary(); else OpenDetails(); }
                if (Time.realtimeSinceStartupAsDouble >= _nextSummary)
                {
                    _nextSummary = Time.realtimeSinceStartupAsDouble + .2;
                    if (_training.Opponents.Count > 0 && _training.Opponents[0] != null)
                    {
                        var actor = _training.Opponents[0];
                        var combat = actor.GetComponent<UPlayGround.Components.EnemyCombat>();
                        var movement = actor.GetComponent<UPlayGround.MovementController.ActorMovementController>();
                        Diagnostics.TargetSummary = $"{actor.name} #{actor.GetInstanceID()}\n{(actor.IsAlive() ? "생존" : "사망")} / {(_training.IsEnemyStopped ? "정지" : "진행")}\n상태: {movement.CurrentState?.StateId}\n공격: {combat.CurrentAbility?.abilityId ?? "없음"}\n대상: {actor.Detection.CurrentTarget?.name ?? "없음"}\n거리: {(actor.Detection.CurrentTarget != null ? actor.Detection.DistanceToTarget.ToString("0.0") + "m" : "미측정")}\nHP: {actor.GetHealthPercent():P0}";
                        Diagnostics.Revision++;
                    }
                    if (!_detail && _summary != null) _summary.text = "Laya · " + Diagnostics.Connection + "\n기록 " + Diagnostics.Records.Count + "건 · 상세는 개발 치트 패널 또는 F8";
                }
                yield return null;
            }
        }

        public async UniTask CheckConnection(string endpoint, int profile)
        {
            if (_checking || Assets == null || profile < 0 || profile >= Assets.profiles.Length) return;
            _checking = true; _verifiedEndpoint = null;
            Diagnostics.Connection = "연결 검사 중"; Diagnostics.Revision++;
            try
            {
                if (!LayaProtocol.TryEndpoint(endpoint, out _)) throw new InvalidOperationException("서버 루트 주소만 입력하세요. 계정·토큰·쿼리 제외");
                var p = Assets.profiles[profile];
                var reply = await LayaDecisionClient.Send(endpoint, null, p.timeoutSeconds, _lifetime.Token);
                if (reply.Error != null) throw new InvalidOperationException(reply.Error);
                var health = JObject.Parse(reply.Body);
                if (!LayaProtocol.ValidateHealth(health, p))
                    throw new InvalidOperationException("몬스터 worker-config 또는 모델 정보 불일치");
                _verifiedEndpoint = endpoint; _verifiedProfile = profile;
                _remote?.RecheckConnection(endpoint, p);
                PlayerPrefs.SetString("Laya.Endpoint", endpoint);
                Diagnostics.Connection = "연결 검사 통과 · " + endpoint;
                Diagnostics.Metadata = LayaProtocol.Redact(health.ToString());
            }
            catch (Exception error) { Diagnostics.Connection = "연결 실패: " + LayaProtocol.Redact(error.Message); }
            finally { _checking = false; Diagnostics.Revision++; }
        }

        private void StartExperiment(int index, LayaTrainingMode mode, string endpoint)
        {
            if (_training.HasEncounter) { Diagnostics.Connection = "기존 대련 있음 · 훈련장 재시작 후 시작하세요"; Diagnostics.Revision++; return; }
            if (index < 0 || index >= Assets.profiles.Length) return;
            if (mode != LayaTrainingMode.Native && (_verifiedEndpoint != endpoint || _verifiedProfile != index))
            { Diagnostics.Connection = "선택 프로필로 연결 검사를 먼저 통과해야 합니다."; Diagnostics.Revision++; return; }
            var profile = Assets.profiles[index];
            if (!_training.TryStartSingleDefinition(profile.definition, profile.startDistance, out string message))
            { Diagnostics.Connection = message; Diagnostics.Revision++; return; }
            Diagnostics = new LayaDiagnostics { ProfileIndex = index, ModeIndex = (int)mode, Capacity = profile.historyCapacity, BodyBudget = profile.bodyBudgetBytes,
                Connection = mode == LayaTrainingMode.Native ? "기존 AI · 요청 없음" : "연결됨 · " + mode,
                Metadata = Diagnostics.Metadata + "\nUnity " + Application.unityVersion + " / build " + Application.buildGUID
                    + " / profile " + profile.name + " / descriptions " + profile.descriptionVersion };
            LastDiagnostics = Diagnostics;
            _remote = _training.Opponents[0].gameObject.AddComponent<EnemyRemoteIntentController>();
            _remote.Configure(profile, mode, endpoint, Diagnostics);
            _training.Opponents[0].Detection.AcquireTarget(UISvc.Party.ActiveCharacter.transform);
            UISvc.UI.HideUI(UI_Scene_TrainingGround.UIKey); ShowSummary();
        }
        private void StopExperiment()
        { _remote?.StopExperiment("사용자 종료"); _training.SetEnemyStopped(true); SetPaused(false); Export(); }
        private void SetPaused(bool pause)
        {
            if (_paused == pause) return;
            if (pause) _pauseRequest = Svc.GameTime.Request(0);
            else if (_pauseRequest >= 0) { Svc.GameTime?.Release(_pauseRequest); _pauseRequest = -1; }
            _paused = pause;
        }
        public void Export()
        {
#if UNITY_EDITOR
            string root = System.IO.Path.GetFullPath("Tools/LayaWorker/results/training");
#else
            string root = System.IO.Path.Combine(Application.persistentDataPath, "LayaTraining");
#endif
            _ = Diagnostics.ExportAsync(root);
        }
        private void OnDestroy()
        {
            _remote?.StopExperiment("씬 종료"); ShowSummary(); SetPaused(false); _view?.Dispose();
            _lifetime.Cancel(); _lifetime.Dispose(); LastDiagnostics = Diagnostics;
            if (Current == this) Current = null;
        }
    }
}
#endif
