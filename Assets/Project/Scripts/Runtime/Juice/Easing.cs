using UnityEngine;

namespace Fabula.Incrementant.Juice
{
    /// <summary>Curvas de animação em [0,1] → [0,1] (com overshoot nas curvas Back/Elastic). Lógica pura.</summary>
    public static class Easing
    {
        public delegate float Fn(float t);

        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float InQuad(float t) => t * t;

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

        public static float OutBack(float t)
        {
            const float s = 1.70158f;
            float u = t - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c) + 1f;
        }
    }
}
