using UnityEngine;

namespace UPlayGround.Data.UI
{
    [CreateAssetMenu(fileName = "FirstTimeGuideConfig", menuName = "UPlayGround/UI/First Time Guide Config")]
    public sealed class FirstTimeGuideConfigSO : ScriptableObject
    {
        [SerializeField] private GuidePopupDataSO _combatGuide;
        [SerializeField] private GuidePopupDataSO _companionGuide;
        [SerializeField] private GuidePopupDataSO _equipmentGuide;

        public GuidePopupDataSO CombatGuide => _combatGuide;
        public GuidePopupDataSO CompanionGuide => _companionGuide;
        public GuidePopupDataSO EquipmentGuide => _equipmentGuide;
    }
}
