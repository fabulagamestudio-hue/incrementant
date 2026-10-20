using System;

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

        // ---- obra
        public int DumpOpeningsBonus;

        /// <summary>Q3: a rainha põe ovos mesmo fora da sala dela.</summary>
        public bool QueenLaysAnywhere;

        // ---- defesa
        public float WarningBonusSeconds, WallBreakTime,  SoldierDamageFactor, SoldierHealthFactor, SoldierHealTime;

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

            DumpOpeningsBonus = Owns("T1") ? 2 : 0;
            QueenLaysAnywhere = Owns("Q3");

            WarningBonusSeconds = (Owns("M0") ? 4f : 0f) + Extra("M+2");
            WallBreakTime = M("M+1");
            SoldierDamageFactor = M("S+1");
            SoldierHealthFactor = M("S+2");
            SoldierHealTime = M("S+3");
        }
    }
}
