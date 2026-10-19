using System.Collections.Generic;
using Fabula.Incrementant.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Audio
{
    /// <summary>
    /// Serviço de efeitos sonoros. Os sons vêm de <see cref="SfxSynth"/> (criados na primeira vez que tocam) e saem por um
    /// conjunto fixo de <see cref="AudioSource"/> num objeto que sobrevive às cenas. Sincronia com as configurações:
    /// o volume geral entra pelo <see cref="AudioListener.volume"/> (vale na hora, até para o que já está tocando) e o
    /// volume de efeitos (<see cref="GameSettings.SfxVolume"/>) é reaplicado a cada <see cref="GameSettings.Changed"/>,
    /// então mexer no slider muda também sons longos e a chuva em andamento.
    /// </summary>
    public static class GameAudio
    {
        const int PoolSize = 20;
        const float LoopFadeSeconds = 0.8f;

        struct Rule
        {
            public float Volume;
            public float MinInterval;
            public float PitchJitter;

            public Rule(float volume, float minInterval = 0f, float pitchJitter = 0f)
            {
                Volume = volume;
                MinInterval = minInterval;
                PitchJitter = pitchJitter;
            }
        }

        static readonly Rule[] Rules = BuildRules();
        static readonly AudioClip[] Clips = new AudioClip[(int)SfxId.Count];
        static readonly float[] LastPlayed = new float[(int)SfxId.Count];
        static Runner _runner;

        static Rule[] BuildRules()
        {
            var r = new Rule[(int)SfxId.Count];
            r[(int)SfxId.UiClick] = new Rule(0.5f, 0.03f, 0.03f);
            r[(int)SfxId.UiConfirm] = new Rule(0.55f, 0.1f);
            r[(int)SfxId.UiDeny] = new Rule(0.5f, 0.15f);
            r[(int)SfxId.UiTick] = new Rule(0.35f, 0.08f, 0.05f);
            r[(int)SfxId.Mark] = new Rule(0.35f, 0.05f, 0.04f);
            r[(int)SfxId.Unmark] = new Rule(0.35f, 0.05f, 0.04f);
            r[(int)SfxId.Buy] = new Rule(0.6f, 0.2f);
            // Sons que a colônia dispara muitas vezes por segundo: volume baixo, intervalo mínimo e variação de tom.
            r[(int)SfxId.Dig] = new Rule(0.22f, 0.07f, 0.15f);
            r[(int)SfxId.Drop] = new Rule(0.25f, 0.08f, 0.12f);
            r[(int)SfxId.Build] = new Rule(0.35f, 0.08f, 0.1f);
            r[(int)SfxId.Plant] = new Rule(0.3f, 0.08f, 0.1f);
            r[(int)SfxId.EggLaid] = new Rule(0.5f, 0.1f, 0.04f);
            r[(int)SfxId.AntBorn] = new Rule(0.5f, 0.1f, 0.04f);
            r[(int)SfxId.AntDie] = new Rule(0.5f, 0.1f, 0.08f);
            r[(int)SfxId.Drowned] = new Rule(0.5f, 0.2f, 0.05f);
            r[(int)SfxId.RaidWarning] = new Rule(0.7f, 2f);
            r[(int)SfxId.RaidStart] = new Rule(0.8f, 2f);
            r[(int)SfxId.RaidEnd] = new Rule(0.6f, 1f);
            r[(int)SfxId.TermiteHit] = new Rule(0.4f, 0.06f, 0.1f);
            r[(int)SfxId.TermiteKilled] = new Rule(0.5f, 0.08f, 0.1f);
            r[(int)SfxId.AntHurt] = new Rule(0.3f, 0.08f, 0.1f);
            r[(int)SfxId.Crumble] = new Rule(0.4f, 0.1f, 0.1f);
            r[(int)SfxId.GameOver] = new Rule(0.8f, 2f);
            r[(int)SfxId.Rain] = new Rule(0.5f);
            r[(int)SfxId.EmergencyBell] = new Rule(0.65f, 1f);
            return r;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Com o domain reload desligado os estáticos sobrevivem ao Play anterior: recomeça do zero.
            _runner = null;
            for (int i = 0; i < Clips.Length; i++)
            {
                Clips[i] = null;
                LastPlayed[i] = float.NegativeInfinity;
            }
        }

        /// <summary>Toca um efeito curto. Ignorado com efeitos ou volume geral zerados e dentro do intervalo mínimo do som.</summary>
        public static void Play(SfxId id) => Play(id, 1f);

        public static void Play(SfxId id, float volumeScale)
        {
            if (!Application.isPlaying || id >= SfxId.Count || SfxSynth.IsLoop(id))
                return;
            if (GameSettings.SfxVolume <= 0.001f || AudioListener.volume <= 0.001f)
                return;
            Rule rule = Rules[(int)id];
            float now = Time.unscaledTime;
            if (now - LastPlayed[(int)id] < rule.MinInterval)
                return;
            LastPlayed[(int)id] = now;
            float pitch = 1f + (Random.value * 2f - 1f) * rule.PitchJitter;
            GetRunner().PlayOneShot(ClipOf(id), rule.Volume * volumeScale, pitch);
        }

        /// <summary>
        /// Liga, regula ou desliga um som em laço (a chuva). <paramref name="level"/> de 0 a 1 é a intensidade do som em si;
        /// o volume de efeitos é aplicado por cima e acompanha o slider. A mudança de nível é suave.
        /// </summary>
        public static void SetLoop(SfxId id, float level)
        {
            if (!Application.isPlaying || !SfxSynth.IsLoop(id))
                return;
            if (_runner == null && level <= 0f)
                return;
            GetRunner().SetLoopTarget(id, ClipOf(id), Mathf.Clamp01(level));
        }

        /// <summary>Silencia na hora tudo que está tocando (saída da partida).</summary>
        public static void StopAll()
        {
            if (_runner != null)
                _runner.StopEverything();
        }

        static Runner GetRunner()
        {
            if (_runner != null)
                return _runner;
            var go = new GameObject("GameAudio");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
            return _runner;
        }

        static AudioClip ClipOf(SfxId id)
        {
            AudioClip clip = Clips[(int)id];
            if (clip != null)
                return clip;
            float[] samples = SfxSynth.Render(id);
            clip = AudioClip.Create("sfx_" + id, samples.Length, 1, SfxSynth.SampleRate, false);
            clip.SetData(samples, 0);
            Clips[(int)id] = clip;
            return clip;
        }

        sealed class Runner : MonoBehaviour
        {
            sealed class Loop
            {
                public AudioSource Source;
                public float Level;
                public float Target;
                public float Rule;
            }

            readonly AudioSource[] _pool = new AudioSource[PoolSize];
            readonly float[] _baseVolume = new float[PoolSize];
            readonly Dictionary<SfxId, Loop> _loops = new Dictionary<SfxId, Loop>();

            void Awake()
            {
                for (int i = 0; i < PoolSize; i++)
                {
                    AudioSource source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    _pool[i] = source;
                }
            }

            void OnEnable() => GameSettings.Changed += ApplyVolumes;

            void OnDisable() => GameSettings.Changed -= ApplyVolumes;

            public void PlayOneShot(AudioClip clip, float volume, float pitch)
            {
                int slot = -1;
                float oldest = -1f;
                for (int i = 0; i < PoolSize; i++)
                {
                    if (!_pool[i].isPlaying)
                    {
                        slot = i;
                        break;
                    }
                    // Sem fonte livre, toma a que está tocando há mais tempo (o som mais antigo é o menos notado).
                    if (_pool[i].time > oldest)
                    {
                        oldest = _pool[i].time;
                        slot = i;
                    }
                }
                AudioSource source = _pool[slot];
                source.Stop();
                source.clip = clip;
                source.pitch = pitch;
                _baseVolume[slot] = volume;
                source.volume = volume * GameSettings.SfxVolume;
                source.Play();
            }

            public void SetLoopTarget(SfxId id, AudioClip clip, float level)
            {
                if (!_loops.TryGetValue(id, out Loop loop))
                {
                    if (level <= 0f)
                        return;
                    AudioSource source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    source.loop = true;
                    source.clip = clip;
                    loop = new Loop { Source = source, Rule = Rules[(int)id].Volume };
                    _loops[id] = loop;
                }
                loop.Target = level;
                if (level > 0f && !loop.Source.isPlaying)
                    loop.Source.Play();
            }

            public void StopEverything()
            {
                for (int i = 0; i < PoolSize; i++)
                    _pool[i].Stop();
                foreach (Loop loop in _loops.Values)
                {
                    loop.Target = 0f;
                    loop.Level = 0f;
                    loop.Source.Stop();
                }
            }

            /// <summary>Slider de efeitos mudou: ajusta o volume do que já está tocando (o geral é o AudioListener).</summary>
            void ApplyVolumes()
            {
                float sfx = GameSettings.SfxVolume;
                for (int i = 0; i < PoolSize; i++)
                    if (_pool[i].isPlaying)
                        _pool[i].volume = _baseVolume[i] * sfx;
                foreach (Loop loop in _loops.Values)
                    loop.Source.volume = loop.Level * loop.Rule * sfx;
            }

            void Update()
            {
                if (_loops.Count == 0)
                    return;
                float sfx = GameSettings.SfxVolume;
                float step = Time.unscaledDeltaTime / LoopFadeSeconds;
                foreach (Loop loop in _loops.Values)
                {
                    loop.Level = Mathf.MoveTowards(loop.Level, loop.Target, step);
                    loop.Source.volume = loop.Level * loop.Rule * sfx;
                    if (loop.Level <= 0f && loop.Target <= 0f && loop.Source.isPlaying)
                        loop.Source.Stop();
                }
            }
        }
    }
}
