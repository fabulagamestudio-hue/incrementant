using System;
using Fabula.Incrementant.Colony;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Números das câmaras e galerias do catálogo que os sistemas da colônia consultam (documento 39), calculados de uma vez
    /// a partir de <see cref="UpgradeLevels"/> em <see cref="Recompute"/> (ao comprar e ao carregar o save). Sem nenhum nível
    /// todos valem o neutro (1 para fatores, 0 para somas), então os sistemas leem direto, sem condicional. "Tempo" é fator
    /// sobre uma duração (menor = mais rápido); "Rate" é fator sobre uma velocidade.
    /// </summary>
    public sealed class UpgradeEffects
    {
        UpgradeLevels _levels = new UpgradeLevels();

        // ---- movimento
        public float WorkerSpeedRate, CarrierSpeedRate;
        /// <summary>L+3: carregadora com carga na mão. S+4: soldado em batalha (KI-038).</summary>
        public float CarrierLoadedSpeedRate, SoldierBattleSpeedRate;
        /// <summary>Fora do formigueiro (X0, X+2) e nos túneis batidos (T2).</summary>
        public float OutdoorSpeedRate, TrailSpeedRate;

        // ---- escavar, cortar e carregar
        public float WorkerCutTime, CutterCutTime, DiggerDigTime, AllDigTime, RockBreakTime;
        public int CarrierLoadBonus, DiggerSoilBonus;
        /// <summary>E3: a escavadora tira duas células por carga.</summary>
        public bool WideFront;

        // ---- cultivo
        /// <summary>Tempo do canteiro (C1, C+1, C+3); <see cref="GardenerBedTime"/> vale a mais com jardineira cuidando (J+1).</summary>
        public float BedTime, GardenerBedTime;
        /// <summary>Nutrientes por planta (C3) e valor de cada entrega (C+2).</summary>
        public float PlantYield, DeliveryValue;
        /// <summary>X+1: plantas de fora crescem mais rápido. X1: plantas cortadas perto da entrada voltam.</summary>
        public float OutdoorGrowthRate;

        // ---- berçário e rainha
        public float LarvaTime, PupaTime, NurseryCellsFactor, NurseryCapacityFactor, AdultSlotCellsFactor;
        public float LayTime, BroodCostFactor, QueenHealthFactor, VitalityFactor;
        public float CarePauseFactor;
        public int NurseCapacityBonus;

        // ---- orvalho, depósito e resíduos
        public float DewDropTime, GardenerDewRate, DewYield;
        public int DewRoomWaterBonus;
        public float StorageCapacityFactor, WasteTime;
        /// <summary>R2: fator sobre o tempo de a terra virar areia e água na área de resíduos.</summary>
        public float DirtWaterTime;

        // ---- obra
        public int DumpOpeningsBonus;

        /// <summary>Q3: a rainha põe ovos mesmo fora da sala dela.</summary>
        public bool QueenLaysAnywhere;

        /// <summary>M2: no aviso de ataque os soldados se dividem em grupos nas entradas e só saem quando um cupim chega perto (KI-030).</summary>
        public bool SoldierPosting;

        // ---- defesa
        public float WarningBonusSeconds, WallBreakTime,  SoldierDamageFactor, SoldierHealthFactor, SoldierHealTime;

        /// <summary>Células a mais que o pincel pode pintar por ambiente (Q+4, C+4, B+3, R+1, G+3 e D+3 para cada depósito; KI-043).</summary>
        public int QueenRoomCellsBonus, NutrientRoomCellsBonus, NurseryRoomCellsBonus, WasteRoomCellsBonus, DewRoomCellsBonus, DepotCellsBonus;

        /// <summary>T+3: salas não conectadas a mais em cada ambiente (KI-043).</summary>
        public int PaintRoomsBonus;

        // ---- fazenda de pulgões
        /// <summary>A0: a fazenda existe (pincel liberado e a fazendeira pode ser comprada).</summary>
        public bool FarmUnlocked;
        /// <summary>A+1: pulgões a mais no limite da fazenda. A+3: células a mais da fazenda.</summary>
        public int AphidCapBonus, FarmRoomCellsBonus;
        /// <summary>A1: fator sobre o tempo entre um pulgão novo e o seguinte. A+2: fator sobre o tempo da ordenha.</summary>
        public float AphidBreedTime, MilkTime;
        /// <summary>Z+1: velocidade da fazendeira.</summary>
        public float FarmerSpeedRate;

        // ---- minérios, forja e equipamento (REQ-071)
        /// <summary>V0, V1, Y0, Y1, Y2: libera o pincel do depósito de minério, da fundição, da forja e dos dois armazéns.</summary>
        public bool OreStoreUnlocked, SmelterUnlocked, ForgeUnlocked, ArmoryUnlocked, WardrobeUnlocked;
        /// <summary>V+1 e Y+1: fator sobre o tempo da barra e do equipamento. Y+2: fator sobre a durabilidade do equipamento novo.</summary>
        public float SmeltTime, ForgeTime, EquipDurability;
        /// <summary>E+4: fator sobre o tempo de cavar de quem tem ferramenta. S+5: fator sobre o dano de quem tem arma.
        /// O+4: fator sobre o dano recebido de quem tem armadura.</summary>
        public float SharpToolTime, WeaponDamageBoost, ArmorTakenBoost;

        public int PaintCellsBonus(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return QueenRoomCellsBonus;
                case PaintRoom.Nutrient: return NutrientRoomCellsBonus;
                case PaintRoom.Nursery: return NurseryRoomCellsBonus;
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt:
                case PaintRoom.WasteNutrient: return WasteRoomCellsBonus;
                case PaintRoom.Dew: return DewRoomCellsBonus;
                case PaintRoom.Farm: return FarmRoomCellsBonus;
                default: return DepotCellsBonus;
            }
        }

        public UpgradeLevels Levels => _levels;

        public UpgradeEffects()
        {
            Recompute(_levels);
        }

        public bool Owns(string id) => _levels.Level(id) > 0;

        float M(string id) => _levels.Multiplier(id);

        /// <summary>Soma que a galeria aditiva dá acima do valor base (L+1: +1 por nível).</summary>
        float Extra(string id) => UpgradeCatalog.ValueAt(UpgradeCatalog.Get(id), _levels.Level(id)) - UpgradeCatalog.Get(id).Base;

        public void Recompute(UpgradeLevels levels)
        {
            _levels = levels;
            bool x0 = Owns("X0"), t2 = Owns("T2");

            WorkerSpeedRate = M("O+1");
            CarrierSpeedRate = M("L+2");
            CarrierLoadedSpeedRate = M("L+3");
            SoldierBattleSpeedRate = M("S+4");
            OutdoorSpeedRate = (x0 ? 1.1f : 1f) * M("X+2");
            TrailSpeedRate = t2 ? 1.1f : 1f;

            WorkerCutTime = M("O+3");
            CutterCutTime = M("F+1");
            DiggerDigTime = M("E+1");
            AllDigTime = M("T+2");
            RockBreakTime = M("E+3");
            CarrierLoadBonus = (int)Math.Round(Extra("L+1"));
            DiggerSoilBonus = (int)Math.Round(Extra("E+2"));
            WideFront = Owns("E3");

            BedTime = (Owns("C1") ? 1f / 1.3f : 1f) / M("C+1") * M("C+3");
            GardenerBedTime = 1f / M("J+1");
            PlantYield = Owns("C3") ? 1.2f : 1f;
            DeliveryValue = M("C+2");
            OutdoorGrowthRate = M("X+1");

            LarvaTime = (Owns("B1") ? 0.8f : 1f) * M("B+1");
            PupaTime = (Owns("B2") ? 0.8f : 1f) * M("B+1");
            NurseryCellsFactor = M("B+2");
            NurseryCapacityFactor = Owns("B3") ? 1.5f : 1f;
            AdultSlotCellsFactor = M("H+1");
            LayTime = M("Q+1");
            BroodCostFactor = Owns("N3") ? Math.Max(0.6f, M("Q+2") - 0.1f) : M("Q+2");
            QueenHealthFactor = M("Q+3");
            VitalityFactor = M("O+2");
            CarePauseFactor = M("N+1");
            NurseCapacityBonus = (int)Math.Round(Extra("N+2"));

            DewDropTime = M("G+1");
            GardenerDewRate = M("J+2");
            DewYield = Owns("G1") ? 2f : 1f;
            DewRoomWaterBonus = (int)Math.Round(Extra("G+2"));
            StorageCapacityFactor = (Owns("D1") ? 1.5f : 1f) * M("D+1");
            WasteTime = Owns("R1") ? 0.5f : 1f;
            DirtWaterTime = Owns("R2") ? 0.5f : 1f;

            DumpOpeningsBonus = Owns("T1") ? 2 : 0;
            QueenLaysAnywhere = Owns("Q3");

            WarningBonusSeconds = (Owns("M0") ? 4f : 0f) + Extra("M+2");
            WallBreakTime = M("M+1");
            SoldierDamageFactor = M("S+1");
            SoldierHealthFactor = M("S+2");
            SoldierHealTime = M("S+3");
            SoldierPosting = Owns("M2");

            QueenRoomCellsBonus = (int)Math.Round(Extra("Q+4"));
            NutrientRoomCellsBonus = (int)Math.Round(Extra("C+4"));
            NurseryRoomCellsBonus = (int)Math.Round(Extra("B+3"));
            WasteRoomCellsBonus = (int)Math.Round(Extra("R+1"));
            DewRoomCellsBonus = (int)Math.Round(Extra("G+3"));
            DepotCellsBonus = (int)Math.Round(Extra("D+3"));
            PaintRoomsBonus = (int)Math.Round(Extra("T+3"));

            FarmUnlocked = Owns("A0");
            AphidCapBonus = (int)Math.Round(Extra("A+1"));
            FarmRoomCellsBonus = (int)Math.Round(Extra("A+3"));
            AphidBreedTime = Owns("A1") ? 0.5f : 1f;
            MilkTime = M("A+2");
            FarmerSpeedRate = M("Z+1");

            OreStoreUnlocked = Owns("V0");
            SmelterUnlocked = Owns("V1");
            ForgeUnlocked = Owns("Y0");
            ArmoryUnlocked = Owns("Y1");
            WardrobeUnlocked = Owns("Y2");
            SmeltTime = M("V+1");
            ForgeTime = M("Y+1");
            EquipDurability = M("Y+2");
            SharpToolTime = M("E+4");
            WeaponDamageBoost = M("S+5");
            ArmorTakenBoost = M("O+4");
        }
    }
}
