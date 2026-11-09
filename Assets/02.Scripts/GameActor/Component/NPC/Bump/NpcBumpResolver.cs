using UnityEngine;
using UPlayGround.Data.Actor;
using Motions = UPlayGround.Data.Actor.Animation.MotionTags;

namespace UPlayGround.Components
{
    /// <summary>충돌을 받아줄 수 있는 NPC의 현재 상황. 상황이 반응의 상한을 정한다.</summary>
    public enum NpcBumpContext
    {
        /// <summary>대기·배회 중. 모든 단계의 반응을 허용한다.</summary>
        Free,
        /// <summary>고정 작업 중. 자리를 지키므로 상체 기울기(Lean)까지만 허용한다.</summary>
        Working,
        /// <summary>대화·전투 반응 등 방해하면 안 되는 상태. 반응하지 않는다.</summary>
        Locked,
    }

    /// <summary>플레이어 충돌의 부위·강도·밀림 방향을 정하는 순수 판정. 컴포넌트와 상태가 같은 규칙을 쓴다.</summary>
    public static class NpcBumpResolver
    {
        /// <summary>속도와 NPC 상황으로 반응 단계를 정한다.</summary>
        public static NpcBumpTier ResolveTier(NpcBumpProfileSO profile, float speed, NpcBumpContext context)
        {
            if (profile == null || context == NpcBumpContext.Locked || speed < profile.minimumSpeed)
                return NpcBumpTier.None;

            NpcBumpTier tier = speed >= profile.shoveSpeed ? NpcBumpTier.Shove
                : speed >= profile.staggerSpeed ? NpcBumpTier.Stagger
                : NpcBumpTier.Lean;
            return context == NpcBumpContext.Working && tier > NpcBumpTier.Lean ? NpcBumpTier.Lean : tier;
        }

        /// <summary>NPC 로컬 좌표(+x 오른쪽, +z 정면)의 접촉점으로 닿은 부위를 정한다.</summary>
        public static NpcBumpPart ResolvePart(NpcBumpProfileSO profile, Vector3 localPoint, float actorHeight)
        {
            float heightRatio = actorHeight > 0f ? localPoint.y / actorHeight : 1f;
            if (heightRatio < profile.hipHeightRatio) return NpcBumpPart.Hip;
            if (localPoint.x <= -profile.shoulderOffset) return NpcBumpPart.LeftShoulder;
            if (localPoint.x >= profile.shoulderOffset) return NpcBumpPart.RightShoulder;
            return localPoint.z >= 0f ? NpcBumpPart.Chest : NpcBumpPart.Back;
        }

        /// <summary>닿은 부위를 피격 모션 방향으로 옮긴다. 골반은 앞쪽 반응을 쓴다.</summary>
        public static NpcBumpMotionDirection ResolveMotionDirection(NpcBumpPart part) => part switch
        {
            NpcBumpPart.Back => NpcBumpMotionDirection.Back,
            NpcBumpPart.LeftShoulder => NpcBumpMotionDirection.Left,
            NpcBumpPart.RightShoulder => NpcBumpMotionDirection.Right,
            _ => NpcBumpMotionDirection.Front,
        };

        /// <summary>단계와 방향에 맞는 전용 모션 슬롯. 모션 없이 상체 연출만 쓰는 Lean 이하는 default를 돌려준다.</summary>
        public static UPlayGround.Gameplay.Tag.GameplayTag ResolveMotionTag(NpcBumpTier tier, NpcBumpMotionDirection direction)
        {
            if (tier < NpcBumpTier.Stagger) return default;
            bool isShove = tier == NpcBumpTier.Shove;
            return direction switch
            {
                NpcBumpMotionDirection.Back => isShove ? Motions.NpcBump_Shove_B : Motions.NpcBump_Stagger_B,
                NpcBumpMotionDirection.Left => isShove ? Motions.NpcBump_Shove_L : Motions.NpcBump_Stagger_L,
                NpcBumpMotionDirection.Right => isShove ? Motions.NpcBump_Shove_R : Motions.NpcBump_Stagger_R,
                _ => isShove ? Motions.NpcBump_Shove_F : Motions.NpcBump_Stagger_F,
            };
        }

        /// <summary>단계별 밀림 거리(m). Lean 이하는 제자리에서 버틴다.</summary>
        public static float GetPushDistance(NpcBumpProfileSO profile, NpcBumpTier tier) => tier switch
        {
            NpcBumpTier.Stagger => profile.staggerDistance,
            NpcBumpTier.Shove => profile.shoveDistance,
            _ => 0f,
        };

        /// <summary>KCC 지수 감쇠 넉백이 목표 거리에서 멈추도록 초기 속도를 구한다(거리 = 속도 / 감쇠).</summary>
        public static float GetPushSpeed(NpcBumpProfileSO profile, NpcBumpTier tier)
            => GetPushDistance(profile, tier) * profile.pushDrag;

        /// <summary>
        /// 상체 반응의 시간 곡선(0~1). 빠르게 최대치까지 오른 뒤 회복 시간에 걸쳐 0으로 돌아온다.
        /// 상태를 들고 있지 않는 닫힌 식이라 프레임 간 누적 오차가 없다.
        /// </summary>
        public static float EvaluateEnvelope(NpcBumpProfileSO profile, float elapsed)
        {
            if (elapsed <= 0f || elapsed >= profile.recoverSeconds) return 0f;
            if (elapsed < profile.attackSeconds)
                return Mathf.SmoothStep(0f, 1f, elapsed / profile.attackSeconds);
            float decay = (elapsed - profile.attackSeconds) / (profile.recoverSeconds - profile.attackSeconds);
            return 1f - Mathf.SmoothStep(0f, 1f, decay);
        }

        /// <summary>플레이어의 진행 방향을 수평으로 투영해 밀림 방향을 정한다. 속도가 없으면 접촉 법선의 반대로 민다.</summary>
        public static Vector3 ResolvePushDirection(Vector3 playerVelocity, Vector3 contactNormal)
        {
            Vector3 direction = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.ProjectOnPlane(-contactNormal, Vector3.up);
            return direction.sqrMagnitude < 0.0001f ? Vector3.zero : direction.normalized;
        }

        /// <summary>
        /// 상체 기울기 각도(도). 단계 배율과 부위 가중을 곱한다.
        /// 접촉이 있던 쪽의 어깨는 비틀림이 커지므로 별도 값으로 돌려준다.
        /// </summary>
        public static (float lean, float twist) ResolveUpperBodyAngles(
            NpcBumpProfileSO profile, NpcBumpTier tier, NpcBumpPart part)
        {
            float scale = profile.baseLeanDegrees * profile.GetTierAngleScale(tier);
            var response = profile.GetPartResponse(part);
            return (scale * response.leanWeight, scale * response.twistWeight);
        }
    }
}
