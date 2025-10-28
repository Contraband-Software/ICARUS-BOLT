using Content.System.Firmware;
using Content.System.Modules;
using ProgressionV2;
using Resources;
using Resources.System;
using Software.Contraband.StateMachines;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class Run : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;
        private bool coyoteActive = false;
        private float coyoteEnterTime = 0f;
        private bool isSprinting = false;
        private Quaternion currentSurfaceQuat = Quaternion.identity;
        private Vector3 lastMovementDirection = Vector3.zero;


        public Run(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.allowBoost = true;
            // bhop tech
            if (stateHandler.JumpBufferActive())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jump)]);
                return;
            }
            stateHandler.animator.SetBool("isRunning", true);
            coyoteActive = false;
            isSprinting = false;
            stateHandler.pCon.lastVelocity = stateHandler.pCon.rb.velocity;
            stateHandler.pCon.entryVelocity = stateHandler.pCon.rb.velocity;
            stateHandler.pCon.currentStateFrame = 0;
            stateHandler.pCon.entryVelocityCompensated = false;
            CheckSprint();

            stateHandler.ikCon.FadeInRig(IKController.IKRig.LegsLook);
            stateHandler.ikCon.FadeInRig(IKController.IKRig.HeadLook);
            stateHandler.ikCon.FadeInRig(IKController.IKRig.TorsoLook);

            stateHandler.animCon.SetAirAnimationState(PlayerAnimationController.AnimStateBlends.NONE);
        }
        protected override void ExitState()
        {
            stateHandler.pCon.rb.useGravity = true;
            stateHandler.animator.SetBool("isRunning", false);
            isSprinting = false;
        }

        public override void UpdateState()
        {
            //if (stateHandler.JumpBufferActive()) return;

            // TRANSITION TO MANTLE
            if (stateHandler.CheckCanMantle())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Mantle)]);
            }


            // TRANSITION TO FALLING
            if (coyoteActive && Time.time - coyoteEnterTime >= stateHandler.pCon.coyoteTime)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
            }

            // ENTER/EXIT SPRINT
            CheckSprint();

            if (isSprinting)
            {
                // Deplete fuel
                float deplete = stateHandler.pCon.Stats.GetStat(Stat.SprintFuelUse) * -1f;
                deplete *= (stateHandler.pCon.Stats.GetStat(Stat.FuelEfficiency) * Time.deltaTime);
                stateHandler.pCon.Fuel.AddLevel(Fuel.Tank.Legs, deplete);
            }
        }
        public override void FixedUpdateState()
        {
            //if (stateHandler.JumpBufferActive()) return;

            //Debug.Log("Start FixedUpdateState V: " + stateHandler.pCon.rb.velocity);
            stateHandler.pCon.CompensateGroundEntryVelocity();

            //TRANSITION TO Idle
            if (Mathf.Abs(stateHandler.forwardInput) + Mathf.Abs(stateHandler.horizontalInput) == 0)
            {
                stateHandler.pCon.ToggleStationaryPhysicMaterial(true);
                if(stateHandler.pCon.rb.velocity.magnitude < 0.1)
                {
                    stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
                }
            }
            else
            {
                stateHandler.pCon.ToggleStationaryPhysicMaterial(false);
            }

            // AFFECTED BY RABBIT, GRASSHOPPER, SPRINT

            if (stateHandler.pCon.IsGrounded())
            {
                currentSurfaceQuat = stateHandler.pCon.currentSurfaceQuaternion;
            }

            //ACTIVATE COYOTE TIME
            if (!stateHandler.pCon.IsGrounded()
                && !coyoteActive)
            {
                //dont enter coyote time if its not a substantial drop

                // Get the bottom center of the collider
                float bodyColliderHalfHeight = stateHandler.pCon.bodyCollider.height * 0.5f;
                Vector3 bottomPoint = stateHandler.pCon.bodyCollider.bounds.center - Vector3.up * bodyColliderHalfHeight
                    + new Vector3(0, 0.1f, 0);
                var ray = new Ray(bottomPoint, Vector3.down);

                Debug.DrawLine(bottomPoint,
                    bottomPoint + Vector3.down * stateHandler.pCon.coyoteTimeHeightThreshold, Color.magenta);
                if (!Physics.Raycast(ray,
                    out RaycastHit hitInfo,
                    stateHandler.pCon.coyoteTimeHeightThreshold,
                    stateHandler.pCon.terrainLayer))
                {
                    coyoteActive = true;
                    coyoteEnterTime = Time.time;
                    stateHandler.pCon.rb.useGravity = false;
                    Debug.Log("COYOTE ENTER");
                }
                VerticalVelocity();
            }

            // DEACTIVATE COYOTE IF RE-GROUNDED IN COYOTE
            if (coyoteActive && stateHandler.pCon.IsGrounded())
            {
                coyoteActive = false;
                stateHandler.pCon.rb.useGravity = true;
            }

            // Check If on Impossible slope or standard slope
            float slopeMultiplier = stateHandler.pCon.GetSlopeMultiplier(
                stateHandler.pCon.currentSurfaceSlope,
                stateHandler.pCon.currentSurfaceSlopeRelativeToPlayer);
            

            if(stateHandler.pCon.IsGrounded() && slopeMultiplier == 0)
            {
                VerticalVelocity();
                XZVelocity_Air();
            }
            else
            {
                XZVelocity_Ground();
            }

            stateHandler.ikCon.IKRun(
                new Vector2(stateHandler.horizontalInput, stateHandler.forwardInput),
                stateHandler.pCon.rb,
                stateHandler.pCon.transform
                );
        }

        private void CheckSprint()
        {
            if (stateHandler.holdingSprint)
            {
                if (InventoryData.Inventory.HasActiveFirmware<SprintFirmware>()
                    || stateHandler.pCon.hasSprint)
                {
                    isSprinting = true;
                }
            }
            else
            {
                isSprinting = false;
            }
        }

        public override void HandleJumpReleased()
        {
            if (coyoteActive)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jump)]);
            }
            else
            {
                base.HandleJumpReleased();
            }
        }

        public override void HandleBoostPressed()
        {
            //TRANSITION TO BOOST CHARGE, IF IN COYOTE
            if (coyoteActive && stateHandler.allowBoost)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(BoostCharge)]);
            }
        }



        #region MOVEMENT_MANIPULATION

        public override void XZVelocity_Ground()
        {
            // Split between X and Z velocity
            float fInput = stateHandler.forwardInput;
            float hInput = stateHandler.horizontalInput;

            UpgradeableStats stats = stateHandler.pCon.Stats;
            float maxForwardSpeed = stats.GetStat(Stat.RunSpeed);
            float maxReverseSpeed = stats.GetStat(Stat.RunSpeedReverse);
            float xzAccel = stats.GetStat(Stat.RunAcceleration);
            float xzDecel = stats.GetStat(Stat.RunDeceleration);
            float maxStrafeSpeed = stats.GetStat(Stat.RunSpeedStrafe);

            // apply sprint multiplier
            if (isSprinting)
            {
                maxForwardSpeed *= stateHandler.pCon.Stats.GetStat(Stat.SprintMultiplier);
            }

            // Z Velocity (forward and reverse)

            float maxZSpeed = 0f;
            if (fInput > 0) { maxZSpeed = maxForwardSpeed; }
            if (fInput < 0) { maxZSpeed = maxReverseSpeed; }
            float zTargetVelocity = fInput * maxZSpeed;

            // Calculate difference between current velocity and desired velocity
            // using Local forward velocity
            float currentZVelocity = Vector3.Dot(stateHandler.pCon.rb.velocity, stateHandler.transform.forward);
            float zVelocityDiff = zTargetVelocity - currentZVelocity;

            // apply standard acceleration/deceleration
            float zAcceleration = 0f;
            //Debug.Log("Target V: " + zTargetVelocity +  " Current: " + currentZVelocity + " fIn: " + fInput);
            //Debug.Log(stateHandler.pCon.rb.velocity);
            if(zVelocityDiff < 0)
            {
                zAcceleration = (currentZVelocity > 0) ? xzDecel : xzAccel;
            }
            if(zVelocityDiff > 0)
            {
                zAcceleration = (currentZVelocity > 0) ? xzAccel : xzDecel;
            }

            // Calculate force along z-axis to apply to the player
            float zMovement = zVelocityDiff * zAcceleration;
            //Debug.Log("ZMove: " + zMovement);


            // X Velocity (strafe)
            float maxXSpeed = maxStrafeSpeed;
            float xTargetVelocity = hInput * maxXSpeed;
            float xAcceleration = (hInput != 0) ? xzAccel : xzAccel;
            float xVelocityDiff = xTargetVelocity
                - Vector3.Dot(stateHandler.pCon.rb.velocity, stateHandler.transform.right);
            float xMovement = xVelocityDiff * xAcceleration;

            Vector3 movementForce = (zMovement * stateHandler.transform.forward)
                + (xMovement * stateHandler.transform.right);

            // pre-slope movement direction
            Vector3 preSlopeDirection = Vector3.Normalize(movementForce);
            lastMovementDirection = preSlopeDirection;

            // apply slope multiplier
            movementForce *= stateHandler.pCon.GetSlopeMultiplier(
                stateHandler.pCon.currentSurfaceSlope,
                stateHandler.pCon.currentSurfaceSlopeRelativeToPlayer);

            // cut upward vertical velocity if slope says you cant climb
            if (movementForce == Vector3.zero)
            {
                Vector3 currentVelocity = stateHandler.pCon.rb.velocity;
                if (currentVelocity.y > 0)
                {
                    currentVelocity.y *= 0.5f;
                    stateHandler.pCon.rb.velocity = currentVelocity;
                }
            }
            // align movement to match slope
            movementForce = currentSurfaceQuat * movementForce;

            //Debug.Log("Movement Force: " + movementForce);

            // aligned to slope movement direction
            Vector3 alignedToSlopeDirection = Vector3.Normalize(movementForce);

            if (coyoteActive)
            {
                Debug.Log("COYOTE ACTIVE");
                Vector3 midpointDirection = (preSlopeDirection + alignedToSlopeDirection) / 2;
                midpointDirection.Normalize();
                movementForce = midpointDirection * movementForce.magnitude;
            }


            //Debug.Log("APplying movement force: " + movementForce);

            stateHandler.pCon.rb.AddForce(movementForce, ForceMode.Force);
        }
        #endregion
    }
}
