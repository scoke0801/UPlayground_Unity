using System.Collections;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
{
    public readonly struct DefenseSuccessFeedbackContext
    {
        public readonly PlayerActor Player;
        public readonly GameActor Attacker;
        public readonly AttackData IncomingAttack;
        public readonly Vector3 Position;
        public readonly string FxKey;

        public DefenseSuccessFeedbackContext(
            PlayerActor player,
            GameActor attacker,
            AttackData incomingAttack,
            Vector3 position,
            string fxKey = null)
        {
            Player = player;
            Attacker = attacker;
            IncomingAttack = incomingAttack;
            Position = position;
            FxKey = fxKey;
        }
    }

    /// <summary>
    /// 패리/퍼펙트 가드/퍼펙트 회피 성공 순간의 짧은 고정 피드백을 담당한다.
    /// 기존 PlayerGuard 3초 슬로우 대신 성공 지점이 읽히는 짧은 freeze/tail을 제공한다.
    /// </summary>
    public sealed class DefenseSuccessFeedbackHandler : GameHandlerBase
    {
        private readonly DefenseSuccessFeedbackProfile _parryProfile =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.Parry);
        private readonly DefenseSuccessFeedbackProfile _perfectGuardProfile =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectGuard);
        private readonly DefenseSuccessFeedbackProfile _perfectDodgeProfile =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectDodge);
        private readonly DefenseSuccessFeedbackProfile _dashEvadeProfile =
            DefenseSuccessFeedbackProfile.CreateDashEvade();

        private Coroutine _routine;
        private GameActor _frozenPlayer;
        private GameActor _frozenAttacker;
        private float _originalPlayerScale = 1f;
        private float _originalAttackerScale = 1f;

        public void Play(DefenseSuccessType type, in DefenseSuccessFeedbackContext context)
            => Play(GetProfile(type), context);

        /// <summary>
        /// 대시 회피 전용 피드백. 포스트프로세스(볼륨) 플래시는 적용하지 않고
        /// 타임스케일 슬로우/카메라/FX 연출만 발동한다.
        /// </summary>
        public void PlayDashEvade(in DefenseSuccessFeedbackContext context)
            => Play(_dashEvadeProfile, context, applyPostProcess: false);

        public float GetCounterWindowDuration(DefenseSuccessType type)
            => GetProfile(type).counterWindowDuration;

        public void Play(DefenseSuccessFeedbackProfile profile, in DefenseSuccessFeedbackContext context)
            => Play(profile, context, applyPostProcess: true);

        public void Play(
            DefenseSuccessFeedbackProfile profile,
            in DefenseSuccessFeedbackContext context,
            bool applyPostProcess)
        {
            if (profile == null)
                return;

            var host = GameCombatManager.Instance;
            if (host == null)
                return;

            StopCurrentFeedback(host, true);

            _routine = host.StartCoroutine(PlayRoutine(profile, context));
            if (applyPostProcess)
                PlayPostProcess(profile);
            PlayCamera(profile, context);
            PlayFxAndReward(profile, context);
        }

        public override void Dispose()
        {
            StopImmediate();
        }

        public override void OnSceneChanged(string sceneType)
        {
            StopImmediate();
        }

        private DefenseSuccessFeedbackProfile GetProfile(DefenseSuccessType type)
        {
            return type switch
            {
                DefenseSuccessType.Parry => _parryProfile,
                DefenseSuccessType.PerfectGuard => _perfectGuardProfile,
                DefenseSuccessType.PerfectDodge => _perfectDodgeProfile,
                _ => _perfectGuardProfile,
            };
        }

        private IEnumerator PlayRoutine(DefenseSuccessFeedbackProfile profile, DefenseSuccessFeedbackContext context)
        {
            GameActor player = context.Player;
            GameActor attacker = context.Attacker;

            _frozenPlayer = player;
            _frozenAttacker = attacker;
            _originalPlayerScale = player != null ? player.LocalTimeScale : 1f;
            _originalAttackerScale = attacker != null ? attacker.LocalTimeScale : 1f;

            if (player != null)
                player.LocalTimeScale = Mathf.Min(_originalPlayerScale, profile.freezeTimeScale);
            if (attacker != null)
                attacker.LocalTimeScale = Mathf.Min(_originalAttackerScale, profile.freezeTimeScale);

            float freezeEnd = Time.realtimeSinceStartup + Mathf.Max(0f, profile.freezeDuration);
            float attackerFreezeEnd = Time.realtimeSinceStartup + Mathf.Max(profile.freezeDuration, profile.attackerFreezeDuration);
            float tailEnd = freezeEnd + Mathf.Max(0f, profile.tailDuration);

            while (Time.realtimeSinceStartup < freezeEnd)
                yield return null;

            if (player != null)
                player.LocalTimeScale = _originalPlayerScale;
            _frozenPlayer = null;

            if (profile.tailDuration > 0f)
            {
                GameObjectManager.Instance?.SetGlobalTimeScaleExceptPlayer(
                    profile.tailTimeScale,
                    profile.tailDuration);

                if (attacker != null && Time.realtimeSinceStartup < attackerFreezeEnd)
                    attacker.LocalTimeScale = Mathf.Min(_originalAttackerScale, profile.freezeTimeScale);
            }

            while (Time.realtimeSinceStartup < attackerFreezeEnd)
                yield return null;

            if (attacker != null)
            {
                attacker.LocalTimeScale = Time.realtimeSinceStartup < tailEnd
                    ? profile.tailTimeScale
                    : _originalAttackerScale;
            }

            _frozenAttacker = null;
            _routine = null;
        }

        private void PlayPostProcess(DefenseSuccessFeedbackProfile profile)
        {
            GameCombatManager.Instance?.GameHitStop?.FlashPostProcess(
                profile.postProcessPeakWeight,
                profile.postProcessHoldDuration,
                profile.postProcessFadeOutDuration,
                profile.minPostProcessVisibleDuration);
        }

        private void PlayCamera(DefenseSuccessFeedbackProfile profile, in DefenseSuccessFeedbackContext context)
        {
            var combatCamera = CameraManager.Instance?.CombatCamera;
            if (combatCamera == null)
                return;

            switch (profile.successType)
            {
                case DefenseSuccessType.PerfectDodge:
                    combatCamera.PlayPerfectDodge(context.IncomingAttack, profile.shakeKey);
                    break;
                case DefenseSuccessType.Parry:
                case DefenseSuccessType.PerfectGuard:
                    combatCamera.PlayPerfectGuard(context.IncomingAttack, profile.shakeKey);
                    break;
            }
        }

        private void PlayFxAndReward(DefenseSuccessFeedbackProfile profile, in DefenseSuccessFeedbackContext context)
        {
            string fxKey = !string.IsNullOrWhiteSpace(context.FxKey) ? context.FxKey : profile.fxKey;
            if (!string.IsNullOrWhiteSpace(fxKey))
                GameObjectManager.Instance?.ShowFX(fxKey, context.Position);

            if (profile.spawnVitalOrb)
                GameCombatManager.Instance?.GameVitalOrb?.TrySpawn(profile.vitalOrbTrigger, context.Position);
        }

        private void StopImmediate()
        {
            StopCurrentFeedback(GameCombatManager.Instance, true);
        }

        private void StopCurrentFeedback(GameCombatManager host, bool restoreTimeScale)
        {
            if (_routine != null && host != null)
                host.StopCoroutine(_routine);

            if (restoreTimeScale)
                RestoreFrozenActors();

            _routine = null;
        }

        private void RestoreFrozenActors()
        {
            if (_frozenPlayer != null)
                _frozenPlayer.LocalTimeScale = _originalPlayerScale;

            if (_frozenAttacker != null && _frozenAttacker != _frozenPlayer)
                _frozenAttacker.LocalTimeScale = _originalAttackerScale;

            _frozenPlayer = null;
            _frozenAttacker = null;
        }
    }
}
