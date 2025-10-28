using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static IKSystem;

/// <summary>
/// An IK Element is the general class for some 
/// non-Animation Rigging IK element
/// </summary>
public abstract class IKElement : MonoBehaviour
{
    [SerializeField] private bool isAdditive = false;
    public virtual bool IsAdditive => isAdditive;
    public virtual bool ForceBonePoseReset => false;
    [Range(0f, 1f)]
    [SerializeField] private float weight = 1f;

    public Transform IKObject;
    // Some IK's may affect multiple objects than just the target object
    protected List<Transform> sideEffectedIKObjects = new List<Transform>();

    //IK's of same layer number all mix together. An IK with higher layer will explicity execute AFTER 
    //IK's of a lower layer have changed the bone's pose. Where the bone is now, compared to the original pose,
    //is now the "animation delta"
    [SerializeField] private int IKLayer = 1;
    private IKGroup ofIKGroup;

    private bool calculationCached = false;
    public bool CalculationCached => calculationCached;
    Dictionary<Transform, IKTransformationWorld> cachedCalculation;

    public virtual void Initialize() { }

    public Transform GetIKObject()
    {
        return IKObject;
    }
    public List<Transform> GetSideEffectedIKObjects()
    {
        return sideEffectedIKObjects;
    }

    public void SetWeight(float weight)
    {
        this.weight = Mathf.Clamp01(weight);
    }
    public float GetWeight()
    {
        return weight;
    }
    public int GetLayer()
    {
        return IKLayer;
    }
    public void SetOwningIKGroup(IKGroup ikgroup)
    {
        if (ofIKGroup != null)
        {
            throw new IKElementAlreadyInIKGroupException("This IKElement already belongs to an IKGroup.");
        }

        ofIKGroup = ikgroup;
    }
    /// <summary>
    /// Get the IKGroup to which this IKElement SHOULD solely belong to
    /// </summary>
    /// <returns></returns>
    public IKGroup GetOwningIKGroup()
    {
        if(ofIKGroup == null)
        {
            throw new NullReferenceException("IKElement on GameObject: " + gameObject.name + " does not belong to an IKGroup");
        }
        return ofIKGroup;
    }

    /// <summary>
    /// Return the IKElements weight multiplied by its owning groups weight
    /// </summary>
    /// <returns></returns>
    public float GetTotalWeight()
    {
        return GetWeight() * GetOwningIKGroup().GetWeight();
    }

    public Dictionary<Transform, IKTransformationWorld> GetCachedCalculation()
    {
        return cachedCalculation;
    }

    public void SetCachedCalculation(Dictionary<Transform, IKTransformationWorld> cachedCalculation)
    {
        this.cachedCalculation = cachedCalculation;
        calculationCached = true;
    }
    public void ReleaseCachedCalculation()
    {
        this.cachedCalculation = new Dictionary<Transform, IKTransformationWorld>();
        calculationCached = false;
    }

    public abstract Dictionary<Transform, IKSystem.IKTransformationWorld> CalculateIK();
}
