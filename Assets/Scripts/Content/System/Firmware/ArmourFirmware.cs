using Resources.Firmware;
using Resources.System;
using UnityEngine;

namespace Content.System.Firmware
{
    [CreateAssetMenu(menuName = "Game/Firmware/Armour")]
    public class ArmourFirmware : FirmwareUpgradeAsset
    {
        public override float GetStatChange(Stat stat, int onTier)
        {
            switch (stat)
            {
                case Stat.Armour:
                    return 10f * (1 + onTier);
            }
    
            throw new NoMoreStatsException();
        }
    
        public override float GetStatMultiplier(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
        }
    }
}