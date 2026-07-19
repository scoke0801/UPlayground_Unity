using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.UI
{
    public static class UICombatElementDisplay
    {
        public static string Label(CombatElement element) => element switch
        {
            CombatElement.Fire => "불",
            CombatElement.Water => "물",
            CombatElement.Nature => "자연",
            CombatElement.Light => "빛",
            CombatElement.Dark => "어둠",
            _ => "무속성",
        };

        public static Color Color(CombatElement element) => element switch
        {
            CombatElement.Fire => new Color32(0xFF, 0x68, 0x42, 0xFF),
            CombatElement.Water => new Color32(0x46, 0xA8, 0xFF, 0xFF),
            CombatElement.Nature => new Color32(0x58, 0xD6, 0x78, 0xFF),
            CombatElement.Light => new Color32(0xFF, 0xDC, 0x72, 0xFF),
            CombatElement.Dark => new Color32(0xB0, 0x76, 0xFF, 0xFF),
            _ => new Color32(0xC2, 0xC8, 0xD0, 0xFF),
        };

        // 구분자로 가운뎃점(U+00B7)을 쓰면 TMP 기본 폰트 아틀라스에 해당 글리프가 없어
        // 두부(□)로 표시된다. 아틀라스에 항상 포함되는 ASCII 콜론으로 대체한다.
        public static string RichLabel(CombatElement element) =>
            $"속성: {Label(element)}";
    }
}
