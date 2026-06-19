using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    [CreateAssetMenu(fileName = "PoiseData", menuName = "UPlayGround/적/Poise")]
    public class PoiseSO : ScriptableObject
    {
        [Tooltip("최대 Poise. 높을수록 경직이 잘 안 됨")]
        public float maxPoise = 100f;

        [Tooltip("Poise Break 후 회복까지 대기 시간(초)")]
        public float recoveryDelay = 2f;

        [Tooltip("초당 Poise 회복량")]
        public float recoveryRate = 40f;

        [Tooltip("공격 모션 중 Hyper Armor 표시용 플래그. Poise 피해 자체는 차단하지 않음")]
        public bool hasHyperArmor = false;
    }
}
