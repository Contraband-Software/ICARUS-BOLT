using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// The aim of this IK is to make an IKObject act as if its the child of some other transform
/// 
/// Currently, I could only make it so that a sibling can act as a parent. I do this by 
/// looking at the difference in local rotation of the target "parent" from its base local rotation.
/// Because the IKObject and target "parent" are siblings in the same local space, mimicking the change
/// of ones local rotation will trivially make it look like the sibling is a child.
/// </summary>

public class IKSiblingAsParent : IKElementHasTarget
{
    public override bool IsAdditive => false;
    public override bool ForceBonePoseReset => true;

    private Quaternion basePoseLocalRotation;
    private Vector3 basePoseLocalPosition;

    private void Start()
    {
        basePoseLocalRotation = target.localRotation;
        basePoseLocalPosition = target.localPosition;
    }

    public override Dictionary<Transform, IKSystem.IKTransformationWorld> CalculateIK()
    {

        // Step 1: Calculate the local rotation difference from base pose local rotation
        Quaternion localRotationDeltaFromBasePose = target.localRotation * Quaternion.Inverse(basePoseLocalRotation);

        // Step 2: Calculate the local translation difference (relative to the base pose)
        Vector3 localPositionDeltaFromBasePose = target.localPosition - basePoseLocalPosition;

        // Step 3: Transform the translation by the rotation
        Vector3 transformedlocalPositionDeltaFromBasePose = localRotationDeltaFromBasePose * localPositionDeltaFromBasePose;

        // Step 4: Get translation in world space
        Vector3 worldSpaceTranslation = transform.parent != null
            ? transform.parent.TransformDirection(transformedlocalPositionDeltaFromBasePose)
            : transformedlocalPositionDeltaFromBasePose;

        // Step 5: Apply Weight
        float finalWeight = GetTotalWeight();
        Quaternion weightedRotation = Quaternion.Slerp(
            Quaternion.identity,
            localRotationDeltaFromBasePose,
            finalWeight);

        Vector3 weightedTranslation = worldSpaceTranslation * finalWeight;

        IKSystem.IKTransformationWorld ikTW = new IKSystem.IKTransformationWorld(weightedTranslation, weightedRotation);

        Dictionary<Transform, IKSystem.IKTransformationWorld> ikR = new Dictionary<Transform, IKSystem.IKTransformationWorld>
        {
            { IKObject, ikTW }
        };
        return ikR;
    }

    /// <summary>
    /// Set a new sibling as parent in runtime.
    /// Whatever the parents transform is at the time of being set,
    /// that will be considered the parents "base pose"
    /// </summary>
    /// <param name="target"></param>
    public void SetSiblingAsParent(Transform target)
    {
        this.target = target;
        basePoseLocalRotation = target.localRotation;
        basePoseLocalPosition = target.localPosition;
    }
}
