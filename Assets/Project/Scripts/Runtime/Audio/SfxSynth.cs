using System;

namespace Fabula.Incrementant.Audio
{
    /// <summary>
    /// Síntese dos efeitos sonoros: cada <see cref="SfxId"/> vira um trecho mono de amostras, calculado em código (tons com
    /// envelope, ruído filtrado e arpejos), sem arquivos de áudio. Determinístico: o ruído usa semente fixa por som.
    /// Lógica pura (sem Unity), testável em EditMode.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 32000;
        const float Peak = 0.9f;

        enum Wave { Sine, Triangle, Square, Saw }

        public static bool IsLoop(SfxId id) => id == SfxId.Rain;

        /// <summary>Amostras do som, normalizadas para o pico de 0,9. O volume relativo entre sons é decidido por quem toca.</summary>
        public static float[] Render(SfxId id)
        {
            var b = new Buffer(Length(id), (uint)((int)id + 1) * 2654435761u);
            switch (id)
            {
                case SfxId.UiClick:
                    b.Tone(0f, 0.07f, 1100f, 700f, Wave.Triangle, 0.9f, 0.002f, 45f);
                    break;
                case SfxId.UiConfirm:
                    b.Tone(0f, 0.12f, 660f, 660f, Wave.Triangle, 0.8f, 0.004f, 18f);
                    b.Tone(0.08f, 0.2f, 990f, 990f, Wave.Triangle, 0.8f, 0.004f, 14f);
                    break;
                case SfxId.UiDeny:
                    b.Tone(0f, 0.18f, 220f, 150f, Wave.Square, 0.7f, 0.004f, 14f);
                    b.Tone(0.09f, 0.18f, 200f, 120f, Wave.Square, 0.7f, 0.004f, 14f);
                    break;
                case SfxId.UiTick:
                    b.Tone(0f, 0.035f, 1500f, 1300f, Wave.Sine, 0.8f, 0.001f, 90f);
                    break;
                case SfxId.Mark:
                    b.Tone(0f, 0.05f, 900f, 1250f, Wave.Sine, 0.8f, 0.002f, 60f);
                    break;
                case SfxId.Unmark:
                    b.Tone(0f, 0.05f, 1100f, 750f, Wave.Sine, 0.8f, 0.002f, 60f);
                    break;
                case SfxId.Buy:
                    Arpeggio(b, 0f, 0.09f, 0.35f, 12f, Wave.Triangle, 523f, 659f, 784f, 1047f);
                    break;
                case SfxId.Dig:
                    b.Noise(0f, 0.08f, 0.8f, 0.30f, 0.001f, 40f);
                    b.Tone(0f, 0.06f, 190f, 90f, Wave.Sine, 0.5f, 0.001f, 45f);
                    break;
                case SfxId.Drop:
                    b.Noise(0f, 0.1f, 0.7f, 0.18f, 0.001f, 32f);
                    b.Tone(0f, 0.09f, 130f, 70f, Wave.Sine, 0.6f, 0.001f, 30f);
                    break;
                case SfxId.Build:
                    b.Tone(0f, 0.14f, 150f, 60f, Wave.Sine, 0.9f, 0.001f, 24f);
                    b.Noise(0f, 0.08f, 0.5f, 0.2f, 0.001f, 40f);
                    break;
                case SfxId.Plant:
                    b.Tone(0f, 0.09f, 420f, 880f, Wave.Sine, 0.9f, 0.002f, 38f);
                    break;
                case SfxId.EggLaid:
                    b.Tone(0f, 0.16f, 380f, 720f, Wave.Sine, 0.9f, 0.01f, 16f);
                    b.Tone(0.02f, 0.14f, 760f, 1440f, Wave.Sine, 0.25f, 0.01f, 18f);
                    break;
                case SfxId.AntBorn:
                    Arpeggio(b, 0f, 0.07f, 0.3f, 14f, Wave.Triangle, 523f, 659f, 784f);
                    break;
                case SfxId.AntDie:
                    b.Tone(0f, 0.28f, 420f, 130f, Wave.Triangle, 0.9f, 0.004f, 9f);
                    break;
                case SfxId.RaidWarning:
                    for (int i = 0; i < 4; i++)
                        b.Tone(i * 0.3f, 0.28f, i % 2 == 0 ? 520f : 390f, i % 2 == 0 ? 520f : 390f, Wave.Square, 0.55f, 0.01f, 5f);
                    break;
                case SfxId.RaidStart:
                    b.Tone(0f, 1.1f, 110f, 98f, Wave.Saw, 0.6f, 0.08f, 2.2f);
                    b.Tone(0f, 1.1f, 165f, 147f, Wave.Saw, 0.35f, 0.08f, 2.2f);
                    b.Noise(0f, 0.6f, 0.4f, 0.12f, 0.02f, 4f);
                    break;
                case SfxId.RaidEnd:
                    Arpeggio(b, 0f, 0.13f, 0.5f, 6f, Wave.Triangle, 392f, 494f, 587f, 784f);
                    break;
                case SfxId.TermiteHit:
                    b.Noise(0f, 0.06f, 0.6f, 0.4f, 0.001f, 55f);
                    b.Tone(0f, 0.08f, 320f, 190f, Wave.Square, 0.5f, 0.001f, 40f);
                    break;
                case SfxId.TermiteKilled:
                    b.Noise(0f, 0.22f, 0.7f, 0.5f, 0.001f, 14f);
                    b.Tone(0f, 0.25f, 240f, 55f, Wave.Triangle, 0.9f, 0.001f, 12f);
                    break;
                case SfxId.AntHurt:
                    b.Tone(0f, 0.09f, 780f, 320f, Wave.Square, 0.5f, 0.001f, 35f);
                    break;
                case SfxId.Crumble:
                    for (int i = 0; i < 6; i++)
                        b.Noise(i * 0.045f + b.Rand() * 0.02f, 0.1f, 0.5f + b.Rand() * 0.3f, 0.35f, 0.001f, 28f);
                    b.Tone(0f, 0.2f, 100f, 55f, Wave.Sine, 0.6f, 0.002f, 12f);
                    break;
                case SfxId.GameOver:
                    Arpeggio(b, 0f, 0.3f, 0.4f, 4f, Wave.Triangle, 392f, 330f, 262f, 196f);
                    break;
                case SfxId.Rain:
                    b.Rain();
                    break;
                case SfxId.EmergencyBell:
                    EmergencyBell(b);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
            return b.Finish(IsLoop(id));
        }

        /// <summary>Duração em segundos de cada som (o buffer ganha folga para a cauda do envelope).</summary>
        public static float Seconds(SfxId id)
        {
            switch (id)
            {
                case SfxId.UiClick: return 0.12f;
                case SfxId.UiConfirm: return 0.4f;
                case SfxId.UiDeny: return 0.35f;
                case SfxId.UiTick: return 0.08f;
                case SfxId.Mark:
                case SfxId.Unmark: return 0.1f;
                case SfxId.Buy: return 0.7f;
                case SfxId.Dig: return 0.15f;
                case SfxId.Drop: return 0.18f;
                case SfxId.Build: return 0.25f;
                case SfxId.Plant: return 0.15f;
                case SfxId.EggLaid: return 0.3f;
                case SfxId.AntBorn: return 0.4f;
                case SfxId.AntDie: return 0.45f;
                case SfxId.RaidWarning: return 1.3f;
                case SfxId.RaidStart: return 1.4f;
                case SfxId.RaidEnd: return 1.1f;
                case SfxId.TermiteHit: return 0.14f;
                case SfxId.TermiteKilled: return 0.4f;
                case SfxId.AntHurt: return 0.15f;
                case SfxId.Crumble: return 0.45f;
                case SfxId.GameOver: return 1.5f;
                case SfxId.Rain: return 2.5f;
                case SfxId.EmergencyBell: return 4f;
                default: return 0.5f;
            }
        }

        static int Length(SfxId id) => (int)(Seconds(id) * SampleRate);

        /// <summary>
        /// Sino de aldeia tocado por corda: o badalo bate nas laterais de um lado e do outro, em ritmo quase regular com
        /// pequenas irregularidades da mão (tempo e força), e cada batida soa por cima da cauda da anterior.
        /// </summary>
        static void EmergencyBell(Buffer b)
        {
            const float baseHz = 760f;
            float t = 0.02f;
            for (int i = 0; i < 11; i++)
            {
                bool otherSide = i % 2 == 1;
                // A batida do outro lado pega outra parte do bronze: um pouco mais grave e mais fraca.
                float hz = baseHz * (otherSide ? 0.992f : 1f) * (1f + (b.Rand() - 0.5f) * 0.004f);
                float strength = (otherSide ? 0.82f : 1f) * (0.88f + b.Rand() * 0.12f);
                b.BellStrike(t, hz, strength);
                t += 0.235f + (b.Rand() - 0.5f) * 0.05f;
            }
        }

        static void Arpeggio(Buffer b, float start, float step, float noteLength, float decay, Wave wave, params float[] notes)
        {
            for (int i = 0; i < notes.Length; i++)
                b.Tone(start + i * step, noteLength, notes[i], notes[i], wave, 0.8f, 0.004f, decay);
        }

        sealed class Buffer
        {
            readonly float[] _samples;
            uint _state;

            public Buffer(int length, uint seed)
            {
                _samples = new float[length];
                _state = seed == 0 ? 1u : seed;
            }

            /// <summary>Aleatório em [0, 1) determinístico (xorshift32).</summary>
            public float Rand()
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (_state & 0xFFFFFF) / (float)0x1000000;
            }

            float Signed() => Rand() * 2f - 1f;

            /// <summary>Tom com frequência que desliza de <paramref name="f0"/> a <paramref name="f1"/>; ataque linear e queda exponencial.</summary>
            public void Tone(float start, float length, float f0, float f1, Wave wave, float amp, float attack, float decay)
            {
                int first = (int)(start * SampleRate);
                int count = Math.Min((int)(length * SampleRate), _samples.Length - first);
                double phase = 0.0;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    float u = count > 1 ? i / (float)(count - 1) : 0f;
                    double f = f0 + (f1 - f0) * u;
                    phase += f / SampleRate;
                    phase -= Math.Floor(phase);
                    float env = Math.Min(1f, t / Math.Max(attack, 1e-4f)) * (float)Math.Exp(-t * decay);
                    _samples[first + i] += Shape(wave, (float)phase) * amp * env;
                }
            }

