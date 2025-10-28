using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This IK makes a bone look at a target, making a given axis point at it. 
/// It uses the stableAxis to define the axis which will be kept stable in the rotation.
/// </summary>
public class IKAim : IKElementHasTarget
{
    public IKSystem.Axis aimAxis = IKSystem.Axis.Z;
    public IKSystem.Axis stableAxis = IKSystem.Axis.Y;

    public override Dictionary<Transform, IKSystem.IKTransformationWorld> CalculateIK()
    {
        // Calculate the target direction in world space
        Vector3 targetDirectionWorld = (target.position - IKObject.position).normalized;
        if (targetDirectionWorld == Vector3.zero)
        {
            return new Dictionary<Transform, IKSystem.IKTransformationWorld>()
            {
                {IKObject, new IKSystem.IKTransformationWorld() }
            };
        }

        // Determine the "aim axis" and its world space equivalent
        Vector3 aimAxisLocal = IKSystem.GetDirectionVectorFromAxis(aimAxis);
        Vector3 stableAxisLocal = IKSystem.GetDirectionVectorFromAxis(stableAxis);
        Vector3 currentAimAxisWorld = IKObject.TransformDirection(aimAxisLocal);
        Vector3 stableAxisWorld = IKObject.TransformDirection(stableAxisLocal);

        // Project the target direction if the stabilizing axis is meaningful
        Vector3 finalTargetDirection = aimAxis == stableAxis
            ? targetDirectionWorld
            : Vector3.ProjectOnPlane(targetDirectionWorld, stableAxisWorld).normalized;

        // Use Quaternion.FromToRotation to calculate the rotation needed to align the aim axis with the projected target
        Quaternion rotationToTarget = Quaternion.FromToRotation(currentAimAxisWorld, finalTargetDirection);

        // Apply this IKElement weight to rotation, and its IKGroup weight
        float finalWeight = GetTotalWeight();
        Quaternion weightedRotationToTarget = Quaternion.Slerp(
            Quaternion.identity,
            rotationToTarget,
            finalWeight);

        IKSystem.IKTransformationWorld ikTW = new IKSystem.IKTransformationWorld(weightedRotationToTarget);

        // Optional Debugging
        Debug.DrawRay(IKObject.position, targetDirectionWorld * 4f, Color.cyan);  // Target direction
        Debug.DrawRay(IKObject.position, currentAimAxisWorld * 4f, Color.yellow); // Current aim axis
        Debug.DrawRay(IKObject.position, stableAxisWorld * 4f, Color.magenta); // Stabilizing axis
        Debug.DrawRay(IKObject.position, finalTargetDirection * 4f, Color.blue); // Projected target direction

        Dictionary<Transform, IKSystem.IKTransformationWorld> ikR = new Dictionary<Transform, IKSystem.IKTransformationWorld>()
        {
            { IKObject, ikTW }
        };
        return ikR;
    }
}
