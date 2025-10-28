using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKGroup : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float weight = 1f;

    public List<IKElement> elements;

    private void Start()
    {
        foreach(IKElement element in elements)
        {
            element.SetOwningIKGroup(this);
        }
    }

    public void SetWeight(float weight)
    {
        this.weight = Mathf.Clamp01(weight);
    }
    public float GetWeight()
    {
        return weight;
    }

    public List<IKElement> GetIKElements()
    {
        return elements;
    }
}
