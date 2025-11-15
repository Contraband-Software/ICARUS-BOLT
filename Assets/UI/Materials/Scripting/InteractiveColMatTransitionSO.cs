using Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct InteractiveColMatTransition
{
    public string colorName;
    public Easing.EaseType easeInType;
    public float easeInDuration;
    public Easing.EaseType easeOutType;
    public float easeOutDuration;
}

[CreateAssetMenu(
    fileName = "InteractiveColMatTransitions",
    menuName = "UI/InteractiveColor/TransitionSO",
    order = 1)]

public class InteractiveColMatTransitionSO : ScriptableObject
{
    [SerializeField] List<InteractiveColMatTransition> transitions = new();

    public bool HasTransition(string colorName)
    {
        return transitions.Exists(t => t.colorName == colorName);
    }

    public InteractiveColMatTransition GetTransition(string colorName)
    {
        return transitions.Find(t => t.colorName == colorName);
    }
}
