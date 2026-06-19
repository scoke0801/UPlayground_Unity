using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.UI;

namespace UPlayGround.Component
{
    /// <summary>
    /// 궁극기 중 획득한 잠금을 기록하고 자신이 변경한 상태만 복구한다.
    /// 중단/비활성화/오류 경로가 모두 같은 Release를 사용하도록 한다.
    /// </summary>
    public sealed class UltimateGameplayLockContext
    {
        private static readonly UIKeyType[] HudKeys =
        {
            UIKeyType.HudPlayerInfo,
            UIKeyType.Minimap,
            UIKeyType.HudParty,
            UIKeyType.HudQuest,
            UIKeyType.HudSkill,
            UIKeyType.OffscreenThreatIndicator
        };

        private readonly List<IEnemyAIController> _frozenControllers = new();
        private readonly List<MonsterActor> _reactionSuppressedTargets = new();
        private readonly List<UIKeyType> _hiddenHudKeys = new();

        private PlayerActor _caster;
        private bool _previousInputSuppressed;
        private bool _previousCasterInvincible;
        private bool _previousCameraInputLocked;
        private bool _ownsInputLock;
        private bool _ownsCasterInvincibility;
        private bool _ownsCameraInputLock;
        private bool _isAcquired;

        public void Acquire(
            PlayerActor caster,
            PlayerCombat combat,
            UltimateRuntimeContext runtimeContext,
            UltimateGameplayLockSettings settings)
        {
            if (_isAcquired || caster == null || settings == null)
                return;

            _caster = caster;
            _isAcquired = true;

            if (settings.lockPlayerInput)
            {
                _previousInputSuppressed = caster.IsInputSuppressed;
                if (!_previousInputSuppressed)
                {
                    caster.SetInputSuppressed(true);
                    _ownsInputLock = true;
                }
            }

            if (settings.lockCameraInput && CameraManager.Instance != null)
            {
                _previousCameraInputLocked = CameraManager.Instance.IsInputLocked();
                CameraManager.Instance.SetInputLock(true);
                _ownsCameraInputLock = !_previousCameraInputLocked;
            }

            if (settings.releaseLockOnOnEnter)
                CameraManager.Instance?.ReleaseLockOn();

            if (settings.ignoreCasterDamage)
            {
                _previousCasterInvincible = caster.IsInvincible;
                if (!_previousCasterInvincible)
                {
                    caster.SetInvincible(true);
                    _ownsCasterInvincibility = true;
                }
            }

            if (settings.pauseEnemyAI && combat != null && settings.enemyFreezeRadius > 0f)
            {
                var candidates = new List<IEnemyAIController>();
                combat.FillEnemyAIControllersInRadius(settings.enemyFreezeRadius, candidates);
                foreach (IEnemyAIController controller in candidates)
                {
                    if (controller is not Behaviour behaviour
                        || behaviour == null
                        || !behaviour.enabled)
                    {
                        continue;
                    }

                    controller.Freeze();
                    _frozenControllers.Add(controller);
                }
            }

            if (runtimeContext != null)
            {
                foreach (Transform target in runtimeContext.Targets)
                {
                    MonsterActor monster = target != null
                        ? target.GetComponent<MonsterActor>()
                          ?? target.GetComponentInParent<MonsterActor>()
                        : null;
                    if (monster == null)
                        continue;

                    if (settings.freezeTargets
                        && monster.AIController is Behaviour targetBehaviour
                        && targetBehaviour.enabled
                        && !_frozenControllers.Contains(monster.AIController))
                    {
                        monster.AIController.Freeze();
                        _frozenControllers.Add(monster.AIController);
                    }

                    if (settings.ignoreTargetReactions)
                    {
                        monster.SetExternalHitReactionSuppressed(true);
                        _reactionSuppressedTargets.Add(monster);
                    }
                }
            }

            if (settings.hideHud)
                HideHud();
        }

        public void Release()
        {
            if (!_isAcquired)
                return;

            for (int i = _frozenControllers.Count - 1; i >= 0; i--)
            {
                IEnemyAIController controller = _frozenControllers[i];
                if (controller is UnityEngine.Object unityObject && unityObject != null)
                    controller.Unfreeze();
            }
            _frozenControllers.Clear();

            for (int i = _reactionSuppressedTargets.Count - 1; i >= 0; i--)
            {
                MonsterActor monster = _reactionSuppressedTargets[i];
                if (monster != null)
                    monster.SetExternalHitReactionSuppressed(false);
            }
            _reactionSuppressedTargets.Clear();

            RestoreHud();

            if (_ownsCasterInvincibility && _caster != null)
                _caster.SetInvincible(_previousCasterInvincible);

            if (_ownsCameraInputLock)
                CameraManager.Instance?.SetInputLock(_previousCameraInputLocked);

            if (_ownsInputLock && _caster != null)
                _caster.SetInputSuppressed(_previousInputSuppressed);

            _caster = null;
            _ownsInputLock = false;
            _ownsCasterInvincibility = false;
            _ownsCameraInputLock = false;
            _previousInputSuppressed = false;
            _previousCasterInvincible = false;
            _previousCameraInputLocked = false;
            _isAcquired = false;
        }

        private void HideHud()
        {
            UIManager uiManager = UIManager.Instance;
            if (uiManager == null)
                return;

            foreach (UIKeyType key in HudKeys)
            {
                GameObject active = uiManager.GetActiveUI(key);
                UI_Base ui = active != null ? active.GetComponent<UI_Base>() : null;
                if (ui == null || !ui.IsVisible)
                    continue;

                _hiddenHudKeys.Add(key);
                uiManager.HideUI(key);
            }
        }

        private void RestoreHud()
        {
            UIManager uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                foreach (UIKeyType key in _hiddenHudKeys)
                    uiManager.ShowUI(key);
            }

            _hiddenHudKeys.Clear();
        }
    }
}
