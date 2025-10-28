using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This IK requires no target, it will simply override the current bone
/// position and rotation.
/// 
/// When the repose IK has value, it will be executing upon that value in each IK cycle.
/// This means if you set say a value of 5 for X, it will continually be applying that value. So once you set it once,
/// that bone will be reposed to that value until you unset the value.
/// </summary>
public class IKRepose : IKElementHasValue
{
    public override Dictionary<Transform, IKSystem.IKTransformationWorld> CalculateIK()
    {
        Vector3 translationWorld = Vector3.zero;
        Quaternion rotationWorld = Quaternion.identity;

        switch (m_position)
        {
            case (ValueMode.NEW_WORLDSPACE):
                translationWorld = v_position - IKObject.transform.position;
                break;
            case (ValueMode.OFFSET_WORLDSPACE):
                translationWorld = v_position;
                break;
            case (ValueMode.OFFSET_LOCALSPACE):
                translationWorld = IKObject.transform.TransformDirection(v_position);
                break;
            case (ValueMode.NEW_LOCALSPACE):
                translationWorld = (IKObject.transform.parent != null)
                    ? IKObject.transform.parent.TransformPoint(v_position)
                    : v_position;
                break;
        }
        switch (m_rotation)
        {
            case ValueMode.NEW_WORLDSPACE:
                rotationWorld = v_rotation * Quaternion.Inverse(IKObject.transform.rotation);
                break;
            case ValueMode.OFFSET_WORLDSPACE:
                rotationWorld = v_rotation;
                break;
            case ValueMode.OFFSET_LOCALSPACE:
                rotationWorld = IKObject.transform.rotation * v_rotation * Quaternion.Inverse(IKObject.transform.rotation);
                break;
            case ValueMode.NEW_LOCALSPACE:
                // v_rotation is provided as the target rotation in local space.
                // First, convert it into world space.
                Quaternion targetWorldRotation = (IKObject.transform.parent != null)
                    ? IKObject.transform.parent.rotation * v_rotation
                    : v_rotation;
                // Now compute the delta rotation needed to reach the target.
                rotationWorld = targetWorldRotation * Quaternion.Inverse(IKObject.transform.rotation);
                break;
        }

        // Apply IK weights
        float totalWeight = GetTotalWeight();
        translationWorld *= totalWeight;
        rotationWorld = Quaternion.Slerp(Quaternion.identity, rotationWorld, totalWeight);

        IKSystem.IKTransformationWorld ikTW = new IKSystem.IKTransformationWorld(translationWorld, rotationWorld);

        Dictionary<Transform, IKSystem.IKTransformationWorld> ikR = new Dictionary<Transform, IKSystem.IKTransformationWorld>
        {
            { IKObject, ikTW }
        };
        return ikR;
    }
}
