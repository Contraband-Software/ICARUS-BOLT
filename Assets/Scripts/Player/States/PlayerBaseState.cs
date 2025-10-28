using Content.System.Modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Software.Contraband.StateMachines;
using UnityEngine.InputSystem;
using Resources;
using Resources.System;
using ProgressionV2;

namespace Player
{
    public abstract class PlayerBaseState : BaseState,
        IPlayerInputHandler,
        IStateCollisionHandler
    {
        protected PlayerStateHandler stateHandler;

        protected PlayerBaseState(PlayerStateHandler stateHandler)
        {
            this.stateHandler = stateHandler;
        }

        private float _tmpRotationVelocity;
        private float _rotationSmoothTime = 0.1f;

        public abstract void UpdateState();

        public abstract void FixedUpdateState();

        public virtual void LateUpdateState() { }

        public virtual void HandleJumpPressed() { }

        /// <summary>
        /// By default, any state can switch to jump
        /// </summary>
        public virtual void HandleJumpReleased()
        {
            Debug.Log("Default jump ");
            if(stateHandler.pCon.IsGrounded())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jump)]);
            }
        }

        public virtual void HandleBoostPressed() { }

        public virtual void HandleBoostReleased() { }

        public virtual void HandleGraplePressed() {
            if(InventoryData.Inventory.HasActiveModule<GrapleModule>() || stateHandler.pCon.hasGraple){
                stateHandler.SwitchState(stateHandler.States[typeof(Grapple)]);
            }
        }

        public virtual void HandleGrapleReleased() {
            stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
        }

        public virtual void HandleSlide()
        {
        }

        public virtual void HandleSlideCancel()
        {
        }

        public virtual void OnTriggerEnter2D(Collider2D collision)
        {
        }

        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
        }

        // Overridable Default Methods

        #region GROUND

        public virtual void XZVelocity_Ground()
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
            //Debug.Log("Target V: " + zTargetVelocity + " Current: " + currentZVelocity + " fIn: " + fInput);
            if (zVelocityDiff < 0)
            {
                zAcceleration = (currentZVelocity > 0) ? xzDecel : xzAccel;
            }
            if (zVelocityDiff > 0)
            {
                zAcceleration = (currentZVelocity > 0) ? xzAccel : xzDecel;
            }

            // Calculate force along z-axis to apply to the player
            float zMovement = zVelocityDiff * zAcceleration;


            // X Velocity (strafe)
            float maxXSpeed = maxStrafeSpeed;
            float xTargetVelocity = hInput * maxXSpeed;

            float currentXVelocity = Vector3.Dot(stateHandler.pCon.rb.velocity, stateHandler.transform.right);
            float xVelocityDiff = xTargetVelocity - currentXVelocity;
            float xAcceleration = 0f;
            //target X less than current
            if (xVelocityDiff < 0)
            {
                xAcceleration = (currentXVelocity > 0) ? xzDecel : xzAccel;
            }
            if (xVelocityDiff > 0)
            {
                xAcceleration = (currentXVelocity > 0) ? xzDecel : xzAccel;
            }

            float xMovement = xVelocityDiff * xAcceleration;

            Vector3 movementForce = (zMovement * stateHandler.transform.forward)
                + (xMovement * stateHandler.transform.right);

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

            // apply movement on a slope
            movementForce = stateHandler.pCon.currentSurfaceQuaternion * movementForce;

            stateHandler.pCon.rb.AddForce(movementForce, ForceMode.Force);
        }

        #endregion

        #region AIR
        public virtual void XZVelocity_Air()
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

            // these multipliers dampen by how much you can acc/dec-elerate in a direction
            float airBrakeMultiplier = stats.GetStat(Stat.AirBrakeMultiplier);
            float airStrafeMultiplier = stats.GetStat(Stat.AirStrafeMultiplier);
            float airDecelMultiplier = stateHandler.pCon.airDecel;

            // Z Velocity (forward and reverse)
            float maxZSpeed = 0f;
            if (fInput > 0) { maxZSpeed = maxForwardSpeed; }
            if (fInput < 0) { maxZSpeed = maxReverseSpeed; }
            float zTargetVelocity = fInput * maxZSpeed;

            // take standard acc/dec-eleration and apply airborne multiplier
            // ONLY APPLY REVERSE ACCELERATION AS AIR STRAFE ACCELERATION
            float zAcceleration = (fInput < 0) ?
                xzAccel * airBrakeMultiplier
                : xzDecel * airDecelMultiplier;

            // Calculate difference between current velocity and desired velocity
            // using Local forward velocity
            float zVelocityDiff = zTargetVelocity
                - Vector3.Dot(stateHandler.pCon.rb.velocity, stateHandler.transform.forward);
            // Calculate force along z-axis to apply to the player
            float zMovement = zVelocityDiff * zAcceleration;


            // X Velocity (strafe)
            float maxXSpeed = maxStrafeSpeed;
            float xTargetVelocity = hInput * maxXSpeed;
            float xAcceleration = (hInput != 0) ?
                xzAccel * airStrafeMultiplier
                : xzDecel * airDecelMultiplier;
            float xVelocityDiff = xTargetVelocity
                - Vector3.Dot(stateHandler.pCon.rb.velocity, stateHandler.transform.right);
            float xMovement = xVelocityDiff * xAcceleration;

            Vector3 movementForce = (zMovement * stateHandler.transform.forward)
                + (xMovement * stateHandler.transform.right);

            stateHandler.pCon.rb.AddForce(movementForce, ForceMode.Force);
        }

        public virtual void VerticalVelocity()
        {
            //apply increased gravity
            float acceleration = (stateHandler.pCon.rb.velocity.y < 0) ? stateHandler.pCon.downwardAccel : stateHandler.pCon.upwardDecel;
            acceleration *= Time.fixedDeltaTime;
            stateHandler.pCon.rb.velocity = new Vector3(
                stateHandler.pCon.rb.velocity.x,
                stateHandler.pCon.rb.velocity.y - acceleration,
                stateHandler.pCon.rb.velocity.z);
        }
        #endregion

        public virtual void LookInCameraDirection(Vector3 cameraForward)
        {
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);

            var targetHorizontalAngle = Mathf.SmoothDampAngle(
                stateHandler.pCon.rb.rotation.eulerAngles.y,
                cameraRotation.eulerAngles.y,
                ref _tmpRotationVelocity,
                _rotationSmoothTime,
                float.MaxValue,
                Time.fixedDeltaTime);

            Quaternion targetRotation = Quaternion.Euler(0.0f, targetHorizontalAngle, 0.0f);
            stateHandler.pCon.rb.MoveRotation(targetRotation);
        }

        public virtual void HandleSprintPressed() { }
        public virtual void HandleSprintReleased() { }

        public virtual void HandleJetpackPressed() { }
        public virtual void HandleJetpackReleased() { }

        protected Vector3 Flatten(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
