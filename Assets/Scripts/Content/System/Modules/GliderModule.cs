using Resources.Modules;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Content.System.Modules
{
    [CreateAssetMenu(menuName = "Game/Modules/Glider")]
    public class GliderModule : ModuleUpgradeAsset
    {
        public override float GetStatChange(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
        }

        public override float GetStatMultiplier(Stat stat, int onTier)
        {
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