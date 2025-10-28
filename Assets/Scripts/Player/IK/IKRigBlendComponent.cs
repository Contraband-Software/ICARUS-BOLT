using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using UnityEngine.Animations.Rigging;

namespace Player
{
    public class IKRigBlendComponent : MixedValue
    {
        private readonly IKController.IKRig ikRig;
        private Rig unityRig { get; } = null;
        private IKGroup customRig { get; } = null;

        public IKRigBlendComponent(
            IKController.IKRig ikRig,
            MonoBehaviour monoHook,
            float fInR,
            float fOutR,
            Easing.EaseType fInEase = Easing.EaseType.Linear,
            Easing.EaseType fOutEase = Easing.EaseType.Linear,
            Rig unityRig = null,
            IKGroup customRig = null
            ) : base(monoHook, fInR, fOutR, fInEase, fOutEase)
        {
            this.ikRig = ikRig;
            this.unityRig = unityRig;
            this.customRig = customRig;

            InvalidRigMessage();
        }

        private void InvalidRigMessage()
        {
            if(unityRig && customRig)
            {
                Debug.LogWarning("IKRigBlendComponent for " + ikRig.ToString() +
                    " has two Rigs assigned instead of 1");
                return;
            }

            if (unityRig || customRig) return;
            Debug.LogWarning("IKRigBlendComponent for " + ikRig.ToString() +
                " has no Rig assigned");
        }

        protected override void SetValue(float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);
            if(customRig) customRig.SetWeight(value);
            if(unityRig) unityRig.weight = value;
            InvalidRigMessage();
        }

        public override float GetValue()
        {
            if (customRig) return customRig.GetWeight();
            if (unityRig) return unityRig.weight;
            InvalidRigMessage();
            return 0f;
        }
    }
}
