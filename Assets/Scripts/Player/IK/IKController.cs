using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Helpers;
using Player;
using static IKController;

/// <summary>
/// The IK Controller is responsible for determining which IKElements
/// are active and passing necessary data into them.
/// This includes also controlling Animation Rigging Rigs
/// 
/// GLOSSARY:
/// IKGroup (Class): Custom IK version of "Rig"
/// Rig (Class): Animation Rigging package IK Rig
/// </summary>
/// 

public class IKController : MonoBehaviour
{
    public enum IKRig
    {
        Idle,
        LegsLook,
        HeadLook,
        TorsoLook,
        GliderRotations,
        GliderArms,
        JetpackTilt,
        MantleArms
    }

    public enum IKRigGroups
    {
        GENERAL
    }

    public struct RotationStep
    {
        public Quaternion rotation;
        public Vector3 direction;
        public float signedAngleUsed;

        public RotationStep(Quaternion r, Vector3 d, float angle)
        {
            rotation = r;
            direction = d;
            signedAngleUsed = angle;
        }
    }

    [Header("Component Refs")]
    [SerializeField] private IKRigBlendMixer ikRigMixer;

    [Header("Legs")]
    [SerializeField] private IKAim legsLookIK;

    [Header("Head")]
    [SerializeField] private IKAim headLookIK;

    [Header("Torso")]
    [SerializeField] private IKAim torsoLookIK;

    [Header("Glider")]
    [SerializeField] private IKRepose gliderCentreReposeIK;
    [SerializeField] private IKRepose gliderLeftReposeIK;
    [SerializeField] private IKRepose gliderRightReposeIK;

    [Header("Glider Arms")]
    [SerializeField] private IKTwoJoint gliderLeftArmTwoJointIK;
    [SerializeField] private IKTwoJoint gliderRightArmTwoJointIK;
    [SerializeField] private Transform leftArmHint;
    [SerializeField] private Transform rightArmHint;

    private Vector3 handBasePosLoc = new Vector3(-0.0031266f, -0.004941f, 0.00513f);
    private Vector3 handHighPosLoc = new Vector3(-0.0135f, -0.0027f, 0.00675f);
    private Vector3 handLowPosLoc = new Vector3(0.00054f, 0.002052f, 0.00594f);

    private Vector3 hintBasePosLoc = new Vector3(0.0015255f, 0.006912f, 0.005319f);
    private Vector3 hintHighPosLoc = new Vector3(0.00027f, 0.006912f, 0f);
    private Vector3 hintLowPosLoc = new Vector3(0.0081f, 0.006912f, 0.00405f);

    [Header("Jetpack Tilt")]
    [SerializeField] private IKRepose jetpackTiltBodyReposeIK;
    private Coroutine jetpackDecayCoroutine;
    private float baseTiltSpeed = 1f;

    [Header("MantleArms")]
    [SerializeField] private IKTwoJoint mantleLeftArmTwoJointIK;
    [SerializeField] private IKTwoJoint mantleRightArmTwoJointIK;
    [SerializeField] private Transform mantleLeftArmHint;
    [SerializeField] private Transform mantleRightArmHint;
    private Vector3 mantleHandHintBasePosLoc = new Vector3(0.00054f, 0.00605f, 0.00448f);
    private Vector3 mantleHandTarget_postJump = new Vector3(0.00077f, 0.00145f, 0.00674f);

    // Internals
    private Vector3 holdLegTargetPos;
    private Vector3 holdTorsoTargetPos;
    private bool legsResetting;

    private Vector3 current_HeadLookTargetDirectionLocal;
    private Vector3 current_LegLookTargetDirectionLocal;
    private Vector3 current_TorsoLookTargetDirectionLocal;
    private Vector3 current_LegLookTargetDirectionWorld;
    private Vector3 current_TorsoLookTargetDirectionWorld;

    private Vector3 current_TorsoLookTargetDirectionLocalToHeadIK;
    private Vector3 current_LegTargetDirectionLocalToHeadIK;

    private float signedAngleBetweenHeadAndTorsoTargets;
    private float signedAngleBetweenHeadAndLegsTargets;
    private float signedAngleBetweenTorsoAndLegsTargets;

    private Vector3 current_GliderRotateToVelocityTargetDir = Vector3.zero;

    public void Initialize()
    {
        ikRigMixer.Initialize();
    }

    /// <summary>
    /// Declare which Rig to fade in
    /// </summary>
    public void FadeInRig(IKRig ikRig, float? fadeInRate = null, Easing.EaseType? easeType = null)
    {
        ikRigMixer.StartFade(IKRigGroups.GENERAL, ikRig, 1f, fadeInRate, easeType);
    }

    /// <summary>
    /// Declare which Rig to fade out
    /// </summary>
    public void FadeOutRig(IKRig ikRig, float? fadeOutRate = null, Easing.EaseType? easeType = null)
    {
        ikRigMixer.StartFade(IKRigGroups.GENERAL, ikRig, 0f, fadeOutRate, easeType);
    }

    /// <summary>
    /// Hard set a rigs weight
    /// </summary>
    /// <param name="ikRig"></param>
    /// <param name="weight"></param>
    public void SetRigWeight(IKRig ikRig, float weight)
    {
        ikRigMixer.SetComponentValueAndStopFade(IKRigGroups.GENERAL, ikRig, weight);
    }

    /// <summary>
    /// Set rig to weight 0 and deactivate it
    /// </summary>
    /// <param name="ikRig"></param>
    public void DeactivateRig(IKRig ikRig)
    {
        SetRigWeight(ikRig, 0f);
    }
    public void ActivateRig(IKRig ikRig)
    {
        SetRigWeight(ikRig, 1f);
    }

