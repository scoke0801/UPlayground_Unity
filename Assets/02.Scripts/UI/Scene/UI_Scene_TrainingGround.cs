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
        [Tooltip("아직 배우지 않은 기술도 훈련장 안에서만 쓰게 하는 토글.")]
        [SerializeField] private Button _allSkillsButton;
        [Tooltip("혼합 대련 배치 패널과 자리를 나눠 쓰므로 패널이 열리면 숨긴다.")]
        [SerializeField] private GameObject _practiceHint;
        [SerializeField] private Button _targetButton, _attackButton;
        [SerializeField] private Button _startButton, _restartButton, _returnButton, _closeButton;
        [SerializeField] private GameObject _formationPanel;
        [SerializeField] private Button[] _lateralButtons, _forwardButtons;
        private readonly List<GameplayAbilitySO> _attacks = new();
        private IUITrainingService _session;
        private IGameTimeService _time;
        private bool _didPauseGame;
        private bool _selectingMap;
        private bool _cameraTestMap;
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
            _modeButton.onClick.AddListener(() => { if (_selectingMap) { _cameraTestMap = !_cameraTestMap; Refresh(); return; }
                _mode = (TrainingEncounter)(((int)_mode + 1) % 3);
                _distance = _mode == TrainingEncounter.Boss ? _config.bossDistance : _config.singleDistance; Refresh(); });
            _tierButton.onClick.AddListener(() => { _tier = (_tier + 1) % _config.tiers.Length; Refresh(); });
            _weaponButton.onClick.AddListener(() => { _weapon = (_weapon + 1) % _config.weaponLabels.Length; Refresh(); });
            _bossButton.onClick.AddListener(() => { _boss = (_boss + 1) % _config.bosses.Length; Refresh(); });
            _distanceDownButton.onClick.AddListener(() => ChangeDistance(-1));
            _distanceUpButton.onClick.AddListener(() => ChangeDistance(1));
            _invincibleButton.onClick.AddListener(() => { _session?.SetInvincible(!_session.IsInvincible); Refresh(); });
            _stopButton.onClick.AddListener(() => { _session?.SetEnemyStopped(!_session.IsEnemyStopped); Refresh(); });
            _allSkillsButton.onClick.AddListener(() => { _session?.SetAllSkillsUnlocked(!_session.IsAllSkillsUnlocked); Refresh(); });
            _partyButton.onClick.AddListener(() => UISvc.UI.ShowUI(UIKeyType.Party, CanvasLayer.Popup));
            _targetButton.onClick.AddListener(SelectNextTarget);
            _attackButton.onClick.AddListener(SelectNextAttack);
            _startButton.onClick.AddListener(StartTraining);
            _restartButton.onClick.AddListener(() => { HideImmediately(); _session?.Restart(); });
            _returnButton.onClick.AddListener(() =>
            {
                if (_selectingMap) { HideImmediately(); TrainingGroundTravel.Return(_config); return; }
                _selectingMap = true;
                _cameraTestMap = TrainingGroundTravel.IsCameraTraining;
                _statusText.text = "훈련할 맵을 선택하세요.";
                Refresh();
                SetDefaultFocus(_modeButton);
            });
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
            _selectingMap = _session == null;
            _cameraTestMap = false;
            _session?.Configure(_config);
            _time = Svc.GameTime;
            if (_time != null && !_time.IsPaused)
            {
                _time.SetPause(true);
                _didPauseGame = true;
            }
            _statusText.text = _selectingMap ? "훈련할 맵을 선택하세요." : "상대를 고르고 훈련을 시작하세요.";
            RefreshAttacks();
            Refresh();
            SetDefaultFocus(_modeButton);
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
            if (_selectingMap)
            {
                string sceneName = _cameraTestMap ? _config.cameraTestSceneName : _config.sceneName;
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    _statusText.text = "선택한 맵을 불러올 수 없습니다.";
                    return;
                }
                HideImmediately();
                TrainingGroundTravel.TravelTo(sceneName, _cameraTestMap, _session != null);
                return;
            }
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
            foreach (var button in new[] { _tierButton, _weaponButton, _bossButton, _distanceDownButton,
                _distanceUpButton, _invincibleButton, _stopButton, _allSkillsButton, _partyButton, _targetButton,
                _attackButton, _restartButton })
                button.gameObject.SetActive(!_selectingMap);
            _returnButton.gameObject.SetActive(!_selectingMap || _session != null);
            _distanceText.gameObject.SetActive(!_selectingMap);
            SetLabel(_startButton, _selectingMap ? "입장" : "훈련 시작");
            SetLabel(_returnButton, _selectingMap ? "훈련장 나가기" : "맵 변경 / 나가기");
            if (_selectingMap)
            {
                _formationPanel.SetActive(false);
                SetLabel(_modeButton, "훈련 맵     " + (_cameraTestMap ? "카메라 테스트 맵" : "기존 맵") + "  >");
                _modeButton.interactable = _startButton.interactable = true;
                return;
            }
            bool ready = _session != null;
            bool canPrepare = ready;
            SetLabel(_modeButton, "훈련 방식     " + (_mode == TrainingEncounter.Single ? "개인 대련" : _mode == TrainingEncounter.Mixed ? "혼합 대련" : "보스 대련") + "  >");
            SetLabel(_tierButton, "상대 수준     " + _config.tiers[_tier].label + "  >");
            SetLabel(_weaponButton, "상대 무기     " + _config.weaponLabels[_weapon] + "  >");
            SetLabel(_bossButton, "대련 상대     " + (_config.bosses[_boss]?.displayName ?? "없음") + "  >");
            _distanceText.text = $"시작 거리   {_distance:0} m";
            SetLabel(_invincibleButton, "피해 받지 않기     " + (_session?.IsInvincible == true ? "켜짐" : "꺼짐"));
            SetLabel(_stopButton, "상대 행동 멈추기     " + (_session?.IsEnemyStopped == true ? "켜짐" : "꺼짐"));
            SetLabel(_allSkillsButton, "모든 기술 사용     " + (_session?.IsAllSkillsUnlocked == true ? "켜짐" : "꺼짐"));
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
            _invincibleButton.interactable = _stopButton.interactable = _allSkillsButton.interactable = ready;
            _restartButton.interactable = ready && _session.HasEncounter;
            _targetButton.interactable = ready && _session.Opponents.Count > 1;
            _attackButton.interactable = CurrentTarget != null && CurrentTarget.IsAlive() && _attacks.Count > 0;
            _formationPanel.SetActive(_mode == TrainingEncounter.Mixed);
            if (_practiceHint != null) _practiceHint.SetActive(_mode != TrainingEncounter.Mixed);
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
        public static bool IsCameraTraining { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _returnScene = null; IsCameraTraining = false; }

        /// <summary>훈련장 준비 화면 또는 맵 선택을 연다.</summary>
        public static void Enter(TrainingGroundConfigSO config)
        {
            if (config == null) return;
            UISvc.UI.ShowUI(UI_Scene_TrainingGround.UIKey);
        }

        /// <summary>현재 위치를 기록하고 선택한 훈련 맵으로 이동한다.</summary>
        public static void TravelTo(string sceneName, bool isCameraTraining, bool isChangingTrainingMap = false)
        {
            IsCameraTraining = isCameraTraining;
            if (!isChangingTrainingMap)
            {
                _returnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                _returnPosition = UISvc.Party?.ActiveCharacter != null ? UISvc.Party.ActiveCharacter.transform.position : Vector3.zero;
            }
            UISvc.Scene.LoadScene(sceneName);
        }

        /// <summary>입장 전 위치로 돌아가며 직접 실행한 훈련장은 지정된 지역으로 나간다.</summary>
        public static void Return(TrainingGroundConfigSO config)
        {
            IsCameraTraining = false;
            if (!string.IsNullOrEmpty(_returnScene)) UISvc.Scene.LoadScene(_returnScene, _returnPosition);
            else UISvc.Scene.LoadScene(config.fallbackReturnScene);
        }
    }
}
