using Resources.Modules;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Content.System.Modules
{
    [CreateAssetMenu(menuName = "Game/Modules/Grasshopper")]
    public class GrasshopperModule : ModuleUpgradeAsset
    {
        [SerializeField] float jumpDistanceMultiplier;
        [SerializeField] float runSpeedMultiplier;
        public override float GetStatChange(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
        }

        public override float GetStatMultiplier(Stat stat, int onTier)
        {
            switch (stat)
            {
                case Stat.PrimaryJumpDistance:
                    return jumpDistanceMultiplier;
                case Stat.RunSpeed: 
                    return runSpeedMultiplier;

            }
            throw new NoMoreStatsException();
        }

        public override void OnAdd()
        {
            base.OnAdd();
        }

        public override void OnRemove()
        {
            base.OnRemove();
        }
    }
}
