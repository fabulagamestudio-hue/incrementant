using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Desenho da tela Evolução (documento 48): a posição de cada nó e, por ligação (requisito → nó), os encontros por onde
    /// o túnel passa. A primeira versão sai do <see cref="UpgradeTreeGenerator"/>; o desenvolvedor redesenha na janela
    /// <c>Incrementant/Árvore/Editor de desenho</c>. Fixado (<c>Pinned</c>) = o gerador não mexe mais. Não entra no save: muda
    /// o desenho, não o progresso. O setup do HUD lê este asset ao montar a tela (<see cref="UpgradeLayout"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Incrementant/Desenho da árvore de evolução", fileName = "UpgradeTreeDrawing")]
    public sealed class UpgradeTreeDrawing : ScriptableObject
    {
        public const string AssetPath = "Assets/Project/Data/UpgradeTreeDrawing.asset";

        [Serializable]
        public sealed class Node
        {
            public string Id;
            public Vector2 Position;
            public bool Pinned;
        }

        /// <summary>Encontro de túneis: um salão pequeno onde vários túneis se cruzam, ou só uma dobra de um túnel.</summary>
        [Serializable]
        public sealed class Junction
        {
            public string Id;
            public Vector2 Position;
            public bool Pinned;
        }

        /// <summary>Túnel de um requisito (<see cref="From"/>) ao nó (<see cref="To"/>), passando pelos encontros de <see cref="Via"/>.</summary>
        [Serializable]
        public sealed class Tunnel
        {
            public string From;
            public string To;
            public List<string> Via = new List<string>();
            public bool Pinned;
        }

        [SerializeField] List<Node> _nodes = new List<Node>();
        [SerializeField] List<Junction> _junctions = new List<Junction>();
        [SerializeField] List<Tunnel> _tunnels = new List<Tunnel>();

        public List<Node> Nodes => _nodes;
        public List<Junction> Junctions => _junctions;
        public List<Tunnel> Tunnels => _tunnels;

        public Node FindNode(string id) => _nodes.Find(n => n.Id == id);
        public Junction FindJunction(string id) => _junctions.Find(j => j.Id == id);
        public Tunnel FindTunnel(string from, string to) => _tunnels.Find(t => t.From == from && t.To == to);

        /// <summary>Id livre para um encontro desenhado à mão ("J1", "J2"…).</summary>
        public string NewJunctionId()
        {
            for (int i = 1; ; i++)
            {
                string id = "J" + i;
                if (FindJunction(id) == null)
                    return id;
            }
        }
    }
}
