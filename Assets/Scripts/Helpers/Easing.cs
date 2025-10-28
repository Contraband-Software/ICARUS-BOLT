using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers
{
    public class Easing{
        public enum EaseType
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseInExpo,
            EaseOutExpo,
            EaseInOutExpo
        }

        public static float ApplyEasing(float t, EaseType easeType)
        {

            switch (easeType)
            {
                // Quadratic Easing (t^2)
                case EaseType.EaseInQuad:
                    return t * t;
                case EaseType.EaseOutQuad:
                    return 1 - (1 - t) * (1 - t);
                case EaseType.EaseInOutQuad:
                    return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

                // Cubic Easing (t^3)
                case EaseType.EaseInCubic:
                    return t * t * t;
                case EaseType.EaseOutCubic:
                    return 1 - Mathf.Pow(1 - t, 3);
                case EaseType.EaseInOutCubic:
                    return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

                // Exponential Easing
                case EaseType.EaseInExpo:
                    return (t == 0) ? 0 : Mathf.Pow(2, 10 * (t - 1));
                case EaseType.EaseOutExpo:
                    return (t == 1) ? 1 : 1 - Mathf.Pow(2, -10 * t);
                case EaseType.EaseInOutExpo:
                    if (t == 0) return 0;
                    if (t == 1) return 1;
                    return t < 0.5
                        ? Mathf.Pow(2, 20 * t - 10) / 2
                        : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
                default:
                    return t;
            }
        }
    }
}