    /// <summary>
    /// Calculate rotation targets for legs, torso, and head,
    /// and control interpolation for each element to the target
    /// </summary>
    public void IKRun(
        Vector2 moveInput,
        Rigidbody playerRb,
        Transform playerTransform
        )
    {
        Vector3 localVelocity = playerTransform.InverseTransformDirection(playerRb.velocity);

        UpdateCommonMath();
        // NOTE: This IK assumes that the rigs follow the convention:
        // - Group
        // -- Constraint
        // --- Target
        // Where all groups are properly aligned

        // world direction of players local forward axis
        Vector3 worldForward = playerTransform.TransformDirection(Vector3.forward);

        // We as a whole rotate in the direction of our local velocity
        // -- LEGS
        /// Hard target
        Vector3 legTargetDirectionLocal = new Vector3(moveInput.x, 0, moveInput.y);
        if (legTargetDirectionLocal == Vector3.zero)
        {
            legTargetDirectionLocal = Vector3.forward;
        }

        if (legTargetDirectionLocal.z < 0)
        {
            legTargetDirectionLocal.x *= -1f;
            legTargetDirectionLocal.z *= -1f;
        }
        if (legTargetDirectionLocal.x != 0 && legTargetDirectionLocal.z == 0)
        {
            legTargetDirectionLocal = Vector3.forward;
        }
        legTargetDirectionLocal.Normalize();

        // Take velocity that we are running at, make the targetdirection have the same
        // component ratio as our localVelocity
        localVelocity.y = 0f;
        if(Mathf.Abs(moveInput.x) > 0 && Mathf.Abs(moveInput.y) > 0)
        {
            legTargetDirectionLocal = new Vector3(
                legTargetDirectionLocal.x * Mathf.Abs(localVelocity.x),
                0f,
                legTargetDirectionLocal.z * Mathf.Abs(localVelocity.z)
                );
            legTargetDirectionLocal.Normalize();
        }

        /// Check velocity threshold
        float velocityThreshold = 3f;
        if (localVelocity.magnitude < velocityThreshold)
        {
            legTargetDirectionLocal = current_LegLookTargetDirectionLocal;
        }
        Vector3 legEasedRotatedLocalPos =
            EasedRotationToAlignToTarget_GetNextStep(
            current_LegLookTargetDirectionLocal,
            legTargetDirectionLocal,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            300f,
            30f).direction;
        legsLookIK.SetTargetPositionByLocalDirection(legEasedRotatedLocalPos, 5f);


        // -- HEAD
        Vector3 headTargetDirectionLocal = headLookIK.target.InverseTransformDirection(worldForward);
        headTargetDirectionLocal.y = 0f;
        headTargetDirectionLocal.Normalize();

        Vector3 headEasedRotatedLocalPos =
            EasedRotationToAlignToTarget_GetNextStep(
            current_HeadLookTargetDirectionLocal,
            headTargetDirectionLocal,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            300f,
            30f).direction;
        headLookIK.SetTargetPositionByLocalDirection(headEasedRotatedLocalPos, 5f);


        // -- TORSO
        // Calculate the midpoint between the new head and leg target directions (disregarding Y axis)
        Vector3 torsoTargetDirectionLocal = new Vector3(
            (legEasedRotatedLocalPos.x * 0.3f + headEasedRotatedLocalPos.x * 0.7f),
            0f, // Disregard the Y axis
            (legEasedRotatedLocalPos.z * 0.3f + headEasedRotatedLocalPos.z * 0.7f)
        );
        Vector3 torsoEasedRotationLocalPos =
            EasedRotationToAlignToTarget_GetNextStep(
            current_TorsoLookTargetDirectionLocal,
            torsoTargetDirectionLocal,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            300f,
            90f).direction;
        torsoLookIK.SetTargetPositionByLocalDirection(torsoEasedRotationLocalPos, 5f);
    }

    // NOTE: we should apply the idleIk when we are "stationary" in Run (on slope eg)
    public void IKIdle(
        Transform playerTransform
        )
    {
        UpdateCommonMath();

        // Reset Torso to Legs if head near legs
        if (Mathf.Abs(signedAngleBetweenHeadAndLegsTargets) < 30)
        {
            // torso go back towards legs
            Vector3 torsoEasedRotatedLocalPos =
                EasedRotationToAlignToTarget_GetNextStep(
                current_TorsoLookTargetDirectionLocalToHeadIK,
                current_LegTargetDirectionLocalToHeadIK,
                Vector3.up,
                Easing.EaseType.EaseOutExpo,
                90f,
                30f).direction.normalized * 5f;
            holdTorsoTargetPos = headLookIK.transform.TransformPoint(torsoEasedRotatedLocalPos);
        }
        else
        {
            // Torso follows close behind head, aiming to be between head and legs
            Vector3 torsoTargetDirectionLocal =
                (current_LegTargetDirectionLocalToHeadIK * 0.3f + current_HeadLookTargetDirectionLocal * 0.7f).normalized;

            Vector3 easedRotatedLocalPos =
                EasedRotationToAlignToTarget_GetNextStep(
                current_TorsoLookTargetDirectionLocalToHeadIK,
                torsoTargetDirectionLocal,
                Vector3.up,
                Easing.EaseType.EaseOutExpo,
                150f,
                100f).direction.normalized * 5f;
            holdTorsoTargetPos = headLookIK.transform.TransformPoint(easedRotatedLocalPos);
        }

        // Reset Legs to Head if Head > X deg from Legs
        if(Mathf.Abs(signedAngleBetweenHeadAndLegsTargets) > 110)
        {
            legsResetting = true;
        }
        // Stop Resetting Legs
        if(legsResetting && Mathf.Abs(signedAngleBetweenHeadAndLegsTargets) < 3)
        {
            legsResetting = false; 
        }

        if(legsResetting)
        {
            // Legs go back towards head
            Vector3 legsEasedRotatedLocalPos =
                EasedRotationToAlignToTarget_GetNextStep(
                current_LegTargetDirectionLocalToHeadIK,
                current_HeadLookTargetDirectionLocal,
                Vector3.up,
                Easing.EaseType.EaseOutExpo,
                250f,
                130f).direction;
            holdLegTargetPos = headLookIK.transform.TransformPoint(legsEasedRotatedLocalPos);

            // Torso goes back towards head
            Vector3 torsoEasedRotatedLocalPos =
                EasedRotationToAlignToTarget_GetNextStep(
                current_TorsoLookTargetDirectionLocalToHeadIK,
                current_HeadLookTargetDirectionLocal,
                Vector3.up,
                Easing.EaseType.EaseOutExpo,
                350f,
                130f).direction.normalized * 5f;
            holdTorsoTargetPos = headLookIK.transform.TransformPoint(torsoEasedRotatedLocalPos);
        }

        // Prohibit Head from being > 70 deg from torso
        // We do this by decreasing the weight of HeadLook, so the target actually stays
        // in place for us while the head appears to not looking at it directly.

        // This works but needs some smoothing

        float absHeadTorsoAngle = Mathf.Abs(signedAngleBetweenHeadAndTorsoTargets);
        float headThresholdAngle = 30f;
        float headMaxMisalignmentAngle = 180f;
        if (absHeadTorsoAngle <= headThresholdAngle)
        {
            SetRigWeight(IKRig.HeadLook, 1f);
        }
        else
        {
            float normalized = Mathf.Clamp01((
                absHeadTorsoAngle - headThresholdAngle) 
                / (headMaxMisalignmentAngle - headThresholdAngle));
            float easedValue = Easing.ApplyEasing(normalized, Easing.EaseType.Linear);

            // Use the eased value to gradually decrease the weight from 1 to 0.
            // When easedValue is 0, weight is 1; when easedValue is 1, weight is 0.

            // Perhaps dont use lerping?

            // try the comment below instead (commented Wednesday):
            // Direct mapping: When easedValue is 0, weight = 1; when easedValue is 1, weight = 0.3.
            float weight = 1f - 0.7f * easedValue;

            //float weight = Mathf.Lerp(1f, 0.3f, easedValue);
            SetRigWeight(IKRig.HeadLook, weight);
        }



        legsLookIK.SetTargetPositionByWorldPosition(holdLegTargetPos);
        torsoLookIK.SetTargetPositionByWorldPosition(holdTorsoTargetPos);
    }

