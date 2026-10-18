using System.Text;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Diagnostics;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Painel de estatísticas da simulação (F11): FPS, tempo por etapa (média e pico) e o que está sendo simulado
    /// (formigas por estado e papel, campos refeitos por segundo, grade). O texto é remontado a cada
    /// <see cref="RefreshSeconds"/>, não a cada frame, para o painel pesar pouco no que mede.
    /// </summary>
    public sealed class SimStatsOverlay
    {
        const float RefreshSeconds = 0.25f;

        static readonly string[] StageLabels =
        {
            "Chuva", "Água", "Vegetação", "Granular", "Colônia (Tick)", "  preparo", "  campos (rebuild)", "    walkability",
            "    sala da rainha", "    despejo", "    casa/colônia", "    berçário", "    nutrientes", "    escavação", "    jardim",
            "    construção", "  rainha", "  criação", "  formigas (UpdateAnt)", "  afogamento", "Render terreno", "Render formigas",
            "Render crias", "Render cenário/plantas",
        };

        // Mesma ordem de ProfileCounter, a partir de CauseUnderground.
        static readonly string[] CauseLabels =
        {
            "túnel", "céu", "perto-marca", "obra", "marcas", "rainha", "nutrientes", "colheita", "claims", "jardim", "berçário",
        };

        readonly StringBuilder _text = new StringBuilder(1536);
        readonly int[] _byState = new int[System.Enum.GetValues(typeof(AntState)).Length];
        readonly int[] _byRole = new int[System.Enum.GetValues(typeof(AntRole)).Length];
        string _cached = string.Empty;
        float _nextRefresh;
        float _frameMs;
        int _maxTickFrames;
        int _frames;
        GUIStyle _style;
        Texture2D _background;

        /// <summary>Frames em que o limite de ticks por frame foi atingido (a simulação não acompanhou o tempo real).</summary>
        public void NoteFrame(float unscaledDelta, bool tickLimitHit)
        {
            _frameMs = Mathf.Lerp(_frameMs, unscaledDelta * 1000f, 0.1f);
            _frames++;
            if (tickLimitHit)
                _maxTickFrames++;
        }

        public void Draw(Rect area, AntColony colony, PixelGrid grid, VegetationSimulation vegetation, int speed, bool paused,
                         float ticksPerSecond, int maxTicksPerFrame)
        {
            EnsureStyles();
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Rebuild(colony, grid, vegetation, speed, paused, ticksPerSecond, maxTicksPerFrame);
            }
            // Fundo opaco: o painel fica sobre o HUD e o cenário, e o texto precisa ler em qualquer um.
            GUI.DrawTexture(area, _background);
            GUI.Label(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, area.height - 12f), _cached, _style);
        }

        void Rebuild(AntColony colony, PixelGrid grid, VegetationSimulation vegetation, int speed, bool paused,
                     float ticksPerSecond, int maxTicksPerFrame)
        {
            SimProfiler p = colony.Profiler;
            StringBuilder t = _text;
            t.Clear();
            float fps = 1000f / Mathf.Max(_frameMs, 0.01f);
            t.Append("<b>Estatísticas [F11]</b>\n");
            t.Append($"{fps:0} fps · frame {_frameMs:0.0} ms · x{speed}{(paused ? " (pausado)" : string.Empty)}\n");
            t.Append($"Ticks/s {p.RatePerSecond(ProfileCounter.Ticks):0} (alvo {ticksPerSecond * speed:0}, máx {maxTicksPerFrame * speed}/frame)");
            t.Append($" · frames no limite: {_maxTickFrames}/{_frames}\n");
            t.Append($"Campos refeitos/s: {p.RatePerSecond(ProfileCounter.FieldRebuilds):0.0}\n\n");

            t.Append("<b>Etapa                       média    pico</b>\n");
            for (int i = 0; i < (int)ProfileStage.Count; i++)
            {
                var stage = (ProfileStage)i;
                float peak = p.PeakMs(stage);
                string row = $"{StageLabels[i],-26} {p.AverageMs(stage),6:0.00}  {peak,6:0.00}";
                // Pico alto fica em vermelho: é o tick/frame que o jogador sente como engasgo.
                t.Append(peak >= 8f ? $"<color=#ff7060>{row}</color>\n" : $"{row}\n");
            }

            CountAnts(colony);
            t.Append($"\n<b>Formigas: {colony.Ants.Count}</b> (máx {colony.MaxAnts}) · afogadas {colony.DrownedAnts}\n");
            AppendCounts(t, _byRole, typeof(AntRole));
            AppendCounts(t, _byState, typeof(AntState));

            RectInt active = grid.Active;
            t.Append($"\n<b>Mundo</b> {active.width}x{active.height} de {grid.Width}x{grid.Height}\n");
            t.Append($"Plantas {vegetation.PlantCount} · resíduo solto {grid.PlantDebrisCount} · colhido {colony.HarvestedPixels}\n");
            t.Append($"Marcados {grid.MarkedCount} cavar · {grid.BuildMarkedCount} construir\n");
            t.Append($"Nutrientes {grid.NutrientCount} ({grid.CuringNutrientCount} curando) · slots livres {colony.FreeNutrientSlots}\n");
            t.Append($"Água {grid.WaterCount} px · cria {colony.Brood.Count} · colheita {(colony.HarvestPaused ? "PAUSADA" : "ativa")}\n");
            _cached = t.ToString();
        }

        void CountAnts(AntColony colony)
        {
            System.Array.Clear(_byState, 0, _byState.Length);
            System.Array.Clear(_byRole, 0, _byRole.Length);
            foreach (Ant ant in colony.Ants)
            {
                _byState[(int)ant.State]++;
                _byRole[(int)ant.Role]++;
            }
        }

        static void AppendCounts(StringBuilder t, int[] counts, System.Type enumType)
        {
            string[] names = System.Enum.GetNames(enumType);
            int onLine = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0)
                    continue;
                if (onLine > 0)
                    t.Append(onLine % 3 == 0 ? "\n" : " · ");
                t.Append(names[i]).Append(' ').Append(counts[i]);
                onLine++;
            }
            t.Append('\n');
        }

        void EnsureStyles()
        {
            if (_style != null)
                return;
            _style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = false };
            _style.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Lucida Console" }, 12);
            _background = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            _background.SetPixel(0, 0, new Color(0.06f, 0.05f, 0.05f, 0.94f));
            _background.Apply(false);
        }
    }
}
