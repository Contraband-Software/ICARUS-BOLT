using Content.System.Modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

namespace Player
{
    public class Glide : PlayerBaseState
    {
        public Glide(PlayerStateHandler stateHandler)
            : base(stateHandler) { }


        private PlayerAnimationController.AnimStateBlends animState;

        private float bankingPower = 0f; // Value between -1 and 1. -1: Fully Left Banking, 1: Fully Right Banking.
        private bool isBanking = false;

        private float glideFallRate = 1.1f;
        private float gliderForwardAccel = 2.43f;
        private float gliderBankAccel = 1.5f;
        private float gliderBankDecel = 2f;
        private float maxGlideSpeed = 13.5f;
        private float maxGlideBankForce = 8.1f;

        private float glideSpeedHardLimit = 21.6f;
        private float glideDragStrength = 0.55f;

        private float gliderYVelocitySmoothDamp = 0f;
        private float gliderSmoothingTime = 0.5f;

        protected override void EnterState()
        {
            gliderYVelocitySmoothDamp = 0f;
            isBanking = false;
            bankingPower = 0f;
            stateHandler.animator.SetBool("isGliding", true);
            animState = PlayerAnimationController.AnimStateBlends.GLIDE;
            stateHandler.animCon.SetAirAnimationState(animState);
            stateHandler.pCon.ToggleGliderModel(true);

            stateHandler.animCon.FadeInAirAnimBlend(animState, 2f);
            stateHandler.ikCon.ActivateRig(IKController.IKRig.GliderRotations);
            stateHandler.ikCon.FadeInRig(IKController.IKRig.GliderArms, 2f, Easing.EaseType.EaseOutExpo);
            stateHandler.ikCon.IKGliderEntry(stateHandler.pCon.transform.forward, stateHandler.pCon.rb.velocity);

            stateHandler.events.glider.OnGliderEntry.Invoke();
        }

        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isGliding", false);
            stateHandler.pCon.ToggleGliderModel(false);

            stateHandler.ikCon.DeactivateRig(IKController.IKRig.GliderRotations);
            stateHandler.ikCon.FadeOutRig(IKController.IKRig.GliderArms, 4f, Easing.EaseType.EaseOutExpo);

            stateHandler.events.glider.OnGliderExit.Invoke();
        }

        public override void UpdateState()
        {
            ControlBanking();

            stateHandler.ikCon.IKGlider(
                stateHandler.pCon.transform.forward,
                stateHandler.pCon.rb.velocity,
                stateHandler.pCon.rb,
                bankingPower);
            stateHandler.ikCon.IKGliderArms(bankingPower, stateHandler.horizontalInput);
        }

        public override void LateUpdateState()
        {
        }

        // Disallow rotating character with look
        public override void LookInCameraDirection(Vector3 cameraForward)
        {
            return;
        }

        public override void FixedUpdateState()
        {
            XZVelocity_Air();
            VerticalVelocity();

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
                        stateHandler.SwitchState(stateHandler.States[typeof(Run)]);
                        return;
                    }
                    else
                    {
                        stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
                        return;
                    }
                }
            }


            if(Mathf.Abs(bankingPower) > 0.3f && !isBanking)
            {
                isBanking = true;
                stateHandler.events.glider.OnGliderBankingEntry.Invoke();
            }
            if(Mathf.Abs(bankingPower) <= 0.3f && isBanking)
            {
                isBanking = false;
                stateHandler.events.glider.OnGliderBankingExit.Invoke();
            }

            //TRANSITION TO FALLING
            CheckGliding();
        }

        private void CheckGliding()
        {
            if(!stateHandler.holdingGlide)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
                return;
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

        #region MOVEMENT_MANIPULATION
        private void ControlBanking()
        {
            float hInput = stateHandler.horizontalInput;
            // Banking Power update
            if (hInput != 0)
            {
                float accel = gliderBankAccel;
                // If we are banking, then change direction, double the acceleration of banking
                //if (Mathf.Sign(hInput) != Mathf.Sign(bankingPower)) accel *= 2f;
                bankingPower += hInput * accel * Time.deltaTime;

            }
            else
            {
                // Decay to 0 when no input
                float absPower = Mathf.Abs(bankingPower);
                float decel = 0f;
                if (absPower > 0.5f)
                {
                    decel = gliderBankDecel;
                }
                else
                {
                    // Remap [0, 0.5] -> [0, 1] for a smooth fade-out
                    float t = absPower / 0.5f;
                    decel = gliderBankDecel * t;
                }
                bankingPower = Mathf.MoveTowards(bankingPower, 0f, decel * Time.deltaTime);
            }
            bankingPower = Mathf.Clamp(bankingPower, -1f, 1f);
        }



        public override void VerticalVelocity()
        {
            // apply updraft
            if (stateHandler.pCon.rb.velocity.y < 0)
            {
                float targetFallSpeed = -glideFallRate;
                float newYVelocity = Mathf.SmoothDamp(
                    stateHandler.pCon.rb.velocity.y,
                    targetFallSpeed,
                    ref gliderYVelocitySmoothDamp,
                    gliderSmoothingTime
                  );

                stateHandler.pCon.rb.velocity = new Vector3(
                    stateHandler.pCon.rb.velocity.x,
                    newYVelocity,
                    stateHandler.pCon.rb.velocity.z);
            }
        }
        public override void XZVelocity_Air()
        {
            // --- Input ---
            float hInput = stateHandler.horizontalInput;
            hInput = hInput == 0 ? 0 : Mathf.Sign(hInput);

            // --- Forward Velocity ---
            Vector3 velocity = stateHandler.pCon.rb.velocity;
            Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            Vector3 forward = flatVelocity.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float forwardVel = Vector3.Dot(flatVelocity, forward);
            float targetForwardVel = maxGlideSpeed;

            Vector3 forwardDragForce = Vector3.zero;
            if(forwardVel > glideSpeedHardLimit)
            {
                float excessSpeed = forwardVel - glideSpeedHardLimit;
                Vector3 dragDir = -forward;
                forwardDragForce = dragDir * excessSpeed * glideDragStrength;
            }

            Vector3 forwardAccelForce = forward * gliderForwardAccel;
            if (Mathf.Abs(forwardVel) >= maxGlideSpeed) forwardAccelForce = Vector3.zero;

            Debug.Log("Glide SPeed: " + forwardVel + " Fwd Force: " + forwardAccelForce + " Drag: " + forwardDragForce);

            Vector3 sideBankingForce = right * bankingPower * maxGlideBankForce;
            Debug.Log("Banking: " + bankingPower + " BankingForce: " + sideBankingForce);

            // --- Final force ---
            Vector3 finalForce = forwardAccelForce + sideBankingForce + forwardDragForce;
            //Vector3 finalForce = forwardAccelForce;
            stateHandler.pCon.rb.AddForce(finalForce, ForceMode.Acceleration);

        }
        #endregion
    }
}
