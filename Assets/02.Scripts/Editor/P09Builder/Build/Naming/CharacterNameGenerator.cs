namespace UPlayGround.Editor.P09Builder
{
    public static class CharacterNameGenerator
    {
        public static string Generate(CharacterBuildConfig cfg, NameSequenceRegistry registry)
        {
            if (cfg == null) return "Unnamed";

            if (cfg.UseManualName && !string.IsNullOrWhiteSpace(cfg.ManualName))
                return cfg.ManualName.Trim();

            var key = BuildNameKey(cfg);
            int seq = registry != null ? registry.NextSequence(key) : 1;

            return $"{key}_{seq:000}";
        }

        /// <summary>
        /// 카운터를 증가시키지 않고 다음에 생성될 이름을 미리 보여준다.
        /// 미리보기 UI 전용.
        /// </summary>
        public static string Preview(CharacterBuildConfig cfg, NameSequenceRegistry registry)
        {
            if (cfg == null) return "Unnamed";

            if (cfg.UseManualName && !string.IsNullOrWhiteSpace(cfg.ManualName))
                return cfg.ManualName.Trim();

            var key = BuildNameKey(cfg);
            int seq = registry != null ? registry.Peek(key) : 1;

            return $"{key}_{seq:000}";
        }

        public static string GetKindFolderName(BuilderActorKind kind)
        {
            switch (kind)
            {
                case BuilderActorKind.Enemy:  return "Enemy";
                case BuilderActorKind.Player: return "Player";
                case BuilderActorKind.Npc:    return "Npc";
                default: return "Misc";
            }
        }

        private static string BuildNameKey(CharacterBuildConfig cfg)
        {
            var typePrefix = GetTypePrefix(cfg);
            var appearance = GetAppearanceSource(cfg);
            var gender = cfg.Sex == BuilderSex.Male ? "M" : "F";
            var weaponType = GetWeaponType(cfg);

            return string.IsNullOrEmpty(appearance)
                ? $"{typePrefix}_{gender}_{weaponType}"
                : $"{typePrefix}_{appearance}_{gender}_{weaponType}";
        }

        private static string GetTypePrefix(CharacterBuildConfig cfg)
        {
            switch (cfg.ActorKind)
            {
                case BuilderActorKind.Enemy:
                    var grade = cfg.Stats != null ? cfg.Stats.grade.ToString() : "Normal";
                    return grade == "Normal" ? "Enemy" : $"Enemy_{grade}";
                case BuilderActorKind.Player:
                    return cfg.PlayerCharacterType != UPlayGround.Data.EnumType.CharacterActorType.None
                        ? $"Player_{cfg.PlayerCharacterType}"
                        : "Player";
                case BuilderActorKind.Npc:
                    return "Npc";
                default: return "ACT";
            }
        }

        private static string GetAppearanceSource(CharacterBuildConfig cfg)
        {
            return cfg != null && cfg.IsRandomAppearance ? "Random" : string.Empty;
        }

        private static string GetWeaponType(CharacterBuildConfig cfg)
        {
            if (cfg == null) return "Unarmed";

            if (cfg.UseWeaponGroup && cfg.WeaponGroupSo != null)
                return "WeaponGroup";

            if (cfg.SwordSo != null)
            {
                if (cfg.SubSwordSo != null) return "DualSword";
                if (cfg.ShieldSo != null) return "SwordShield";
                return "Sword";
            }

            if (cfg.GreatSwordSo != null) return "GreatSword";
            if (cfg.BowSo != null) return "Bow";
            if (cfg.StaffSo != null) return "Staff";
            if (cfg.SpearSo != null) return "Spear";
            if (cfg.DualAxeSo != null) return "DualAxe";
            if (cfg.WhipSo != null) return "Whip";
            if (cfg.ShieldSo != null) return "Shield";
            if (cfg.SubSwordSo != null) return "SubSword";

            return "Unarmed";
        }
    }
}
