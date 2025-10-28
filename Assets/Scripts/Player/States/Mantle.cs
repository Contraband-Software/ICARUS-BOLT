using Helpers;
using Resources.System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using ProgressionV2;

namespace Player
{
    public class Mantle : PlayerBaseState
    {

        bool debug = false;
        
        
        private enum MantlePhase
        {
            APPROACH,
            PULLUP,
            LAUNCH,
            PUSHOVER
        }

        // ## PHASES ##
        // APPROACH:
        //  Player is moved in towards the closest approach point,
        //  Potentially also pulling up simultaneously if it is viable
        // 
        // PULLUP:
        //  Player solely focuses on pulling himself up
        //
        // LAUNCH:
        //  Player gets a_grav burst of velocity that will land him at the landing point

        public Mantle(PlayerStateHandler stateHandler)
        : base(stateHandler) { }

        private MantleSensor.MantleResult mantleSensorResult;

        Vector3 entryVelocity = Vector3.zero;
        Vector3 mantleDirection = Vector3.zero;


        // GENERAL 
        private const float launchWhenFeetBelowClearanceBy = 1f;
        private const float additionalJumpHeight = 0.25f;
        // anything below this will be just insta launched over
        private const float tallestNonStruggleLedgeHeight = 1.9f;
        private const float fullStretchedHeight = 2.95f; //height from hand to foot when arms fully raised

        // APPROACH
        private const float minApproachSpeed = 2.7f;
        private const float approachSpeedFinalMultiplier = 0.7f;
        private const float approachStopDistance = 0.16f;

        // LAUNCH 
        private float predeterminedLaunchFeetY = 0f;
        private float predeterminedLaunchVelocity = 0f;

        // LANDING
        private float xVelocityToLanding = 0f;
        private float xAccelAfterLaunch = 0f;


        private float approachDuration = 0f;
        private float approachStartTime;
        private float approachEntryDistToTarget = 0f;
        private float approachFeetStartY = 0f;
        private Vector3 approachDir = Vector3.zero;
        private float approachEntrySpeedFlat = 0f;
        private bool collisionOn = true;
        private float FeetPlayerOffset = 0f;


        // Freefall
        private bool IsFreeFalling => stateHandler.pCon.rb.velocity.y < 0 && !stateHandler.pCon.IsGrounded();



        // IK
        private bool lookAtCamera = false;

        MantlePhase phase;

        protected override void EnterState()
        {
            stateHandler.CancelJump();
            stateHandler.ikCon.IKMantleEntry();
            lookAtCamera = false;

            // Copy in MantleResult
            mantleSensorResult = stateHandler.pCon.GetLastMantleSensorResult();

            if(debug)
            {
                Debug.Log("ENTER MANTLE: MANTLE PARAMS: " +
                    mantleSensorResult.ToString());
            }

            // Catch Entry Velocity
            entryVelocity = stateHandler.pCon.rb.velocity;

            // Turn Off Gravity
            stateHandler.pCon.rb.useGravity = false;
            collisionOn = true;
            FeetPlayerOffset = stateHandler.pCon.transform.position.y - stateHandler.pCon.groundCheck.position.y;

            stateHandler.ikCon.FadeInRig(IKController.IKRig.MantleArms, 9f, Easing.EaseType.EaseOutQuad);

            mantleDirection = (mantleSensorResult.landingPos - stateHandler.pCon.transform.position).normalized;

            // Move closest approach point closer
            mantleSensorResult.closestApproachPoint -=
                Flatten(mantleDirection).normalized 
                * stateHandler.pCon.highestRadiusBodyCollider.bounds.extents.x * 0.3f;

            // Decide which MantlePhase to start in
            phase = MantlePhase.APPROACH;

            switch(phase)
            {
                case MantlePhase.APPROACH:
                    EnterApproachPhase(entryVelocity);
                    break;
                case MantlePhase.PULLUP:
                    break;
                case MantlePhase.PUSHOVER:
                    break;
                default:
                    break;
            }
        }

