using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

namespace Player
{
    public class AnimationBlendComponent : MixedValue
    {
        private readonly string blendString;
        private Animator animator;

        public AnimationBlendComponent(
            Animator animator,
            MonoBehaviour monoHook,
            string blendString,
            float fInR,
            float fOutR,
            Easing.EaseType fInEase = Easing.EaseType.Linear,
            Easing.EaseType fOutEase = Easing.EaseType.Linear
            ) : base(monoHook, fInR, fOutR, fInEase, fOutEase)
        {
            this.animator = animator;
            this.blendString = blendString;

            NoAnimator();
            InvalidBlendString();
        }

        private void NoAnimator()
        {
            if (!animator)
            {
                Debug.LogWarning("Animator for " + blendString + " is invalid");
            }
        }
        private void InvalidBlendString()
        {
            if (!animator) return;
            bool valid = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == blendString)
                {
                    valid = true;
                    break;
                }
            }
            if (!valid)
            {
                Debug.LogWarning("Animator invalid parameter:  "  + blendString);
            }
        }
        protected override void SetValue(float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);
            animator.SetFloat(blendString, value);
        }

        public override float GetValue()
        {
            return animator.GetFloat(blendString);
        }
    }
}
