#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Group;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.CombatValidation
{
    /// <summary>명시적 실행 인자가 있는 격리 Development Player에서 전투 연결을 확인한다.</summary>
    public sealed class CombatEncounterPlayerProbe : MonoBehaviour
    {
        private readonly List<string> _checks = new();
        private readonly List<string> _errors = new();
        private readonly List<MonsterActor> _spawned = new();
        private PlayerActor _player;
        private Gamepad _pad;
        private string _output;
        private Vector3 _origin;
        private Quaternion _rotation;
        private int _failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-combatEncounterValidation") < 0) return;
            var host = new GameObject("CombatEncounterValidation");
            DontDestroyOnLoad(host);
            host.AddComponent<CombatEncounterPlayerProbe>();
        }

        private IEnumerator Start()
        {
            _output = Path.Combine(Application.persistentDataPath, "EncounterValidation");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-probeOutput") _output = args[i + 1];
            Directory.CreateDirectory(_output);
            Application.logMessageReceived += CaptureError;
            StartCoroutine(Watchdog());
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _pad = InputSystem.AddDevice<Gamepad>();
            float deadline = Time.realtimeSinceStartup + 150f;
            while (_player == null && Time.realtimeSinceStartup < deadline)
            {
                if (GameManager.Instance != null && GameManager.Instance.BootState == GameBootState.Ready)
                    _player = PartyManager.Instance?.ActiveCharacter;
                yield return null;
            }
            Check("boot", _player != null, "활성 플레이어 준비");
            if (_player == null) { Finish(); yield break; }
            foreach (var device in InputSystem.devices) if (device != _pad) InputSystem.DisableDevice(device);
            PlayerActor.IsTrainingInvincible = true;
            yield return new WaitForSecondsRealtime(3f);
            yield return Press(GamepadButton.East);
            yield return new WaitForSecondsRealtime(.7f);
            foreach (var actor in FindObjectsByType<MonsterActor>(FindObjectsSortMode.None)) actor.gameObject.SetActive(false);
            _origin = _player.transform.position;
            _rotation = _player.transform.rotation;
            var forward = _rotation * Vector3.forward;
            var right = _rotation * Vector3.right;
            Services.TryGet<ICombatEncounterService>(out var encounter);
            Check("service", encounter?.EncounterProfile != null, "프로필과 서비스 등록");
            if (encounter == null) { Finish(); yield break; }
            encounter.ResetEncounter();

            var far = Spawn("Enemy_P09_Armor07_DualAxe", _origin + forward * 5f);
            yield return new WaitForSecondsRealtime(1f);
            Check("sweep", encounter.CurrentEncounter == CombatEncounterKind.Sweep, encounter.CurrentEncounter.ToString());
            CameraManager.Instance.SetRotationSmooth(_rotation.eulerAngles.y, 12f, .1f);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Press(GamepadButton.RightStick);
            Check("lock", CameraManager.Instance.GetLockOnTarget() != null, "5m 표적 락온");
            _player.PlaceAtPose(_origin, _rotation * Quaternion.Euler(0f, 180f, 0f));
            yield return Press(GamepadButton.West);
            yield return new WaitForSecondsRealtime(.2f);
            float dot = far != null ? Vector3.Dot(_player.transform.forward,
                Vector3.ProjectOnPlane(far.transform.position - _player.transform.position, Vector3.up).normalized) : -1f;
            Check("far-alignment", dot > .95f, "dot=" + dot + "; state=" + _player.PlayerController.CurrentState.StateId);
            Check("far-approach", Vector3.Distance(_player.transform.position, _origin) < 1.5f,
                "5m 표적 접근 이동=" + Vector3.Distance(_player.transform.position, _origin));
            yield return new WaitForSecondsRealtime(2f);
            _player.PlaceAtPose(_origin, _rotation * Quaternion.Euler(0f, 180f, 0f));
            InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
            yield return new WaitForSecondsRealtime(.3f);
            dot = far != null ? Vector3.Dot(_player.transform.forward,
                Vector3.ProjectOnPlane(far.transform.position - _player.transform.position, Vector3.up).normalized) : -1f;
            Check("guard-entry", _player.PlayerController.CurrentState.StateId == ActorStateId.Guard && dot > .95f, "dot=" + dot);
            if (far != null) far.PlaceAtPose(_origin + right * 5f, _rotation);
            yield return new WaitForSecondsRealtime(.4f);
            dot = far != null ? Vector3.Dot(_player.transform.forward,
                Vector3.ProjectOnPlane(far.transform.position - _player.transform.position, Vector3.up).normalized) : -1f;
            Check("guard-tracking", dot > .95f, "dot=" + dot);
            InputSystem.QueueStateEvent(_pad, new GamepadState());
            yield return new WaitForSecondsRealtime(.2f);
            CameraManager.Instance.ReleaseLockOn();
            ClearActors();
            _player.PlaceAtPose(_origin, _rotation);
            yield return new WaitForSecondsRealtime(.5f);

            // 실물 입력 경로의 공격 3회로 계측 세션과 피해 연결을 확인한다.
            var near = Spawn("Enemy_P09_Armor07_DualAxe", _origin + forward * 1.2f);
            yield return new WaitForSecondsRealtime(.8f);
            for (int i = 0; i < 3; i++) { yield return Press(GamepadButton.West); yield return new WaitForSecondsRealtime(.5f); }
            Check("attack-telemetry", EncounterCompositionTelemetry.Metrics.Records[(int)CombatEncounterKind.Sweep].comboActions > 0,
                "소탕 공격 시작 계측");
            yield return new WaitForSecondsRealtime(2f);
            ClearActors();
            _player.PlaceAtPose(_origin, _rotation);
            yield return new WaitForSecondsRealtime(.5f);

            var elite = Spawn("Enemy_P09_Armor08_DualAxe", _origin + forward * 3f);
            var inside = Spawn("Enemy_P09_Armor07_DualAxe", _origin + forward * 3f + right * 2f);
            var outside = Spawn("Enemy_P09_Armor07_DualAxe", _origin + forward * 3f + right * 6f);
            yield return new WaitForSecondsRealtime(1f);
            float healthBefore = inside != null ? inside.CurrentHealth : -1f;
            if (elite != null)
            {
                elite.BreakGauge?.ForceExpose();
                elite.OnTakeSpecialBreakAttack(_player, 0f, 1f, 1f);
            }
            yield return new WaitForSecondsRealtime(.15f);
            Check("elite-shockwave", inside != null && inside.ActorController.CurrentState.StateId == ActorStateId.Knockdown,
                "범위 안 state=" + inside?.ActorController?.CurrentState?.StateId);
            Check("shockwave-no-damage", inside != null && Mathf.Approximately(healthBefore, inside.CurrentHealth),
                "health=" + healthBefore + " -> " + inside?.CurrentHealth);
            Check("shockwave-outside", outside != null && outside.ActorController.CurrentState.StateId == ActorStateId.Idle,
                "범위 밖 state=" + outside?.ActorController?.CurrentState?.StateId);

            var group = new GameObject("ValidationSweepGroup").AddComponent<MonsterGroupController>();
            SetGroupFixture(group, "_rangedSlotCap", 1);
            SetGroupFixture(group, "_breatherDuration", .8f);
            SetGroupFixture(group, "_offscreenAggroMultiplier", .3f);
            var members = new MonsterActor[6];
            for (int i = 0; i < members.Length; i++)
                members[i] = Spawn("Enemy_P09_Armor07_DualAxe", _origin + forward * (6f + i % 2) + right * (i - 2.5f) * 1.5f, group);
            yield return new WaitForSecondsRealtime(.7f);
            Check("group-registration", group.AliveCount == 6, "ActorSpawnManager 등록=" + group.AliveCount);
            Check("group-caps", group.CurrentMeleeSlotLimit == 2 && group.CurrentRangedSlotLimit == 1,
                "melee=" + group.CurrentMeleeSlotLimit + "; ranged=" + group.CurrentRangedSlotLimit);
            group.RequestAttackSlot(members[0], AttackType.Melee);
            group.RequestAttackSlot(members[1], AttackType.Melee);
            var priorityElite = Spawn("Enemy_P09_Armor08_DualAxe", _origin + forward * 7f + right * 4f, group);
            yield return new WaitForSecondsRealtime(.7f);
            bool acquired = group.RequestAttackSlot(priorityElite, AttackType.Melee);
            var owners = typeof(MonsterGroupController).GetField("_meleeSlotOwners",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(group)
                as Dictionary<MonsterActor, MemberPriority>;
            Check("elite-priority", acquired && owners != null && owners.ContainsKey(priorityElite) && owners.Count == 2,
                "정예 슬롯 선점 / 소유=" + owners?.Count);

            _player.PlaceAtPose(_origin, _rotation);
            members[2]?.PlaceAtPose(_origin + right * 2f, _rotation);
            for (int i = 0; i < 6; i++)
            {
                members[0]?.ReceiveHit(HitRequest.FromAttackData(new AttackData
                {
                    attacker = _player, damage = 1f, reactionType = AttackReactionType.None,
                    hitTarget = members[0].gameObject, hitPoint = members[0].transform.position,
                    abilityId = "Validation.ContinuousHit",
                }));
                yield return new WaitForSecondsRealtime(.7f);
            }
            var responder = typeof(MonsterGroupController).GetField("_comboResponder",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(group) as MonsterActor;
            Check("long-combo-response", responder == members[2], "측면 대응자=" + responder?.name);

            encounter.SetArenaActive("validation-arena", true);
            yield return new WaitForSecondsRealtime(.3f);
            Check("duel-arena", encounter.CurrentEncounter == CombatEncounterKind.Duel, encounter.CurrentEncounter.ToString());
            yield return Press(GamepadButton.West);
            yield return new WaitForSecondsRealtime(.5f);
            Check("duel-telemetry", EncounterCompositionTelemetry.Metrics.Records[(int)CombatEncounterKind.Duel].comboActions > 0,
                "결투 공격 시작 계측");
            encounter.SetArenaActive("validation-arena", false);
            yield return new WaitForSecondsRealtime(.5f);
            Check("duel-hold", encounter.CurrentEncounter == CombatEncounterKind.Duel, "해제 직후 유예");
            yield return new WaitForSecondsRealtime(2.5f);
            Check("duel-release", encounter.CurrentEncounter != CombatEncounterKind.Duel, encounter.CurrentEncounter.ToString());
            var boss = Spawn("BossAlternateSelfRaon", _origin + forward * 8f);
            yield return new WaitForSecondsRealtime(.6f);
            Check("boss-detection", boss != null && encounter.CurrentEncounter == CombatEncounterKind.Duel,
                "보스 인지·20m 반경: " + encounter.CurrentEncounter);
            if (boss != null)
            {
                // 공격 모션의 연출 검사가 아니라 공격 상태를 소비하는 슬롯 계약을 분리 검증한다.
                boss.ActorController.TransitionToState(new ValidationBossAttackState(boss.ActorController));
                yield return new WaitForSecondsRealtime(.3f);
                Check("boss-group-suppression", encounter.IsBossAttacking && group.CurrentMeleeSlotLimit == 0
                    && !group.RequestAttackSlot(members[0], AttackType.Melee), "공격 상태에서 잡몹 신규 슬롯 0");
                boss.ActorController.TransitionToState(ActorStateId.Idle);
            }
            if (boss != null) { boss.Detection.ForceResetTarget(); Destroy(boss.gameObject); }
            yield return new WaitForSecondsRealtime(2.5f);
            Check("boss-release", encounter.CurrentEncounter != CombatEncounterKind.Duel, encounter.CurrentEncounter.ToString());
            PlayerActor.IsTrainingInvincible = false;
            _player.ReceiveHit(HitRequest.FromAttackData(new AttackData
            {
                attacker = outside, damage = 1f, reactionType = AttackReactionType.Hit, forceReaction = true,
                reactionDuration = .3f, hitTarget = _player.gameObject, hitPoint = _player.transform.position,
                abilityId = "Validation.ControlLoss",
            }));
            yield return new WaitForSecondsRealtime(1.5f);
            _player.ReceiveHit(HitRequest.FromAttackData(new AttackData
            {
                attacker = outside, damage = 100000f, reactionType = AttackReactionType.Hit,
                hitTarget = _player.gameObject, hitPoint = _player.transform.position, abilityId = "Validation.FatalHit",
            }));
            yield return new WaitForSecondsRealtime(.2f);
            EncounterCompositionTelemetry.FinishObservation();
            float lostControl = 0f;
            int fatalHits = 0;
            foreach (var record in EncounterCompositionTelemetry.Metrics.Records)
            {
                lostControl += record.lostControlSeconds;
                for (int i = 0; i < record.lastDeathHitCount; i++)
                    if (record.lastDeathHits[i].abilityId == "Validation.FatalHit") fatalHits++;
            }
            Check("control-loss-telemetry", lostControl > 0f, "상태 이벤트 조작 불능=" + lostControl);
            Check("fatal-hit-telemetry", fatalHits == 1, "사망 알림 뒤 최종 치명타 포함=" + fatalHits);
            encounter.ResetEncounter();
            Check("reset", encounter.CurrentEncounter == CombatEncounterKind.Explore, "씬·로드 공통 리셋 계약");
            Finish();
        }

        private MonsterActor Spawn(string id, Vector3 position, MonsterGroupController group = null)
        {
            var actor = ActorSpawnManager.Instance.SpawnActor(id, position, _rotation * Quaternion.Euler(0f, 180f, 0f),
                group, group != null ? group.transform : null) as MonsterActor;
            if (actor == null) { Check("spawn", false, id); return null; }
            _spawned.Add(actor);
            actor.IsTrainingOpponent = true;
            actor.AIController?.Freeze();
            actor.ActorController.TransitionToState(ActorStateId.Idle);
            actor.ActorController.Motor.SetPositionAndRotation(position, _rotation * Quaternion.Euler(0f, 180f, 0f));
            actor.Detection?.AcquireTarget(_player.transform, acquiredExternally: true);
            return actor;
        }

        private void ClearActors()
        { foreach (var actor in _spawned) if (actor != null) Destroy(actor.gameObject); _spawned.Clear(); }

        private static void SetGroupFixture(MonsterGroupController group, string field, object value)
            => typeof(MonsterGroupController).GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(group, value);

        private sealed class ValidationBossAttackState : EnemyActorState
        {
            public ValidationBossAttackState(ActorMovementController controller) : base(controller) { }
            public override ActorStateId StateId => ActorStateId.Attack;
            public override bool CanTransitionState(ActorStateId fromState) => true;
        }

        private IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(button));
            yield return null;
            yield return new WaitForSecondsRealtime(.06f);
            InputSystem.QueueStateEvent(_pad, new GamepadState());
            yield return null;
        }

        private IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(240f);
            Check("timeout", false, "자동 검증 제한 시간 초과");
            Finish();
        }

        private void Check(string id, bool passed, string detail)
        { _checks.Add(id + "\t" + (passed ? "PASS" : "FAIL") + "\t" + detail); if (!passed) _failures++; }

        private void CaptureError(string message, string stack, LogType type)
        { if (type is LogType.Error or LogType.Exception or LogType.Assert) _errors.Add(message + "\n" + stack); }

        private void Finish()
        {
            CombatTelemetrySession.Flush();
            if (Directory.Exists(CombatTelemetrySession.OutputDirectory))
                foreach (var file in Directory.GetFiles(CombatTelemetrySession.OutputDirectory, "*.json"))
                    File.Copy(file, Path.Combine(_output, Path.GetFileName(file)), true);
            File.WriteAllLines(Path.Combine(_output, "player-checks.tsv"), _checks);
            File.WriteAllLines(Path.Combine(_output, "player-errors.txt"), _errors);
            Application.logMessageReceived -= CaptureError;
            Application.Quit(_failures == 0 && _errors.Count == 0 ? 0 : 1);
        }
    }
}
#endif