        protected override void ExitState() {

            // Turn Gravity Back On
            stateHandler.pCon.rb.useGravity = true;
            stateHandler.pCon.ToggleCollision(true);

            stateHandler.ikCon.IKMantleExit();

            //stateHandler.ikCon.DeactivateRig(IKController.IKRig.MantleArms);
        }

        public override void UpdateState()
        {
            
        }
        public override void LookInCameraDirection(Vector3 cameraForward)
        {
            if (!lookAtCamera) return;
            Debug.Log("LOOK IN CAM DIR");
            base.LookInCameraDirection(cameraForward);
        }

        public override void FixedUpdateState()
        {
            EnforceMinimumY();

            if(phase == MantlePhase.APPROACH)
            {
                Debug.Log("Mantle Update APPROACH");
                UpdateApproachPhase();
            }
            else if(phase == MantlePhase.LAUNCH)
            {
                Debug.Log("Mantle Update LAUNCH");
                UpdateLaunchPhase();
            }

            VerticalVelocity();

            stateHandler.ikCon.IKMantleArms(
                mantleSensorResult,
                Flatten(mantleDirection).normalized,
                stateHandler.pCon.transform.position);

            if(phase == MantlePhase.APPROACH)
            {
                Debug.Log("IKMantle APPROACH");
                stateHandler.ikCon.IKMantleApproach(
                    stateHandler.pCon.transform.forward,
                    Flatten(mantleDirection).normalized,
                    stateHandler.pCon.rb);

            }
            else if(phase == MantlePhase.LAUNCH)
            {
                Debug.Log("IKMantle LAUNCH");
                stateHandler.ikCon.IKMantleLaunch(stateHandler.pCon.transform.forward);
            }
        }

        private void EnforceMinimumY()
        {
            if(
                stateHandler.pCon.rb.velocity.y < 0 &&
                mantleSensorResult.yClearancePoint - stateHandler.pCon.groundCheck.position.y 
                > fullStretchedHeight)
            {
                Vector3 controlledPos = stateHandler.pCon.transform.position;
                controlledPos.y = mantleSensorResult.yClearancePoint - fullStretchedHeight + FeetPlayerOffset;
                //stateHandler.pCon.rb.MovePosition(controlledPos);
                Vector3 currentVelocity = stateHandler.pCon.rb.velocity;
                currentVelocity.y = 0;
                stateHandler.pCon.rb.velocity = currentVelocity;
            }
        }


        private void EnterApproachPhase(Vector3 entryVelocity)
        {
            //UnityEditor.EditorApplication.isPaused = true;

            // Turn off collision
            stateHandler.pCon.ToggleCollision(false);
            collisionOn = false;

            approachStartTime = Time.time;

            // Entry Speed and Direction
            Vector3 dToTarget_flat = Flatten(mantleSensorResult.closestApproachPoint -
                stateHandler.pCon.transform.position);

            approachEntryDistToTarget = dToTarget_flat.magnitude;
            approachEntrySpeedFlat = Flatten(entryVelocity).magnitude;

            // At minimum entry speed is hard set to minApproachSpeed, and we have no limit on entry
            // max speed. The speed at which we enter at will be decayed.
            approachEntrySpeedFlat = Mathf.Max(approachEntrySpeedFlat, minApproachSpeed);
            approachDir = dToTarget_flat.normalized;

            // Adjust approachStopDistance if we're already past the approach point
            float dot = Vector3.Dot(Flatten(mantleDirection).normalized, approachDir);
            if (dot < 0f)
            {
                // We're facing away or already past the point — allow backing up
                float pushBackDist = approachStopDistance;
                mantleSensorResult.closestApproachPoint -= Flatten(mantleDirection).normalized * pushBackDist;

                // Recompute the new distance and direction
                dToTarget_flat = Flatten(mantleSensorResult.closestApproachPoint - 
                    stateHandler.pCon.transform.position);
                approachEntryDistToTarget = dToTarget_flat.magnitude;
                approachDir = dToTarget_flat.normalized;
            }

            // Calculate based off distance to approach point and starting velocity, how long it should take to 
            // reach the approach point
            // d = 0.5 * (v0 + v1) * t
            approachDuration = approachEntryDistToTarget / 
                (0.5f * (approachEntrySpeedFlat + approachEntrySpeedFlat * approachSpeedFinalMultiplier));

            // Positions
            approachFeetStartY = stateHandler.pCon.groundCheck.position.y;

            // Pre-determine foot Y for Launch 
            predeterminedLaunchFeetY = Mathf.Max(
                mantleSensorResult.yClearancePoint - launchWhenFeetBelowClearanceBy, approachFeetStartY);

            // Pre-determine launch velocity 
            float verticalDistance = mantleSensorResult.yClearancePoint - predeterminedLaunchFeetY
                + additionalJumpHeight;
            float gravityAccel = Mathf.Abs(Physics.gravity.y) + stateHandler.pCon.upwardDecel;
            predeterminedLaunchVelocity = Mathf.Sqrt(2f * gravityAccel * verticalDistance);

        }

