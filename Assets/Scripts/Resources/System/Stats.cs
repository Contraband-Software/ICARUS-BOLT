using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Resources.System
{
    [Serializable]
    public enum Stat
    {
        // Generic stats
        Armour,
        FuelEfficiency,

        PrimaryJumpHeight,
        PrimaryJumpDistance,
        TimeToMaxChargeJump,

        RunSpeed,
        RunSpeedReverse,
        RunSpeedStrafe,
        RunAcceleration,
        RunDeceleration,

        AirStrafeMultiplier,
        AirBrakeMultiplier,

        JumpMaxFuelUse,
        BoostMinFuelUse,
        BoostMaxFuelUse,
        SprintFuelUse,

        BoostPower,
        SprintMultiplier,
        JetpackSpeed,
        GrappleLength,

        GrasshopperDistanceMultiplier,
        GrasshopperSpeedMultiplier,
        RabbitHeightMultiplier,
        RabbitSpeedMultiplier,

        JetpackPower,
        JetpackFuelUse,
        JetpackHandling
    }

    [Serializable]
    public class StatValue
    {
        public Stat stat;
        public Sprite icon;
        public string friendlyName = "";
        public float value;
    }
    
    [CreateAssetMenu(menuName = "Game/BaseStats", order = 0)]
    public class Stats : ScriptableObject
    {
        [SerializeField] List<StatValue> values = new();
    
        public StatValue GetStat(Stat stat)
        {
            StatValue query = values.FirstOrDefault(s => s.stat == stat);
            return query;
        }
    }
}