using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using UnityEngine.UI;

public class InteractiveColorBlender : MonoBehaviour
{
    [SerializeField] private Graphic target; // Image, TMP, RawImage, etc.
    [SerializeField] private PalletteColorInteractive palette;

    private Coroutine activeCoroutine;
    private bool CoroutineRunning => activeCoroutine != null;
    public enum TransitionDirection
    {
        IN,
        OUT
    }

    private void SetCoroutine(IEnumerator coroutine)
    {
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(coroutine);
    }
    private void StopActiveCoroutine()
    {
        if (CoroutineRunning)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    private IEnumerator BlendColorRoutine(Color targetColor, float duration, Easing.EaseType easeType)
    {
        Color startColor = target.color;
        float elapsed = 0f;
        duration = Mathf.Max(0.0001f, duration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = Easing.ApplyEasing(t, easeType);
            target.color = Color.Lerp(startColor, targetColor, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // force final color
        target.color = targetColor;
        StopActiveCoroutine();
    }

    private void StartBlend(Color targetColor, float duration, Easing.EaseType easeType)
    {
        StopActiveCoroutine();
        SetCoroutine(BlendColorRoutine(targetColor, duration, easeType));
    }

    #region PUBLIC_API
    public void TransitionToColor(string colorName, TransitionDirection mode)
    {
        if (!palette.HasColorState(colorName))
        {
            Debug.LogWarning("GameObject " + gameObject.name + " That uses InteractiveMatBlender does not have " +
                "a color transition for: " + colorName);
            return;
        }

        PalletteColorInteractiveState state = palette.GetColorState(colorName);

        Easing.EaseType easeType = Easing.EaseType.Linear;
        float easeDuration = 1f;
        if (mode == TransitionDirection.IN)
        {
            easeType = state.easeIn.easeType;
            easeDuration = state.easeIn.easeDuration;
        }
        else if (mode == TransitionDirection.OUT)
        {
            easeType = state.easeOut.easeType;
            easeDuration = state.easeOut.easeDuration;
        }

        Color newColor = state.color;
        StartBlend(newColor, easeDuration, easeType);
    }
    #endregion
}