        private void UpdateApproachPhase()
        {
            if (phase != MantlePhase.APPROACH) return;

            float elapsedTime = Time.time - approachStartTime;
            float t = Mathf.Clamp01(elapsedTime / approachDuration);

            // if we hit ground while falling in the pullup phase, do this
            // to stop player phasing through floor
            if (stateHandler.pCon.rb.velocity.y < 0
                && stateHandler.pCon.IsGrounded())
            {
                Vector3 currentVelocity = stateHandler.pCon.rb.velocity;
                currentVelocity.y = 0;
                stateHandler.pCon.rb.velocity = currentVelocity;
                approachFeetStartY = stateHandler.pCon.groundCheck.position.y;
            }

            // Adjust approach Feet start Y if feet fall below it 
            if (stateHandler.pCon.groundCheck.position.y < approachFeetStartY)
            {
                approachFeetStartY = stateHandler.pCon.groundCheck.position.y;
            }

            // ## FORWARD/REVERSE MOTION ##
            float currentSpeed = Mathf.Lerp(approachEntrySpeedFlat,
                approachEntrySpeedFlat * approachSpeedFinalMultiplier, t);

            Vector3 dToTarget = Flatten(mantleSensorResult.closestApproachPoint -
                stateHandler.pCon.transform.position);
            float dToTarget_mag = dToTarget.magnitude;
            float dot = Vector3.Dot(Flatten(mantleDirection).normalized, dToTarget.normalized);


            Vector3 newVel = currentSpeed * approachDir;

            // ## VERTICAL MOTION ##

            // Find force we must overcome
            float grav_accel = Mathf.Abs(Physics.gravity.y);
            grav_accel += stateHandler.pCon.rb.velocity.y < 0 
                ? stateHandler.pCon.downwardAccel 
                : stateHandler.pCon.upwardDecel;

            bool verticalPortionComplete = false;
            float currentYVelocity = stateHandler.pCon.rb.velocity.y;

            float fullVerticalDistance = mantleSensorResult.yClearancePoint - approachFeetStartY;
            // Safely calculate pull-up interpolation factor
            float denom = predeterminedLaunchFeetY - approachFeetStartY;
            float t_pullup = denom != 0f
                ? (stateHandler.pCon.groundCheck.position.y - approachFeetStartY) / denom
                : 1f;
            t_pullup = Mathf.Clamp(t_pullup, -1f, 1f);

            // Base pullup force on vertical mantle progress
            float upAccel = stateHandler.mantle_pullupForce.Evaluate(t_pullup);
            upAccel += grav_accel;

            // If we are in freefall, we use our braking force
            if (IsFreeFalling)
            {
                upAccel = stateHandler.mantle_fallBrakeForce.Evaluate(currentYVelocity);
            }

            float newYVelocity = currentYVelocity + upAccel * Time.fixedDeltaTime;
            newVel.y = Mathf.Min(newYVelocity, predeterminedLaunchVelocity);

            // If the ledge is small we can just jump straight over
            if(
                (currentYVelocity >= 0 || stateHandler.pCon.IsGrounded()) && 
                fullVerticalDistance < tallestNonStruggleLedgeHeight)
            {
                newVel.y = predeterminedLaunchVelocity;
            }

            // once feet go over the predetermined launch Y, and we are moving upwards, weve completed the
            // pullup part of the approach
            if(stateHandler.pCon.groundCheck.transform.position.y >= predeterminedLaunchFeetY
                && stateHandler.pCon.rb.velocity.y > 0)
            {
                verticalPortionComplete = true;
            }

            if (debug)
            {
                Debug.Log(
                    "dTarget mag: " + dToTarget_mag
                    + "dot: " + dot
                    + "t: " + t
                    + "t_pullup: " + t_pullup
                    + " upAccel: " + upAccel
                    + " grav_accel: " + grav_accel
                    + " c_vel.y: " + currentYVelocity
                    + " newVel.y: " + newVel.y
                    + " full vertical distance: " + fullVerticalDistance
                    + " grounded?: " + stateHandler.pCon.IsGrounded()
                    + " is freefalling?: " + IsFreeFalling
                    + " vertical portion complete?: " + verticalPortionComplete);
            }

            stateHandler.pCon.rb.velocity = newVel;

            if(t >= 1)
            {
                stateHandler.pCon.rb.velocity = new Vector3(0f, stateHandler.pCon.rb.velocity.y, 0f);
                stateHandler.pCon.rb.MovePosition(new Vector3(
                    mantleSensorResult.closestApproachPoint.x,
                    stateHandler.pCon.transform.position.y,
                    mantleSensorResult.closestApproachPoint.z));
            }

            if (verticalPortionComplete)
            {
                stateHandler.pCon.rb.velocity = new Vector3(0f, predeterminedLaunchVelocity, 0f);
                phase = MantlePhase.LAUNCH;
                EnterLaunchPhase();
                return;
            }

            // DEBUG
            if (debug)
            {
                Vector3 launchPoint = new Vector3(
                    mantleSensorResult.landingPos.x,
                    predeterminedLaunchFeetY,
                    mantleSensorResult.landingPos.z);

                Vector3 feetApproachStartPoint = new Vector3(
                    mantleSensorResult.landingPos.x,
                    approachFeetStartY,
                    mantleSensorResult.landingPos.z);

                Vector3 mantleDirFlat = Flatten(mantleDirection).normalized;
                Debug.DrawLine(launchPoint, launchPoint - mantleDirFlat * 5f, new Color(1f, 0.5f, 0f));
                Debug.DrawLine(feetApproachStartPoint, 
                    feetApproachStartPoint - mantleDirFlat * 5f, Color.magenta);

                Vector3 d_origin = stateHandler.transform.position + Vector3.up * 4f;
                Debug.DrawLine(d_origin + Vector3.up * 0.2f,
                    d_origin + Vector3.up * 0.2f + mantleDirFlat * 4f, Color.yellow);
                Debug.DrawLine(d_origin + Vector3.up * 0.4f,
                    d_origin + Vector3.up * 0.4f + Flatten(dToTarget.normalized) * 4f, Color.blue);
            }
        }

