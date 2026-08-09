using System.Collections;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
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
            => Play(GetProfile(type, context.Player), context);

        /// <summary>
        /// 대시 회피 전용 피드백. 포스트프로세스(볼륨) 플래시는 적용하지 않고
        /// 타임스케일 슬로우/카메라/FX 연출만 발동한다.
        /// </summary>
        public void PlayDashEvade(in DefenseSuccessFeedbackContext context)
            => Play(GetDashEvadeProfile(context.Player), context, applyPostProcess: false);

        public float GetCounterWindowDuration(DefenseSuccessType type, PlayerActor player = null)
            => GetProfile(type, player).counterWindowDuration;

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
            GameObjectManager.Instance?.ResetAllActorsTimeScaleIncludingPlayer();
        }

        public override void OnSceneChanged(string sceneType)
        {
            StopImmediate();
            GameObjectManager.Instance?.ResetAllActorsTimeScaleIncludingPlayer();
        }

        private DefenseSuccessFeedbackProfile GetProfile(DefenseSuccessType type, PlayerActor player)
        {
            DefenseSuccessFeedbackProfile configured = player?
                .Definition?
                .EffectiveCombatDefensePolicy?
                .GetFeedbackProfile(type);
            if (configured != null)
                return configured;

            return GetDefaultProfile(type);
        }

        private DefenseSuccessFeedbackProfile GetDashEvadeProfile(PlayerActor player)
            => player?.Definition?.EffectiveCombatDefensePolicy?.dashEvadeFeedback ?? _dashEvadeProfile;

        private DefenseSuccessFeedbackProfile GetDefaultProfile(DefenseSuccessType type)
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
            _originalPlayerScale = ResolveTrueOriginalScale(player);
            _originalAttackerScale = ResolveTrueOriginalScale(attacker);

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
                    combatCamera.PlayPerfectDodge(ToCameraContext(context.IncomingAttack), profile.shakeKey);
                    break;
                case DefenseSuccessType.Parry:
                case DefenseSuccessType.PerfectGuard:
                    combatCamera.PlayPerfectGuard(ToCameraContext(context.IncomingAttack), profile.shakeKey);
                    break;
            }
        }

        private static CombatCameraAttackContext ToCameraContext(AttackData attackData)
        {
            return new CombatCameraAttackContext(
                attackData?.attacker != null ? attackData.attacker.transform : null,
                attackData?.hitTarget != null ? attackData.hitTarget.transform : null,
                attackData?.hitPoint ?? Vector3.zero,
                attackData?.attackDirection ?? Vector3.zero,
                attackData?.attackKind ?? AttackKind.NormalAttack,
                attackData?.reactionType ?? AttackReactionType.Hit);
        }

        private void PlayFxAndReward(DefenseSuccessFeedbackProfile profile, in DefenseSuccessFeedbackContext context)
        {
            string fxKey = !string.IsNullOrWhiteSpace(context.FxKey) ? context.FxKey : profile.fxKey;
            if (!string.IsNullOrWhiteSpace(fxKey))
                GameObjectManager.Instance?.ShowFX(fxKey, context.Position);

            if (!string.IsNullOrWhiteSpace(profile.soundKey))
                SoundManager.Instance?.PlaySfx(profile.soundKey, context.Position);

            if (profile.spawnVitalOrb)
                GameCombatManager.Instance?.GameVitalOrb?.TrySpawn(profile.vitalOrbTrigger, context.Position);
        }

        private void StopImmediate()
        {
            StopCurrentFeedback(GameCombatManager.Instance, true);
        }

        /// <summary>
        /// 캡처 시점에 GameHitStop이 이미 이 actor를 freeze 중이면, 오염된 라이브값이 아니라
        /// 그 핸들러가 보관한 진짜 original을 신뢰한다. 아무도 관리 중이 아니면 라이브값이 곧 진실.
        /// </summary>
        private static float ResolveTrueOriginalScale(GameActor actor)
        {
            if (actor == null)
                return 1f;

            var hitStop = GameCombatManager.Instance?.GameHitStop;
            if (hitStop != null && hitStop.TryGetActorOriginalScale(actor, out var trueOriginal))
                return trueOriginal;

            return actor.LocalTimeScale;
        }

        /// <summary>
        /// actor가 현재 이 핸들러의 freeze로 눌려 있으면 그 진짜 original을 반환한다.
        /// GameHitStop이 freeze 중간값을 original로 오인 캡처하는 것을 막기 위한 교차 조회용.
        /// freeze 단계 종료 시 _frozenPlayer/_frozenAttacker는 null로 비워지므로,
        /// 그 이후엔 라이브값이 곧 진실이 되어 폴백이 정확하다.
        /// </summary>
        public bool TryGetFrozenOriginalScale(GameActor actor, out float original)
        {
            if (actor != null && actor == _frozenPlayer)
            {
                original = _originalPlayerScale;
                return true;
            }
            if (actor != null && actor == _frozenAttacker)
            {
                original = _originalAttackerScale;
                return true;
            }
            original = 1f;
            return false;
        }

        public void StopForCounterAttack(GameActor counterActor)
        {
            if (counterActor == null || _frozenPlayer != counterActor)
                return;

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