            // Parciais de um sino pequeno de bronze (relação com a fundamental, amplitude, queda por segundo): zumbido grave,
            // terça menor e demais parciais inarmônicos; os agudos somem primeiro, o que dá o "tlim" e a cauda metálica.
            static readonly float[,] BellPartials =
            {
                { 0.5f, 0.40f, 2.6f },
                { 1.0f, 1.00f, 3.2f },
                { 1.19f, 0.55f, 3.8f },
                { 1.5f, 0.42f, 4.8f },
                { 2.0f, 0.50f, 5.8f },
                { 2.51f, 0.32f, 7.5f },
                { 3.2f, 0.22f, 10f },
                { 4.1f, 0.15f, 14f },
            };

            /// <summary>Uma batida de badalo: parciais do sino mais um estalo curto e brilhante do metal contra metal.</summary>
            public void BellStrike(float start, float hz, float strength)
            {
                for (int p = 0; p < BellPartials.GetLength(0); p++)
                {
                    float f = hz * BellPartials[p, 0];
                    float amp = BellPartials[p, 1] * strength;
                    float decay = BellPartials[p, 2];
                    Tone(start, 2f, f, f, Wave.Sine, amp, 0.0008f, decay);
                    // Gêmeo levemente desafinado nas duas primeiras: o batimento é o tremor típico de um sino real.
                    if (p == 1 || p == 4)
                        Tone(start, 2f, f * 1.006f, f * 1.006f, Wave.Sine, amp * 0.5f, 0.0008f, decay * 1.1f);
                }
                Noise(start, 0.02f, strength * 0.7f, 0.7f, 0.0003f, 170f);
            }

