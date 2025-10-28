using Resources.Firmware;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Content.System.Firmware
{
    [CreateAssetMenu(menuName = "Game/Firmware/RunSpeed")]
    public class RunSpeedFirmware : FirmwareUpgradeAsset
    {
        [SerializeField] float levelIncrement = 5f;
        public override float GetStatChange(Stat stat, int onTier)
        {
            switch (stat)
            {
                case Stat.RunSpeed:
                    return levelIncrement * (onTier);
            }

            throw new NoMoreStatsException();
        }

        public override float GetStatMultiplier(Stat stat, int onTier)
        {
            throw new NoMoreStatsException();
        }
    }
}
