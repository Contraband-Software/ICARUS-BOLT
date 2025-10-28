using Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        [HideInInspector] public PlayerController pCon;

        [Header("Gliding")]
        [SerializeField] TrailRenderer glide_trail_left;
        [SerializeField] TrailRenderer glide_trail_right;
        private MixedRefValue glide_trail_left_length_fader;
        private MixedRefValue glide_trail_right_length_fader;
        [SerializeField] float glide_trail_time_default = 0.1f;
        [SerializeField] float glide_trail_time_banking = 0.65f;
        [SerializeField] float glide_trail_time_fade_in_rate = 0.5f;
        [SerializeField] float glide_trail_time_fade_out_rate = 0.5f;

        [Header("Fall")]
        [SerializeField] VisualEffect land_soil;

        // Start is called before the first frame update
        void Start()
        {
            InititializeEffects();
        }

        private void InititializeEffects()
        {
        
            glide_trail_left_length_fader = new MixedRefValue(
                this,
                () => glide_trail_left.time,
                v => glide_trail_left.time = v,
                fInEase: Easing.EaseType.EaseOutQuad, fOutEase: Easing.EaseType.EaseOutQuad,
                fInR: glide_trail_time_fade_in_rate, fOutR: glide_trail_time_fade_out_rate);
            glide_trail_right_length_fader = new MixedRefValue(
                this,
                () => glide_trail_right.time,
                v => glide_trail_right.time = v,
                fInEase: Easing.EaseType.EaseOutQuad, fOutEase: Easing.EaseType.EaseOutQuad,
                fInR: glide_trail_time_fade_in_rate, fOutR: glide_trail_time_fade_out_rate);
        }

        #region GLIDING
        public void GliderTrail_ON()
        {
            if(glide_trail_left.emitting) return; // dont re-toggle
            glide_trail_left_length_fader.SetValueAndStopFade(glide_trail_time_default);
            glide_trail_right_length_fader.SetValueAndStopFade(glide_trail_time_default);
            glide_trail_left.emitting = true;
            glide_trail_right.emitting = true;

        }

        public void GliderTrail_OFF()
        {
            if (!glide_trail_left.emitting) return; // dont re-toggle
            glide_trail_left.emitting = false;
            glide_trail_right.emitting = false;
            glide_trail_left_length_fader.SetValueAndStopFade(glide_trail_time_default);
            glide_trail_right_length_fader.SetValueAndStopFade(glide_trail_time_default);
        }

        public void GlideTrail_Banking_ON()
        {
            glide_trail_left_length_fader.StartFade(glide_trail_time_banking);
            glide_trail_right_length_fader.StartFade(glide_trail_time_banking);
        }

        public void GlideTrail_Banking_OFF()
        {
            glide_trail_left_length_fader.StartFade(glide_trail_time_default);
            glide_trail_right_length_fader.StartFade(glide_trail_time_default);
        }
        #endregion


        #region FALLING
        public void VFX_LandSoil_Play()
        {
            float impactVelocity = pCon.HighestRecentFallVelocity;
            Vector3 velocity = pCon.rb.velocity;
            velocity.y = 0f;

            land_soil.SetFloat("ImpactVelocity", impactVelocity);
            land_soil.SetVector3("Velocity", velocity);
            land_soil.SendEvent("OnPlay");
        }
        #endregion
    }
}
