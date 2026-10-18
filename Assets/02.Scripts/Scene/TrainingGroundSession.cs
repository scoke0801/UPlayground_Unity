using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.UI;
using UPlayGround.Group;
using UPlayGround.Manager;
using UPlayGround.UI;

namespace UPlayGround.Training
{
    /// <summary>훈련장 씬에서만 상대와 연습 보조 설정의 수명을 관리한다.</summary>
    public sealed class TrainingGroundSession : MonoBehaviour, IUITrainingService
    {
        private TrainingGroundConfigSO _config;
        private readonly List<MonsterActor> _opponents = new();
        private readonly List<Action> _releaseStops = new();
        private EnemyCombat _practiceCombat;
        private GameplayAbilitySO _previousAttack;
        public TrainingGroundConfigSO Config => _config;
        public bool HasEncounter { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsEnemyStopped { get; private set; }
        public IReadOnlyList<MonsterActor> Opponents => _opponents;

        /// <summary>화면에 표시한 설정을 소환에도 사용하며 진행 중인 대련의 설정은 보존한다.</summary>
        public void Configure(TrainingGroundConfigSO config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (HasEncounter && _config != config)
                throw new InvalidOperationException("대련 중에는 훈련장 설정을 변경할 수 없습니다.");
            _config = config;
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GameManager.Instance.BootState == GameBootState.Ready
                && UIManager.Instance.IsInitialized
                && Services.TryGet<IUIPartyService>(out var party)
                && party.ActiveCharacter != null && Services.TryGet<IUIRuntimeService>(out _)
                && !SceneManager.Instance.IsLoading);
            Services.Register(this);
            UISvc.UI.ShowUI(UI_Scene_TrainingGround.UIKey);
        }

        /// <summary>모션의 무적 창과 별개로 훈련 중 피해를 차단한다.</summary>
        public void SetInvincible(bool value)
        {
            IsInvincible = value;
            PlayerActor.IsTrainingInvincible = value;
        }

        /// <summary>피격 반응을 유지하면서 훈련 상대의 자발적 행동을 멈춘다.</summary>
        public void SetEnemyStopped(bool value)
        {
            foreach (var release in _releaseStops) release();
            _releaseStops.Clear();
            IsEnemyStopped = value;
            if (!value) return;
            foreach (var enemy in _opponents)
            {
                if (enemy == null || !enemy.IsAlive()) continue;
                var detection = enemy.Detection;
                bool wasEnabled = detection != null && detection.enabled;
                var runner = enemy.GetComponent<BehaviorTreeRunner>();
                bool wasPaused = runner != null && runner.IsPaused;
                if (detection != null)
                {
                    detection.enabled = false;
                    detection.ForceResetTarget();
                }
                if (runner != null) runner.IsTrainingPaused = true;
                var hold = enemy.BeginDialogueStage(null);
                _releaseStops.Add(() =>
                {
                    if (enemy == null) return;
                    hold.Dispose();
                    if (runner != null)
                    {
                        runner.IsTrainingPaused = false;
                        if (wasPaused || !enemy.IsAlive()) runner.PauseTree();
                    }
                    if (enemy.IsAlive() && detection != null) detection.enabled = wasEnabled;
                });
            }
        }

