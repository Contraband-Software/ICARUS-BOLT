using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

[System.Serializable]
public struct PalleteColorInteractiveStateTransition
{
    public Easing.EaseType easeType;
    public float easeDuration;
}


[System.Serializable]
public struct PalletteColorInteractiveState
{
    public string colorName;
    public Color color;
    public PalleteColorInteractiveStateTransition easeIn;
    public PalleteColorInteractiveStateTransition easeOut;
}

[CreateAssetMenu(
    fileName = "PalletteColorInteractive",
    menuName = "UI/InteractiveColor/PalletteColorInteractive",
    order = 1)]
public class PalletteColorInteractive : ScriptableObject
{
    [SerializeField] List<PalletteColorInteractiveState> colorStates = new();

    public bool HasColorState(string colorName)
    {
        return colorStates.Exists(t => t.colorName == colorName);
    }

    public PalletteColorInteractiveState GetColorState(string colorName)
    {
        return colorStates.Find(t => t.colorName == colorName);
    }
}
