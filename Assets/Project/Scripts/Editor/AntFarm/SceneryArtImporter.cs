using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// PNGs em <see cref="SceneryFolder"/> são elementos do cenário da superfície: sprite Point, sem compressão nem mipmap,
    /// Read/Write ligado (o cenário lê a máscara), 16 px/célula na arte refinada e 6 PPU na arte legada.
    /// O pivô é o ponto de apoio no chão: PNG novo começa no centro da base; ajuste no Sprite Editor se parte dele
    /// deve ficar enterrada. Adicionar, remover ou mover PNGs atualiza o catálogo (<see cref="SceneryArtExporter.RefreshArtSet"/>).
    /// </summary>
    public sealed class SceneryArtImporter : AssetPostprocessor
    {
        public const string SceneryFolder = "Assets/Project/Art/Scenery/";

        static bool _refreshQueued;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SceneryFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            bool isNew = importer.importSettingsMissing;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            bool detailed = width >= 64 && height >= 64 && width % 16 == 0 && height % 16 == 0;
            settings.spritePixelsPerUnit = detailed ? AntSpriteImporter.WorldPixelsPerCell : AntSpriteImporter.PixelsPerCell;
            // Durante a migração, os PNGs antigos conservam a importação anterior.
            if (detailed)
                importer.maxTextureSize = 2048;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            // Só no primeiro import: depois o pivô é do artista (ou do exportador).
            if (isNew)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }
            importer.SetTextureSettings(settings);
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
                                   .Any(p => p.StartsWith(SceneryFolder) && p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase));
            if (!touched || _refreshQueued || SceneryArtExporter.IsExporting)
                return;
            _refreshQueued = true;
            // Fora do callback de import, para poder criar/salvar o catálogo.
            EditorApplication.delayCall += () =>
            {
                _refreshQueued = false;
                SceneryArtExporter.RefreshArtSet();
            };
        }
    }
}