        /// <summary>전원 배치를 검증하고 실패하면 이번에 생성한 상대를 모두 제거한다.</summary>
        public bool TryStart(TrainingEncounter kind, int tier, int weapon, int boss, float distance,
            Vector2[] formation, out string message)
        {
            message = "훈련을 준비할 수 없습니다.";
            var player = UISvc.Party?.ActiveCharacter;
            if (HasEncounter) { message = "진행 중인 대련이 있습니다. 다시 준비를 눌러 주세요."; return false; }
            if (player == null) { message = "출전 동료를 준비하고 있습니다. 잠시 후 다시 시도해 주세요."; return false; }
            if (!player.IsAlive()) { message = "쓰러진 동료는 대련을 시작할 수 없습니다."; return false; }
            if (_config == null) { message = "훈련장 설정을 불러오지 못했습니다."; return false; }
            if (!Enum.IsDefined(typeof(TrainingEncounter), kind)
                || tier < 0 || tier >= _config.tiers.Length || boss < 0 || boss >= _config.bosses.Length)
            { message = "선택한 훈련 방식 또는 상대 수준을 찾을 수 없습니다."; return false; }
            var members = _config.tiers[tier].members;
            if (weapon < 0 || weapon >= members.Length || formation == null || formation.Length != members.Length)
            { message = "상대 무기와 배치 설정이 맞지 않습니다."; return false; }
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < _config.distanceRange.x || distance > _config.distanceRange.y)
            { message = "시작 거리가 허용 범위를 벗어났습니다."; return false; }
            ActorDefinitionSO[] definitions = kind == TrainingEncounter.Mixed ? members
                : new[] { kind == TrainingEncounter.Boss ? _config.bosses[boss] : members[weapon] };
            var points = new Vector3[definitions.Length];
            var forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < points.Length; i++)
            {
                if (definitions[i] == null || definitions[i].prefab == null || definitions[i].EffectiveAbilitySet == null)
                { message = $"{i + 1}번째 상대의 전투 준비가 누락되었습니다."; return false; }
                Vector2 offset = kind == TrainingEncounter.Mixed ? formation[i] : new Vector2(0, distance);
                if (float.IsNaN(offset.x) || float.IsInfinity(offset.x) || float.IsNaN(offset.y) || float.IsInfinity(offset.y)
                    || offset.x < _config.lateralRange.x || offset.x > _config.lateralRange.y
                    || offset.y < _config.distanceRange.x || offset.y > _config.distanceRange.y)
                { message = $"{i + 1}번째 상대의 배치가 허용 범위를 벗어났습니다."; return false; }
                Vector3 point = player.transform.position + right * offset.x + forward * offset.y;
                if (!Physics.Raycast(point + Vector3.up * 3, Vector3.down, out var hit, 33,
                    LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
                {
                    message = "상대가 설 땅이 없습니다. 위치를 바꿔 주세요.";
                    return false;
                }
                points[i] = hit.point;
            }
            var groupRoot = new GameObject("TrainingOpponents");
            var group = groupRoot.AddComponent<MonsterGroupController>();
            try
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    var actor = ActorSpawnManager.Instance.SpawnActor(definitions[i].actorId, points[i],
                        Quaternion.LookRotation(-forward), group, groupRoot.transform) as MonsterActor;
                    if (actor == null) throw new InvalidOperationException("훈련 상대 생성 실패");
                    actor.IsTrainingOpponent = true;
                    _opponents.Add(actor);
                }
                HasEncounter = true;
                SetEnemyStopped(IsEnemyStopped);
                message = "훈련을 시작합니다.";
                return true;
            }
            catch (Exception exception)
            {
                SetEnemyStopped(false);
                HasEncounter = false;
                groupRoot.SetActive(false);
                Destroy(groupRoot);
                _opponents.Clear();
                Debug.LogException(exception, this);
                message = "상대를 준비하지 못했습니다. 다시 시도해 주세요.";
                return false;
            }
        }

        /// <summary>선택한 상대의 다음 공격 후보를 제한하고 이전 제한은 복원한다.</summary>
        public void SetPracticeAttack(MonsterActor actor, GameplayAbilitySO ability)
        {
            if (_practiceCombat != null) _practiceCombat.TrainingAbility = _previousAttack;
            _practiceCombat = null;
            if (actor == null || !_opponents.Contains(actor)) return;
            _practiceCombat = actor.GetComponent<EnemyCombat>();
            if (_practiceCombat == null) return;
            _previousAttack = _practiceCombat.TrainingAbility;
            _practiceCombat.TrainingAbility = ability;
        }

        /// <summary>잔류 투사체와 전투 상태까지 씬 수명으로 정리하고 다시 준비한다.</summary>
        public void Restart() => UISvc.Scene.LoadScene(_config.sceneName);

        private void OnDestroy()
        {
            if (Services.TryGet<IUIRuntimeService>(out var ui)) ui.HideUI(UI_Scene_TrainingGround.UIKey);
            SetPracticeAttack(null, null);
            SetEnemyStopped(false);
            SetInvincible(false);
            Services.Unregister(this);
        }
    }
}
