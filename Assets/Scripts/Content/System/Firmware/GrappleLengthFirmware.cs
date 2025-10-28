using Resources.Firmware;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Firmware/GrappleLength")]
public class GrappleLengthFirmware : FirmwareUpgradeAsset
{
    [SerializeField] float levelIncrement = 1f;
    public override float GetStatChange(Stat stat, int onTier)
    {
        switch (stat)
        {
            case Stat.GrappleLength:
                return levelIncrement * (1 + onTier);
        }

        throw new NoMoreStatsException();
    }

    public override float GetStatMultiplier(Stat stat, int onTier)
    {
        throw new NoMoreStatsException();
    }
}
