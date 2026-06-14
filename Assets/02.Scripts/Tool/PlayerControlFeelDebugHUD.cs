#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using Game.Input;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

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
}
#endif
