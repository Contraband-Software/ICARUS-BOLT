using Content.System.Modules;
using Resources.System;
using Resources;
using Software.Contraband.StateMachines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProgressionV2;
using Helpers;

namespace Player
{
    public class Jetpack : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;

        public Jetpack(PlayerStateHandler stateHandler)
            : base(stateHandler) { }


        private float maxVerticalVelocity = 6.75f;

        private float dragStrength = 0.405f; 

        protected override void EnterState()
        {
            stateHandler.animator.SetBool("isJetpacking", true);
            Debug.Log("ENTER JETPACK");
            stateHandler.animCon.SetAirAnimationState(PlayerAnimationController.AnimStateBlends.JETPACK);
            stateHandler.animCon.FadeInAirAnimBlend(PlayerAnimationController.AnimStateBlends.JETPACK, 3.5f);
            stateHandler.ikCon.IKJetpackEntry();
            stateHandler.ikCon.FadeInRig(IKController.IKRig.JetpackTilt, 1.5f, Easing.EaseType.EaseOutQuad);
        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isJetpacking", false);
            stateHandler.ikCon.IKJetpackExit();
            //stateHandler.ikCon.FadeOutRig(IKController.IKRig.JetpackTilt, 1.5f, EasingHelper.EaseType.EaseOutQuad);
        }

        public override void UpdateState()
        {
            // Deplete fuel
            float deplete = stateHandler.pCon.Stats.GetStat(Stat.JetpackFuelUse) * -1f;
            deplete *= (stateHandler.pCon.Stats.GetStat(Stat.FuelEfficiency) * Time.deltaTime);
            stateHandler.pCon.Fuel.AddLevel(Fuel.Tank.Propulsion, deplete);
        }

        public override void FixedUpdateState()
        {
            //Able to transition to running/idle if we are not jetpacking

            //TRANSITION TO Idle/RUN
            if (stateHandler.pCon.IsGrounded())
            {

                if (stateHandler.pCon.Fuel.PropulsionLevel == 0
                || !stateHandler.holdingJetpack)
                {
                    if (stateHandler.forwardInput != 0 || stateHandler.horizontalInput != 0)
                    {
                        stateHandler.SwitchState(stateHandler.States[typeof(Run)]);
                    }
                    else
                    {
                        stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
                    }
                }
            }

            // TRANSITION TO FALLING IF FUEL RUNS OUT, OR WE LET GO OF JETPACK BUTTON
            if(stateHandler.pCon.Fuel.PropulsionLevel == 0
                || !stateHandler.holdingJetpack)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
            }

            JetpackVelocity();


            XZVelocity_Air();
            // VerticalVelocity();

