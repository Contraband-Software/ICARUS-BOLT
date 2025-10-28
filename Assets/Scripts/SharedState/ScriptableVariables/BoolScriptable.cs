using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoolScriptable", menuName = "ScriptableVariables/Bool")]
public class BoolScriptable : ScriptableVariable<bool>
{
    public void Toggle() => v = !v;
}