        private void EnterLaunchPhase()
        {
            stateHandler.pCon.rb.useGravity = true;

            // Solve s = ut + 0.5at^2 for t to find out how long itll take for player to land
            float s_y = mantleSensorResult.landingPos.y - stateHandler.pCon.groundCheck.position.y;
            float u_y = stateHandler.pCon.rb.velocity.y;
            float a_grav = -(Mathf.Abs(Physics.gravity.y) + stateHandler.pCon.upwardDecel);

            float A_0 = a_grav;
            float B_0 = 2f * u_y;
            float C_0 = -2f * s_y;

            float discriminant = B_0 * B_0 - 4 * A_0 * C_0;

            float t1 = 0f;
            float t2 = 0f;

            if (discriminant < 0)
            {
                Debug.LogWarning("BIG FUCKING OOPSIE DOOPSIE. NO VALID SOLUTION TO EQUATION");
            }
            else
            {
                float sqrtDisc = Mathf.Sqrt(discriminant);
                t1 = (-B_0 + sqrtDisc) / (2 * A_0);
                t2 = (-B_0 - sqrtDisc) / (2 * A_0);
            }

            if (debug)
            {
                Debug.Log("Mantle Enter Launch Phase Y Portion "
                    + " s_y: " + s_y
                    + " u_y: " + u_y
                    + " a_grav: " + a_grav
                    + " possible solution for t_y: " + t1 + " & " + t2);
            }

            float timeToLanding = Mathf.Max(t1, t2);

            stateHandler.ikCon.FadeOutRig(IKController.IKRig.MantleArms, 1f, Easing.EaseType.EaseOutQuad);

            Vector3 landingPointStartPointDelta = Flatten(mantleSensorResult.landingPos -
                stateHandler.pCon.groundCheck.position);
            Vector3 dir_x = landingPointStartPointDelta.normalized;
            float s_x = landingPointStartPointDelta.magnitude;

            // Calculate velocity needed to reach landing point from acceleration end point
            float accel_period_frac = 0.5f;
            float vel_period_frac = 1f - accel_period_frac;
            xVelocityToLanding = (s_x * vel_period_frac) / (timeToLanding * vel_period_frac);

            // Calculate acceleration needed to reach the above speed in given time
            float u_x = Flatten(stateHandler.pCon.rb.velocity).magnitude;
            xAccelAfterLaunch = (xVelocityToLanding - u_x) / (timeToLanding * accel_period_frac);

            if (debug)
            {
                Debug.Log("Mantle Enter Launch Phase X Portion"
                    + " s_x: " + s_x
                    + " v_x: " + xVelocityToLanding
                    + " a_x: " + xAccelAfterLaunch);
            }
        }


