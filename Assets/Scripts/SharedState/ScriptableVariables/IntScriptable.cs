using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IntScriptable", menuName = "ScriptableVariables/Int")]
public class IntScriptable : ScriptableVariable<int>
{
    public void Add(int amount) => v += amount;
    public void Subtract(int amount) => v -= amount;
}
