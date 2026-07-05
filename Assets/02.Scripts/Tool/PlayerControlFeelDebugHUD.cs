#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Text;
using Game.Input;
using KinematicCharacterController;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UPlayGround.Component;
using UPlayGround.Diagnostics;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace UPlayGround.Tool.Debugging
{
    /// <summary>
    /// 플레이 중 조작감 문제를 빠르게 분류하기 위한 런타임 HUD.
    /// F9로 표시/숨김을 전환한다. Editor/Development Build에서만 컴파일된다.
    /// </summary>
    public sealed class PlayerControlFeelDebugHUD : MonoBehaviour
    {
        private const Key ToggleKey = Key.F9;
        private static readonly StringBuilder Builder = new(2048);

        [SerializeField] private bool _visible;
        [SerializeField] private Vector2 _position = new(12f, 12f);
        [SerializeField] private Vector2 _size = new(520f, 520f);

        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PlayerControlFeelDebugHUD>() != null)
                return;

            var go = new GameObject(nameof(PlayerControlFeelDebugHUD));
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerControlFeelDebugHUD>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            EnsureStyles();

            Rect rect = new Rect(_position, _size);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), BuildText(), _labelStyle);
        }

        private void EnsureStyles()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = false,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
            };

            _boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = Texture2D.grayTexture,
                    textColor = Color.white,
                },
            };
        }

        private static string BuildText()
        {
            Builder.Clear();

            PlayerActor player = ResolvePlayer();
            PlayerMovementController controller = player != null ? player.PlayerController : null;
            PlayerCombat combat = player != null ? player.GetCombat() : null;
            KinematicCharacterMotor motor = controller != null ? controller.Motor : null;
            GameActorState state = controller != null ? controller.CurrentState : null;

            Builder.AppendLine("[Control Feel Debug]  F9 Toggle");
            Builder.AppendLine();

            if (player == null || controller == null)
            {
                Builder.AppendLine("Player: not found");
                return Builder.ToString();
            }

            Builder.Append("Player: ").Append(player.name).AppendLine();
            Builder.Append("State: ").Append(state != null ? state.StateName : "None")
                .Append(" | Grounded: ").Append(motor != null && motor.GroundingStatus.IsStableOnGround)
                .Append(" | MoveAnim: ").Append(player.MoveAnimType)
                .AppendLine();
            Builder.Append("MoveInput: ").Append(FormatVector(controller.MoveInputVector))
                .Append(" | LookInput: ").Append(FormatVector(controller.LookInputVector))
                .AppendLine();
            Builder.Append("Dash: ").Append(controller.IsDashReady ? "Ready" : $"{controller.DashCooldownRemaining:0.00}s")
                .Append(" / ").Append(controller.DashCooldownDuration.ToString("0.00")).Append("s")
                .AppendLine();

            if (combat != null)
            {
                Builder.AppendLine();
                Builder.AppendLine("[Combat]");
                Builder.Append("InCombat: ").Append(combat.IsInCombat)
                    .Append(" | Collision: ").Append(combat.IsPossibleCollide)
                    .Append(" | CancelWindow: ").Append(combat.IsCancelWindowOpen)
                    .Append(" | CanCombo: ").Append(combat.CanCombo)
                    .AppendLine();
                Builder.Append("Phase: ").Append(combat.CurrentHitPhaseIndex)
                    .Append(" / ").Append(combat.LastHitPhaseIndex)
                    .Append(" | CurrentAttack: ").Append(combat.CurrentAttackData != null ? combat.CurrentAttackData.animKey.ToString() : "None")
                    .AppendLine();
                Builder.Append("PerfectDodge: ").Append(combat.IsPerfectDodgeWindow)
                    .Append(" | DodgeCounter: ").Append(combat.IsDodgeCounterAvailable)
                    .Append(" | GuardCounter: ").Append(combat.IsPerfectGuardCounterAvailable)
                    .Append(" | ParryCounter: ").Append(combat.IsParryCounterAvailable)
                    .AppendLine();
            }

            Builder.AppendLine();
            Builder.AppendLine("[InputBuffer]");
            AppendInputBuffer();

            Builder.AppendLine();
            Builder.Append("[Last Interrupt Fail] ")
                .Append(PlayerInterruptResolver.LastFailReason)
                .Append(" - ")
                .Append(PlayerInterruptResolver.LastFailDetail)
                .AppendLine();

            Builder.AppendLine();
            Builder.Append("[LockOn] ");
            Transform lockOn = CameraManager.Instance != null ? CameraManager.Instance.GetLockOnTarget() : null;
            Builder.AppendLine(lockOn != null ? lockOn.name : "None");

            return Builder.ToString();
        }

        private static PlayerActor ResolvePlayer()
        {
            PlayerActor player = GameObjectManager.Instance != null ? GameObjectManager.Instance.Player : null;
            return player != null ? player : FindFirstObjectByType<PlayerActor>();
        }

        private static void AppendInputBuffer()
        {
            InputBuffer buffer = InputManager.Instance != null ? InputManager.Instance.InputBuffer : null;
            if (buffer == null)
            {
                Builder.AppendLine("  buffer missing");
                return;
            }

            var snapshot = buffer.GetSnapshot();
            if (snapshot.Count == 0)
            {
                Builder.AppendLine("  empty");
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                BufferedInput input = snapshot[i];
                Builder.Append("  ")
                    .Append(input.InputName)
                    .Append(" | remain ")
                    .Append(input.RemainingTime.ToString("0.000"))
                    .Append(" / ")
                    .Append(input.BufferTime.ToString("0.000"))
                    .Append("s")
                    .AppendLine();
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
        }
    }

    /// <summary>
    /// F10으로 표시하고 F12로 최근 600프레임의 기준선을 JSON 저장하는 개발용 성능 모니터.
    /// (F11은 개발 치트 패널 전용)
    /// </summary>
    public sealed class RuntimePerformanceMonitor : MonoBehaviour
    {
        private const int Capacity = 600;
        private static readonly StringBuilder TextBuilder = new(1024);
        private readonly float[] _frameMs = new float[Capacity];
        private readonly long[] _gcBytes = new long[Capacity];

        [SerializeField] private bool _visible;
        [SerializeField, Min(1f)] private float _targetFps = 60f;
        [SerializeField, Min(0f)] private float _gcWarningBytesPerFrame = 1024f;
        [SerializeField, Range(0f, 1f)] private float _slowFrameWarningRatio = 0.1f;

        private ProfilerRecorder _gcRecorder;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _setPassRecorder;
        private GUIStyle _style;
        private int _count;
        private int _index;
        private float _frameSum;
        private long _gcSum;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<RuntimePerformanceMonitor>() != null)
                return;

            var go = new GameObject(nameof(RuntimePerformanceMonitor));
            DontDestroyOnLoad(go);
            go.AddComponent<RuntimePerformanceMonitor>();
        }

        private void OnEnable()
        {
            _gcRecorder = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            _drawCallsRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            _setPassRecorder = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        }

        private void OnDisable()
        {
            _gcRecorder.Dispose();
            _drawCallsRecorder.Dispose();
            _setPassRecorder.Dispose();
        }

        private void Update()
        {
            AddSample(Time.unscaledDeltaTime * 1000f, ReadValue(_gcRecorder));

            if (Keyboard.current == null)
                return;
            if (Keyboard.current[Key.F10].wasPressedThisFrame)
                _visible = !_visible;
            // F11은 개발 치트 패널(DevCheatBootstrap) 전용이므로 캡처는 F12를 사용한다.
            if (Keyboard.current[Key.F12].wasPressedThisFrame)
                SaveSnapshot();
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            _style ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                padding = new RectOffset(10, 10, 8, 8),
            };
            GUI.Box(new Rect(548f, 12f, 500f, 280f), BuildText(), _style);
        }

        private void AddSample(float frameMilliseconds, long gcAllocatedBytes)
        {
            if (_count == Capacity)
            {
                _frameSum -= _frameMs[_index];
                _gcSum -= _gcBytes[_index];
            }
            else
            {
                _count++;
            }

            _frameMs[_index] = frameMilliseconds;
            _gcBytes[_index] = Math.Max(0L, gcAllocatedBytes);
            _frameSum += frameMilliseconds;
            _gcSum += _gcBytes[_index];
            _index = (_index + 1) % Capacity;
        }

        private string BuildText()
        {
            CalculateMaximums(out float maxFrameMs, out long maxGcBytes);
            float averageFrameMs = AverageFrameMs;
            float frameBudgetMs = 1000f / Mathf.Max(1f, _targetFps);
            float slowRatio = CalculateSlowFrameRatio(frameBudgetMs);

            TextBuilder.Clear();
            TextBuilder.AppendLine("[Runtime Performance] F10 Toggle / F12 JSON Capture");
            TextBuilder.Append("Scene: ").Append(UnitySceneManager.GetActiveScene().name).AppendLine();
            TextBuilder.Append("Window: ").Append(_count).Append(" frames").AppendLine();
            TextBuilder.Append("Frame Avg: ").Append(averageFrameMs.ToString("0.00")).Append(" ms / ")
                .Append(averageFrameMs > 0f ? (1000f / averageFrameMs).ToString("0.0") : "0").Append(" FPS").AppendLine();
            TextBuilder.Append("Frame Max: ").Append(maxFrameMs.ToString("0.00")).Append(" ms").AppendLine();
            TextBuilder.Append("Slow Frames: ").Append((slowRatio * 100f).ToString("0.0")).Append("% ")
                .Append(slowRatio <= _slowFrameWarningRatio ? "[PASS]" : "[WARN]").AppendLine();
            TextBuilder.Append("GC Avg: ").Append(FormatBytes(AverageGcBytes)).Append(" / frame ")
                .Append(AverageGcBytes <= _gcWarningBytesPerFrame ? "[PASS]" : "[WARN]").AppendLine();
            TextBuilder.Append("GC Max: ").Append(FormatBytes(maxGcBytes)).AppendLine();
            TextBuilder.Append("Draw Calls / SetPass: ").Append(ReadValue(_drawCallsRecorder)).Append(" / ")
                .Append(ReadValue(_setPassRecorder)).AppendLine();
            TextBuilder.Append("Mono Used: ").Append(FormatBytes(Profiler.GetMonoUsedSizeLong())).AppendLine();
            TextBuilder.Append("Total Allocated: ").Append(FormatBytes(Profiler.GetTotalAllocatedMemoryLong()));
            return TextBuilder.ToString();
        }

        private void SaveSnapshot()
        {
            CalculateMaximums(out float maxFrameMs, out long maxGcBytes);
            float frameBudgetMs = 1000f / Mathf.Max(1f, _targetFps);
            var report = new PerformanceSnapshot
            {
                capturedAtUtc = DateTime.UtcNow.ToString("O"),
                scene = UnitySceneManager.GetActiveScene().name,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                sampleCount = _count,
                targetFps = _targetFps,
                averageFrameMilliseconds = AverageFrameMs,
                maximumFrameMilliseconds = maxFrameMs,
                slowFrameRatio = CalculateSlowFrameRatio(frameBudgetMs),
                averageGcAllocatedBytes = AverageGcBytes,
                maximumGcAllocatedBytes = maxGcBytes,
                drawCalls = ReadValue(_drawCallsRecorder),
                setPassCalls = ReadValue(_setPassRecorder),
                monoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                totalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
            };

            string directory = Path.Combine(Application.persistentDataPath, "PerformanceSnapshots");
            Directory.CreateDirectory(directory);
            string sceneName = string.IsNullOrWhiteSpace(report.scene) ? "NoScene" : report.scene;
            string path = Path.Combine(directory, $"{DateTime.Now:yyyyMMdd_HHmmss}_{sceneName}.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            RuntimeLog.Trace(RuntimeLogCategory.Performance, $"[Performance] 기준선 저장 완료: {path}", this);
        }

        private float AverageFrameMs => _count > 0 ? _frameSum / _count : 0f;
        private long AverageGcBytes => _count > 0 ? _gcSum / _count : 0L;

        private float CalculateSlowFrameRatio(float budgetMs)
        {
            if (_count == 0)
                return 0f;

            int slowCount = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_frameMs[i] > budgetMs)
                    slowCount++;
            }
            return (float)slowCount / _count;
        }

        private void CalculateMaximums(out float maxFrameMs, out long maxGcBytes)
        {
            maxFrameMs = 0f;
            maxGcBytes = 0L;
            for (int i = 0; i < _count; i++)
            {
                maxFrameMs = Mathf.Max(maxFrameMs, _frameMs[i]);
                maxGcBytes = Math.Max(maxGcBytes, _gcBytes[i]);
            }
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string statName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, statName, 1);
            }
            catch (Exception exception)
            {
                RuntimeLog.Trace(
                    RuntimeLogCategory.Performance,
                    $"[Performance] ProfilerRecorder 시작 실패: {statName} ({exception.Message})");
                return default;
            }
        }

        private static long ReadValue(ProfilerRecorder recorder)
            => recorder.Valid && recorder.Count > 0 ? recorder.LastValue : 0L;

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L)
                return $"{bytes / (1024f * 1024f):0.00} MB";
            if (bytes >= 1024L)
                return $"{bytes / 1024f:0.00} KB";
            return $"{bytes} B";
        }

        [Serializable]
        private sealed class PerformanceSnapshot
        {
            public string capturedAtUtc;
            public string scene;
            public string unityVersion;
            public string platform;
            public int sampleCount;
            public float targetFps;
            public float averageFrameMilliseconds;
            public float maximumFrameMilliseconds;
            public float slowFrameRatio;
            public long averageGcAllocatedBytes;
            public long maximumGcAllocatedBytes;
            public long drawCalls;
            public long setPassCalls;
            public long monoUsedBytes;
            public long totalAllocatedBytes;
        }
    }
}
#endif
