using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTwoJoint : IKElementHasTarget
{
    // Based on: https://theorangeduck.com/page/simple-two-joint

    // Using Hand/Elbow/Shoulder naming because its a lot fucking easier to understand than "a b c"

    // IKObject: hand, "c"
    public Transform elbowJoint; // "b"
    public Transform elbowHintTarget;
    public Transform shoulderJoint; // "a"
    public override void Initialize()
    {
        sideEffectedIKObjects.Add(elbowJoint);
        sideEffectedIKObjects.Add(shoulderJoint);
    }

    public override Dictionary<Transform, IKSystem.IKTransformationWorld> CalculateIK()
    {
        Vector3 a = shoulderJoint.position;
        Vector3 b = elbowJoint.position;
        Vector3 c = IKObject.position;
        Vector3 t = target.position;

        // Step 1: Extend/Contract the Joint Chain
        // - get lengths of vectors
        float eps = 0.01f; // Use to limit extension of joints to some minimum and maximum amount
        float len_ab = (b - a).magnitude;
        float len_cb = (b - c).magnitude;
        float len_at = Mathf.Clamp(
            (t - a).magnitude,
            eps,
            len_ab + len_cb - eps);

        // - get current interior angles of the shoulder and elbow joints
        float ac_ab_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(
            Vector3.Normalize(c - a),
            Vector3.Normalize(b - a)), -1, 1));
        float ba_bc_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(
            Vector3.Normalize(a - b),
            Vector3.Normalize(c - b)), -1, 1));

        // - Get desired interior angles
        // (Which produces vector from shoulder to hand with same length as the vector between shoulder and target
        float ac_ab_1 = Mathf.Acos(Mathf.Clamp(
            (len_cb * len_cb - len_ab * len_ab - len_at * len_at) / ((-2) * len_ab * len_at),
            -1, 1));
        float ba_bc_1 = Mathf.Acos(Mathf.Clamp(
            (len_at * len_at - len_ab * len_ab - len_cb * len_cb) / ((-2) * len_ab * len_cb),
            -1, 1));

        // - Compute dynamic bend axis from shoulder->elbow and shoulder->hint
        Vector3 hintDir = (elbowHintTarget.position - a).normalized;
        Vector3 axis0 = Vector3.Cross(b - a, hintDir).normalized;

        // - unconstrained joint deltas by finding difference of current -> desired
        Quaternion r_a = Quaternion.AngleAxis(
            Mathf.Rad2Deg * (ac_ab_1 - ac_ab_0),
            axis0);
        Quaternion r_b = Quaternion.AngleAxis(
            Mathf.Rad2Deg * (ba_bc_1 - ba_bc_0),
            axis0);

        // Step 2: Rotate the Hand into Place
        float ac_at_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(
            Vector3.Normalize(c - a),
            Vector3.Normalize(t - a)), -1, 1));
        Vector3 axis1 = Vector3.Normalize(Vector3.Cross(c - a, t - a));
        Quaternion r_a_2 = Quaternion.AngleAxis(Mathf.Rad2Deg * ac_at_0, axis1);
        Quaternion r_a_final = r_a * r_a_2;

        // Apply weights
        float finalWeight = GetTotalWeight();
        Quaternion r_a_final_w = Quaternion.Slerp(
            Quaternion.identity,
            r_a_final,
            finalWeight);
        Quaternion r_b_w = Quaternion.Slerp(
            Quaternion.identity,
            r_b,
            finalWeight);

        // Finalize
        IKSystem.IKTransformationWorld ikTW_shoulder = new IKSystem.IKTransformationWorld(r_a_final_w);
        IKSystem.IKTransformationWorld ikTW_elbow = new IKSystem.IKTransformationWorld(r_b_w);
        IKSystem.IKTransformationWorld ikTW_hand = new IKSystem.IKTransformationWorld();

        Dictionary<Transform, IKSystem.IKTransformationWorld> ikR = new Dictionary<Transform, IKSystem.IKTransformationWorld>
        {
            { shoulderJoint, ikTW_shoulder },
            { elbowJoint, ikTW_elbow },
            { IKObject, ikTW_hand }
        };

        return ikR;
    }
}
