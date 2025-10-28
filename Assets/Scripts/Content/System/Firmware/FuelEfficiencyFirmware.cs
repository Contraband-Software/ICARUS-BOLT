
using Resources.Firmware;
using Resources.System;
using UnityEngine;

namespace Content.System.Firmware
{
    [CreateAssetMenu(menuName = "Game/Firmware/FuelEfficiency")]
    public class FuelEfficiencyFirmware : FirmwareUpgradeAsset
    {
        [SerializeField] float levelIncrement = -0.2f;
        public override float GetStatChange(Stat stat, int onTier)
        {
            switch (stat)
            {
                case Stat.FuelEfficiency:
                    return levelIncrement * (1 + onTier);
            }

            throw new NoMoreStatsException();
        }

        public override float GetStatMultiplier(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
            // switch (stat)
            // {
            //     case Stat.FuelEfficiency:
            //         return 1.5f;
            //     default:
            //         return float.NaN;
            // }
        }
    }
}