    public void IKIdleEntry()
    {
        // We will move targets further out so that we dont move
        // past them on landing into Idle.
        legsLookIK.target.localPosition = legsLookIK.target.localPosition.normalized * 10f;
        torsoLookIK.target.localPosition = torsoLookIK.target.localPosition.normalized * 10f;
        holdLegTargetPos = legsLookIK.target.position;
        holdTorsoTargetPos = torsoLookIK.target.position;
        legsResetting = false;
    }

    public void IKGliderEntry(
        Vector3 playerDirection,
        Vector3 velocity)
    {
        playerDirection.Normalize();

        current_GliderRotateToVelocityTargetDir = gliderCentreReposeIK.GetIKObject().transform.up;
        current_GliderRotateToVelocityTargetDir = NormalizeHorizontal(current_GliderRotateToVelocityTargetDir);

        current_LegLookTargetDirectionWorld = legsLookIK.target.position - legsLookIK.transform.position;
        current_LegLookTargetDirectionWorld = NormalizeHorizontal(current_LegLookTargetDirectionWorld);
        current_TorsoLookTargetDirectionWorld = torsoLookIK.target.position - torsoLookIK.transform.position;
        current_TorsoLookTargetDirectionWorld = NormalizeHorizontal(current_TorsoLookTargetDirectionWorld);

        // This is to stop any state left over from previous glide
        gliderCentreReposeIK.DisregardPositionValue();
        gliderCentreReposeIK.DisregardRotationValue();
        gliderLeftReposeIK.DisregardPositionValue();
        gliderLeftReposeIK.DisregardRotationValue();
        gliderRightReposeIK.DisregardPositionValue();
        gliderRightReposeIK.DisregardRotationValue();

        Vector3 leftHintPosLoc = hintBasePosLoc;
        leftHintPosLoc.z *= -1f;
        leftArmHint.localPosition = leftHintPosLoc;
        rightArmHint.localPosition = hintBasePosLoc;
        Vector3 leftHandPosLoc = handBasePosLoc;
        leftHandPosLoc.z *= -1f;
        gliderLeftArmTwoJointIK.SetTargetPositionByLocalPosition(leftHandPosLoc);
        gliderRightArmTwoJointIK.SetTargetPositionByLocalPosition(handBasePosLoc);
    }

