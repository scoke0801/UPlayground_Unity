using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Config;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.CameraSystem
{
    public readonly struct CombatCameraAttackContext
    {
        public readonly Transform Attacker;
        public readonly Transform Victim;
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;

        public CombatCameraAttackContext(
            Transform attacker,
            Transform victim,
            Vector3 hitPoint,
            Vector3 attackDirection,
            AttackKind attackKind,
            AttackReactionType reactionType)
        {
            Attacker = attacker;
            Victim = victim;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
            AttackKind = attackKind;
            ReactionType = reactionType;
        }
    }

    /// <summary>
    /// 전투 결과를 카메라 의도로 변환하고 CameraManager API 호출을 한곳에 모은다.
    /// P1 단계에서는 기존 PlayerAttackHitFeedbackProfile을 재사용해 회귀 범위를 줄인다.
    /// </summary>
    public sealed class CombatCameraEventRouter
    {
        private readonly CameraManager _cameraManager;
        private CombatCameraProfileDatabaseSO _profileDatabase;
        private FOVCameraEffectData _perfectGuardFovData;

        public CombatCameraEventRouter(CameraManager cameraManager)
        {
            _cameraManager = cameraManager;
        }

        public void SetProfileDatabase(CombatCameraProfileDatabaseSO profileDatabase)
        {
            _profileDatabase = profileDatabase;
        }

        public void SetPerfectGuardFovData(FOVCameraEffectData data)
        {
            _perfectGuardFovData = data;
        }

        public void Play(in CombatCameraIntent intent)
        {
            if (_cameraManager == null)
                return;

            CombatCameraProfileSO profile = _profileDatabase != null
                ? _profileDatabase.GetProfile(intent.Type, intent.Attacker, intent.Victim)
                : null;

            if (profile != null)
            {
                if (!RollTriggerChance(profile))
                    return;

                PlayProfile(intent, profile);
                return;
            }

            PlayFallback(intent);
        }

        public bool TryPlayKill(Transform victim)
        {
            if (_cameraManager == null)
                return false;

            CombatCameraIntent intent = new CombatCameraIntent(
                CombatCameraIntentType.Kill,
                _cameraManager.GetTarget(),
                victim,
                victim != null ? victim.position : Vector3.zero,
                Vector3.zero,
                AttackKind.FinishAttack,
                AttackReactionType.Knockdown,
                CameraShakeIdType.KillCam);

            CombatCameraProfileSO profile = _profileDatabase != null
                ? _profileDatabase.GetProfile(CombatCameraIntentType.Kill, intent.Attacker, intent.Victim)
                : null;

            if (profile != null && HasPlayableProfile(profile))
            {
                bool usesSnapshotSequence = ProfileUsesSnapshotSequence(profile);
                if (!usesSnapshotSequence && !_cameraManager.CanStartKillCamWithoutChance(victim))
                    return false;

                if (!RollTriggerChance(profile))
                    return false;

                PlayProfile(intent, profile);
                if (usesSnapshotSequence)
                    return true;

                return _cameraManager.TryKillCamWithoutChance(victim);
            }

            return _cameraManager.TryKillCam(victim);
        }

        public void PlayPlayerDamaged(bool isHeavyReaction, CameraShakeIdType lightShakeKey, CameraShakeIdType heavyShakeKey)
        {
            Play(new CombatCameraIntent(
                isHeavyReaction ? CombatCameraIntentType.PlayerHeavyDamaged : CombatCameraIntentType.PlayerDamaged,
                null,
                _cameraManager != null ? _cameraManager.GetTarget() : null,
                Vector3.zero,
                Vector3.zero,
                AttackKind.NormalAttack,
                isHeavyReaction ? AttackReactionType.Heavy : AttackReactionType.Hit,
                isHeavyReaction ? heavyShakeKey : lightShakeKey));
        }

        public void PlayPlayerDeath(CameraShakeIdType deathShakeKey)
        {
            Play(new CombatCameraIntent(
                CombatCameraIntentType.PlayerDeath,
                null,
                _cameraManager != null ? _cameraManager.GetTarget() : null,
                Vector3.zero,
                Vector3.zero,
                AttackKind.NormalAttack,
                AttackReactionType.Knockdown,
                deathShakeKey));
        }

        public void PlayPerfectGuard(in CombatCameraAttackContext incomingAttack, CameraShakeIdType shakeKey)
        {
            Play(CreateDefenseIntent(
                CombatCameraIntentType.PerfectGuard,
                incomingAttack,
                shakeKey,
                0.15f,
                0.2f));
        }

        public void PlayPerfectDodge(in CombatCameraAttackContext incomingAttack, CameraShakeIdType shakeKey)
        {
            Play(CreateDefenseIntent(
                CombatCameraIntentType.PerfectDodge,
                incomingAttack,
                shakeKey,
                0.06f,
                0.1f));
        }

        public void PlayDodgeCounter(Transform target, CameraShakeIdType shakeKey)
        {
            Transform playerTarget = _cameraManager != null ? _cameraManager.GetTarget() : null;
            Vector3 hitDirection = Vector3.zero;
            if (playerTarget != null && target != null)
            {
                hitDirection = target.position - playerTarget.position;
                hitDirection.y = 0f;
            }

            Play(new CombatCameraIntent(
                CombatCameraIntentType.DodgeCounter,
                playerTarget,
                target,
                target != null ? target.position : Vector3.zero,
                hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.zero,
                AttackKind.NormalAttack,
                AttackReactionType.Hit,
                shakeKey,
                0.12f,
                0.12f));
        }

        public void PlayPlayerAttackHit(
            in CombatCameraAttackContext attackData,
            in PlayerAttackHitFeedbackProfile profile)
        {
            CombatCameraIntent intent = CreatePlayerAttackHitIntent(attackData, profile);
            Play(intent);
        }

        public static CombatCameraIntent CreatePlayerAttackHitIntent(
            in CombatCameraAttackContext attackData,
            in PlayerAttackHitFeedbackProfile profile)
        {
            AttackKind kind = attackData.AttackKind;
            CombatCameraIntentType intentType = ResolveIntentType(kind);

            CameraShakeIdType shakeKey = intentType is CombatCameraIntentType.SkillHit
                                         or CombatCameraIntentType.ChargeHit
                                         or CombatCameraIntentType.HeavyHit
                                         or CombatCameraIntentType.DashHit
                ? profile.ShakeKeyHeavy
                : profile.ShakeKeyLight;

            float punchStrength;
            float punchDuration;
            switch (intentType)
            {
                case CombatCameraIntentType.SkillHit:
                case CombatCameraIntentType.ChargeHit:
                    punchStrength = profile.PunchStrengthSkill;
                    punchDuration = profile.PunchDurationSkill;
                    break;
                case CombatCameraIntentType.HeavyHit:
                case CombatCameraIntentType.DashHit:
                    punchStrength = profile.PunchStrengthHeavy;
                    punchDuration = profile.PunchDurationHeavy;
                    break;
                default:
                    punchStrength = profile.PunchStrengthLight;
                    punchDuration = profile.PunchDurationLight;
                    break;
            }

            return new CombatCameraIntent(
                intentType,
                attackData.Attacker,
                attackData.Victim,
                attackData.HitPoint,
                attackData.AttackDirection,
                kind,
                attackData.ReactionType,
                shakeKey,
                punchStrength,
                punchDuration);
        }

        private CombatCameraIntent CreateDefenseIntent(
            CombatCameraIntentType intentType,
            in CombatCameraAttackContext incomingAttack,
            CameraShakeIdType shakeKey,
            float punchStrength,
            float punchDuration)
        {
            Vector3 attackDirection = incomingAttack.AttackDirection;
            Transform playerTarget = _cameraManager != null ? _cameraManager.GetTarget() : null;

            return new CombatCameraIntent(
                intentType,
                incomingAttack.Attacker,
                playerTarget,
                incomingAttack.HitPoint,
                attackDirection.sqrMagnitude > 0.0001f ? -attackDirection.normalized : Vector3.zero,
                incomingAttack.AttackKind,
                incomingAttack.ReactionType,
                shakeKey,
                punchStrength,
                punchDuration);
        }

        private static CombatCameraIntentType ResolveIntentType(AttackKind kind)
        {
            return kind switch
            {
                AttackKind.ChargeAttack => CombatCameraIntentType.ChargeHit,
                AttackKind.SkillAttack => CombatCameraIntentType.SkillHit,
                AttackKind.HeavyAttack => CombatCameraIntentType.HeavyHit,
                AttackKind.DashAttack => CombatCameraIntentType.DashHit,
                AttackKind.JumpAttack => CombatCameraIntentType.DashHit,
                _ => CombatCameraIntentType.LightHit
            };
        }

        public void PlaySoftTargetAssist(
            Transform target,
            float yawStrength,
            float maxAngle,
            float duration = 0.12f,
            float manualInputSuppressDuration = 0.35f)
        {
            if (!CanPlaySoftTargetAssist(target, maxAngle, manualInputSuppressDuration, out float fullTargetYaw))
                return;

            // 보정의 "양"을 강도×접근성스케일로 결정한다. duration은 고정 스무딩 시간.
            // (과거에는 duration에 스케일을 곱해 100% 타겟 yaw로 스냅 → "확확 따라가는" 원인이었다)
            float strength = Mathf.Clamp01(yawStrength) * GetAutoCorrectionScale();
            if (strength <= 0f)
                return;

            float currentYaw = _cameraManager.GetCurrentYaw();
            float blendedYaw = currentYaw + Mathf.DeltaAngle(currentYaw, fullTargetYaw) * strength;
            _cameraManager.SetRotationSmooth(blendedYaw, _cameraManager.GetCurrentPitch(), duration);
        }

        private bool CanPlaySoftTargetAssist(
            Transform target,
            float maxAngle,
            float manualInputSuppressDuration,
            out float fullTargetYaw)
        {
            fullTargetYaw = 0f;

            if (_cameraManager == null || target == null)
                return false;

            // 하드락은 명시적 플레이어 선택이므로 soft target assist가 개입하지 않는다.
            if (_cameraManager.IsLockOnActive())
                return false;

            if (GetAutoCorrectionScale() <= 0f)
                return false;

            if (_cameraManager.TimeSinceLastManualCameraInput < manualInputSuppressDuration)
                return false;

            Transform playerTarget = _cameraManager.GetTarget();
            if (playerTarget == null)
                return false;

            Vector3 toTarget = target.position - playerTarget.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
                return false;

            fullTargetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            // 앵글 게이트(핵심 가드): 카메라 정면에서 maxAngle 이내의 적만 보정한다.
            // 옆/뒤 적을 때려도 카메라가 강제로 확 돌아가지 않게 막는다.
            if (maxAngle > 0f &&
                Mathf.Abs(Mathf.DeltaAngle(_cameraManager.GetCurrentYaw(), fullTargetYaw)) > maxAngle)
                return false;

            return true;
        }

        private void PlayFallback(in CombatCameraIntent intent)
        {
            if (intent.Type == CombatCameraIntentType.PerfectGuard && _perfectGuardFovData != null)
                _cameraManager.PlayEffect(_perfectGuardFovData);

            float shakeScale = GetShakeScale();
            if (intent.PunchStrength > 0f && intent.HitDirection.sqrMagnitude > 0.0001f)
                _cameraManager.Punch(
                    intent.HitDirection,
                    intent.PunchStrength * shakeScale,
                    intent.PunchDuration);

            if (shakeScale > 0f && intent.ShakeKey != CameraShakeIdType.None)
                _cameraManager.StartShake(
                    intent.ShakeKey,
                    intent.HitDirection,
                    shakeScale * GetCadenceScale(intent.Type),
                    intent.HitPoint);
        }

        private void PlayProfile(in CombatCameraIntent intent, CombatCameraProfileSO profile)
        {
            float sequenceScale = GetSequenceIntensity();

            if (profile.effects != null && sequenceScale > 0f)
            {
                foreach (CameraEffectData effect in profile.effects)
                {
                    if (effect != null)
                        _cameraManager.PlayEffect(effect);
                }
            }

            float shakeScale = GetShakeScale();
            if (profile.usePunch && shakeScale > 0f && intent.HitDirection.sqrMagnitude > 0.0001f)
                _cameraManager.Punch(
                    intent.HitDirection,
                    profile.punchStrength * shakeScale,
                    profile.punchDuration);

            if (shakeScale > 0f && profile.shakeKey != CameraShakeIdType.None)
                _cameraManager.StartShake(
                    profile.shakeKey,
                    intent.HitDirection,
                    shakeScale * GetCadenceScale(intent.Type),
                    intent.HitPoint);

            if (profile.useSnapshotSequence && profile.snapshotProfile != null && sequenceScale > 0f)
                _cameraManager.PushCameraSnapshotSequence(profile.snapshotProfile);

            if (profile.enableSoftTargetAssist && intent.Victim != null)
                PlaySoftTargetAssist(
                    intent.Victim,
                    profile.softTargetYawStrength,
                    profile.softTargetMaxAngle,
                    profile.softTargetYawDuration,
                    profile.manualInputSuppressDuration);
        }

        private static bool HasPlayableProfile(CombatCameraProfileSO profile)
        {
            if (profile == null)
                return false;

            bool hasEffects = profile.effects != null && profile.effects.Exists(e => e != null);
            return hasEffects
                   || profile.shakeKey != CameraShakeIdType.None
                   || profile.usePunch
                   || (profile.useSnapshotSequence && profile.snapshotProfile != null)
                   || profile.enableSoftTargetAssist;
        }

        private static bool ProfileUsesSnapshotSequence(CombatCameraProfileSO profile)
        {
            return profile != null && profile.useSnapshotSequence && profile.snapshotProfile != null;
        }

        private static bool RollTriggerChance(CombatCameraProfileSO profile)
        {
            return profile == null || profile.triggerChance >= 1f || Random.value <= profile.triggerChance;
        }

        private static SettingsData GetSettingsData()
        {
            ISettingsService settings = Svc.Settings;
            return settings != null && settings.IsLoaded ? settings.Data : null;
        }

        /// <summary>
        /// 카덴스(Tier 3-G): 타입별 막타 강조. 가산형 보이스의 콤보 누적 위에 얹어
        /// 스킬/마무리/처치를 더 도드라지게 한다. (콤보 인덱스 기반 곡선은 후속 과제)
        /// </summary>
        private static float GetCadenceScale(CombatCameraIntentType type)
        {
            return type switch
            {
                CombatCameraIntentType.Kill        => 1.15f,
                CombatCameraIntentType.SkillHit     => 1.15f,
                CombatCameraIntentType.ChargeHit    => 1.15f,
                CombatCameraIntentType.HeavyHit     => 1.05f,
                CombatCameraIntentType.DashHit      => 1.05f,
                CombatCameraIntentType.PerfectGuard => 1.05f,
                CombatCameraIntentType.DodgeCounter => 1.05f,
                _                                   => 1.0f,
            };
        }

        private float GetShakeScale()
        {
            SettingsData data = GetSettingsData();
            if (data != null && !data.screenShake)
                return 0f;

            float settingsScale = _cameraManager != null ? _cameraManager.SettingsCombatCameraShakeScale : 1f;
            return Mathf.Max(0f, settingsScale * (data != null ? data.cameraShakeScale : 1f));
        }

        private float GetAutoCorrectionScale()
        {
            SettingsData data = GetSettingsData();
            if (data != null && !data.aimAssist)
                return 0f;

            float settingsScale = _cameraManager != null ? _cameraManager.SettingsCombatCameraAutoCorrectionScale : 1f;
            return Mathf.Clamp01(settingsScale * (data != null ? data.combatCameraAutoCorrection : 1f));
        }

        private float GetSequenceIntensity()
        {
            SettingsData data = GetSettingsData();
            float settingsScale = _cameraManager != null ? _cameraManager.SettingsCombatCameraSequenceIntensity : 1f;
            return Mathf.Clamp01(settingsScale * (data != null ? data.combatCameraSequenceIntensity : 1f));
        }
    }
}
