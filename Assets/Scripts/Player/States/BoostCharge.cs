using Resources.System;
using Software.Contraband.StateMachines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class BoostCharge : PlayerBaseState
    {
        float upDecel;
        float downAccel;
        float entryTime;
        float baseTimeToMaxCharge;

        public BoostCharge(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.animator.SetBool("isBoostCharge", true);
            upDecel = stateHandler.pCon.upDecelBoost;
            downAccel = stateHandler.pCon.downAccelBoost;
            entryTime = Time.time;
            baseTimeToMaxCharge = stateHandler.pCon.Stats.GetBaseStatInfo(
                Stat.TimeToMaxChargeJump).value;

            // cut horizontal velocity 
            Vector3 velocity = stateHandler.pCon.rb.velocity;
            velocity.x *= stateHandler.pCon.velocityCut;
            velocity.z *= stateHandler.pCon.velocityCut;
            stateHandler.pCon.rb.velocity = velocity;

            stateHandler.animCon.SetAirAnimationState(PlayerAnimationController.AnimStateBlends.BOOSTCHARGE);
            stateHandler.animCon.FadeInAirAnimBlend(PlayerAnimationController.AnimStateBlends.BOOSTCHARGE, 1f);
        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isBoostCharge", false);
            stateHandler.pCon.playerStateShared.GeneralChargePercent.v = 0f;
            stateHandler.lastBoostDuration = Time.time - entryTime;
            stateHandler.animator.SetFloat("GlideBlend", 0f);
            stateHandler.allowBoost = false;
        }

        public override void UpdateState()
        {
            // if boost is held too long (longer than BASE TIME TO MAX CHARGE)
            // then make the downward acceleration normal
            float gracePeriod = stateHandler.pCon.boostGracePeriod;
            var t = Time.time - entryTime;
            if(Time.time - entryTime > baseTimeToMaxCharge + gracePeriod)
            {
                downAccel = stateHandler.pCon.downwardAccel;
            }
            //Debug.Log(t);

            UpdateGeneralChargePercentage();

            // shrink camera base Z value to get closer as youre charging
        }

        public override void FixedUpdateState()
        {

            if (stateHandler.pCon.IsGrounded())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
            }

            XZVelocity_Air();
            VerticalVelocity();
        }

        public override void HandleJumpPressed()
        {
            // avoid default jump transition
            return;
        }

        public override void HandleJumpReleased()
        {
            // avoid default jump transition
            return;
        }

        public override void HandleBoostReleased()
        {
            // TRANSITION TO BOOST
            stateHandler.SwitchState(stateHandler.States[typeof(Boost)]);
        }

        private void UpdateGeneralChargePercentage()
        {
            float timeToMaxCharge = stateHandler.pCon.Stats.GetStat(Stat.TimeToMaxChargeJump);
            float jumpChargeDuration = Time.time - entryTime;
            float powerPercentage = Mathf.Clamp(
                (Mathf.Pow(jumpChargeDuration, 2) + 0.1f) / timeToMaxCharge,
                0f,
                1f);
            stateHandler.pCon.playerStateShared.GeneralChargePercent.v = powerPercentage;
        }

        #region MOVEMENT_MANIPULATION
        public override void VerticalVelocity()
        {
            //apply decreased gravity to maintain altitude
            float acceleration = (stateHandler.pCon.rb.velocity.y < 0) ? downAccel : upDecel;
            acceleration *= Time.fixedDeltaTime;
            stateHandler.pCon.rb.velocity = new Vector3(
                stateHandler.pCon.rb.velocity.x,
                stateHandler.pCon.rb.velocity.y - acceleration,
                stateHandler.pCon.rb.velocity.z);
        }
        #endregion
    }
}