        private void UpdateLaunchPhase()
        {
            if (phase != MantlePhase.LAUNCH) return;
            lookAtCamera = true;

            Vector3 landingPointDelta = Flatten(mantleSensorResult.landingPos -
                stateHandler.pCon.groundCheck.position);
            Vector3 dir_x = landingPointDelta.normalized;

            Vector3 c_velocity = stateHandler.pCon.rb.velocity;

            // Launch Y Apex reached
            if (c_velocity.y < 0f && !collisionOn)
            {
                stateHandler.pCon.ToggleCollision(true);
                collisionOn = true;
            }

            Vector3 new_vel = c_velocity;

            float dot = Vector3.Dot(Flatten(mantleDirection).normalized, dir_x);

            // ## FORWARD MOTION ##
            Vector3 xAccel = Vector3.zero;
            // If we are holding key to keep running, we accelerate into the run
            if(stateHandler.forwardInput > 0)
            {
                if(Flatten(c_velocity).magnitude < stateHandler.pCon.Stats.GetStat(Stat.RunSpeed))
                {
                    UpgradeableStats stats = stateHandler.pCon.Stats;
                    xAccel = stats.GetStat(Stat.RunAcceleration) * Flatten(mantleDirection).normalized * 2f;
                }
            }

            else
            {
                if (Flatten(c_velocity).magnitude < xVelocityToLanding && dot > 0f)
                {
                    xAccel = xAccelAfterLaunch * dir_x;
                }
            }

            new_vel += xAccel * Time.fixedDeltaTime;
            stateHandler.pCon.rb.velocity = new_vel;

            if (c_velocity.y < 0 && stateHandler.pCon.IsGrounded())
            {
                // transition to idle
                if (stateHandler.forwardInput + stateHandler.horizontalInput == 0)
                {
                    stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
                }

                // Transition to running 
                if (stateHandler.forwardInput + stateHandler.horizontalInput != 0)
                {
                    stateHandler.SwitchState(stateHandler.States[typeof(Run)]);
                }
            }

            // DEBUG
            if(debug)
            {
            Vector3 launchPoint = new Vector3(
                mantleSensorResult.landingPos.x,
                predeterminedLaunchFeetY,
                mantleSensorResult.landingPos.z);

            Vector3 feetApproachStartPoint = new Vector3(
                mantleSensorResult.landingPos.x,
                approachFeetStartY,
                mantleSensorResult.landingPos.z);

            Vector3 mantleDirFlat = Flatten(mantleDirection).normalized;
            Debug.DrawLine(launchPoint, launchPoint - mantleDirFlat * 5f, new Color(1f, 0.5f, 0f));
            Debug.DrawLine(feetApproachStartPoint,
                feetApproachStartPoint - mantleDirFlat * 5f, Color.magenta);
            }
        }
    }
}