    /// <summary>
    /// Run this IK control from FixedUpdate
    /// </summary>
    /// <param name="playerDirection"></param>
    /// <param name="velocity"></param>
    /// <param name="rb"></param>
    public void IKGlider(
        Vector3 playerDirection,
        Vector3 velocity,
        Rigidbody rb,
        float bankingPower
        )
    {
        playerDirection = NormalizeHorizontal(playerDirection);
        velocity = NormalizeHorizontal(velocity);
        bankingPower = Mathf.Clamp(bankingPower, -1f, 1f);

        // Dont execute if velocity is zero
        if (velocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // -- Root rotated to velocity
        // rotation of root dictates the direction bones will rotate (clockwise/counterclockwise)
        RotationStep rootRotationStep = EasedRotationToAlignToTarget_GetNextStep(
            playerDirection,
            velocity,
            Vector3.up,
            Easing.EaseType.EaseOutQuad,
            300f,
            180f);
        float rootRotationDirection = Mathf.Sign(rootRotationStep.signedAngleUsed);
        RotateRootPlayer(rb, rootRotationStep.rotation);

        // Legs and Torso follow camera rotation direction if its a long rotation to make
        float legAngleToTarget = Vector3.SignedAngle(current_LegLookTargetDirectionWorld, velocity.normalized, Vector3.up);
        float torsoAngleToTarget = Vector3.SignedAngle(current_TorsoLookTargetDirectionWorld, velocity.normalized, Vector3.up);

        // -- Legs delayed rotation to face velocity
        current_LegLookTargetDirectionWorld
            = EasedRotationToAlignToTarget_GetNextStep(
            current_LegLookTargetDirectionWorld,
            velocity.normalized,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            200f,
            180f,
            Mathf.Abs(legAngleToTarget) > 120f,
            rootRotationDirection).direction;
        current_LegLookTargetDirectionWorld.Normalize();

        // -- Torso delayed rotation to face velocity
        current_TorsoLookTargetDirectionWorld
            = EasedRotationToAlignToTarget_GetNextStep(
            current_TorsoLookTargetDirectionWorld,
            velocity.normalized,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            250f,
            180f,
            Mathf.Abs(torsoAngleToTarget) > 120f,
            rootRotationDirection).direction;
        current_TorsoLookTargetDirectionWorld.Normalize();

        // Set Targets
        Vector3 legsNewLookTargetWorldPos = legsLookIK.transform.position + (current_LegLookTargetDirectionWorld * 5f);
        legsLookIK.SetTargetPositionByWorldPosition(legsNewLookTargetWorldPos);
        Vector3 torsoNewLookTargetWorldPos = torsoLookIK.transform.position + (current_TorsoLookTargetDirectionWorld * 5f);
        torsoLookIK.SetTargetPositionByWorldPosition(torsoNewLookTargetWorldPos);


        // -- Tilt Glider to Banking
        Transform centreT = gliderCentreReposeIK.GetIKObject().transform;
        Transform leftT = gliderLeftReposeIK.GetIKObject().transform;
        Transform rightT = gliderRightReposeIK.GetIKObject().transform;

        float maxBankAngle = 25f; // Adjust as desired
        float currentBankAngle = -bankingPower * maxBankAngle;
        Quaternion bankRot = Quaternion.AngleAxis(currentBankAngle, centreT.up);

        // Centre Bone
        gliderCentreReposeIK.DisregardPositionValue();
        gliderCentreReposeIK.SetValueRotationAsWorldspaceOffset(bankRot);

        // Left Bone
        Vector3 centreToLeftOffset = leftT.position - centreT.position;
        Vector3 rotatedLeftOffset = bankRot * centreToLeftOffset;
        Vector3 newLeftOffset = rotatedLeftOffset - centreToLeftOffset;
        gliderLeftReposeIK.SetValuePositionAsWorldspaceOffset(newLeftOffset);
        gliderLeftReposeIK.SetValueRotationAsWorldspaceOffset(bankRot);

        // Right Bone
        Vector3 centreToRightOffset = rightT.position - centreT.position;
        Vector3 rotatedRightOffset = bankRot * centreToRightOffset;
        Vector3 newRightOffset = rotatedRightOffset - centreToRightOffset;
        gliderRightReposeIK.SetValuePositionAsWorldspaceOffset(newRightOffset);
        gliderRightReposeIK.SetValueRotationAsWorldspaceOffset(bankRot);
    }

    public void IKGliderArms(float bankingPower, float horizontalInput)
    {

        // IMPROVEMENT:
        // Use our input to determine where the arms are headed
        // This way the arms can go up/down before the glider tilts
        float motionIntent = horizontalInput;
        motionIntent = motionIntent == 0 ? 0 : Mathf.Sign(motionIntent);

        Vector3 currentLeftHandPosLoc = gliderLeftArmTwoJointIK.target.localPosition;
        Vector3 currentRightHandPosLoc = gliderRightArmTwoJointIK.target.localPosition;
        Vector3 currentLeftHintPosLoc = leftArmHint.localPosition;
        Vector3 currentRightHintPosLoc = rightArmHint.localPosition;

        Vector3 targetLeftHandPosLoc;
        Vector3 targetRightHandPosLoc;
        Vector3 targetLeftHintPosLoc;
        Vector3 targetRightHintPosLoc;

        Vector3 newLeftHandPosLoc;
        Vector3 newRightHandPosLoc;

        Vector3 newLeftHintPosLoc;
        Vector3 newRightHintPosLoc;

        float standardSpeed = 0.05f;
        float maxDistance = Vector3.Distance(handLowPosLoc, handHighPosLoc);
        float maxDistanceHint = Vector3.Distance(hintLowPosLoc, hintHighPosLoc);
        float standardSpeedHint = standardSpeed * (maxDistanceHint / maxDistance);

        if (motionIntent == 0)
        {
            // Arms both reset to base
            targetLeftHandPosLoc = handBasePosLoc;
            targetRightHandPosLoc = handBasePosLoc;
            targetLeftHintPosLoc = hintBasePosLoc;
            targetRightHintPosLoc = hintBasePosLoc;
        }
        else if(motionIntent > 0)
        {
            // Left arm up, right arm down
            targetLeftHandPosLoc = handHighPosLoc;
            targetRightHandPosLoc = handLowPosLoc;
            targetLeftHintPosLoc = hintHighPosLoc;
            targetRightHintPosLoc = hintLowPosLoc;
        }
        else
        {
            // Left arm down, right arm up
            targetLeftHandPosLoc = handLowPosLoc;
            targetRightHandPosLoc = handHighPosLoc;
            targetLeftHintPosLoc = hintLowPosLoc;
            targetRightHintPosLoc = hintHighPosLoc;
        }
        targetLeftHandPosLoc.z *= -1f;
        targetLeftHintPosLoc.z *= -1f;

        newLeftHandPosLoc = EasedPositionAlignToTarget_GetNextStep(
            currentLeftHandPosLoc,
            targetLeftHandPosLoc,
            Easing.EaseType.EaseOutExpo,
            standardSpeed,
            maxDistance);
        newRightHandPosLoc = EasedPositionAlignToTarget_GetNextStep(
            currentRightHandPosLoc,
            targetRightHandPosLoc,
            Easing.EaseType.EaseOutExpo,
            standardSpeed,
            maxDistance);

        newLeftHintPosLoc = EasedPositionAlignToTarget_GetNextStep(
            currentLeftHintPosLoc,
            targetLeftHintPosLoc,
            Easing.EaseType.Linear,
            standardSpeedHint * 1.5f,
            maxDistanceHint);
        newRightHintPosLoc = EasedPositionAlignToTarget_GetNextStep(
            currentRightHintPosLoc,
            targetRightHintPosLoc,
            Easing.EaseType.Linear,
            standardSpeedHint * 1.5f,
            maxDistanceHint);

        leftArmHint.localPosition = newLeftHintPosLoc;
        rightArmHint.localPosition = newRightHintPosLoc;

        gliderLeftArmTwoJointIK.SetTargetPositionByLocalPosition(newLeftHandPosLoc);
        gliderRightArmTwoJointIK.SetTargetPositionByLocalPosition(newRightHandPosLoc);

    }

    public void IKJetpackEntry()
    {
        jetpackTiltBodyReposeIK.DisregardPositionValue();
        if (jetpackDecayCoroutine != null){
            StopCoroutine(jetpackDecayCoroutine);
            jetpackDecayCoroutine = null;
        }
    }
    public void IKJetpackExit()
    {
        if (jetpackDecayCoroutine != null){
            StopCoroutine(jetpackDecayCoroutine);
        }
        jetpackDecayCoroutine = StartCoroutine(IKJetpackTiltExitCoroutine());
    }
    public void IKJetpackTilt(
        float forwardInput,
        float horizontalInput)
    {
        // use value stored in IKRepose

        float maxTiltAngle = 20f; //pitch (forward/back)
        float maxRollAngle = 20f; //roll (left/right)

        float pitch = forwardInput * maxTiltAngle;
        float roll = -horizontalInput * maxRollAngle;
        Quaternion currentTilt = jetpackTiltBodyReposeIK.GetRotationValue();
        Quaternion targetTilt = Quaternion.Euler(pitch, 0f, roll);

        Vector3 currentEulerAngles = currentTilt.eulerAngles;
        float currentPitch = currentEulerAngles.x;
        float currentRoll = currentEulerAngles.z;

        Vector3 targetEulerAngles = targetTilt.eulerAngles;
        float targetPitch = targetEulerAngles.x;
        float targetRoll = targetEulerAngles.z;

        // --- Handle angle wrapping for smooth interpolation ---
        currentPitch = WrapAngle(currentPitch);
        targetPitch = WrapAngle(targetPitch);

        currentRoll = WrapAngle(currentRoll);
        targetRoll = WrapAngle(targetRoll);

        // --- Ease Misalignment Pattern ---
        float pitchDiff = targetPitch - currentPitch;
        float rollDiff = targetRoll - currentRoll;

        float pitchMisal01 = Mathf.Clamp01(Mathf.Abs(pitchDiff) / 40f);
        float rollMisal01 = Mathf.Clamp01(Mathf.Abs(rollDiff) / 40f);

        // Apply easing based on misalignment
        float easedPitch = Easing.ApplyEasing(pitchMisal01, Easing.EaseType.Linear) * pitchDiff;
        float easedRoll = Easing.ApplyEasing(rollMisal01, Easing.EaseType.Linear) * rollDiff;

        // Use the entire difference without clamping for smoother interpolation
        float finalPitch = Mathf.MoveTowards(currentPitch, currentPitch + easedPitch, baseTiltSpeed);
        float finalRoll = Mathf.MoveTowards(currentRoll, currentRoll + easedRoll, baseTiltSpeed);

        // Create final rotation quaternion
        Quaternion finalRotation = Quaternion.Euler(finalPitch, 0f, finalRoll);

        // --- Apply the final rotation as local offset ---
        jetpackTiltBodyReposeIK.SetValueRotationAsLocalOffset(finalRotation);
    }
    private IEnumerator IKJetpackTiltExitCoroutine()
    {
        Vector3 startEuler = jetpackTiltBodyReposeIK.GetRotationValue().eulerAngles;
        float startPitch = WrapAngle(startEuler.x);
        float startRoll = WrapAngle(startEuler.z);

        float decayDuration = 0.75f;
        float timeElapsed = 0f;

        while (timeElapsed < decayDuration)
        {
            float t = timeElapsed / decayDuration;
            t = Easing.ApplyEasing(t, Easing.EaseType.EaseOutQuad); // Eased progression from 0 to 1

            // Lerp toward 0 rotation (neutral)
            float newPitch = Mathf.Lerp(startPitch, 0f, t);
            float newRoll = Mathf.Lerp(startRoll, 0f, t);

            Quaternion finalRotation = Quaternion.Euler(newPitch, 0f, newRoll);
            jetpackTiltBodyReposeIK.SetValueRotationAsLocalOffset(finalRotation);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final state is cleanly set
        jetpackTiltBodyReposeIK.SetValueRotationAsLocalOffset(Quaternion.identity);
        jetpackDecayCoroutine = null;
    }

    public void IKMantleArms(
        MantleSensor.MantleResult mantleSensorResult,
        Vector3 mantleDirection, 
        Vector3 playerPos)
    {
        Vector3 leftHandGripPos = mantleSensorResult.leftHandPos;
        Vector3 rightHandGripPos = mantleSensorResult.rightHandPos;

        // Move target towards a point under the shoulder
        float gripAvgY = (leftHandGripPos.y + rightHandGripPos.y) / 2f;
        float playerY_gripAvgY_diff = playerPos.y - gripAvgY;
        //Debug.Log("playerY_gripAvgY_diff: " +  playerY_gripAvgY_diff);

        Vector3 finalLeftHandPos = leftHandGripPos;
        Vector3 finalRightHandPos = rightHandGripPos;

        // only do this if we are moving upwards
        if(playerY_gripAvgY_diff > 0.54f)
        {
            Vector3 leftHandTarget_postJump = mantleHandTarget_postJump;
            leftHandTarget_postJump.z *= -1f;

            Vector3 leftHandTarget_postJump_world = mantleLeftArmTwoJointIK.target.parent.TransformPoint(leftHandTarget_postJump);
            Vector3 rightHandTarget_postJump_world = mantleRightArmTwoJointIK.target.parent.TransformPoint(mantleHandTarget_postJump);

            float d_toMaxY = Mathf.InverseLerp(0.54f, 1.9f, playerY_gripAvgY_diff);
            float d_toMaxY_eased = d_toMaxY;
            //Debug.Log("d_toMax: " + d_toMaxY + " d_toMax_eased: " + d_toMaxY_eased);
            d_toMaxY_eased = Easing.ApplyEasing(d_toMaxY, Easing.EaseType.EaseOutQuad);


            finalLeftHandPos = Vector3.Lerp(leftHandGripPos, leftHandTarget_postJump_world, d_toMaxY_eased);
            finalRightHandPos = Vector3.Lerp(rightHandGripPos, rightHandTarget_postJump_world, d_toMaxY_eased);
        }

        // Set Hand Targets to grip points
        mantleLeftArmTwoJointIK.SetTargetPositionByWorldPosition(finalLeftHandPos);
        mantleRightArmTwoJointIK.SetTargetPositionByWorldPosition(finalRightHandPos);

        // Set Hints
        Vector3 baseHandHintPos = mantleHandHintBasePosLoc;
        Vector3 baseLeftHandHintPos = baseHandHintPos;
        baseLeftHandHintPos.z *= -1f;
        mantleLeftArmHint.localPosition = baseLeftHandHintPos;
        mantleRightArmHint.localPosition = baseHandHintPos;
    }

    public void IKMantleEntry()
    {
        legsLookIK.target.SetParent(null);
        torsoLookIK.target.SetParent(null);

        current_LegLookTargetDirectionWorld = legsLookIK.target.position - legsLookIK.transform.position;
        current_LegLookTargetDirectionWorld = NormalizeHorizontal(current_LegLookTargetDirectionWorld);
        current_TorsoLookTargetDirectionWorld = torsoLookIK.target.position - torsoLookIK.transform.position;
        current_TorsoLookTargetDirectionWorld = NormalizeHorizontal(current_TorsoLookTargetDirectionWorld);
    }

    public void IKMantleApproach(
        Vector3 playerDirection,  
        Vector3 mantleDirection,
        Rigidbody rb)
    {
        playerDirection = NormalizeHorizontal(playerDirection);
        mantleDirection = NormalizeHorizontal(mantleDirection);
        // Move Torso legs and head to face mantle direction

        // -- Root rotated to velocity
        // rotation of root dictates the direction bones will rotate (clockwise/counterclockwise)
        RotationStep rootRotationStep = EasedRotationToAlignToTarget_GetNextStep(
            playerDirection,
            mantleDirection,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            300f,
            180f);
        float rootRotationDirection = Mathf.Sign(rootRotationStep.signedAngleUsed);
        RotateRootPlayer(rb, rootRotationStep.rotation);

        // Legs and Torso follow camera rotation direction if its a long rotation to make
        float legAngleToTarget = Vector3.SignedAngle(current_LegLookTargetDirectionWorld, mantleDirection, Vector3.up);
        float torsoAngleToTarget = Vector3.SignedAngle(current_TorsoLookTargetDirectionWorld, mantleDirection, Vector3.up);

        // -- Legs delayed rotation to face mantle direction
        current_LegLookTargetDirectionWorld
            = EasedRotationToAlignToTarget_GetNextStep(
            current_LegLookTargetDirectionWorld,
            mantleDirection,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            60f,
            90f,
            Mathf.Abs(legAngleToTarget) > 120f,
            rootRotationDirection).direction;
        current_LegLookTargetDirectionWorld.Normalize();

        // -- Torso delayed rotation to face mantle direction
        current_TorsoLookTargetDirectionWorld
            = EasedRotationToAlignToTarget_GetNextStep(
            current_TorsoLookTargetDirectionWorld,
            mantleDirection,
            Vector3.up,
            Easing.EaseType.EaseOutExpo,
            200f,
            90f,
            Mathf.Abs(torsoAngleToTarget) > 120f,
            rootRotationDirection).direction;
        current_TorsoLookTargetDirectionWorld.Normalize();

        // Set Targets
        Vector3 legsNewLookTargetWorldPos = legsLookIK.transform.position + (current_LegLookTargetDirectionWorld * 5f);
        legsLookIK.SetTargetPositionByWorldPosition(legsNewLookTargetWorldPos);
        Vector3 torsoNewLookTargetWorldPos = torsoLookIK.transform.position + (current_TorsoLookTargetDirectionWorld * 5f);
        torsoLookIK.SetTargetPositionByWorldPosition(torsoNewLookTargetWorldPos);
    }

    public void IKMantleLaunch(
        Vector3 playerDirection
        )
    {
        playerDirection = NormalizeHorizontal(playerDirection);

        // Move torso and legs back to face the player forward

        //Legs to go same direction as torso when it rotates

        // -- Torso 
        RotationStep torsoRotationStep = EasedRotationToAlignToTarget_GetNextStep(
            current_TorsoLookTargetDirectionWorld,
            playerDirection,
            Vector3.up,
            Easing.EaseType.EaseOutQuad,
            600f,
            180f);
        float torsoRotationDirection = Mathf.Sign(torsoRotationStep.signedAngleUsed);

        Debug.DrawLine(torsoLookIK.transform.position + Vector3.up * 6f,
        torsoLookIK.transform.position + (Vector3.up * 6f) + current_TorsoLookTargetDirectionWorld * 3f,
        Color.red);

        current_TorsoLookTargetDirectionWorld = torsoRotationStep.direction.normalized;

        Debug.DrawLine(torsoLookIK.transform.position + Vector3.up * 6f,
        torsoLookIK.transform.position + (Vector3.up * 6f) + current_TorsoLookTargetDirectionWorld * 2f,
        Color.green);

        // -- Legs
        float legAngleToTarget = Vector3.SignedAngle(current_LegLookTargetDirectionWorld, playerDirection, Vector3.up);

        current_LegLookTargetDirectionWorld
            = EasedRotationToAlignToTarget_GetNextStep(
            current_LegLookTargetDirectionWorld,
            playerDirection,
            Vector3.up,
            Easing.EaseType.EaseOutQuad,
            500f,
            180f,
            Mathf.Abs(legAngleToTarget) > 120f,
            torsoRotationDirection).direction;
        current_LegLookTargetDirectionWorld.Normalize();

        // Set Targets
        Vector3 legsNewLookTargetWorldPos = legsLookIK.transform.position + (current_LegLookTargetDirectionWorld * 5f);
        legsLookIK.SetTargetPositionByWorldPosition(legsNewLookTargetWorldPos);
        Vector3 torsoNewLookTargetWorldPos = torsoLookIK.transform.position + (current_TorsoLookTargetDirectionWorld * 5f);
        torsoLookIK.SetTargetPositionByWorldPosition(torsoNewLookTargetWorldPos);
    }

    public void IKMantleExit()
    {
        legsLookIK.target.SetParent(legsLookIK.transform);
        torsoLookIK.target.SetParent(torsoLookIK.transform);
    }

    /// <summary>
    /// Call when you want the Legs, Head and Torso targets to go back to normal
    /// </summary>
    public void IKDirectionForward(
        Easing.EaseType torsoEase,
        float torsoSpeed,
        float torsoMaxMis,
        Easing.EaseType legsEase,
        float legsSpeed,
        float legsMaxMis
        )
    {
        UpdateCommonMath();

        Vector3 torsoEasedRotatedLocalPos =
           EasedRotationToAlignToTarget_GetNextStep(
           current_TorsoLookTargetDirectionLocal,
           Vector3.forward,
           Vector3.up,
           torsoEase,
           torsoSpeed,
           torsoMaxMis).direction;
        torsoLookIK.SetTargetPositionByLocalDirection(torsoEasedRotatedLocalPos, 5f);

        Vector3 legsEasedRotatedLocalPos =
           EasedRotationToAlignToTarget_GetNextStep(
           current_LegLookTargetDirectionLocal,
           Vector3.forward,
           Vector3.up,
           legsEase,
           legsSpeed,
           legsMaxMis).direction;
        legsLookIK.SetTargetPositionByLocalDirection(legsEasedRotatedLocalPos, 5f);
    }

    private Vector3 NormalizeHorizontal(Vector3 v)
    {
        v.y = 0f;
        v.Normalize();
        return v;
    }

    /// <summary>
    /// Actually rotate the player itself, not the bones. Physically changes to players orientation in the world
    /// </summary>
    /// <param name="rotation">Rotation to be applied to Root player</param>
    public void RotateRootPlayer(
        Rigidbody rb,
        Quaternion rotation)
    {
        rb.MoveRotation(rb.rotation * rotation);
    }

    /// <summary>
    /// Use easing to rotate from a current direction to target direction.
    /// Rotation is based on how misaligned to the target the current direction is.
    /// Performs one eased rotation tick. 
    /// Both vectors will be considered to be within same heirarchal context.
    /// </summary>
    /// <param name="currentDirection"></param>
    /// <param name="targetDirection"></param>
    /// <param name="rotationAxis">On which axis to perform rotation</param>
    /// <param name="easeType"></param>
    /// <param name="speed">Maximum degrees per second</param>
    /// <param name="maxMisalignment">Above this angle, the rotation will always be full speed</param>
    /// <param name="forceRotationSign">Whether to force direction of rotation or not</param>
    /// <param name="forcedRotationSign">Pass +1/-1 to force direction of rotation</param>
    /// <returns></returns>
    RotationStep EasedRotationToAlignToTarget_GetNextStep(
        Vector3 currentDirection,
        Vector3 targetDirection,
        Vector3 rotationAxis,
        Easing.EaseType easeType,
        float speed,
        float maxMisalignment,
        bool forceRotationSign = false,
        float? forcedRotationSign = null
        )
    {
        if (maxMisalignment <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMisalignment),
            "maxMisalignment must be greater than 0 to avoid division by zero.");
        }
        // Ensures input directions are normalized and flat
        currentDirection = Vector3.ProjectOnPlane(currentDirection.normalized, rotationAxis);
        targetDirection = Vector3.ProjectOnPlane(targetDirection.normalized, rotationAxis);

        // Signed angle around the axis — critical for directionality
        float angleToTarget = Vector3.SignedAngle(currentDirection, targetDirection, rotationAxis);

        // Force rotation direction if requested
        if (forceRotationSign && forcedRotationSign.HasValue)
        {
            float sign = Mathf.Sign(forcedRotationSign.Value); // Normalize to -1 or +1;
            if (sign != 0 && Mathf.Sign(angleToTarget) != sign)
            {
                // Recalculate to force the longer rotation in desired direction
                angleToTarget = sign * (360f - Mathf.Abs(angleToTarget));
            }
        }
        if (Mathf.Abs(angleToTarget) >= 360f - 0.1f)
        {
            angleToTarget = 0f;
        }


        // 0 = aligned perfectly, 1 = max misaligned
        float misalignment01 = Mathf.Clamp01(Mathf.Abs(angleToTarget) / maxMisalignment);
        float easedMisalignment = Easing.ApplyEasing(misalignment01, easeType);

        // Calculate eased rotation angle this step
        float easedRotationAngle = easedMisalignment * Time.deltaTime * speed;

        // Don't overshoot the target
        easedRotationAngle = Mathf.Clamp(easedRotationAngle, 0f, Mathf.Abs(angleToTarget)) * Mathf.Sign(angleToTarget);
        Quaternion finalRotation = Quaternion.AngleAxis(easedRotationAngle, rotationAxis);
        return new RotationStep(finalRotation, finalRotation * currentDirection, angleToTarget);  
    }

