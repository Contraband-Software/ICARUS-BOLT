using Software.Contraband.StateMachines;
using UnityEngine;
using Resources;
using Resources.System;
using Resources.Modules;
using Resources.Firmware;
using Content.System.Firmware;
using Content.System.Modules;
using ProgressionV2;

namespace Player
{
    public class Jump : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;

        public Jump(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        bool jetpackBuffered = false;
        float timeInState = 0;

        protected override void EnterState()
        {
            float chargeTime = stateHandler.jumpChargeTime;
            //if (stateHandler.JumpBufferActive()) chargeTime = stateHandler.bufferedJumpChargeTime;
            stateHandler.ClearJumpBuffer();
            stateHandler.animator.SetTrigger("Jump");
            Debug.Log("ENTER JUMP");
            Vector3 velocity = stateHandler.pCon.rb.velocity;
            float jumpForce = stateHandler.CalculateJumpPower(chargeTime);

            Debug.Log("Jump Power: " + jumpForce);

            jetpackBuffered = false;
            timeInState = 0;

            velocity.y = jumpForce;

            // Boost directional velocity -> Jump Distance
            float jumpDistance = stateHandler.pCon.Stats.GetStat(Stat.PrimaryJumpDistance);

            velocity.x *= jumpDistance;
            velocity.z *= jumpDistance;

            stateHandler.pCon.rb.velocity = velocity;

            // Deplete fuel
            float deplete = (jumpForce / stateHandler.pCon.Stats.GetStat(Stat.PrimaryJumpHeight));
            deplete *= stateHandler.pCon.Stats.GetStat(Stat.JumpMaxFuelUse);
            deplete -= 1;
            Mathf.Clamp(deplete, 0, Mathf.Infinity);
            deplete *= stateHandler.pCon.Stats.GetStat(Stat.FuelEfficiency);
            stateHandler.pCon.Fuel.AddLevel(Fuel.Tank.Legs, -deplete);

            stateHandler.animCon.SetAirAnimationStateBlendValue(PlayerAnimationController.AnimStateBlends.FALL, 0f);
        }
        protected override void ExitState()
        {
            stateHandler.animator.ResetTrigger("Jump");
        }

        public override void UpdateState()
        {
            // TRANSITION TO MANTLE
            if (stateHandler.CheckCanMantle())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Mantle)]);
            }
        }

        public override void FixedUpdateState()
        {

            timeInState += Time.fixedDeltaTime;

            //TRANSITION TO FALLING
            if (stateHandler.pCon.rb.velocity.y < 0)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
            }
            // TRANSITION TO JETPACK
            CheckJetpack();

            XZVelocity_Air();
            VerticalVelocity();
        }

        private void CheckJetpack()
        {
            if (timeInState > 0.2f)
            {
                jetpackBuffered = true;
            }

            if (!stateHandler.holdingJetpack)
            {
                jetpackBuffered = true;
                return;  
            }
            if (!jetpackBuffered)
            {
                return;
            }
            if (InventoryData.Inventory.HasActiveModule<JetpackModule>()
                || stateHandler.pCon.hasJetpack)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jetpack)]);
            }
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

        public override void HandleJetpackPressed()
        {
            //avoid default
            return;
        }

        public override void HandleBoostPressed()
        {
            //TRANSITION TO BOOST CHARGE
            if (stateHandler.allowBoost && !stateHandler.pCon.IsGrounded())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(BoostCharge)]);
            }
        }

        #region MOVEMENT_MANIPULATION
        #endregion
    }
}