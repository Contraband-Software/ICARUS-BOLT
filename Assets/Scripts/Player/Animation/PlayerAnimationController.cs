using Resources.System;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using System.Linq;
using System.Collections.ObjectModel;

namespace Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        public enum AnimStateBlends
        {
            NONE,
            FALL,
            BOOSTCHARGE,
            JETPACK,
            GLIDE
        }

        public enum AnimBlendGroups
        {
            AIR
        }


        public AnimStateBlends currentAirAnim = AnimStateBlends.NONE;

        // State
        [HideInInspector] public float lastFrameVelocityY = 0f;

        // Refs
        private PlayerController pCon;
        private PlayerStateHandler stateHandler;
        private Animator animator;
        [SerializeField] private AnimationBlendMixer playerAnimBlendMixer;

        public void Initialize(
            PlayerController pCon,
            PlayerStateHandler stateHandler,
            Animator animator)
        {
            this.pCon = pCon;
            this.stateHandler = stateHandler;
            this.animator = animator;
            playerAnimBlendMixer = GetComponent<AnimationBlendMixer>(); 
            playerAnimBlendMixer.Initialize(animator);
            ResetAirAnimBlends();
        }

        public void AnimationControl()
        {
            float planeVelocity = stateHandler.GetPlaneVelocity();
            float verticalVelocity = stateHandler.GetVerticalVelocity();
            bool isGrounded = stateHandler.pCon.IsGrounded();
            float forwardInput = stateHandler.forwardInput;
            float horizontalInput = stateHandler.horizontalInput;

            // Generic Shit
            animator.SetFloat("PlaneV", planeVelocity);
            animator.SetFloat("VerticalV", verticalVelocity);
            animator.SetBool("isGrounded", isGrounded);

            float yAcc = verticalVelocity - lastFrameVelocityY;
            lastFrameVelocityY = verticalVelocity;
            animator.SetFloat("VerticalAcc", yAcc);


            // Grounded BlendTree Control
            float maxRunSpeed = pCon.Stats.GetStat(Stat.RunSpeed);

            // "RunSpeed" is a percent of our maximum speed in that direction
            // "GroundAnimSp" is the multiplier of the speed of the blendtree
            // if isGrounded()
            float runSpeedDirectionalMax = 0f;
            float forwardRunMax = maxRunSpeed;
            float strafeRunMax = pCon.Stats.GetStat(Stat.RunSpeedStrafe);
            float reverseRunMax = pCon.Stats.GetStat(Stat.RunSpeedReverse);

            // Intent to move forward
            if (forwardInput > 0)
            {
                runSpeedDirectionalMax = forwardRunMax * forwardInput;
            }
            // Intent to reverse
            else if (forwardInput < 0)
            {
                runSpeedDirectionalMax = reverseRunMax * MathF.Abs(forwardInput);
            }
            // Intent to strafe
            if (horizontalInput != 0)
            {
                runSpeedDirectionalMax += strafeRunMax * MathF.Abs(horizontalInput);
            }

            //prevent division by zero
            if (runSpeedDirectionalMax == 0) { runSpeedDirectionalMax = maxRunSpeed; }


            // blendtree animation speed should be percent of our speed
            // vs maximum speed
            float groundAnimSpeed = 1f + planeVelocity / maxRunSpeed;

            //dampen RunSpeed variable scaled by accel/deceleration?

            ControlAirAnimSpeed();

            animator.SetFloat("RunSpeed", planeVelocity / runSpeedDirectionalMax, 0.15f, Time.deltaTime);
            animator.SetFloat("GroundAnimSp", groundAnimSpeed);
        }
        private void ControlAirAnimSpeed()
        {
            float verticalVelocity = stateHandler.GetVerticalVelocity();

            if (currentAirAnim == AnimStateBlends.FALL)
            {
                // min anim speed = 0.25
                // max = 1, at y vel -40
                animator.SetFloat(
                    "AirSpeed",
                    Mathf.Clamp(verticalVelocity / -40f, 0.25f, 1f),
                    0.1f, Time.deltaTime);
            }
            else if (currentAirAnim == AnimStateBlends.BOOSTCHARGE)
            {
                // min anim speed = 0.6
                // max = 1, at y vel -40
                animator.SetFloat(
                    "AirSpeed",
                    Mathf.Clamp(verticalVelocity / -40f, 0.6f, 1f),
                    0.1f, Time.deltaTime);
            }
            else if (currentAirAnim == AnimStateBlends.JETPACK)
            {
                animator.SetFloat("AirSpeed", 1f);
            }
            else if (currentAirAnim == AnimStateBlends.GLIDE)
            {
                animator.SetFloat("AirSpeed", 1f);
            }
        }


        public void SetAirAnimationState(AnimStateBlends state)
        {
            currentAirAnim = state;

            // Start decay for other states
            foreach (AnimStateBlends animState in Enum.GetValues(typeof(AnimStateBlends)))
            {
                if (animState == currentAirAnim) continue;
                if (animState == AnimStateBlends.NONE) continue;
                playerAnimBlendMixer.StartDefaultFadeOut(AnimBlendGroups.AIR, animState);
            }
        }

        public void SetAirAnimationStateBlendValue(AnimStateBlends state, float v)
        {
            playerAnimBlendMixer.SetComponentValueAndStopFade(AnimBlendGroups.AIR, state, v);
        }

        public void FadeInAirAnimBlend(AnimStateBlends state, float? rate = null, Easing.EaseType? ease = null)
        {
            playerAnimBlendMixer.StartFade(AnimBlendGroups.AIR, state, 1f, rate, ease);
        }

        public void ResetAirAnimBlends()
        {
            playerAnimBlendMixer.HardResetAllOfGroup(AnimBlendGroups.AIR);
        }

    }
}
