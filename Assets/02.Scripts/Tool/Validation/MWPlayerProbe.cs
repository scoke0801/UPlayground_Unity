#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Cysharp.Threading.Tasks;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.MovementController;
using UPlayGround.State;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.CombatValidation
{
    // 격리 Player에만 포함한다. 위치·표적 준비는 API, 공격은 가상 패드의 실제 입력 경로다.
    public sealed class MWPlayerProbe : MonoBehaviour
    {
        private PlayerActor _player;
        private MotionWarpController _warp;
        private Gamepad _pad;
        private MonsterActor _enemy;
        private MonsterActor _far;
        private GameObject _terrain;
        private Vector3 _origin;
        private Quaternion _rotation;
        private string _stage = "boot";
        private string _output;
        private float _deadline;
        private float _nextCapture;
        private int _capture;
        private float _approachDistance = 3.2f;
        private float _basicDistance = 1.8f;
        private string[] _scenarios;
        private CharacterActorType? _onlyCharacter;
        private readonly StringBuilder _steps = new("frame,time,character,scenario,motion,motionTime,rawX,rawZ,correctionX,correctionZ,kccX,kccZ,rotationCorrection,predictionError,target,selection,rejection,rawY,correctionY,kccY,targetX,targetY,targetZ,source,flags,poseTime,trackingCommitted,session\n");
        private readonly StringBuilder _frames = new("frame,time,scale,character,scenario,state,motion,motionTime,x,y,z,yaw,target,lockOn,ability,enemyHealth\n");
        private readonly StringBuilder _actions = new();
        private readonly List<string> _errors = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-motionWarpValidation") < 0) return;
            var host = new GameObject("MotionWarpValidationProbe");
            DontDestroyOnLoad(host);
            host.AddComponent<MWPlayerProbe>();
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _output = Path.Combine(Application.persistentDataPath, "MotionWarpValidation");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-probeOutput") _output = args[i + 1];
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-probeFps" && int.TryParse(args[i + 1], out int fps)) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = fps; }
                if (args[i] == "-basicDistance") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _basicDistance);
                if (args[i] == "-approachDistance") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _approachDistance);
                if (args[i] == "-scenarios") _scenarios = args[i + 1].Split(',');
                if (args[i] == "-character" && Enum.TryParse(args[i + 1], out CharacterActorType type)) _onlyCharacter = type;
            }
            Directory.CreateDirectory(_output);
            Application.logMessageReceived += CaptureError;
            _pad = InputSystem.AddDevice<Gamepad>();
            _deadline = Time.realtimeSinceStartup + 600f;
            float bootEnd = Time.realtimeSinceStartup + 150f;
            while (_player == null)
            {
                if (Time.realtimeSinceStartup > bootEnd) { Finish("부팅 시간 초과"); yield break; }
                if (GameManager.Instance != null && GameManager.Instance.BootState == GameBootState.Ready)
                    _player = PartyManager.Instance?.ActiveCharacter;
                yield return null;
            }
            _origin = _player.transform.position;
            _rotation = _player.transform.rotation;
            Application.runInBackground = true;
            // 이 Player 프로세스 안에서만 실물 입력을 제외해 작업 중 Alt/메뉴 입력의 간섭을 막는다.
            foreach (var device in InputSystem.devices) if (device != _pad) InputSystem.DisableDevice(device);
            PlayerActor.IsTrainingInvincible = true;
            yield return new WaitForSecondsRealtime(3f);
            yield return Press(GamepadButton.East);
            yield return new WaitForSecondsRealtime(.7f);
            foreach (var actor in FindObjectsByType<MonsterActor>(FindObjectsSortMode.None))
                actor.gameObject.SetActive(false);
            var party = PartyManager.Instance;
            var primary = _onlyCharacter ?? CharacterActorType.Raon;
            var secondary = primary == CharacterActorType.Raon ? CharacterActorType.Hwarin : CharacterActorType.Raon;
            party.EnsureCharacterUnlocked(primary);
            party.EnsureCharacterUnlocked(secondary);
            CharacterActorType[] order = { primary, secondary };
            var prepare = party.SetBattleOrderAsync(order).AsTask();
            while (!prepare.IsCompleted) yield return null;
            if (prepare.IsFaulted || party.BattleOrder.Count != 2
                || party.BattleOrder[0] != order[0]
                || party.BattleOrder[1] != order[1])
            { Finish("파티 준비 실패"); yield break; }
            var preparedModel = party.ActiveCharacter.GetComponentInChildren<CharacterModelData>();
            if (preparedModel == null || preparedModel.characterType != order[0])
            {
                _actions.AppendLine("파티 순서 변경 뒤 모델 동기화를 위한 실제 교체");
                party.ClearAllSwapCooldowns();
                party.RequestSwapTo(1);
                yield return new WaitForSecondsRealtime(3f);
                party.ClearAllSwapCooldowns();
                party.RequestSwapTo(0);
                yield return new WaitForSecondsRealtime(3f);
            }
            for (int index = 0; index < (_onlyCharacter.HasValue ? 1 : 2); index++)
            {
                party.ClearAllSwapCooldowns();
                if (party.ActiveIndex != index) party.RequestSwapTo(index);
                yield return new WaitForSecondsRealtime(2f);
                _player = party.ActiveCharacter;
                var model = _player.GetComponentInChildren<CharacterModelData>();
                var equipment = _player.GetPlayerEquipment();
                _actions.AppendLine($"준비: 요청={order[index]}; 활성={party.ActiveCharacterType}; 모델={model?.characterType}; 무기={equipment?.GetMainWeaponType()}; 정의무기={model?.defaultWeaponType}; 매핑={(_player.Animator as UPlayGround.Animation.PlayerActorAnimator)?.PlayerMotionSet?.name}");
                if (model == null || model.characterType != order[index] || equipment == null || equipment.GetMainWeaponType() != model.defaultWeaponType)
                { Finish("캐릭터/무기 준비 불일치"); yield break; }
                if (_warp != null) _warp.MotorStepEvaluated -= RecordStep;
                _warp = _player.GetComponent<MotionWarpController>();
                if (_warp != null) _warp.MotorStepEvaluated += RecordStep;
                foreach (string scenario in _scenarios ?? new[] { "basic", "combo", "comboReach", "comboWall", "heavy", "approach", "approachWall", "approachSlope", "approachStep", "sideNearFrontFar", "inputSide", "lockNear", "lockFar", "height", "death", "wall", "slope", "step", "slow", "hitstop", "evadeAfterCommit", "cancel", "swap", "finish" })
                    yield return RunScenario(scenario);
            }
            Finish(null);
        }

        private IEnumerator RunScenario(string scenario)
        {
            _stage = "setup";
            Time.timeScale = 1f;
            InputSystem.QueueStateEvent(_pad, new GamepadState());
            CameraManager.Instance.ReleaseLockOn();
            if (_enemy != null) Destroy(_enemy.gameObject);
            if (_far != null) Destroy(_far.gameObject);
            if (_terrain != null) Destroy(_terrain);
            yield return new WaitForSecondsRealtime(.5f);
            _player.PlaceAtPose(_origin, _rotation);
            CameraManager.Instance.SetRotationSmooth(_rotation.eulerAngles.y, 12f, .1f);
            Vector3 forward = _rotation * Vector3.forward;
            Vector3 right = _rotation * Vector3.right;
            Vector3 point = _origin + forward * _basicDistance;
            if (scenario == "comboReach" || scenario == "comboWall" || scenario == "slow" || scenario == "hitstop") point = _origin + forward * 1.8f;
            if (scenario == "finish") point = _origin + Quaternion.AngleAxis(35f, Vector3.up) * forward * 1.1f;
            if (scenario == "noTarget") point = _origin + forward * 6f;
            if (scenario == "lockFar") point = _origin + forward * 6f;
            if (scenario == "sideNearFrontFar" || scenario == "inputSide") point = _origin + Quaternion.AngleAxis(35f, Vector3.up) * forward * 1.15f;
            if (scenario == "height")
            {
                point += Vector3.up * 1.2f;
                MakeTerrain(point - Vector3.up * .1f, new Vector3(1f, .2f, 1f), _rotation);
            }
            if (scenario.StartsWith("approach")) point = _origin + forward * 14f;
            if (scenario == "wall") MakeTerrain(_origin + forward * .72f + Vector3.up, new Vector3(3f, 2f, .15f), _rotation);
            if (scenario == "slope")
            {
                MakeTerrain(_origin + forward * .8f - Vector3.up * .2f, new Vector3(4f, .3f, 4f), _rotation * Quaternion.Euler(-12f, 0, 0));
                _player.PlaceAtPose(_origin + Vector3.up * .4f, _rotation);
                point += Vector3.up * .45f;
            }
            if (scenario == "step") MakeTerrain(_origin + forward * 1.1f + Vector3.up * .08f, new Vector3(3f, .16f, 1f), _rotation);
            _enemy = Spawn(point);
            if (_enemy != null) _enemy.name = "ProbeNearTarget";
            if (scenario == "sideNearFrontFar" || scenario == "inputSide") _far = Spawn(_origin + forward * 5.5f);
            if (_far != null) _far.name = "ProbeFarTarget";
            yield return new WaitForSecondsRealtime(.7f);
            if (_enemy == null) { _errors.Add("표적 준비 실패 " + scenario); yield break; }
            PinTarget(_enemy, point);
            if (scenario == "finish")
            {
                _enemy.SetHealth(1f);
                _enemy.BreakGauge?.ForceExpose();
                _actions.AppendLine("피니시 준비: " + _player.GetCombat().IsFinishableTarget(_enemy.transform));
            }
            if (_far != null) PinTarget(_far, _origin + forward * 5.5f);
            yield return null;
            if (scenario == "lockNear" || scenario == "lockFar")
            {
                yield return Press(GamepadButton.RightStick);
                yield return new WaitForSecondsRealtime(.3f);
            }
            _stage = scenario;
            _capture = 0;
            _nextCapture = 0f;
            _actions.AppendLine($"{PartyManager.Instance.ActiveCharacterType}/{scenario}: start={_player.transform.position},target={_enemy.transform.position},lock={CameraManager.Instance.GetLockOnTarget()?.name}");
            if (scenario == "slow") Time.timeScale = .35f;
            if (scenario == "comboWall") StartCoroutine(BlockAfterCorrection());
            if (scenario.StartsWith("approach"))
            {
                _stage = "runup";
                InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick));
                yield return new WaitForSecondsRealtime(.1f);
                InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = Vector2.up });
                float sprintDeadline = Time.realtimeSinceStartup + 5f;
                while (_player.MoveAnimType != BaseMoveAnimType.Sprint && Time.realtimeSinceStartup < sprintDeadline) yield return null;
                PinTarget(_enemy, _player.transform.position + _player.transform.forward * _approachDistance);
                _actions.AppendLine($"접근 공격 입력: move={_player.MoveAnimType}; start={_player.transform.position}; target={_enemy.transform.position}");
                _stage = scenario;
                _capture = 0;
                if (scenario == "approachWall") StartCoroutine(BlockAfterCorrection());
                yield return Press(GamepadButton.North, Vector2.up);
            }
            else yield return Press(scenario == "heavy" || scenario == "finish" ? GamepadButton.North : GamepadButton.West,
                scenario == "inputSide" ? Vector2.right : Vector2.zero);
            if (scenario == "chain")
            {
                var model = _player.GetComponentInChildren<CharacterModelData>();
                int count = model.abilitySet.GetCombatSequence(UPlayGround.Data.Ability.PlayerCombatAbilitySlot.LightCombo).Count;
                for (int comboIndex = 1; comboIndex < count; comboIndex++)
                {
                    float comboDeadline = Time.realtimeSinceStartup + 4f;
                    while (!_player.GetCombat().CanCombo && Time.realtimeSinceStartup < comboDeadline) yield return null;
                    if (!_player.GetCombat().CanCombo)
                    { _actions.AppendLine("연계 창 미발동: index=" + comboIndex); break; }
                    if (_enemy != null && _enemy.CurrentHealth > 0f) _enemy.SetHealth(_enemy.MaxHealth);
                    _actions.AppendLine("전체 연계 입력: index=" + comboIndex);
                    yield return Press(GamepadButton.West);
                    yield return new WaitForSecondsRealtime(.12f);
                }
            }
            if (scenario == "combo" || scenario == "comboReach" || scenario == "comboWall" || scenario == "slow" || scenario == "hitstop")
            {
                float comboDeadline = Time.realtimeSinceStartup + 4f;
                while (!_player.GetCombat().CanCombo && Time.realtimeSinceStartup < comboDeadline) yield return null;
                _actions.AppendLine("연계 입력: window=" + _player.GetCombat().CanCombo);
                yield return Press(GamepadButton.West);
            }
            if (scenario == "death")
            {
                yield return new WaitForSecondsRealtime(.10f);
                _enemy.SetHealth(0f);
                _actions.AppendLine("대상 체력 0 설정");
            }
            if (scenario == "cancel")
            {
                float cancelDeadline = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < cancelDeadline
                    && (_player.GetCombat().ResolveCancelMask() & PlayerInterruptAction.Dodge) == 0)
                    yield return null;
                _actions.AppendLine("회피 캔슬 입력 직전: mask=" + _player.GetCombat().ResolveCancelMask()
                    + "; state=" + _player.GetComponent<PlayerMovementController>().CurrentState.StateId);
                yield return Press(GamepadButton.LeftShoulder);
            }
            if (scenario == "swap")
            {
                yield return new WaitForSecondsRealtime(.1f);
                PartyManager.Instance.ClearAllSwapCooldowns();
                _actions.AppendLine($"교체 입력 직전: warp={_warp.IsMotionWarping}; committed={_warp.HasCommittedAttackTracking}; target={SafeName(_warp.ActiveTarget.anchor)}");
                yield return Press(GamepadButton.DpadRight);
            }
            if (scenario == "hitstop")
            {
                yield return new WaitForSecondsRealtime(.12f);
                GameCombatManager.Instance.GameHitStop.Execute(.2f, .02f);
            }
            if (scenario == "evadeAfterCommit")
            {
                float end = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < end && !_warp.HasCommittedAttackTracking) yield return null;
                var movement = _enemy.GetComponent<ActorMovementController>();
                if (_warp.HasCommittedAttackTracking && EnemyDodgeState.CanExecute(_enemy))
                {
                    _enemy.Detection.AcquireTarget(_player.transform);
                    movement.TransitionToState(new EnemyDodgeState(movement,
                        _enemy.GetComponent<EnemyAIContext>(), _enemy.Detection, right, MotionTags.Dodge_R));
                    _actions.AppendLine("추적 종료 후 실제 EnemyDodgeState: " + movement.CurrentState.StateId
                        + "; 대상 위치=" + _enemy.transform.position + "; 플레이어 yaw=" + _player.transform.eulerAngles.y);
                }
                else _errors.Add("추적 종료/회피 준비 실패: " + PartyManager.Instance.ActiveCharacterType);
            }
            yield return new WaitForSecondsRealtime(scenario == "slow" || scenario == "finish" ? 4f : 2f);
            Time.timeScale = 1f;
            _actions.AppendLine($"end={_player.transform.position},selection={_player.GetCombat().AttackTargetSelectionReason},rejection={_warp.LastFailureReason}");
            if (scenario == "evadeAfterCommit")
                _actions.AppendLine("회피 후 대상 위치=" + _enemy.transform.position + "; 플레이어 yaw=" + _player.transform.eulerAngles.y);
            if (scenario == "swap")
            {
                _actions.AppendLine("교체 결과: " + PartyManager.Instance.ActiveCharacterType);
                _enemy.gameObject.SetActive(false);
                yield return null;
                PartyManager.Instance.ClearAllSwapCooldowns();
                PartyManager.Instance.RequestSwapTo(0);
                yield return new WaitForSecondsRealtime(2f);
            }
            Flush();
        }

        private MonsterActor Spawn(Vector3 point)
        {
            var enemy = ActorSpawnManager.Instance.SpawnActor("Enemy_P09_Armor07_DualAxe", point, _rotation * Quaternion.Euler(0,180,0)) as MonsterActor;
            if (enemy != null) { enemy.IsTrainingOpponent = true; enemy.AIController?.Freeze(); }
            return enemy;
        }

        private IEnumerator BlockAfterCorrection()
        {
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline && _warp.CorrectionStepDelta.sqrMagnitude < .00000001f) yield return null;
            if (_warp.CorrectionStepDelta.sqrMagnitude < .00000001f)
            { _actions.AppendLine("이동 보정 미발동: 보정 중 벽 충돌 시나리오 미검증"); yield break; }
            MakeTerrain(_player.transform.position + _player.transform.forward * .6f + Vector3.up,
                new Vector3(3f, 2f, .15f), _player.transform.rotation);
            _actions.AppendLine("이동 보정 발동 뒤 벽 생성");
            yield return new WaitForSecondsRealtime(.12f);
            Destroy(_terrain);
            _actions.AppendLine("벽 제거: 이후 보정 재분배 여부 확인");
        }

        private void PinTarget(MonsterActor enemy, Vector3 position)
        {
            enemy.AIController?.Freeze();
            var ai = enemy.GetComponent<EnemyAIController>();
            if (ai != null) ai.enabled = false;
            var movement = enemy.GetComponent<ActorMovementController>();
            movement.TransitionToState(ActorStateId.Idle);
            movement.Motor.SetPositionAndRotation(position, _rotation * Quaternion.Euler(0,180,0));
            movement.Motor.BaseVelocity = Vector3.zero;
        }

        private void MakeTerrain(Vector3 point, Vector3 scale, Quaternion rotation)
        {
            _terrain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _terrain.name = "검증 전용 지형";
            _terrain.layer = LayerMask.NameToLayer("Ground");
            _terrain.transform.SetPositionAndRotation(point, rotation);
            _terrain.transform.localScale = scale;
        }

        private IEnumerator Press(GamepadButton button, Vector2 movement = default)
        {
            var pressed = new GamepadState { leftStick = movement }.WithButton(button);
            if (button == GamepadButton.LeftShoulder) pressed = pressed.WithButton(GamepadButton.RightShoulder);
            InputSystem.QueueStateEvent(_pad, pressed);
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(.06f);
            if (button == GamepadButton.North && _stage.StartsWith("approach"))
            {
                // 입력 뒤 표적을 재배치하면 실제 접근 거리를 측정할 수 없다.
                if (_stage == "approachSlope")
                {
                    MakeTerrain(_player.transform.position + _player.transform.forward * 1.2f + Vector3.up * .15f,
                        new Vector3(3f, .2f, 3f), _player.transform.rotation * Quaternion.Euler(-12f, 0, 0));
                    PinTarget(_enemy, _player.transform.position + _player.transform.forward * _approachDistance + Vector3.up * .4f);
                }
                if (_stage == "approachStep")
                {
                    MakeTerrain(_player.transform.position + _player.transform.forward * 1.1f + Vector3.up * .08f,
                        new Vector3(3f, .16f, 1f), _player.transform.rotation);
                    PinTarget(_enemy, _player.transform.position + _player.transform.forward * _approachDistance + Vector3.up * .16f);
                }
                _actions.AppendLine($"접근 확정 입력 직전: player={_player.transform.position}; target={_enemy.transform.position}");
            }
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = movement });
            if (movement != Vector2.zero)
            {
                yield return new WaitForSecondsRealtime(.25f);
                InputSystem.QueueStateEvent(_pad, new GamepadState());
            }
        }

        private static string SafeName(UnityEngine.Object value) => value != null ? value.name : string.Empty;

        private void RecordStep(MotionWarpController warp)
        {
            var raw = warp.OriginalStepDelta;
            var correction = warp.CorrectionStepDelta;
            var actual = warp.ActualMotorStepDelta;
            var a = _player.Animator.CaptureDebugSnapshot();
            Vector3 target = warp.ActiveTarget.anchor != null ? warp.ActiveTarget.anchor.position : Vector3.zero;
            string flags = warp.ConstraintFlags.ToString().Replace(',', '|');
            _steps.AppendLine(FormattableString.Invariant($"{Time.frameCount},{Time.time:R},{PartyManager.Instance.ActiveCharacterType},{_stage},{SafeName(a.SourceAsset)},{a.GlobalTime:R},{raw.x:R},{raw.z:R},{correction.x:R},{correction.z:R},{actual.x:R},{actual.z:R},{warp.RotationCorrectionDegrees:R},{warp.PredictionError:R},{SafeName(warp.ActiveTarget.anchor)},{_player.GetCombat().AttackTargetSelectionReason},{warp.LastFailureReason},{raw.y:R},{correction.y:R},{actual.y:R},{target.x:R},{target.y:R},{target.z:R},{warp.RootMotionSourceQuality},{flags},{_player.Animator.CurrentEvaluatedMotionSetTime:R},{warp.HasCommittedAttackTracking},{warp.ActiveHandle.Sequence}"));
        }

        private void LateUpdate()
        {
            // 설정 초기화가 덮어쓴 뒤에도 검증 프로세스는 포커스 없이 진행해야 한다.
            Application.runInBackground = true;
            if (_deadline > 0 && Time.realtimeSinceStartup > _deadline) { Finish("실행 시간 초과"); return; }
            if (_player?.Animator == null || _warp == null) return;
            var a = _player.Animator.CaptureDebugSnapshot();
            var p = _player.transform.position;
            _frames.AppendLine(FormattableString.Invariant($"{Time.frameCount},{Time.time:R},{Time.timeScale:R},{PartyManager.Instance.ActiveCharacterType},{_stage},{_player.GetComponent<PlayerMovementController>()?.CurrentState?.StateId},{SafeName(a.SourceAsset)},{a.GlobalTime:R},{p.x:R},{p.y:R},{p.z:R},{_player.transform.eulerAngles.y:R},{SafeName(_warp.ActiveTarget.anchor)},{CameraManager.Instance.IsLockOnActive()},{_player.Abilities?.CurrentAbilityId},{(_enemy != null ? _enemy.CurrentHealth : -1f):R}"));
            bool longCapture = _stage == "slow" || _stage == "finish";
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture") >= 0 && _stage != "boot" && _stage != "setup" && Time.unscaledTime >= _nextCapture && _capture < (longCapture ? 16 : 10))
            {
                _nextCapture = Time.unscaledTime + (longCapture ? .25f : .1f);
                ScreenCapture.CaptureScreenshot(Path.Combine(_output, $"{PartyManager.Instance.ActiveCharacterType}_{_stage}_{_capture++:00}.png"));
            }
        }

        private void CaptureError(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && _errors.Count < 40)
                _errors.Add(message + "\n" + stack);
        }

        private void Flush()
        {
            File.WriteAllText(Path.Combine(_output, "kcc-steps.csv"), _steps.ToString());
            File.WriteAllText(Path.Combine(_output, "frames.csv"), _frames.ToString());
            File.WriteAllText(Path.Combine(_output, "actions.txt"), _actions.ToString());
        }

        private void Finish(string failure)
        {
            _deadline = 0f;
            Application.logMessageReceived -= CaptureError;
            if (_warp != null) _warp.MotorStepEvaluated -= RecordStep;
            Flush();
            File.WriteAllText(Path.Combine(_output, "result.txt"), "Unity=" + Application.unityVersion + "\n" + (failure ?? "시퀀스 종료: 실제 발동 및 수치·화면 별도 판정 필요") + "\n" + string.Join("\n", _errors));
            InputSystem.RemoveDevice(_pad);
            Application.Quit(failure == null && _errors.Count == 0 ? 0 : 1);
        }
    }
}

#endif
