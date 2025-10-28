using System.Collections;
using UnityEngine;

namespace Helpers
{
    public abstract class MixedValue
    {
        protected MonoBehaviour monoHook;
        public float DefaultFadeInRate { get; private set; }
        public float DefaultFadeOutRate { get; private set; }
        public Easing.EaseType DefaultFadeInEase { get; private set; }
        public Easing.EaseType DefaultFadeOutEase { get; private set; }
        private Coroutine activeCoroutine;
        private bool CoroutineRunning => activeCoroutine != null;

        public MixedValue(
           MonoBehaviour monoHook,
           float fInR = 1f,
           float fOutR = 1f,
           Easing.EaseType fInEase = Easing.EaseType.Linear,
           Easing.EaseType fOutEase = Easing.EaseType.Linear
           )
        {
            this.monoHook = monoHook;
            DefaultFadeInRate = fInR; DefaultFadeOutRate = fOutR;
            DefaultFadeInEase = fInEase; DefaultFadeOutEase = fOutEase;
        }

        protected abstract void SetValue(float v);

        private void SetCoroutine(IEnumerator coroutine)
        {
            StopActiveCoroutine();
            activeCoroutine = monoHook.StartCoroutine(coroutine);
        }
        private void StopActiveCoroutine()
        {
            if (CoroutineRunning)
            {
                monoHook.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }
        private void SetValueAndStopCoroutine(float v)
        {
            StopActiveCoroutine();
            SetValue(v);
        }

        private IEnumerator FadeValue(float target, float rate, Easing.EaseType easeType)
        {
            float startVal = GetValue();
            float elapsedTime = 0f;
            float duration = Mathf.Abs(target - startVal) / rate;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float easedT = Easing.ApplyEasing(t, easeType);
                float newVal = Mathf.Lerp(startVal, target, easedT);
                SetValue(newVal);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            SetValue(target);
            StopActiveCoroutine();
        }

        #region PUBLIC_API
        public abstract float GetValue();

        public void StartFade(float target, float? rate = null, Easing.EaseType? easeType = null)
        {
            StopActiveCoroutine();
            float current_value = GetValue();
            if (current_value == target) return;
            float finalRate = rate ?? (target < current_value ? DefaultFadeOutRate : DefaultFadeInRate);
            Easing.EaseType finalEase = easeType ?? (target < current_value ? DefaultFadeOutEase : DefaultFadeInEase);

            SetCoroutine(FadeValue(target, finalRate, finalEase));
        }

        public void StartDefaultFadeOut()
        {
            StopActiveCoroutine();
            if (GetValue() == 0f) return;
            SetCoroutine(FadeValue(0, DefaultFadeOutRate, DefaultFadeOutEase));
        }

        public void SetValueAndStopFade(float v)
        {
            SetValueAndStopCoroutine(v);
        }
        #endregion
    }
}
