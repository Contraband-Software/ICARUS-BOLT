using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers
{
    public class MixedRefValue : MixedValue
    {
        private Func<float> getter;
        private Action<float> setter;

        public MixedRefValue(
            MonoBehaviour monoHook,
            Func<float> getter,
            Action<float> setter,
            float fInR = 1f,
            float fOutR = 1f,
            Easing.EaseType fInEase = Easing.EaseType.Linear,
            Easing.EaseType fOutEase = Easing.EaseType.Linear
        ) : base(monoHook, fInR, fOutR, fInEase, fOutEase)
        {
            this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public override float GetValue() => getter();
        protected override void SetValue(float v) => setter(v);
    }
}
