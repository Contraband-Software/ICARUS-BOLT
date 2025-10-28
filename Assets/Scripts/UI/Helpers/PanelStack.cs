using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelStack : MonoBehaviour
{
    private Stack<StackablePanel> stack = new Stack<StackablePanel>();



    public void Push(StackablePanel panel)
    {
        if (Size() > 0 && Top() == panel) return;

        stack.Push(panel);
        Debug.Log("Pushed: " + panel.name);
        panel.OnBecomeTop?.Invoke();
    }

    /// <summary>
    /// Allows the panel a chance to stop the pop
    /// </summary>
    public void TryPop()
    {
        if (Size() == 0) return;

        Top().OnTryPop?.Invoke();
    }

    /// <summary>
    /// Forcefully pop the top element
    /// </summary>
    public void ForcePop()
    {
        if (Size() == 0) return;

        Top().OnPop?.Invoke();

        Debug.Log("Popped: " + Top().name);
        stack.Pop();

        if (Size() == 0) return;
        Top().OnBecomeTop.Invoke();
    }

    public int Size()
    {
        return stack.Count;
    }

    public void PopAllRecursive()
    {
        if(Size() == 0) return;

        while(Size() > 0)
        {
            ForcePop();
        }
    }

    public void Empty()
    {
        stack = new Stack<StackablePanel>();
    }

    public StackablePanel Top()
    {
        return stack.Peek();
    }
}
