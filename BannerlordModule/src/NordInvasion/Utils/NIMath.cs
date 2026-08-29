using System;

namespace NordInvasion.Utils
{
    /// <summary>
    /// Вспомогательные math-функции. MathF/Math.Clamp недоступны на net472.
    /// </summary>
    public static class NIMath
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
