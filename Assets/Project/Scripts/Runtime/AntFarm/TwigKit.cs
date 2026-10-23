using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Kit do graveto (piloto de 2026-10-23, experimento 8x8): as peças desenhadas a 8 texels por célula e as faixas das
    /// medidas sorteadas. Cada graveto é montado por <see cref="TwigKitBuilder"/> no tamanho do mundo do graveto antigo, em
    /// vez de ser um PNG inteiro. Peças em <c>Art/SceneryKits/Twig</c> (Read/Write, Point; ver <c>SceneryKitImporter</c>).
    /// </summary>
    [CreateAssetMenu(menuName = "Incrementant/Kit do graveto", fileName = "TwigKit")]
    public sealed class TwigKit : ScriptableObject
    {
        [Header("Peças (8 texels por célula)")]
        [SerializeField] Texture2D _bark;
        [SerializeField] Texture2D _cutEnd;
        [SerializeField] Texture2D _brokenEnd;
        [SerializeField] Texture2D _branchTip;
        [SerializeField] Texture2D[] _knots = System.Array.Empty<Texture2D>();

        [Header("Medidas, em células (as do graveto antigo: 198 a 364 de comprimento, 54 a 66 de altura)")]
        [SerializeField] Vector2 _length = new Vector2(200f, 360f);
        [Tooltip("Quanto a ponta fina sobe ou desce em relação à grossa.")]
        [SerializeField] Vector2 _rise = new Vector2(-2f, 3f);
        [Tooltip("Barriga do tronco no meio.")]
        [SerializeField] Vector2 _bow = new Vector2(-1.5f, 2.5f);
        [SerializeField] Vector2 _branchAt = new Vector2(0.3f, 0.7f);
        [SerializeField] Vector2 _branchLength = new Vector2(34f, 46f);
        [Tooltip("Graus a partir da horizontal.")]
        [SerializeField] Vector2 _branchAngle = new Vector2(55f, 72f);
        [SerializeField] Vector2Int _knotCount = new Vector2Int(1, 3);

        TwigPieces _pieces;

        /// <summary>Medidas sorteadas pela <paramref name="seed"/> (a mesma seed dá o mesmo graveto).</summary>
        public TwigShape RandomShape(int seed)
        {
            // Embaralha a seed: o System.Random dá primeiros valores quase iguais para seeds próximas (seed do mapa + índice).
            uint mixed = (uint)seed * 2654435761u;
            mixed ^= mixed >> 15;
            var rng = new System.Random((int)(mixed * 2246822519u));
            float Range(Vector2 r) => Mathf.Lerp(r.x, r.y, (float)rng.NextDouble());
            return new TwigShape
            {
                Length = Range(_length),
                Rise = Range(_rise),
                Bow = Range(_bow),
                BranchAt = Range(_branchAt),
                BranchLength = Range(_branchLength),
                BranchAngle = Range(_branchAngle),
                BranchTowardTip = rng.NextDouble() < 0.5,
                Knots = rng.Next(_knotCount.x, _knotCount.y + 1),
                Mirror = rng.NextDouble() < 0.5,
                Seed = rng.Next(),
            };
        }

        /// <summary>Graveto montado; null (com erro no console) se faltar peça ou se alguma não for legível.</summary>
        public PixelImage Build(int seed)
        {
            TwigPieces pieces = Pieces();
            return pieces != null ? TwigKitBuilder.Build(pieces, RandomShape(seed)) : null;
        }

        TwigPieces Pieces()
        {
            if (_pieces != null)
                return _pieces;
            var pieces = new TwigPieces
            {
                Bark = PixelImage.From(_bark),
                CutEnd = PixelImage.From(_cutEnd),
                BrokenEnd = PixelImage.From(_brokenEnd),
                BranchTip = PixelImage.From(_branchTip),
                Knots = new PixelImage[_knots.Length],
            };
            for (int i = 0; i < _knots.Length; i++)
                pieces.Knots[i] = PixelImage.From(_knots[i]);
            if (pieces.Bark == null || pieces.CutEnd == null || pieces.BrokenEnd == null || pieces.BranchTip == null
                || System.Array.Exists(pieces.Knots, k => k == null))
            {
                Debug.LogError($"{name}: falta peça no kit do graveto ou alguma não tem Read/Write. Rode Incrementant/Setup/Criar kit do graveto.", this);
                return null;
            }
            return _pieces = pieces;
        }

        void OnValidate() => _pieces = null;
    }
}
