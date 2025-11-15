using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Helpers;
using System;

public class InteractiveMatBlender : MonoBehaviour
{
    [SerializeField] Image Element;
    [SerializeField] InteractiveColMatTransitionSO TransitionMap;

    private Coroutine activeCoroutine;
    private bool CoroutineRunning => activeCoroutine != null;

    #region INTERNAL

    private Material mat;
    private void Awake()
    {
        if (Element != null && Element.material != null)
        {
            // Clone the material so this Image has its own material instance
            Element.material = new Material(Element.material);
            mat = Element.material;
        }
    }
    void SetBlendValue(Material mat, float v)
    {
        mat.SetFloat("_BlendValue", v);
    }
    float GetBlendValue(Material mat)
    {
        return mat.GetFloat("_BlendValue");
    }

    void SetCurrentColor(Material mat, Color color)
    {
        mat.SetColor("_CurrentColor", color);
    }
    Color GetCurrentColor(Material mat)
    {
        return mat.GetColor("_CurrentColor");
    }

    void SetTargetColor(Material mat, Color color)
    {
        mat.SetColor("_TargetColor", color);
    }

    Color GetTargetColor(Material mat)
    {
        return mat.GetColor("_TargetColor");
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

    public enum TransitionDirection
    {
        IN,
        OUT
    }

    private IEnumerator BlendValue(Material mat, float duration, Easing.EaseType easeType)
    {
        float startVal = GetBlendValue(mat);
        float elapsedTime = 0f;
        float target = 1f;

        // Avoid divide-by-zero or negative duration
        duration = Mathf.Max(0.0001f, duration);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float easedT = Easing.ApplyEasing(t, easeType);
            float newVal = Mathf.Lerp(startVal, target, easedT);

            SetBlendValue(mat, newVal);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final value
        SetBlendValue(mat, target);
        StopActiveCoroutine();
    }

    private void StartBlend(Material mat, float duration, Easing.EaseType easeType)
    {
        StopActiveCoroutine();
        SetCoroutine(BlendValue(mat, duration, easeType));
    }

    #endregion

    #region PUBLIC_API
    public void TransitionToColor(string colorName, TransitionDirection mode)
    {
        if (!TransitionMap.HasTransition(colorName))
        {
            Debug.LogWarning("GameObject " + gameObject.name + " That uses InteractiveMatBlender does not have " +
                "a color transition for: " + colorName);
            return;
        }

        if(!mat.HasColor(colorName))
        {
            Debug.LogWarning("GameObject " + gameObject.name + " That uses Interactive Color material " +
                "does not have defined color: " + colorName);
            return;
        }

        StopActiveCoroutine();

        Color currentTargetColor = GetTargetColor(mat);
        Color currentColor = GetCurrentColor(mat);
        float currentBlendValue = GetBlendValue(mat);
        Color liveColor = Color.Lerp(currentColor, currentTargetColor, currentBlendValue);
        Color newTargetColor = mat.GetColor(colorName);

        SetCurrentColor(mat, liveColor);
        SetBlendValue(mat, 0f);
        if (newTargetColor == liveColor)
        {
            return;
        }
        SetTargetColor(mat, newTargetColor);

        InteractiveColMatTransition transition = TransitionMap.GetTransition(colorName);
        Easing.EaseType easeType = Easing.EaseType.Linear;
        float easeDuration = 1f;
        if(mode == TransitionDirection.IN)
        {
            easeType = transition.easeInType;
            easeDuration = transition.easeInDuration;
        }
        else if(mode == TransitionDirection.OUT)
        {
            easeType = transition.easeOutType;
            easeDuration = transition.easeOutDuration;
        }

        StartBlend(mat, easeDuration, easeType);
    }
    #endregion
}
