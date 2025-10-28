using Resources.Firmware;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Firmware/Acceleration")]
public class AccelerationFirmware : FirmwareUpgradeAsset
{
    [SerializeField] float levelIncrement = 3f;
    public override float GetStatChange(Stat stat, int onTier)
    {
        switch (stat)
        {
            case Stat.RunAcceleration:
                return levelIncrement * (1 + onTier);
        }

        throw new NoMoreStatsException();
    }

    public override float GetStatMultiplier(Stat stat, int onTier)
    {
        throw new NoMoreStatsException();
    }
}
