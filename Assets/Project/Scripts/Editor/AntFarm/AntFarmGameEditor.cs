using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.World;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Inspector do AntFarmGame: mantém o preview da cena coerente com o modo do mapa
    /// (imagem → o PNG aparece na cena; procedural → sem preview, o mapa só existe no Play).
    /// </summary>
    [CustomEditor(typeof(AntFarmGame))]
    public sealed class AntFarmGameEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var game = (AntFarmGame)target;
            EditorGUILayout.HelpBox(
                game.Source == MapSource.Procedural
                    ? "Modo Procedural: um mapa novo é gerado a cada Play (Randomize Seed) ou sempre o mesmo da Seed. Ninho e despejo vêm do gerador; os marcadores são ignorados."
                    : "Modo Image: o mapa é o PNG em Map (visível na cena) e ninho/despejo são os objetos Nest, DumpStart e DumpEnd.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                SyncPreview(game);
        }

        /// <summary>Atualiza o sprite de preview do WorldView e o enquadramento da câmera conforme o modo.</summary>
        public static void SyncPreview(AntFarmGame game)
        {
            if (game.View == null || !game.View.TryGetComponent(out SpriteRenderer spriteRenderer))
                return;

            Texture2D map = game.Source == MapSource.Image ? game.Map : null;
            Sprite sprite = map != null ? AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(map)) : null;
            if (spriteRenderer.sprite != sprite)
            {
                Undo.RecordObject(spriteRenderer, "Preview do mapa");
                spriteRenderer.sprite = sprite;
            }

            var serialized = new SerializedObject(game);
            var camera = serialized.FindProperty("_camera").objectReferenceValue as Camera;
            if (camera == null)
                return;
            // Procedural: a câmera começa na área inicial, no alto e ao centro do mundo expansível.
            RectInt area = map != null
                ? new RectInt(0, 0, map.width, map.height)
                : (serialized.FindProperty("_terrainSettings").boxedValue as TerrainSettings ?? new TerrainSettings()).StartArea;
            Undo.RecordObject(camera.transform, "Enquadrar mapa");
            Undo.RecordObject(camera, "Enquadrar mapa");
            AntFarmGame.FrameCamera(camera, game.View.transform.position + new Vector3(area.x, area.y), area.width, area.height, 4f);
        }
    }
}