            /// <summary>Ruído passa-baixa (<paramref name="smooth"/> perto de 0 = grave e abafado, perto de 1 = chiado).</summary>
            public void Noise(float start, float length, float amp, float smooth, float attack, float decay)
            {
                int first = (int)(start * SampleRate);
                int count = Math.Min((int)(length * SampleRate), _samples.Length - first);
                float lp = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    lp += (Signed() - lp) * smooth;
                    float env = Math.Min(1f, t / Math.Max(attack, 1e-4f)) * (float)Math.Exp(-t * decay);
                    _samples[first + i] += lp * amp * env * 2f;
                }
            }

            /// <summary>Chuva em laço: chiado contínuo com leve ondulação; o fim é fundido com o começo para o laço não estalar.</summary>
            public void Rain()
            {
                int n = _samples.Length;
                int fade = SampleRate / 5;
                // Gera n + fade amostras contínuas e funde o excedente no começo: a última amostra emenda na primeira.
                var src = new float[n + fade];
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < src.Length; i++)
                {
                    lp += (Signed() - lp) * 0.55f;
                    lp2 += (lp - lp2) * 0.35f;
                    float swell = 0.85f + 0.15f * (float)Math.Sin(i * 2.0 * Math.PI * 3.0 / n);
                    src[i] = (lp * 0.6f + lp2 * 0.9f) * swell;
                }
                for (int i = 0; i < n; i++)
                {
                    float w = i < fade ? i / (float)fade : 1f;
                    _samples[i] = src[i] * w + (i < fade ? src[n + i] * (1f - w) : 0f);
                }
            }

            public float[] Finish(bool loop)
            {
                float max = 0f;
                for (int i = 0; i < _samples.Length; i++)
                    max = Math.Max(max, Math.Abs(_samples[i]));
                if (max > 1e-6f)
                {
                    float gain = Peak / max;
                    for (int i = 0; i < _samples.Length; i++)
                        _samples[i] *= gain;
                }
                // Som curto fecha com rampa para nunca terminar em degrau (estalo); o laço já é emendado em Rain().
                if (!loop)
                {
                    int ramp = Math.Min(64, _samples.Length);
                    for (int i = 0; i < ramp; i++)
                        _samples[_samples.Length - 1 - i] *= i / (float)ramp;
                }
                return _samples;
            }

            static float Shape(Wave wave, float p)
            {
                switch (wave)
                {
                    case Wave.Triangle: return p < 0.5f ? 4f * p - 1f : 3f - 4f * p;
                    case Wave.Square: return (p < 0.5f ? 1f : -1f) * 0.6f;
                    case Wave.Saw: return (2f * p - 1f) * 0.7f;
                    default: return (float)Math.Sin(p * 2.0 * Math.PI);
                }
            }
        }
    }
}
