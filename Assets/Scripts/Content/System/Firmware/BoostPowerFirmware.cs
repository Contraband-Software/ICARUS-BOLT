using Resources.Firmware;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Content.System.Firmware
{
    [CreateAssetMenu(menuName = "Game/Firmware/BoostPower")]
    public class BoostPowerFirmware : FirmwareUpgradeAsset
    {
        [SerializeField] float levelIncrement = 10f;
        public override float GetStatChange(Stat stat, int onTier)
        {
            switch (stat)
            {
                case Stat.BoostPower:
                    return levelIncrement * (1 + onTier);
            }

            throw new NoMoreStatsException();
        }

        public override float GetStatMultiplier(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
        }
    }
}
