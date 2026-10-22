using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Fabula.Incrementant.AntFarm;

namespace Fabula.Incrementant.EditorTools.Setup
{
    [InitializeOnLoad]
    public static class ValidateRefinedSceneryTemporary
    {
        [Serializable] public sealed class Item { public string path; public int width; public int height; public float pivotX; public float pivotY; public string guid; }
        [Serializable] public sealed class Request { public string nonce; public Item[] items; public bool tests; }
        [Serializable] public sealed class Report { public string nonce; public bool success; public int sprites; public long pixels; public string[] passedTests; public string[] errors; }
        static readonly string Folder = Path.Combine(Path.GetTempPath(), "incrementant-scenery-refinement-20261021");
        static string _last;
        static double _next;
        static ValidateRefinedSceneryTemporary() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _next = EditorApplication.timeSinceStartup + 2;
            string path = Path.Combine(Folder, "unity-request.json");
            if (!File.Exists(path)) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); } catch { return; }
            if (request == null || string.IsNullOrEmpty(request.nonce) || request.nonce == _last) return;
            _last = request.nonce;
            var errors = new List<string>();
            var passed = new List<string>();
            var report = new Report { nonce = request.nonce };
            try
            {
                var palette = new HashSet<int>();
                foreach (string line in File.ReadLines("Assets/Project/Art/Palette/MasterPalette.hex"))
                {
                    string hex = line.Trim().TrimStart('#');
                    if (!Regex.IsMatch(hex, "^[0-9A-Fa-f]{6}$")) continue;
                    palette.Add(Convert.ToInt32(hex,16));
                    if (palette.Count == 32) break;
                }
                foreach (Item item in request.items ?? Array.Empty<Item>())
                {
                    AssetDatabase.ImportAsset(item.path,ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(item.path) as TextureImporter;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.path);
                    if (sprite == null || importer == null) { errors.Add(item.path + ": sprite ausente"); continue; }
                    report.sprites++;
                    if (sprite.rect.width != item.width || sprite.rect.height != item.height || sprite.pixelsPerUnit != 16f) errors.Add(item.path + ": dimensoes/PPU");
                    if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || !importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed) errors.Add(item.path + ": importacao");
                    if (Mathf.Abs(sprite.pivot.x-item.pivotX*item.width) > 0.05f || Mathf.Abs(sprite.pivot.y-item.pivotY*item.height) > 0.05f) errors.Add(item.path + ": pivo");
                    if (AssetDatabase.AssetPathToGUID(item.path) != item.guid) errors.Add(item.path + ": GUID");
                    var stamp = SceneryStamp.FromSprite(sprite);
                    if (stamp == null || stamp.VisualSprite != sprite || stamp.Width != item.width*6/16 || stamp.Height != item.height*6/16) errors.Add(item.path + ": mascara/escala");
                    Color32[] colors = sprite.texture.GetPixels32();
                    report.pixels += colors.Length;
                    foreach (Color32 c in colors)
                        if (c.a != 0 && (c.a != 255 || !palette.Contains((c.r<<16)|(c.g<<8)|c.b))) { errors.Add(item.path + ": cor/alpha"); break; }
                }
                if (request.tests)
                {
                    Type type = null;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) { type=assembly.GetType("Fabula.Incrementant.Tests.SurfaceSceneryTests",false); if(type != null) break; }
                    if (type == null) errors.Add("Assembly de testes nao carregada.");
                    else
                    {
                        object instance = Activator.CreateInstance(type);
                        foreach (string name in new[] { "PlantMask_AlignsFractionalPivotToArtPixelWithoutChangingSprite", "DetailedStamp_PreservesOnePixelPlantTips", "PlantMask_UsesOneTexelPerCellAndKeepsBuriedBase", "PlantMask_ShaderCutsHarvestedCellsAndTintsMarkedCells", "DetailedStamp_KeepsWorldSizeAndCustomPivot", "DetailedPebbles_PreserveBackdropWithoutPaintingCoarsePixels" })
                        {
                            try
                            {
                                MethodInfo method = type.GetMethod(name);
                                if (method == null) { errors.Add(name + ": metodo ausente"); continue; }
                                method.Invoke(instance,null); passed.Add(name);
                            }
                            catch (Exception e) { errors.Add(name + ": " + (e.InnerException ?? e).Message); }
                        }
                    }
                }
            }
            catch (Exception e) { errors.Add(e.ToString()); }
            report.passedTests = passed.ToArray(); report.errors = errors.ToArray(); report.success = errors.Count == 0;
            File.WriteAllText(Path.Combine(Folder,"unity-result.json"),JsonUtility.ToJson(report,true));
        }
    }
}
