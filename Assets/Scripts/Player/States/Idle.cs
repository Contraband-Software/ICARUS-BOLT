using Player;
using Software.Contraband.StateMachines;
using UnityEngine;

namespace Player
{
    [DefaultState]
    public class Idle : PlayerBaseState
    {

        public Idle(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.allowBoost = true;
            // rejump tech
            if (stateHandler.JumpBufferActive())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jump)]);
                return;
            }

            stateHandler.animator.SetBool("isIdle", true);
            stateHandler.pCon.lastVelocity = stateHandler.pCon.rb.velocity;
            stateHandler.pCon.entryVelocity = stateHandler.pCon.rb.velocity;
            stateHandler.pCon.currentStateFrame = 0;
            stateHandler.pCon.entryVelocityCompensated = false;

            stateHandler.pCon.ToggleStationaryPhysicMaterial(true);

            stateHandler.ikCon.IKIdleEntry();
            stateHandler.ikCon.ActivateRig(IKController.IKRig.LegsLook);
            stateHandler.ikCon.ActivateRig(IKController.IKRig.HeadLook);
            stateHandler.ikCon.ActivateRig(IKController.IKRig.TorsoLook);

            stateHandler.animCon.SetAirAnimationState(PlayerAnimationController.AnimStateBlends.NONE);

            VelocityControl();
        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isIdle", false);
            stateHandler.pCon.ToggleStationaryPhysicMaterial(false);
        }

        public override void UpdateState()
        {
            //if (stateHandler.JumpBufferActive()) return;

            // TRANSITION TO MANTLE
            if (stateHandler.CheckCanMantle())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Mantle)]);
            }


            //TRANSITION TO RUNNING
            if (Mathf.Abs(stateHandler.forwardInput) + Mathf.Abs(stateHandler.horizontalInput) != 0)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Run)]);
            }

            stateHandler.ikCon.IKIdle(stateHandler.pCon.transform);
            stateHandler.ikCon.IKGliderArms(stateHandler.pCon.forceGliderTilt, stateHandler.pCon.fakeGliderHorizontalInput);
        }

        public override void FixedUpdateState()
        {
            //if (stateHandler.JumpBufferActive()) return;

            stateHandler.pCon.CompensateGroundEntryVelocity();

            //TRANSITION TO FALLING
            if (!stateHandler.pCon.IsGrounded() && stateHandler.pCon.rb.velocity.y < 0)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
            }

            VelocityControl();
        }

        private void VelocityControl()
        {
            float slopeMultiplier = stateHandler.pCon.GetSlopeMultiplier(
                stateHandler.pCon.currentSurfaceSlope,
                stateHandler.pCon.currentSurfaceSlopeRelativeToPlayer);

            if (stateHandler.pCon.IsGrounded() && slopeMultiplier == 0)
            {
                XZVelocity_Air();
            }
            else
            {
                XZVelocity_Ground();
            }
            VerticalVelocity();
        }

        #region MOVEMENT_MANIPULATION
        #endregion
    }
}
