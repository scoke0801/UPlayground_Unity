// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-04-11 09:39
namespace UPlayGround.Data.Path
{
    /// <summary>CameraShakeIdType — CameraShake 키 열거형 (자동 생성)</summary>
    public enum CameraShakeIdType
    {
        None = 0,
        CriticalHit = 1,
        Explosion = 2,
        HeavyHit = 3,
        KillCam = 4,
        LiteHit = 5,
        PoiseBreak = 6,
        MediumHit = 7,
        PlayerHit_Heavy = 8,
        PlayerHit_Light = 9,
        PlayerHit = 10,
        PlayerHeavyHit = 11,
        PlayerDeath = 12,
    }

    public static class CameraShakeIdTypeExtensions
    {
        /// <summary>enum 값을 CameraShake 키 문자열로 변환한다.</summary>
        public static string ToKey(this CameraShakeIdType type) => type switch
        {
            CameraShakeIdType.CriticalHit => "CriticalHit",
            CameraShakeIdType.Explosion => "Explosion",
            CameraShakeIdType.HeavyHit => "HeavyHit",
            CameraShakeIdType.KillCam => "KillCam",
            CameraShakeIdType.LiteHit => "LiteHit",
            CameraShakeIdType.PoiseBreak => "PoiseBreak",
            CameraShakeIdType.MediumHit => "MediumHit",
            CameraShakeIdType.PlayerHit_Heavy => "PlayerHit_Heavy",
            CameraShakeIdType.PlayerHit_Light => "PlayerHit_Light",
            CameraShakeIdType.PlayerHit => "PlayerHit",
            CameraShakeIdType.PlayerHeavyHit => "PlayerHeavyHit",
            CameraShakeIdType.PlayerDeath => "PlayerDeath",
            _ => string.Empty,
        };
    }
}
