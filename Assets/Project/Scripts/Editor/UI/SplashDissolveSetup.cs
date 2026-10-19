using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fabula.Incrementant.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Monta a dissolução do logotipo do splash: assa o logotipo em texturas de posição/cor/atraso (um texel por bloco
    /// de terra), constrói o VFX Graph <c>LogoDissolve.vfx</c> e liga tudo na cena Splash. Idempotente: texturas e
    /// grafo são regravados no mesmo arquivo (GUID preservado) e o objeto da cena é reaproveitado.
    /// O VFX Graph não tem API pública de edição; o grafo é montado por reflexão sobre o assembly do editor do pacote
    /// <c>com.unity.visualeffectgraph</c>, então uma atualização do pacote pode exigir ajuste aqui.
    /// </summary>
    public static class SplashDissolveSetup
    {
        public const string Folder = "Assets/Project/VFX/SplashLogo";
        const string PositionsPath = Folder + "/LogoPositions.asset";
        const string ColorsPath = Folder + "/LogoColors.asset";
        const string DelaysPath = Folder + "/LogoDelays.asset";
        public const string VfxPath = Folder + "/LogoDissolve.vfx";
        const string ObjectName = "LogoDissolve";

        // Lado de cada bloco de terra, em pixels do PNG do logotipo (1672 x 941).
        const int CellPixels = 6;
        // Fração mínima de opacidade média da célula para virar um bloco.
        const float MinCellAlpha = 0.4f;
        const int TextureWidth = 256;

        // Dissolução: a onda vai da esquerda para a direita e de cima para baixo, com sorteio por bloco.
        const float SweepSeconds = 2.2f;
        const float JitterSeconds = 0.5f;
        const float FallSeconds = 1.7f;
        const float Gravity = 28f;
        const float TurbulenceIntensity = 3f;
        const float TurbulenceFrequency = 0.6f;

        [MenuItem("Incrementant/Setup/Splash - dissolução da logo")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BuildAssets();
            Scene scene = SceneManager.GetSceneByPath(SplashSceneSetup.ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(SplashSceneSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                ConfigureScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log($"[SplashDissolveSetup] Dissolução da logo pronta em {SplashSceneSetup.ScenePath}.");
        }

        /// <summary>Gera texturas e grafo (idempotente). Retorna o asset do VFX.</summary>
        public static VisualEffectAsset BuildAssets()
        {
            Directory.CreateDirectory(Folder);
            Bake(out Texture2D positions, out Texture2D colors, out Texture2D delays, out int count, out float cellSize);
            VisualEffectAsset vfx = VfxGraphBuilder.Build(VfxPath, positions, colors, delays, count, cellSize);
            Debug.Log($"[SplashDissolveSetup] {count} blocos de {cellSize:0.000} un; VFX em {VfxPath}.");
            return vfx;
        }

        /// <summary>Liga o dissolve na cena aberta: câmera, canvas na câmera, objeto do VFX e a referência do SplashScreen.</summary>
        public static void ConfigureScene(Scene scene)
        {
            VisualEffectAsset vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
            if (vfx == null)
                throw new InvalidOperationException($"{VfxPath} ausente: rode Incrementant/Setup/Splash - dissolução da logo.");

            var roots = scene.GetRootGameObjects();
            var splash = roots.SelectMany(r => r.GetComponentsInChildren<SplashScreen>(true)).Single();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));

            // O canvas precisa desenhar por trás dos blocos: em modo overlay a UI cobriria o VFX (que é um objeto de mundo).
            camera.orthographic = true;
            camera.orthographicSize = SplashLogoDissolve.BakedHeight * 0.5f;
            camera.transform.position = Vector3.zero;
            camera.cullingMask = ~0;
            var canvas = splash.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;

            Transform existing = splash.transform.parent != null ? null : FindRoot(roots, ObjectName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(ObjectName, typeof(VisualEffect), typeof(SplashLogoDissolve));
            if (go.scene != scene)
                SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = new Vector3(0f, 0f, 10f);

            var effect = go.GetComponent<VisualEffect>();
            effect.visualEffectAsset = vfx;
            // Nenhum evento no Awake: o SplashScreen dispara o OnPlay quando o tempo parado acaba.
            effect.initialEventName = "Aguardando";

            var dissolve = go.GetComponent<SplashLogoDissolve>();
            var so = new SerializedObject(dissolve);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            var splashSo = new SerializedObject(splash);
            splashSo.FindProperty("_dissolve").objectReferenceValue = dissolve;
            splashSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
        }

        static Transform FindRoot(GameObject[] roots, string name)
        {
            foreach (GameObject root in roots)
                if (root.name == name)
                    return root.transform;
            return null;
        }

        // ----- Bake -----

        static void Bake(out Texture2D positions, out Texture2D colors, out Texture2D delays, out int count, out float cellSize)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(SplashSceneSetup.LogoPath)))
                throw new InvalidOperationException($"Não consegui ler {SplashSceneSetup.LogoPath}.");
            Color32[] pixels = source.GetPixels32();
            int width = source.width, height = source.height;
            Object.DestroyImmediate(source);

            float worldHeight = SplashLogoDissolve.BakedHeight;
            float worldWidth = worldHeight * width / height;
            cellSize = CellPixels * worldHeight / height;
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;

            var cellPositions = new List<Color>();
            var cellColors = new List<Color>();
            var cellDelays = new List<float>();
            var random = new System.Random(20260418);
            int cellsX = Mathf.CeilToInt(width / (float)CellPixels);
            int cellsY = Mathf.CeilToInt(height / (float)CellPixels);

            for (int cy = 0; cy < cellsY; cy++)
            {
                for (int cx = 0; cx < cellsX; cx++)
                {
                    int x0 = cx * CellPixels, y0 = cy * CellPixels;
                    int x1 = Mathf.Min(x0 + CellPixels, width), y1 = Mathf.Min(y0 + CellPixels, height);
                    float alphaSum = 0f, r = 0f, g = 0f, b = 0f;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            Color32 p = pixels[y * width + x];
                            float a = p.a / 255f;
                            alphaSum += a;
                            r += p.r / 255f * a;
                            g += p.g / 255f * a;
                            b += p.b / 255f * a;
                        }
                    }
                    int area = (x1 - x0) * (y1 - y0);
                    if (alphaSum / area < MinCellAlpha || alphaSum <= 0f)
                        continue;

                    // Textura do PNG: linha 0 embaixo (mesma orientação do mundo, y para cima).
                    float u = (x0 + (x1 - x0) * 0.5f) / width;
                    float v = (y0 + (y1 - y0) * 0.5f) / height;
                    Color color = new Color(r / alphaSum, g / alphaSum, b / alphaSum, 1f);
                    if (linear)
                        color = color.linear;

                    cellPositions.Add(new Color((u - 0.5f) * worldWidth, (v - 0.5f) * worldHeight, 0f, 0f));
                    cellColors.Add(color);
                    cellDelays.Add(SweepSeconds * (0.65f * u + 0.35f * (1f - v)) + (float)random.NextDouble() * JitterSeconds);
                }
            }

            count = cellPositions.Count;
            if (count == 0)
                throw new InvalidOperationException("O logotipo não tem pixels opacos.");
            int rows = Mathf.CeilToInt(count / (float)TextureWidth);

            var positionPixels = new Color[TextureWidth * rows];
            var colorPixels = new Color[TextureWidth * rows];
            var delayPixels = new Color[TextureWidth * rows];
            for (int i = 0; i < count; i++)
            {
                positionPixels[i] = cellPositions[i];
                colorPixels[i] = cellColors[i];
                delayPixels[i] = new Color(cellDelays[i], 0f, 0f, 0f);
            }

            positions = SaveTexture(PositionsPath, TextureFormat.RGBAFloat, rows, positionPixels);
            colors = SaveTexture(ColorsPath, TextureFormat.RGBAFloat, rows, colorPixels);
            delays = SaveTexture(DelaysPath, TextureFormat.RFloat, rows, delayPixels);
        }

        static Texture2D SaveTexture(string path, TextureFormat format, int rows, Color[] pixels)
        {
            var texture = new Texture2D(TextureWidth, rows, format, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(texture, path);
                return texture;
            }
            EditorUtility.CopySerialized(texture, existing);
            Object.DestroyImmediate(texture);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ----- VFX Graph (reflexão) -----

        static class VfxGraphBuilder
        {
            const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            static readonly Assembly EditorAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");

            public static VisualEffectAsset Build(string path, Texture2D positions, Texture2D colors, Texture2D delays, int count, float cellSize)
            {
                if (EditorAssembly == null)
                    throw new InvalidOperationException("Pacote com.unity.visualeffectgraph não está instalado.");

                // Reescreve o arquivo no mesmo caminho: o .meta (GUID) é preservado.
                var asset = (VisualEffectAsset)Static("UnityEditor.VisualEffectAssetEditorUtility", "CreateNewAsset", path);
                object resource = Static("UnityEditor.VFX.VisualEffectObjectExtensions", "GetResource", asset);
                object graph = Static("UnityEditor.VFX.VisualEffectResourceExtensions", "GetOrCreateGraph", resource);
                // O recurso fica em cache pelo caminho: sem esvaziar, cada execução empilharia mais um sistema no mesmo grafo.
                foreach (object child in Seq(graph, "children").ToList())
                    Call(graph, "RemoveChild", child);

                // Fluxo: spawner (burst único) -> inicialização -> atualização -> saída (quad opaco).
                object spawner = Add(graph, "UnityEditor.VFX.VFXBasicSpawner");
                object init = Add(graph, "UnityEditor.VFX.VFXBasicInitialize");
                object update = Add(graph, "UnityEditor.VFX.VFXBasicUpdate");
                object output = Add(graph, "UnityEditor.VFX.VFXPlanarPrimitiveOutput");
                Call(spawner, "LinkTo", init);
                Call(init, "LinkTo", update);
                Call(update, "LinkTo", output);

                object burst = AddBlock(spawner, "UnityEditor.VFX.VFXSpawnerBurst");
                SetSlot(burst, "Count", (float)count);

                // Capacidade acima do total de blocos; o burst cria todos de uma vez.
                object data = Call(init, "GetData");
                Setting(data, "capacity", (uint)Mathf.NextPowerOfTwo(count));

                Setting(output, "primitiveType", EnumValue(output, "primitiveType", "Quad"));
                Setting(output, "blendMode", EnumValue(output, "blendMode", "Opaque"));

                // Inicialização: cada partícula lê o seu texel (id sequencial) -> posição e cor. O atraso assado na textura
                // vira vida útil (atraso + tempo de queda): sem idade negativa, a partícula morre quando termina de cair.
                object block = AddBlock(init, "UnityEditor.VFX.Block.AttributeFromMap");
                ConfigureMap(block, "position", positions);
                block = AddBlock(init, "UnityEditor.VFX.Block.AttributeFromMap");
                ConfigureMap(block, "color", colors);
                block = AddBlock(init, "UnityEditor.VFX.Block.AttributeFromMap");
                ConfigureMap(block, "lifetime", delays);
                SetSlot(block, "valueBias", FallSeconds);

                block = AddBlock(init, "UnityEditor.VFX.Block.SetAttribute");
                Setting(block, "attribute", "size");
                SetSlot(block, "size", cellSize);

                // Atualização: as forças só agem depois que o atraso passa (idade >= vida - tempo de queda); antes disso o
                // bloco fica parado no logotipo. gate = saturate((idade - vida + queda) * 1000), 0 ou 1.
                object age = Add(graph, "UnityEditor.VFX.VFXAttributeParameter");
                Setting(age, "attribute", "age");
                object lifetime = Add(graph, "UnityEditor.VFX.VFXAttributeParameter");
                Setting(lifetime, "attribute", "lifetime");
                object remaining = Add(graph, "UnityEditor.VFX.Operator.Subtract");
                Link(age, 0, remaining, 0);
                Link(lifetime, 0, remaining, 1);
                object shifted = Add(graph, "UnityEditor.VFX.Operator.Add");
                Link(remaining, 0, shifted, 0);
                SetInput(shifted, 1, FallSeconds);
                object ramp = Add(graph, "UnityEditor.VFX.Operator.Multiply");
                Link(shifted, 0, ramp, 0);
                SetInput(ramp, 1, 1000f);
                object gate = Add(graph, "UnityEditor.VFX.Operator.Saturate");
                Link(ramp, 0, gate, 0);

                object fall = Add(graph, "UnityEditor.VFX.Operator.Multiply");
                Link(gate, 0, fall, 0);
                Call(fall, "SetOperandType", 1, typeof(Vector3));
                SetInput(fall, 1, new Vector3(0f, -Gravity, 0f));
                object gravity = AddBlock(update, "UnityEditor.VFX.Block.Gravity");
                if (!(bool)Call(Out(fall, 0), "Link", InputSlot(gravity, "Force"), true))
                    throw new InvalidOperationException("Não consegui ligar a força ao bloco Gravity.");

                object swirlIntensity = Add(graph, "UnityEditor.VFX.Operator.Multiply");
                Link(gate, 0, swirlIntensity, 0);
                SetInput(swirlIntensity, 1, TurbulenceIntensity);
                object turbulence = AddBlock(update, "UnityEditor.VFX.Block.Turbulence");
                Setting(turbulence, "Mode", EnumValue(turbulence, "Mode", "Absolute"));
                Call(Out(swirlIntensity, 0), "Link", InputSlot(turbulence, "Intensity"), true);
                SetSlot(turbulence, "Frequency", TurbulenceFrequency);

                Static("UnityEditor.VFX.VisualEffectResourceExtensions", "WriteAssetWithSubAssets", resource);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            }

            static void ConfigureMap(object block, string attribute, Texture2D map)
            {
                Setting(block, "attribute", attribute);
                Setting(block, "SampleMode", EnumValue(block, "SampleMode", "Sequential"));
                SetSlot(block, "attributeMap", map);
            }

            // Operadores numéricos unificados mudam de tipo ao receber um link; as entradas são acessadas por índice.
            static void SetInput(object model, int index, object value)
            {
                object slot = InputSlots(model).ElementAt(index);
                slot.GetType().GetProperty("value", All).SetValue(slot, value);
            }

            static void Link(object from, int outIndex, object to, int inIndex)
            {
                bool linked = (bool)Call(Out(from, outIndex), "Link", InputSlots(to).ElementAt(inIndex), true);
                if (!linked)
                    throw new InvalidOperationException($"Não consegui ligar {from.GetType().Name} -> {to.GetType().Name}.");
            }

            static IEnumerable<object> Seq(object o, string name) => ((System.Collections.IEnumerable)Property(o, name)).Cast<object>();
            static object Out(object model, int index) => Seq(model, "outputSlots").ElementAt(index);
            static IEnumerable<object> InputSlots(object model) => Seq(model, "inputSlots");

            static object InputSlot(object model, string name)
            {
                object slot = InputSlots(model).FirstOrDefault(s => string.Equals(((string)Property(s, "name")).TrimStart('_'), name, StringComparison.OrdinalIgnoreCase));
                if (slot == null)
                    throw new InvalidOperationException($"{model.GetType().Name} não tem a entrada '{name}'. Entradas: {string.Join(", ", InputSlots(model).Select(s => (string)Property(s, "name")))}");
                return slot;
            }

            static void SetSlot(object model, string name, object value)
            {
                object slot = InputSlot(model, name);
                slot.GetType().GetProperty("value", All).SetValue(slot, value);
            }

            static object Add(object graph, string typeName)
            {
                var model = ScriptableObject.CreateInstance(Type(typeName));
                Call(graph, "AddChild", model);
                return model;
            }

            static object AddBlock(object context, string typeName) => Add(context, typeName);

            static void Setting(object model, string name, object value) => Call(model, "SetSettingValue", name, value);

            static object EnumValue(object model, string member, string name)
            {
                Type enumType = (model.GetType().GetField(member, All)?.FieldType) ?? throw new InvalidOperationException($"{model.GetType().Name}.{member} não existe.");
                return Enum.Parse(enumType, name);
            }

            static Type Type(string name) => EditorAssembly.GetType(name, true);

            static object Property(object o, string name)
            {
                PropertyInfo p = FindProperty(o.GetType(), name);
                if (p == null)
                    throw new InvalidOperationException($"{o.GetType().Name} não tem a propriedade '{name}'.");
                return p.GetValue(o);
            }

            static PropertyInfo FindProperty(System.Type type, string name)
            {
                for (System.Type t = type; t != null; t = t.BaseType)
                {
                    PropertyInfo p = t.GetProperty(name, All | BindingFlags.DeclaredOnly);
                    if (p != null)
                        return p;
                }
                return null;
            }

            static object Static(string typeName, string method, params object[] args) => Invoke(Type(typeName), null, method, args);

            static object Call(object target, string method, params object[] args) => Invoke(target.GetType(), target, method, args);

            // Completa parâmetros opcionais com o valor padrão (AddChild, Link, LinkTo...).
            static object Invoke(System.Type type, object target, string method, object[] args)
            {
                foreach (MethodInfo m in type.GetMethods(All).Where(m => m.Name == method))
                {
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length < args.Length || ps.Skip(args.Length).Any(p => !p.HasDefaultValue))
                        continue;
                    if (!args.Select((a, i) => a == null || ps[i].ParameterType.IsInstanceOfType(a) || ps[i].ParameterType.IsAssignableFrom(a.GetType())).All(ok => ok))
                        continue;
                    object[] full = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                        full[i] = i < args.Length ? args[i] : ps[i].DefaultValue;
                    try
                    {
                        return m.Invoke(m.IsStatic ? null : target, full);
                    }
                    catch (TargetInvocationException e)
                    {
                        throw new InvalidOperationException($"{type.Name}.{method} falhou: {e.InnerException?.Message}", e.InnerException);
                    }
                }
                throw new MissingMethodException($"{type.FullName}.{method}({args.Length} argumentos)");
            }
        }
    }
}