    Quaternion EasedQuaternionToAlignToTarget_GetNextStep(
    Quaternion currentRotation,
    Quaternion targetRotation,
    Easing.EaseType easeType,
    float speed,
    float maxMisalignment)
    {
        float angle = Quaternion.Angle(currentRotation, targetRotation);

        if (angle < 0.01f)
            return targetRotation;

        float misalignment01 = Mathf.Clamp01(angle / maxMisalignment);
        float easedT = Easing.ApplyEasing(misalignment01, easeType);

        float step = Mathf.Min(angle, speed * easedT * Time.deltaTime) / angle;

        return Quaternion.Slerp(currentRotation, targetRotation, step);
    }

    /// <summary>
    /// Use easing to move from a current position to target position.
    /// Position is based on remaining distance to the target
    /// Perfoms one eased movement tick
    /// Both vectors will be considered to be within the same heirarchal context.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="targetPosition"></param>
    /// <param name="easeType"></param>
    /// <param name="speed"></param>
    /// <param name="maxDistance"></param>
    /// <param name=""></param>
    /// <returns></returns>
    Vector3 EasedPositionAlignToTarget_GetNextStep(
        Vector3 currentPosition,
        Vector3 targetPosition,
        Easing.EaseType easeType,
        float speed,
        float maxDistance
        )
    {
        if (maxDistance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistance),
            "maxDistance must be greater than 0 to avoid division by zero.");
        }

        float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);
        float misalignment01 = Mathf.Clamp01(distanceToTarget / maxDistance);
        float easedMisalignment = Easing.ApplyEasing(misalignment01, easeType);
        float rawStep = easedMisalignment * Time.deltaTime * speed;
        // clamp so we never move more than the distance remaining
        float clampedStep = Mathf.Min(rawStep, distanceToTarget);
        return Vector3.MoveTowards(currentPosition, targetPosition, clampedStep);
    }

    Vector3 SolveQuadBezierControlPoint(Vector3 A, Vector3 B, Vector3 C, float t0)
    {
        float u0 = 1f - t0;
        float denom = 2f * u0 * t0;
        if (Mathf.Abs(denom) < 1e-6f)
            throw new ArgumentException("t0 must be strictly between 0 and 1");

        // P = (C - u0^2 A - t0^2 B) / (2 u0 t0)
        return (C - u0 * u0 * A - t0 * t0 * B) / denom;
    }

    Vector3 QuadBezier(Vector3 A, Vector3 P, Vector3 B, float t)
    {
        float u = 1f - t;
        return u * u * A
            + 2f * u * t * P
            + t * t * B;
    }

    private float WrapAngle(float angle)
    {
        if (angle > 180f)
            return angle - 360f;
        if (angle < -180f)
            return angle + 360f;
        return angle;
    }

    private void UpdateCommonMath()
    {
        // Current Target in their Local Space
        current_HeadLookTargetDirectionLocal = Vector3.Normalize(headLookIK.target.transform.localPosition);
        current_HeadLookTargetDirectionLocal.y = 0f;
        current_LegLookTargetDirectionLocal = Vector3.Normalize(legsLookIK.target.transform.localPosition);
        current_LegLookTargetDirectionLocal.y = 0f;
        current_TorsoLookTargetDirectionLocal = Vector3.Normalize(torsoLookIK.target.transform.localPosition);
        current_TorsoLookTargetDirectionLocal.y = 0f;

        // Current Targets Relative to Head IK Space
        // Torso Target Direction, Relative to Head
        current_TorsoLookTargetDirectionLocalToHeadIK = headLookIK.transform.InverseTransformPoint(
            holdTorsoTargetPos);
        current_TorsoLookTargetDirectionLocalToHeadIK.y = 0f;
        current_TorsoLookTargetDirectionLocalToHeadIK.Normalize();

        // Leg Target Direction, Relative to Head
        current_LegTargetDirectionLocalToHeadIK = headLookIK.transform.InverseTransformPoint(
            holdLegTargetPos);
        current_LegTargetDirectionLocalToHeadIK.y = 0f;
        current_LegTargetDirectionLocalToHeadIK.Normalize();


        // Commonly used angles
        signedAngleBetweenHeadAndTorsoTargets = Vector3.SignedAngle(
            current_TorsoLookTargetDirectionLocalToHeadIK,
            current_HeadLookTargetDirectionLocal,
            Vector3.up);

        signedAngleBetweenHeadAndLegsTargets = Vector3.SignedAngle(
            current_LegTargetDirectionLocalToHeadIK,
            current_HeadLookTargetDirectionLocal,
            Vector3.up);

        signedAngleBetweenTorsoAndLegsTargets = Vector3.SignedAngle(
            current_LegTargetDirectionLocalToHeadIK,
            current_TorsoLookTargetDirectionLocalToHeadIK,
            Vector3.up);
    }

    bool isVectorBetween(Vector3 test, Vector3 B, Vector3 C, Vector3 referenceNormal)
    {
        // Project vectors onto a plane defined by the reference normal
        test = Vector3.ProjectOnPlane(test, referenceNormal).normalized;
        B = Vector3.ProjectOnPlane(B, referenceNormal).normalized;
        C = Vector3.ProjectOnPlane(C, referenceNormal).normalized;

        // Compute signed angles
        float angleBC = Vector3.SignedAngle(B, C, referenceNormal);
        float angleBA = Vector3.SignedAngle(B, test, referenceNormal);

        // If angleBC is negative, wrap it to positive (ensuring consistent direction)
        if (angleBC < 0) angleBC += 360;
        if (angleBA < 0) angleBA += 360;

        // Check if test's angle is within the arc from B to C
        return angleBA >= 0 && angleBA <= angleBC;
    }

    /// <summary>
    /// Checks if a given directional vector is closer (more aligned) to
    /// the target vector than some other vector
    /// </summary>
    /// <param name="test"></param>
    /// <param name="target"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    bool isVectorCloserOrEqualToTarget(Vector3 test, Vector3 target, Vector3 other, Vector3 referenceNormal)
    {
        // Project vectors onto the ground plane (defined by the reference normal, typically Vector3.up)
        test = Vector3.ProjectOnPlane(test, referenceNormal).normalized;
        target = Vector3.ProjectOnPlane(target, referenceNormal).normalized;
        other = Vector3.ProjectOnPlane(other, referenceNormal).normalized;

        // Compute absolute angles between target and both test and other
        float angleTest = Vector3.Angle(target, test);
        float angleOther = Vector3.Angle(target, other);

        // Return true if test is closer in angle to target than other
        return angleTest <= angleOther;
    }
}
