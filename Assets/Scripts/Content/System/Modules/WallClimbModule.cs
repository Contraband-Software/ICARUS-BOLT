using Resources.Modules;
using Resources.System;
using UnityEngine;

namespace Content.System.Modules
{
    [CreateAssetMenu(menuName = "Game/Modules/WallClimb")]
    public class WallClimbModule : ModuleUpgradeAsset
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