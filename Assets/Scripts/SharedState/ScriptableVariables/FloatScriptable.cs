using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatScriptable", menuName = "ScriptableVariables/Float")]
public class FloatScriptable : ScriptableVariable<float>
{
    public void Add(float amount) => v += amount;
    public void Subtract(float amount) => v -= amount;
}
