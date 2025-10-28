using Content.System.Modules;
using Resources.System;
using Software.Contraband.StateMachines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProgressionV2;

namespace Player
{
    public class Falling : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;
        private PlayerAnimationController.AnimStateBlends animState;

        public Falling(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.animator.SetBool("isFalling", true);
            animState = PlayerAnimationController.AnimStateBlends.FALL;
            stateHandler.animCon.SetAirAnimationState(animState);
            stateHandler.animCon.FadeInAirAnimBlend(animState, 2f);
            Debug.Log("ENTER FALLING");
        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isFalling", false);
        }

        public override void UpdateState()
        {
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
                if(slopeMultiplier > 0)
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

            //TRANSITION TO JETPACK
            CheckJetpack();
            //CHECK GLIDE
            CheckGlide();

            XZVelocity_Air();
            VerticalVelocity();
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

        private void CheckGlide()
        {
            if(!stateHandler.holdingGlide)
            {
                return;
            }
            if(InventoryData.Inventory.HasActiveModule<GliderModule>()
                || stateHandler.pCon.hasGlider)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Glide)]);
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
            return;
        }

        public override void HandleJetpackReleased()
        {
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

        #endregion
    }
}
