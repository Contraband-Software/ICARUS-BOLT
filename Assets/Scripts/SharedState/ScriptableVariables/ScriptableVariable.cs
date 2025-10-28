using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableVariable<T> : ScriptableObject, ISerializationCallbackReceiver
{

    [SerializeField]
    private T _initialValue; // Disk value - never changes at runtime

    private T _runtimeValue; // Runtime value - this is what gets modified
    private bool hasBeenInitialized = false;

    [HideInInspector]
    public Action<T> OnValueChanged;

    public void OnAfterDeserialize()
    {
        InitializeRuntimeValue();
    }

    private void InitializeRuntimeValue()
    {
        _runtimeValue = _initialValue;
        hasBeenInitialized = true;
    }

    public void OnBeforeSerialize() 
    {
    }

    public T InitialValue => _initialValue;

    public T v
    {
        get
        {
            if (!hasBeenInitialized)
                InitializeRuntimeValue();
            return _runtimeValue;
        }
        set
        {
            if (!hasBeenInitialized)
                InitializeRuntimeValue();

            if (!_runtimeValue.Equals(value))
            {
                _runtimeValue = value;
                OnValueChanged?.Invoke(value);
            }
        }
    }

}