            stateHandler.ikCon.IKDirectionForward(
                Easing.EaseType.EaseOutQuad,
                100f,
                30f,
                Easing.EaseType.EaseOutQuad,
                100f,
                30f
                );
            stateHandler.ikCon.IKJetpackTilt(
                stateHandler.forwardInput,
                stateHandler.horizontalInput);
        }

        public override void HandleJumpPressed()
        {
            // avoid default
            return;
        }
        public override void HandleJumpReleased()
        {
            // avoid default
            return;
        }

        public override void HandleJetpackReleased()
        {
            // avoid default
            return;
        }

        public override void HandleBoostPressed()
        {
            //TRANSITION TO BOOST CHARGE
            if (stateHandler.allowBoost)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(BoostCharge)]);
            }
        }

        #region MOVEMENT_MANIPULATION
        private void JetpackVelocity()
        {
            if(stateHandler.pCon.rb.velocity.y < maxVerticalVelocity)
            {
                float thrust = stateHandler.pCon.Stats.GetStat(Stat.JetpackPower);
                stateHandler.pCon.rb.AddForce(Vector3.up * thrust, ForceMode.Force);
            }
        }

        public override void XZVelocity_Air()
        {


            // --- Input --- 
            float fInput = stateHandler.forwardInput;
            float hInput = stateHandler.horizontalInput;

            // --- Get Values ---
            UpgradeableStats stats = stateHandler.pCon.Stats;
            float jetpackHandling = stats.GetStat(Stat.JetpackHandling);
            float jetpackSpeed = stats.GetStat(Stat.JetpackSpeed);
            float xzAccel = stats.GetStat(Stat.RunAcceleration);
            float xzDecel = stats.GetStat(Stat.RunDeceleration);
            float maxStrafeSpeed = stats.GetStat(Stat.RunSpeedStrafe);
            // these multipliers dampen by how much you can acc/dec-elerate in a direction
            float airBrakeMultiplier = stats.GetStat(Stat.AirBrakeMultiplier);
            float airStrafeMultiplier = stats.GetStat(Stat.AirStrafeMultiplier);
            float airDecelMultiplier = stateHandler.pCon.airDecel;

            Vector3 velocity = stateHandler.pCon.rb.velocity;
            Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float flatSpeed = flatVelocity.magnitude;

            Vector3 forward = stateHandler.pCon.transform.forward;
            Vector3 right = stateHandler.pCon.transform.right;

            float fInputAbs = Mathf.Abs(fInput);
            float hInputAbs = Mathf.Abs(hInput);
            float fInputSign = Mathf.Sign(fInput);
            float hInputSign = Mathf.Sign(hInput);

            float forwardVel = Vector3.Dot(flatVelocity, forward);
            float reverseVel = Vector3.Dot(flatVelocity, -forward);
            float rightVel = Vector3.Dot(flatVelocity, right);

            // Clamp tiny velocities to 0
            if (Mathf.Abs(forwardVel) < 0.01f) forwardVel = 0f;
            if (Mathf.Abs(rightVel) < 0.01f) rightVel = 0f;

            // -- Drag opposes velocity direction
            Vector3 dragForce = Vector3.zero;
            if (flatSpeed > jetpackSpeed)
            {
                float excessSpeed = flatSpeed - jetpackSpeed;
                Vector3 dragDir = -(flatVelocity.normalized);
                dragForce = dragStrength * excessSpeed * dragDir;
            }

            // --- Forward/Reverse Velocity --- 
            Vector3 zAccelForce;
            if(fInput != 0){
                zAccelForce = xzAccel * airStrafeMultiplier * jetpackHandling
                    * fInputSign * forward * 10f;
            }
            else{
                float decel = xzDecel * airStrafeMultiplier * 10f;
                float decelAmount = Mathf.Min(Mathf.Abs(forwardVel), decel);
                zAccelForce = decelAmount * Mathf.Sign(forwardVel) * -forward;
            }

            if (fInput > 0 
                && Mathf.Sign(forwardVel) == fInputSign
                && Mathf.Abs(forwardVel) >= jetpackSpeed * fInputAbs)
            {
                zAccelForce = Vector3.zero;
            }
            if(fInput < 0
                && Mathf.Sign(reverseVel) * -1f == fInputSign
                && Mathf.Abs(reverseVel) >= jetpackSpeed * fInputAbs)
            {
                zAccelForce = Vector3.zero;
            }

            // --- Strafe Velocity --- 
            Vector3 xAccelForce;
            if(hInput != 0){
                xAccelForce = xzAccel * airStrafeMultiplier * jetpackHandling
                    * hInputSign * right * 10f;
            }
            else{
                float decel = xzDecel * airStrafeMultiplier * 10f;
                float decelAmount = Mathf.Min(Mathf.Abs(rightVel), decel);
                xAccelForce = decelAmount * Mathf.Sign(rightVel) * -right;
            }

            if(hInput != 0 
                && Mathf.Sign(rightVel) == hInputSign
                && Mathf.Abs(rightVel) >= maxStrafeSpeed * hInputAbs){
                xAccelForce = Vector3.zero;
            }

            Vector3 movementForce = zAccelForce + xAccelForce + dragForce;
            stateHandler.pCon.rb.AddForce(movementForce, ForceMode.Acceleration);
        }
        #endregion
    }
}
