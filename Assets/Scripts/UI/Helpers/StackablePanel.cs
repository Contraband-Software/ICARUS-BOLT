using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StackablePanel : MonoBehaviour
{
    [SerializeField] private PanelStack stack;

    public UnityEvent OnBecomeTop = new UnityEvent();
    public UnityEvent OnTryPop = new UnityEvent();
    public UnityEvent OnPop = new UnityEvent();

    public void PushSelf()
    {
        stack.Push(this);
    }
}
