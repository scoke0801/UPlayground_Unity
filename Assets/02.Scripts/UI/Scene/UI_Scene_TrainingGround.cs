using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>상대 선택·배치·반복 연습을 제공하는 훈련장 화면.</summary>
    public sealed class UI_Scene_TrainingGround : UI_SceneBase
    {
        public const string UIKey = "TrainingGround";
        [SerializeField] private TrainingGroundConfigSO _config;
        [SerializeField] private Button _modeButton, _tierButton, _weaponButton, _bossButton;
        [SerializeField] private Button _distanceDownButton, _distanceUpButton;
        [SerializeField] private TMP_Text _distanceText, _statusText;
        [SerializeField] private Button _invincibleButton, _stopButton, _partyButton;
        [SerializeField] private Button _targetButton, _attackButton;
        [SerializeField] private Button _startButton, _restartButton, _returnButton, _closeButton;
        [SerializeField] private GameObject _formationPanel;
        [SerializeField] private Button[] _lateralButtons, _forwardButtons;
        private readonly List<GameplayAbilitySO> _attacks = new();
        private IUITrainingService _session;
        private IGameTimeService _time;
        private bool _didPauseGame;
        private TrainingEncounter _mode;
        private int _tier, _weapon, _boss, _target, _attack;
        private float _distance;
        private Vector2[] _formation;
        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            base.Awake();
            _distance = _config.singleDistance;
            _formation = (Vector2[])_config.formation.Clone();
            _modeButton.onClick.AddListener(() => { _mode = (TrainingEncounter)(((int)_mode + 1) % 3);
                _distance = _mode == TrainingEncounter.Boss ? _config.bossDistance : _config.singleDistance; Refresh(); });
            _tierButton.onClick.AddListener(() => { _tier = (_tier + 1) % _config.tiers.Length; Refresh(); });
            _weaponButton.onClick.AddListener(() => { _weapon = (_weapon + 1) % _config.weaponLabels.Length; Refresh(); });
            _bossButton.onClick.AddListener(() => { _boss = (_boss + 1) % _config.bosses.Length; Refresh(); });
            _distanceDownButton.onClick.AddListener(() => ChangeDistance(-1));
            _distanceUpButton.onClick.AddListener(() => ChangeDistance(1));
            _invincibleButton.onClick.AddListener(() => { _session?.SetInvincible(!_session.IsInvincible); Refresh(); });
            _stopButton.onClick.AddListener(() => { _session?.SetEnemyStopped(!_session.IsEnemyStopped); Refresh(); });
            _partyButton.onClick.AddListener(() => UISvc.UI.ShowUI(UIKeyType.Party, CanvasLayer.Popup));
            _targetButton.onClick.AddListener(SelectNextTarget);
            _attackButton.onClick.AddListener(SelectNextAttack);
            _startButton.onClick.AddListener(StartTraining);
            _restartButton.onClick.AddListener(() => { HideImmediately(); _session?.Restart(); });
            _returnButton.onClick.AddListener(() => { HideImmediately(); TrainingGroundTravel.Return(_config); });
            _closeButton.onClick.AddListener(Hide);
            for (int i = 0; i < _lateralButtons.Length; i++)
            {
                int index = i;
                _lateralButtons[i].onClick.AddListener(() => { _formation[index].x = NextPosition(_formation[index].x, _config.lateralRange); Refresh(); });
                _forwardButtons[i].onClick.AddListener(() => { _formation[index].y = NextPosition(_formation[index].y, _config.distanceRange); Refresh(); });
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            Services.TryGet<IUITrainingService>(out var session);
            if (!ReferenceEquals(_session, session)) { _target = 0; _attack = 0; }
            _session = session;
            _session?.Configure(_config);
            _time = Svc.GameTime;
            if (_time != null && !_time.IsPaused)
            {
                _time.SetPause(true);
                _didPauseGame = true;
            }
            _statusText.text = _session == null ? "훈련장을 준비하고 있습니다." : "상대를 고르고 훈련을 시작하세요.";
            RefreshAttacks();
            Refresh();
            SetDefaultFocus(_session?.HasEncounter == true ? _closeButton : _modeButton);
        }

        protected override void OnHide()
        {
            ReleasePause();
            base.OnHide();
        }

        protected override void OnDestroy()
        {
            ReleasePause();
            base.OnDestroy();
        }

        private void ReleasePause()
        {
            if (_didPauseGame) _time?.SetPause(false);
            _didPauseGame = false;
        }

        private void HideImmediately()
        {
            // 씬 로딩의 FixedUpdate 대기를 막지 않도록 소유한 정지 요청부터 반환한다.
            ReleasePause();
            Hide();
        }

        private static float NextPosition(float value, Vector2 range) => value >= range.y ? range.x : value + 1;
        private void ChangeDistance(float delta)
        {
            _distance = Mathf.Clamp(_distance + delta, _config.distanceRange.x, _config.distanceRange.y);
            Refresh();
        }

        private void StartTraining()
        {
            if (_session == null)
            {
                _statusText.text = "훈련장을 준비하고 있습니다. 잠시 후 다시 열어 주세요.";
                return;
            }
            if (_session.TryStart(_mode, _tier, _weapon, _boss, _distance, _formation, out string message)) Hide();
            _statusText.text = message;
            Refresh();
        }

        private MonsterActor CurrentTarget => _session != null && _target < _session.Opponents.Count
            ? _session.Opponents[_target] : null;

        private void SelectNextTarget()
        {
            if (_session == null || _session.Opponents.Count == 0) return;
            _session.SetPracticeAttack(null, null);
            _target = (_target + 1) % _session.Opponents.Count;
            _attack = 0;
            RefreshAttacks();
            Refresh();
        }

        private void RefreshAttacks()
        {
            _attacks.Clear();
            var combat = CurrentTarget != null ? CurrentTarget.GetComponent<EnemyCombat>() : null;
            if (combat?.AbilitySet == null) { _attack = 0; return; }
            foreach (var ability in combat.AbilitySet.GetRuntimeAbilities())
                if (ability != null && !_attacks.Contains(ability)) _attacks.Add(ability);
            _attack = Mathf.Clamp(_attack, 0, _attacks.Count);
        }

        private void SelectNextAttack()
        {
            _attack = (_attack + 1) % (_attacks.Count + 1);
            _session?.SetPracticeAttack(CurrentTarget, _attack == 0 ? null : _attacks[_attack - 1]);
            Refresh();
        }

        private void Refresh()
        {
            bool ready = _session != null;
            bool canPrepare = ready && !_session.HasEncounter;
            SetLabel(_modeButton, "훈련 방식     " + (_mode == TrainingEncounter.Single ? "개인 대련" : _mode == TrainingEncounter.Mixed ? "혼합 대련" : "보스 대련") + "  >");
            SetLabel(_tierButton, "상대 수준     " + _config.tiers[_tier].label + "  >");
            SetLabel(_weaponButton, "상대 무기     " + _config.weaponLabels[_weapon] + "  >");
            SetLabel(_bossButton, "대련 상대     " + (_config.bosses[_boss]?.displayName ?? "없음") + "  >");
            _distanceText.text = $"시작 거리   {_distance:0} m";
            SetLabel(_invincibleButton, "피해 받지 않기     " + (_session?.IsInvincible == true ? "켜짐" : "꺼짐"));
            SetLabel(_stopButton, "상대 행동 멈추기     " + (_session?.IsEnemyStopped == true ? "켜짐" : "꺼짐"));
            SetLabel(_targetButton, CurrentTarget != null && CurrentTarget.IsAlive() ? $"연습 상대     {_target + 1}번  >" : "연습 상대     없음");
            string attackName = _attack == 0 ? "모든 공격" : $"공격 {_attack}";
            if (_attack > 0 && !string.IsNullOrWhiteSpace(_attacks[_attack - 1].presentation?.displayName)
                && _attacks[_attack - 1].presentation.displayName != "새 Ability") attackName = _attacks[_attack - 1].presentation.displayName;
            SetLabel(_attackButton, "반복할 공격     " + attackName + "  >");
            _modeButton.interactable = canPrepare;
            _tierButton.interactable = canPrepare && _mode != TrainingEncounter.Boss;
            _weaponButton.interactable = canPrepare && _mode == TrainingEncounter.Single;
            _bossButton.interactable = canPrepare && _mode == TrainingEncounter.Boss;
            _distanceDownButton.interactable = _distanceUpButton.interactable = canPrepare && _mode != TrainingEncounter.Mixed;
            _startButton.interactable = canPrepare;
            _invincibleButton.interactable = _stopButton.interactable = ready;
            _restartButton.interactable = ready && _session.HasEncounter;
            _targetButton.interactable = ready && _session.Opponents.Count > 1;
            _attackButton.interactable = CurrentTarget != null && CurrentTarget.IsAlive() && _attacks.Count > 0;
            _formationPanel.SetActive(_mode == TrainingEncounter.Mixed);
            for (int i = 0; i < _lateralButtons.Length; i++)
            {
                SetLabel(_lateralButtons[i], $"좌우 {_formation[i].x:0} m  >");
                SetLabel(_forwardButtons[i], $"전방 {_formation[i].y:0} m  >");
                _lateralButtons[i].interactable = _forwardButtons[i].interactable = canPrepare;
            }
        }

        private static void SetLabel(Button button, string value) => button.GetComponentInChildren<TMP_Text>().text = value;
    }

    /// <summary>훈련장 왕복 위치를 플레이 세션 동안만 보관한다.</summary>
    public static class TrainingGroundTravel
    {
        private static string _returnScene;
        private static Vector3 _returnPosition;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _returnScene = null;

        /// <summary>훈련장 안에서는 준비 화면을 열고 밖에서는 현재 위치를 기록해 이동한다.</summary>
        public static void Enter(TrainingGroundConfigSO config)
        {
            if (config == null) return;
            if (Services.TryGet<IUITrainingService>(out _))
            {
                UISvc.UI.ShowUI(UI_Scene_TrainingGround.UIKey);
                return;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == config.sceneName) return;
            if (!Application.CanStreamedLevelBeLoaded(config.sceneName)) return;
            _returnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _returnPosition = UISvc.Party?.ActiveCharacter != null ? UISvc.Party.ActiveCharacter.transform.position : Vector3.zero;
            UISvc.Scene.LoadScene(config.sceneName);
        }

        /// <summary>입장 전 위치로 돌아가며 직접 실행한 훈련장은 지정된 지역으로 나간다.</summary>
        public static void Return(TrainingGroundConfigSO config)
        {
            if (!string.IsNullOrEmpty(_returnScene)) UISvc.Scene.LoadScene(_returnScene, _returnPosition);
            else UISvc.Scene.LoadScene(config.fallbackReturnScene);
        }
    }
}
