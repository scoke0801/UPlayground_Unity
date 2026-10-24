#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround.CombatValidation
{
    /// <summary>격리 시험장에서 늑대의 실제 감지·추적·물기·이탈·피격을 계측한다.</summary>
    public sealed class WildlifePlayerProbe : MonoBehaviour
    {
        [SerializeField] private GameObject _wolfPrefab;
        [SerializeField] private GameObject _deerPrefab;
        private readonly List<string> _checks = new();
        private readonly List<string> _errors = new();
        private readonly StringBuilder _trace = new("time,distance,state,playerHealth,wolfHealth\n");
        private string _output;
        private PlayerActor _player;
        private MonsterActor _wolf;
        private bool _hasFinished;

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-wildlifeValidation") < 0) yield break;
            _output = Path.Combine(Application.persistentDataPath, "WildlifeValidation");
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-probeOutput") _output = args[i + 1];
            Directory.CreateDirectory(_output);
            Application.logMessageReceived += CaptureError;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            StartCoroutine(Watchdog());
            float deadline = Time.realtimeSinceStartup + 150;
            while (_player == null && Time.realtimeSinceStartup < deadline)
            {
                if (GameManager.Instance != null && GameManager.Instance.BootState == GameBootState.Ready)
                    _player = PartyManager.Instance?.ActiveCharacter;
                yield return null;
            }
            Check("boot", _player != null);
            if (_player == null) { Finish(); yield break; }
            yield return new WaitForSecondsRealtime(3);
            var pad = InputSystem.AddDevice<Gamepad>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
            yield return new WaitForSecondsRealtime(0.15f);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.7f);
            InputSystem.RemoveDevice(pad);
            PlayerActor.IsTrainingInvincible = false;
            foreach (var monster in FindObjectsByType<MonsterActor>(FindObjectsSortMode.None)) monster.gameObject.SetActive(false);
            Vector3 origin = _player.transform.position;
            Quaternion rotation = _player.transform.rotation;
            yield return CheckDeer(origin + rotation * Vector3.right * 8);
            _wolf = Instantiate(_wolfPrefab, origin + rotation * Vector3.forward * 6, rotation * Quaternion.Euler(0, 180, 0)).GetComponent<MonsterActor>();
            _wolf.IsTrainingOpponent = true;
            yield return null;
            float initialHealth = _player.GetCurrentHealth();
            bool acquired = false, approached = false, attacked = false;
            deadline = Time.realtimeSinceStartup + 30;
            while (Time.realtimeSinceStartup < deadline)
            {
                float distance = Vector3.Distance(_wolf.transform.position, _player.transform.position);
                acquired |= _wolf.Detection.CurrentTarget != null;
                approached |= distance < 2.5f;
                attacked |= _wolf.ActorController.CurrentState.StateId == ActorStateId.Attack;
                _trace.AppendLine($"{Time.time:F3},{distance:F3},{_wolf.ActorController.CurrentState.StateId},{_player.GetCurrentHealth():F2},{_wolf.CurrentHealth:F2}");
                if (_player.GetCurrentHealth() < initialHealth) break;
                yield return new WaitForSecondsRealtime(0.1f);
            }
            Check("proximity-target", acquired);
            Check("chase", approached);
            Check("bite-motion", attacked);
            Check("bite-damage", _player.GetCurrentHealth() < initialHealth);
            ScreenCapture.CaptureScreenshot(Path.Combine(_output, "bite.png"));
            _player.PlaceAtPose(origin + rotation * Vector3.right * 40, rotation);
            yield return new WaitForSecondsRealtime(8);
            Check("release-target", _wolf.Detection.CurrentTarget == null);
            float wolfHealth = _wolf.CurrentHealth;
            var attack = new AttackData { attacker = _player, damage = 1, reactionType = AttackReactionType.Hit,
                hitTarget = _wolf.gameObject, hitPoint = _wolf.transform.position };
            _wolf.ReceiveHit(UPlayGround.Combat.HitRequest.FromAttackData(attack));
            yield return new WaitForSecondsRealtime(0.2f);
            Check("receive-hit", _wolf.CurrentHealth < wolfHealth);
            Finish();
        }

        private IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(210);
            Check("timeout", false);
            Finish();
        }

        private IEnumerator CheckDeer(Vector3 home)
        {
            var bounds = new Bounds(home, new Vector3(100, 30, 100));
            var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
            UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, LayerMask.GetMask("Default", "Ground"),
                UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0, new List<UnityEngine.AI.NavMeshBuildMarkup>(), sources);
            var navigation = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(UnityEngine.AI.NavMesh.GetSettingsByID(0),
                sources, bounds, Vector3.zero, Quaternion.identity);
            var navigationInstance = UnityEngine.AI.NavMesh.AddNavMeshData(navigation);
            var root = Instantiate(_deerPrefab, home, Quaternion.identity);
            var deer = root.GetComponent<Gameplay.World.LivingWorld.WildlifeCreature>();
            var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var state = deer.GetType().GetField("_activity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float deadline = Time.realtimeSinceStartup + 3;
            while (Time.realtimeSinceStartup < deadline)
            {
                deer.Tick(Time.deltaTime, home - Vector3.forward * 2, true);
                yield return null;
            }
            Check("deer-flee", agent.isOnNavMesh && Vector3.Distance(home, root.transform.position) > 2);
            bool returned = false;
            deadline = Time.realtimeSinceStartup + 40;
            while (Time.realtimeSinceStartup < deadline)
            {
                deer.Tick(Time.deltaTime, home + Vector3.right * 50, true);
                if (state.GetValue(deer).ToString() == "Roaming" && Vector3.Distance(home, root.transform.position) < 1)
                { returned = true; break; }
                yield return null;
            }
            Check("deer-return", returned);
            Destroy(root);
            navigationInstance.Remove();
            Destroy(navigation);
        }

        private void Check(string name, bool passed)
        {
            _checks.Add(name + ": " + (passed ? "PASS" : "FAIL"));
            File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
        }

        private void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) _errors.Add(message + "\n" + stack);
        }

        private void Finish()
        {
            if (_hasFinished) return;
            _hasFinished = true;
            Application.logMessageReceived -= CaptureError;
            File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
            File.WriteAllLines(Path.Combine(_output, "errors.txt"), _errors);
            File.WriteAllText(Path.Combine(_output, "frames.csv"), _trace.ToString());
            Application.Quit(_checks.Exists(c => c.EndsWith("FAIL", StringComparison.Ordinal)) || _errors.Count > 0 ? 1 : 0);
        }
    }
}
#endif
