using System.Collections.Generic;
using Resources.System;
using UnityEngine;

namespace Resources.Firmware
{
    // [CreateAssetMenu(menuName = "Game/FirmwareUpgrade")]
    public abstract class FirmwareUpgradeAsset : AbstractUpgradeAsset, IStatChange
    {
        [field: SerializeField, Min(1)] public int MaxTier { get; private set; } = 1;
        [field: SerializeField] public AnimationCurve RarityOverTier { get; private set; }
        [field: SerializeField] public AnimationCurve PointValueOverTier { get; private set; }

        /// <summary>
        /// Return the additional value(s) to be added to a stat or stats
        /// </summary>
        /// <param name="stat"></param>
        /// <param name="onTier"></param>
        /// <returns></returns>
        public abstract float GetStatChange(Stat stat, int onTier);
        public abstract float GetStatMultiplier(Stat stat, int onTier);

        public override float GetRarity() => GetRarity(1);

        public override float GetPointValue() => GetPointValue(1);

        public override float GetRarity(int tier)
        {
            // safeguard
            tier = Mathf.Clamp(tier, 1, MaxTier);
            return RarityOverTier.Evaluate(tier);
        }

        public override float GetPointValue(int tier)
        {
            // safeguard
            tier = Mathf.Clamp(tier, 1, MaxTier);
            return PointValueOverTier.Evaluate(tier);
        }
    }
}