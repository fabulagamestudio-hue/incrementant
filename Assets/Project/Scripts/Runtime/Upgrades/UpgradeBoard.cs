using System.Collections.Generic;
using Fabula.Incrementant.Colony;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>Como um nó aparece na tela Evolução.</summary>
    public enum UpgradeStatus
    {
        /// <summary>Câmara comprada, nó inicial ou galeria com pelo menos um nível.</summary>
        Owned,
        /// <summary>Pode ser comprado agora (sistema existe, requisitos e saldo à parte).</summary>
        Available,
        /// <summary>Falta requisito, condição ou o nível ainda está fechado.</summary>
        Locked,
    }

    /// <summary>
    /// Estado dos nós do <see cref="UpgradeCatalog"/> lido a cada atualização da tela e usado pela compra (o mesmo estado,
    /// sem cópia: nada aqui vai para o save). Nós da rainha: comprados na <see cref="QueenTree"/>; câmaras e galerias com
    /// sistema: níveis em <see cref="UpgradeLevels"/>; pincéis livres contam como adquiridos; o resto aparece como EM BREVE.
    /// Todo nó segue a mesma regra para abrir (<see cref="Unlockable"/>: nível do formigueiro, condição e requisitos,
    /// documento 46); os da rainha somam a ela as regras da própria árvore.
    /// </summary>
    public sealed class UpgradeBoard
    {
        /// <summary>Câmaras e galerias que já têm sistema no jogo (o nível é gravado e o efeito é aplicado); as demais aparecem como EM BREVE.</summary>
        static readonly HashSet<string> LiveNodes = new HashSet<string> 
        {
            // Câmaras. B0 só destrava a fila (o berçário já é pintável desde o início, então não muda o jogo).
            "C1", "C3", "B0", "B1", "B2", "B3", "R1", "R2", "G1", "T1", "T2", "X0", "M0", "M2", "N3", "Q3",
            // Galerias. Fora: D+1, D+2 (depósito guarda 1 item por célula, sem estragar), T+1 (pintar não custa nada),
            // X+3 (as formigas já acham qualquer planta alcançável) e M+1 (os cupins não quebram paredes).
            "H+1", "C+1", "C+2", "C+3", "B+1", "B+2", "G+1", "G+2", "T+2", "X+1", "X+2", "M+2", "Q+1", "Q+2", "Q+3",
            "O+1", "O+2", "O+3", "L+1", "L+2", "L+3", "F+1", "E+1", "E+2", "E+3", "J+1", "J+2", "N+1", "N+2", "S+1", "S+2", "S+3", "S+4",
            // Área pintável dos ambientes (KI-043).
            "Q+4", "C+4", "B+3", "D+3", "R+1", "G+3", "T+3",
            // Fazenda de pulgões e fazendeira (2026-10-22).
            "A0", "A1", "A+1", "A+2", "A+3", "Z+1",
            // Minérios, forja e o equipamento forjado (REQ-071, documento 46, etapa 4).
            "V0", "V1", "V+1", "Y0", "Y1", "Y2", "Y+1", "Y+2", "O+4", "E+4", "S+5",
        };

        QueenTree _tree;
        UpgradeLevels _store;
        readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public int RaidsSurvived { get; private set; }
        public int Adults { get; private set; }
        /// <summary>Profundidade da colônia (abre os anéis): câmaras compradas + níveis + bônus das galerias completas.</summary>
        public int Depth { get; private set; }

        /// <param name="levels">Níveis das galerias que têm sistema (null: só os de <see cref="SetLevelForPreview"/>).</param>
        public void Refresh(QueenTree tree, int raidsSurvived, int adults, UpgradeLevels levels = null)
        {
            _tree = tree;
            _store = levels;
            RaidsSurvived = raidsSurvived;
            Adults = adults;
            int depth = 0;
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Kind == UpgradeKind.Chamber)
                {
                    if (!def.Initial && Owns(def))
                        depth += UpgradeCatalog.PointsPerChamber;
                }
                else
                {
                    int level = Level(def);
                    depth += level * UpgradeCatalog.PointsPerLevel;
                    if (def.Capped && level >= def.Cap)
                        depth += UpgradeCatalog.PointsPerCompletedGallery;
                }
            }
            Depth = depth;
        }

        /// <summary>Nível de uma galeria (0 enquanto as galerias não têm sistema). Só para testes e pré-visualização.</summary>
        public void SetLevelForPreview(string id, int level) => _levels[id] = level;

        public int Level(UpgradeDef def)
        {
            if (def.Kind != UpgradeKind.Gallery)
                return 0;
            if (_levels.TryGetValue(def.Id, out int preview))
                return preview;
            return _store != null ? _store.Level(def.Id) : 0;
        }

        public bool Owns(UpgradeDef def)
        {
            if (def.Kind == UpgradeKind.Gallery)
                return Level(def) > 0;
            if (def.QueenNode.HasValue)
                return _tree != null && _tree.Owns(def.QueenNode.Value);
            return def.Initial || _store != null && _store.Level(def.Id) > 0;
        }

        /// <summary>O jogo já tem o sistema que o nó muda (nós implementados da árvore da rainha e galerias em <see cref="LiveNodes"/>).</summary>
        public static bool HasSystem(UpgradeDef def) =>
            def.QueenNode.HasValue ? QueenTree.Def(def.QueenNode.Value).Implemented : LiveNodes.Contains(def.Id);

        /// <summary>Galeria com sistema e ainda abaixo do teto.</summary>
        public bool CanLevel(UpgradeDef def) =>
            def.Kind == UpgradeKind.Gallery && HasSystem(def) && (!def.Capped || Level(def) < def.Cap);

        /// <summary>Nível do formigueiro aberto pela profundidade (documento 46, "Níveis").</summary>
        public bool TierOpen(int tier) => Depth >= UpgradeCatalog.DepthToOpenTier(tier);

        public bool Meets(UpgradeRequirement requirement)
        {
            UpgradeDef target = UpgradeCatalog.Get(requirement.Id);
            return requirement.Level > 0 ? Level(target) >= requirement.Level : Owns(target);
        }

        public bool ConditionMet(UpgradeCondition condition)
        {
            switch (condition)
            {
                case UpgradeCondition.FirstRaid: return RaidsSurvived >= 1;
                case UpgradeCondition.FiveRaids: return RaidsSurvived >= 5;
                case UpgradeCondition.FiftyAdults: return Adults >= 50;
                default: return true;
            }
        }

        public UpgradeStatus Status(UpgradeDef def)
        {
            if (def.Kind == UpgradeKind.Chamber && Owns(def))
                return UpgradeStatus.Owned;
            if (def.QueenNode.HasValue)
                return _tree != null && _tree.Check(def.QueenNode.Value) == BuyBlock.None && Unlockable(def)
                    ? UpgradeStatus.Available : UpgradeStatus.Locked;
            if (def.Kind == UpgradeKind.Gallery && Level(def) > 0)
                return UpgradeStatus.Owned;
            return (CanLevel(def) || def.Kind == UpgradeKind.Chamber && HasSystem(def)) && Unlockable(def)
                ? UpgradeStatus.Available : UpgradeStatus.Locked;
        }

        /// <summary>Requisitos e condição atendidos e nível aberto (vale para o que ainda não tem sistema).</summary>
        public bool Unlockable(UpgradeDef def)
        {
            if (!TierOpen(def.Tier) || !ConditionMet(def.Condition))
                return false;
            foreach (UpgradeRequirement requirement in def.Requires)
                if (!Meets(requirement))
                    return false;
            return true;
        }
    }
}
