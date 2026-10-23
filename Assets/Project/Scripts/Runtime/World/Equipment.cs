namespace Fabula.Incrementant.World
{
    /// <summary>Tipo de equipamento que a forja faz (documento 45).</summary>
    public enum EquipKind : byte
    {
        None = 0,
        /// <summary>Ferramenta: a escavadora e a operária comum cavam mais rápido.</summary>
        Tool = 1,
        /// <summary>Arma: soldado e cuspideira dão mais dano.</summary>
        Weapon = 2,
        /// <summary>Armadura (roupa): soldado e cuspideira recebem menos dano.</summary>
        Armor = 3,
    }

    /// <summary>
    /// Metais, barras e equipamentos (documento 45, REQ-071). O metal é o nível: 0 cobre, 1 ferro, 2 ouro. Os números são
    /// suposições do agente, a aprovar.
    /// </summary>
    public static class Equipment
    {
        public const int Tiers = 3;

        /// <summary>Tempo de cavar da formiga com a ferramenta, por metal.</summary>
        static readonly float[] ToolTime = { 0.8f, 0.65f, 0.5f };
        /// <summary>Dano da formiga com a arma, por metal.</summary>
        static readonly float[] WeaponDamage = { 1.3f, 1.6f, 2.0f };
        /// <summary>Dano que a formiga recebe com a armadura, por metal.</summary>
        static readonly float[] ArmorTaken = { 0.8f, 0.65f, 0.5f };
        /// <summary>Usos até gastar: células cavadas (ferramenta), golpes dados (arma), golpes recebidos (armadura).</summary>
        static readonly int[] ToolUses = { 200, 400, 800 };
        static readonly int[] WeaponUses = { 40, 80, 160 };
        static readonly int[] ArmorUses = { 30, 60, 120 };

        /// <summary>Metal do minério, da barra ou do equipamento (0 cobre, 1 ferro, 2 ouro); -1 se não é de metal.</summary>
        public static int TierOf(CellMaterial material)
        {
            int m = (int)material;
            if (m >= (int)CellMaterial.CopperOre && m <= (int)CellMaterial.GoldArmor)
                return (m - (int)CellMaterial.CopperOre) % Tiers;
            return -1;
        }

        public static bool IsBar(CellMaterial material) => material >= CellMaterial.CopperBar && material <= CellMaterial.GoldBar;

        public static EquipKind KindOf(CellMaterial material)
        {
            if (material >= CellMaterial.CopperTool && material <= CellMaterial.GoldTool) return EquipKind.Tool;
            if (material >= CellMaterial.CopperWeapon && material <= CellMaterial.GoldWeapon) return EquipKind.Weapon;
            if (material >= CellMaterial.CopperArmor && material <= CellMaterial.GoldArmor) return EquipKind.Armor;
            return EquipKind.None;
        }

        public static bool IsItem(CellMaterial material) => KindOf(material) != EquipKind.None;

        /// <summary>Barra ou equipamento: o que sai da fundição e da forja e nunca vira terra.</summary>
        public static bool IsMetalGood(CellMaterial material) => IsBar(material) || IsItem(material);

        public static CellMaterial BarOf(int tier) => (CellMaterial)((int)CellMaterial.CopperBar + tier);

        public static CellMaterial ItemOf(EquipKind kind, int tier)
        {
            switch (kind)
            {
                case EquipKind.Tool: return (CellMaterial)((int)CellMaterial.CopperTool + tier);
                case EquipKind.Weapon: return (CellMaterial)((int)CellMaterial.CopperWeapon + tier);
                case EquipKind.Armor: return (CellMaterial)((int)CellMaterial.CopperArmor + tier);
                default: return CellMaterial.Empty;
            }
        }

        public static float ToolTimeFactor(int tier) => tier >= 0 ? ToolTime[tier] : 1f;
        public static float WeaponDamageFactor(int tier) => tier >= 0 ? WeaponDamage[tier] : 1f;
        public static float ArmorTakenFactor(int tier) => tier >= 0 ? ArmorTaken[tier] : 1f;

        public static int Uses(EquipKind kind, int tier)
        {
            switch (kind)
            {
                case EquipKind.Tool: return ToolUses[tier];
                case EquipKind.Weapon: return WeaponUses[tier];
                case EquipKind.Armor: return ArmorUses[tier];
                default: return 0;
            }
        }
    }
}
