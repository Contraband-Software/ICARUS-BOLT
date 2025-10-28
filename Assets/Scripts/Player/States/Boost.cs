using Content.System.Modules;
using Resources;
using Resources.System;
using Software.Contraband.StateMachines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using ProgressionV2;

namespace Player
{
    public class Boost : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;

        public Boost(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.animator.SetBool("isBoost", true);
            Debug.Log("ENTER BOOST");
            // Boost in facing direction
            float boostForce = stateHandler.CalculateBoostPower();
            stateHandler.pCon.rb.AddForce(
                stateHandler.transform.forward *  boostForce, ForceMode.Impulse );

            // Deplete fuel
            float percent = (boostForce / stateHandler.pCon.Stats.GetStat(Stat.BoostPower));
            float deplete = stateHandler.pCon.Stats.GetStat(Stat.BoostMinFuelUse)
                + (stateHandler.pCon.Stats.GetStat(Stat.BoostMaxFuelUse) * percent);
            Mathf.Clamp(deplete, 0, Mathf.Infinity);
            deplete *= stateHandler.pCon.Stats.GetStat(Stat.FuelEfficiency);
            stateHandler.pCon.Fuel.AddLevel(Fuel.Tank.Propulsion, -deplete);

            stateHandler.animator.SetTrigger("Boost");
            stateHandler.animCon.SetAirAnimationState(PlayerAnimationController.AnimStateBlends.FALL);
            stateHandler.animCon.FadeInAirAnimBlend(PlayerAnimationController.AnimStateBlends.FALL, 1f);
            stateHandler.animCon.SetAirAnimationStateBlendValue(PlayerAnimationController.AnimStateBlends.BOOSTCHARGE, 0f);
        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isBoost", false);
            stateHandler.animator.ResetTrigger("Boost");
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
            //TRANSITION TO Idle / RUNNING
            if (stateHandler.pCon.IsGrounded())
            {
                //call to recalculate so that we are sure the slope value
                // is up-to-date
                stateHandler.pCon.calculateOfSlopeBelowPlayer();
                // Dont allow if we are to enter an impossible slope
                float slopeMultiplier = stateHandler.pCon.GetSlopeMultiplier(
                    stateHandler.pCon.currentSurfaceSlope,
                    stateHandler.pCon.currentSurfaceSlopeRelativeToPlayer);
                if (slopeMultiplier > 0)
                {
                    if (stateHandler.forwardInput != 0 || stateHandler.horizontalInput != 0)
                    {
                        stateHandler.pCon.RespondToLanding();
                        stateHandler.SwitchState(stateHandler.States[typeof(Run)]);
                    }
                    else
                    {
                        stateHandler.pCon.RespondToLanding();
                        stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
                    }
                }
            }

            CheckJetpack();
            CheckGlider();
            XZVelocity_Air();
            VerticalVelocity();

            stateHandler.ikCon.IKDirectionForward(
                Easing.EaseType.EaseOutCubic,
                200f,
                30f,
                Easing.EaseType.EaseOutCubic,
                200f,
                30f
                );
        }

        private void CheckJetpack()
        {
            if (!stateHandler.holdingJetpack)
            {
                return;
            }
            if (InventoryData.Inventory.HasActiveModule<JetpackModule>()
                || stateHandler.pCon.hasJetpack)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jetpack)]);
            }
        }
        private void CheckGlider()
        {
            if (!stateHandler.holdingGlide)
            {
                return;
            }
            if(InventoryData.Inventory.HasActiveModule<GliderModule>()
                || stateHandler.pCon.hasGlider)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Glide)]);
            }
        }

        public override void HandleJetpackPressed()
        {
            //avoid default
            return;
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

        #region MOVEMENT_MANIPULATION
        #endregion
    }
}
