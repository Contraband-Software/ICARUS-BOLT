using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

namespace Player
{
    [Serializable]
    public struct AnimBlendComponentEntry : IKeyedEntry<PlayerAnimationController.AnimStateBlends>
    {
        public PlayerAnimationController.AnimStateBlends animState;
        public string parameterName;
        public float defaultFadeInRate;
        public float defaultFadeOutRate;
        public Easing.EaseType defaultFadeInEase;
        public Easing.EaseType defaultFadeOutEase;

        public PlayerAnimationController.AnimStateBlends GetKey() => animState;
    }

    public class AnimationBlendMixer : GeneralMixer<
        PlayerAnimationController.AnimBlendGroups,
        PlayerAnimationController.AnimStateBlends,
        AnimBlendComponentEntry,
        AnimationBlendComponent>
    {
        private Animator animator;

        public void Initialize(Animator animator)
        {
            this.animator = animator;
            InitializeMixingGroupMap();
        }

        protected override AnimationBlendComponent LoadMixedComponent(AnimBlendComponentEntry entry)
        {
            AnimationBlendComponent blendComponent = new AnimationBlendComponent(
                animator, this, entry.parameterName, entry.defaultFadeInRate,
                entry.defaultFadeOutRate, entry.defaultFadeInEase, entry.defaultFadeOutEase
             );
            return blendComponent;
        }
    }


}
