using Resources.Firmware;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Firmware/AirStrafe")]
public class AirStrafeFirmware : FirmwareUpgradeAsset
{
    [SerializeField] float levelIncrement = 0.1f;
    public override float GetStatChange(Stat stat, int onTier)
    {
        switch (stat)
        {
            case Stat.AirStrafeMultiplier:
                return levelIncrement * (1 + onTier);
            case Stat.AirBrakeMultiplier:
                return levelIncrement * (1 + onTier);
        }

        throw new NoMoreStatsException();
    }

    public override float GetStatMultiplier(Stat stat, int onTier)
    {
        throw new NoMoreStatsException();
    }
}
