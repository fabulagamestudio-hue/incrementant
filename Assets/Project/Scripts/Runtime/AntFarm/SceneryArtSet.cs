using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Sprites de cada elemento isolado do cenário da superfície (um PNG por variante, pivô no ponto de apoio no chão).
    /// Preenchido pelo Editor a partir das pastas em Assets/Project/Art/Scenery; elemento sem sprites usa a arte procedural.
    /// </summary>
    [CreateAssetMenu(menuName = "Incrementant/Scenery Art Set", fileName = "SceneryArtSet")]
    public sealed class SceneryArtSet : ScriptableObject
    {
        [SerializeField] Sprite[] _grass = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _tufts = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _flowers = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _clovers = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _mushrooms = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _twigs = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _dryLeaves = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _pebbles = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] _grains = System.Array.Empty<Sprite>();

        public Sprite[] Get(SceneryElement element) => element switch
        {
            SceneryElement.Grass => _grass,
            SceneryElement.Tuft => _tufts,
            SceneryElement.Flower => _flowers,
            SceneryElement.Clover => _clovers,
            SceneryElement.Mushroom => _mushrooms,
            SceneryElement.Twig => _twigs,
            SceneryElement.DryLeaf => _dryLeaves,
            SceneryElement.Pebble => _pebbles,
            SceneryElement.Grain => _grains,
            _ => System.Array.Empty<Sprite>(),
        };

        public void Set(SceneryElement element, Sprite[] sprites)
        {
            switch (element)
            {
                case SceneryElement.Grass: _grass = sprites; break;
                case SceneryElement.Tuft: _tufts = sprites; break;
                case SceneryElement.Flower: _flowers = sprites; break;
                case SceneryElement.Clover: _clovers = sprites; break;
                case SceneryElement.Mushroom: _mushrooms = sprites; break;
                case SceneryElement.Twig: _twigs = sprites; break;
                case SceneryElement.DryLeaf: _dryLeaves = sprites; break;
                case SceneryElement.Pebble: _pebbles = sprites; break;
                case SceneryElement.Grain: _grains = sprites; break;
            }
        }

        /// <summary>Converte os sprites em carimbos. Sprites sem Read/Write são pulados com erro no console.</summary>
        public SceneryStampLibrary CreateLibrary()
        {
            var library = new SceneryStampLibrary();
            foreach (SceneryElement element in System.Enum.GetValues(typeof(SceneryElement)))
            {
                var stamps = new System.Collections.Generic.List<SceneryStamp>();
                foreach (Sprite sprite in Get(element) ?? System.Array.Empty<Sprite>())
                {
                    if (sprite == null)
                        continue;
                    SceneryStamp stamp = SceneryStamp.FromSprite(sprite);
                    if (stamp == null)
                        Debug.LogError($"{name}: o sprite '{sprite.name}' precisa de Read/Write ligado no import (PNGs em Art/Scenery recebem isso sozinhos; reimporte).", sprite);
                    else
                        stamps.Add(stamp);
                }
                library.Set(element, stamps.ToArray());
            }
            return library;
        }
    }
}
