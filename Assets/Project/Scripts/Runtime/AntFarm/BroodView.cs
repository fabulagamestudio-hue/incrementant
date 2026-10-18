using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Desenha ovos, larvas e pupas. Só visual: o ovo recém-posto fica no chão onde a rainha o pôs
    /// (<see cref="BroodRecord.LaidX"/>/<see cref="BroodRecord.LaidY"/>) até uma formiga pegá-lo (aí quem o mostra é a
    /// <see cref="AntSwarmView"/>, na boca dela); no berçário, cada cria fica no ponto de
    /// <see cref="AntColony.NurserySpots"/> indicado por <see cref="BroodRecord.Slot"/>. Dois quadros parados alternam.
    /// Os sprites seguem a arte das formigas (18x12, pivô a meia célula acima do chão), então a célula do ponto é a do chão
    /// e o sprite pisa na borda da célula sólida abaixo dela.
    /// </summary>
    public sealed class BroodView : MonoBehaviour
    {
        [SerializeField] Sprite[] _eggFrames;
        [SerializeField] Sprite[] _larvaFrames;
        [SerializeField] Sprite[] _pupaFrames;
        [Tooltip("Segundos de cada quadro parado.")]
        [SerializeField, Min(0.1f)] float _frameSeconds = 0.9f;
        [Tooltip("Atrás das formigas (AntSwarmView usa 10).")]
        [SerializeField] int _sortingOrder = 8;

        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

        public bool HasFrames => Has(_eggFrames) && Has(_larvaFrames) && Has(_pupaFrames);

        static bool Has(Sprite[] frames) => frames != null && frames.Length > 0 && frames[0] != null;

        /// <param name="origin">Posição do mundo da célula (0, 0), a mesma usada pelas formigas.</param>
        public void Render(IReadOnlyList<BroodRecord> brood, IReadOnlyList<Vector2Int> spots, Vector3 origin)
        {
            while (_renderers.Count < brood.Count)
                _renderers.Add(CreateRenderer(_renderers.Count));

            for (int i = 0; i < _renderers.Count; i++)
            {
                SpriteRenderer r = _renderers[i];
                if (i >= brood.Count || !TryCellOf(brood[i], spots, out Vector2Int cell))
                {
                    if (r.gameObject.activeSelf)
                        r.gameObject.SetActive(false);
                    continue;
                }
                BroodRecord b = brood[i];
                if (!r.gameObject.activeSelf)
                    r.gameObject.SetActive(true);

                // Defasa cada cria para não piscarem juntas; as de id ímpar ficam viradas para o outro lado.
                r.sprite = FrameOf(b);
                r.flipX = (b.Id & 1) == 1;
                r.transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            }
        }

        /// <summary>Quadro atual da cria (etapa e defasagem dela), o mesmo no berçário e na boca da formiga que a carrega.</summary>
        public Sprite FrameOf(in BroodRecord b)
        {
            Sprite[] frames = FramesOf(b.Stage);
            return frames[(int)(Time.time / _frameSeconds + b.Id * 0.37f) % frames.Length];
        }

        /// <summary>Onde desenhar a cria; falso se não há o que mostrar (ovo na boca de uma formiga, ou berçário sem pontos).</summary>
        static bool TryCellOf(BroodRecord b, IReadOnlyList<Vector2Int> spots, out Vector2Int cell)
        {
            switch (b.Phase)
            {
                case EggPhase.Laid:
                case EggPhase.Claimed:
                    cell = new Vector2Int(b.LaidX, b.LaidY);
                    return true;
                case EggPhase.Placed:
                    // Mais crias que pontos (berçário recortado depois de a cria chegar): repete os pontos desde o primeiro.
                    cell = spots.Count > 0 && b.Slot >= 0 ? spots[b.Slot % spots.Count] : default;
                    return spots.Count > 0 && b.Slot >= 0;
                default: // Carried e Moving: a cria está na boca de uma formiga
                    cell = default;
                    return false;
            }
        }

        Sprite[] FramesOf(BroodStage stage)
        {
            switch (stage)
            {
                case BroodStage.Egg: return _eggFrames;
                case BroodStage.Larva: return _larvaFrames;
                default: return _pupaFrames;
            }
        }

        SpriteRenderer CreateRenderer(int index)
        {
            var go = new GameObject($"Brood_{index:000}");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sortingOrder = _sortingOrder;
            return r;
        }
    }
}
