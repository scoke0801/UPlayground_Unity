using UnityEngine;

namespace UPlayGround.Data.Pet
{
    /// <summary>아이템 탐색·예약·이탈 허용 범위. 획득 상태는 공용 드롭이 소유한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Loot Profile")]
    public sealed class PetLootProfileSO : ScriptableObject
    {
        [Min(0.05f)] public float searchInterval = 1f;
        [Min(0.1f)] public float searchRadius = 8f;
        [Min(0.1f)] public float maxPlayerDistance = 10f;
        [Min(0.05f)] public float arrivalDistance = 0.7f;
        [Min(0.1f)] public float reservationSeconds = 10f;
        [Min(0.1f)] public float retryDelay = 5f;
    }
}
