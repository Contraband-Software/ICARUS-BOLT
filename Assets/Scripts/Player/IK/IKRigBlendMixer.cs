using Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Player
{
    [Serializable]
    public struct RigBlendComponentEntry : IKeyedEntry<IKController.IKRig>
    {
        public IKController.IKRig ikRig;
        public Rig unityRig;
        public IKGroup customRig;
        public float defaultFadeInRate;
        public float defaultFadeOutRate;
        public Easing.EaseType defaultFadeInEase;
        public Easing.EaseType defaultFadeOutEase;

        public IKController.IKRig GetKey() => ikRig;
    }

    public class IKRigBlendMixer : GeneralMixer<IKController.IKRigGroups, IKController.IKRig, RigBlendComponentEntry, IKRigBlendComponent>
    {
        public void Initialize()
        {
            InitializeMixingGroupMap();
        }
        protected override IKRigBlendComponent LoadMixedComponent(RigBlendComponentEntry entry)
        {
            IKRigBlendComponent blendComponent = new IKRigBlendComponent(
                entry.ikRig, this, 
                entry.defaultFadeInRate,
                entry.defaultFadeOutRate,
                entry.defaultFadeInEase,
                entry.defaultFadeOutEase,
                entry.unityRig, entry.customRig
             );
            return blendComponent;
        }
    }
}
