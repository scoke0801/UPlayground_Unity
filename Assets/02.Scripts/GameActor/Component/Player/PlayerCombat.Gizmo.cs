using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using UPlayGround.Debugging;

namespace UPlayGround.Components
{
    public partial class PlayerCombat : PlayerActorComponent, UPlayGround.Combat.ICombatCollisionExecutor, IDebugGizmoProvider
    {
        #region Debug Gizmo

        public DebugGizmoCategory Category => DebugGizmoCategory.Combat;
        public DebugGizmoContentType ContentType => DebugGizmoContentType.PlayerCombatHit;
        public UnityEngine.Object Owner => this;
        public bool IsAvailable => this != null && isActiveAndEnabled && _showHitDebug;

        public void CollectSnapshot(DebugGizmoFrameSnapshot snapshot)
        {
            if (_currentAttackData == null)
                return;

            snapshot.texts.Add(new DebugGizmoTextEntry
            {
                owner = this,
                category = Category,
                position = transform.position,
                    text = $"attack={_currentAttackData.MotionId} reach={_homingReachRange:F2} angle={_homingReachAngle:F0}",
            });
        }

        public void DrawGizmos(DebugGizmoDrawContext context)
        {
            if (_currentAttackData == null)
                return;

            DrawHitGizmos();

            context.DrawLabel(
                transform.position + Vector3.up * 1.35f,
                    $"Combat: {_currentAttackData.MotionId}\ngroup={_hitboxSet?.ActiveGroupId ?? "-"} phase={_currentAttackData.hitPhaseIndex}");
        }

        #endregion
    }
}
