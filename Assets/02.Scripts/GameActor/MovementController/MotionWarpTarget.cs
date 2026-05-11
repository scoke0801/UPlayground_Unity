using UnityEngine;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// 워프 타겟 오프셋 해석 공간.
    /// </summary>
    public enum WarpTargetSpace
    {
        World,
        AnchorLocal,
        AnchorForward,
    }

    /// <summary>
    /// 워프 타겟의 단일 데이터 모델.
    /// 기존 _target / _targetPosition / _useSnapshot / targetOffset 파편을 흡수한다.
    /// </summary>
    public struct MotionWarpTarget
    {
        public Transform anchor;
        public Vector3 offset;
        public WarpTargetSpace space;
        // true: Live 추적, false: BeginWarpWindow 시점 위치를 스냅샷으로 고정.
        public bool follow;

        public bool IsValid => anchor != null;

        public Vector3 ResolveWorldPosition()
        {
            if (anchor == null) return offset;
            return space switch
            {
                WarpTargetSpace.AnchorLocal   => anchor.TransformPoint(offset),
                WarpTargetSpace.AnchorForward => anchor.position
                                                 + anchor.right   * offset.x
                                                 + anchor.up      * offset.y
                                                 + anchor.forward * offset.z,
                _                             => anchor.position + offset,
            };
        }

        public static MotionWarpTarget None => default;

        public static MotionWarpTarget WorldFollow(Transform anchor, Vector3 offset = default)
            => new MotionWarpTarget
            {
                anchor = anchor,
                offset = offset,
                space  = WarpTargetSpace.World,
                follow = true,
            };

        public static MotionWarpTarget WorldSnapshot(Transform anchor, Vector3 offset = default)
            => new MotionWarpTarget
            {
                anchor = anchor,
                offset = offset,
                space  = WarpTargetSpace.World,
                follow = false,
            };
    }
}
