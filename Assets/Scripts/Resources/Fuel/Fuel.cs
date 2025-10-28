using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Resources
{
    public class Fuel : MonoBehaviour
    {
        public UnityEvent<Tank> FuelDepletedEvent = new();
        
        [Flags]
        public enum Tank
        {
            Propulsion = 0x1,
            Arms = 0x2,
            Legs = 0x4
        }
        
        public static readonly float MaxLevel = 100;
    
        private float propulsionLevel = MaxLevel;
        public float PropulsionLevel
        {
            get => propulsionLevel;
            set
            {
                if (value <= 0)
                {
                    propulsionLevel = 0; 
                    FuelDepletedEvent.Invoke(Tank.Propulsion);
                }
                else if (value >= MaxLevel)
                    propulsionLevel = MaxLevel;
                else
                    propulsionLevel = value;
            }
        }
    
        private float armLevel = MaxLevel;
        public float ArmLevel
        {
            get => armLevel;
            set
            {
                if (value <= 0)
                {
                    armLevel = 0; 
                    FuelDepletedEvent.Invoke(Tank.Arms);
                }
                else if (value >= MaxLevel)
                    armLevel = MaxLevel;
                else
                    armLevel = value;
            }
        }
        private float legLevel = MaxLevel;
        public float LegLevel
        {
            get => legLevel;
            set
            {
                if (value <= 0)
                {
                    legLevel = 0; 
                    FuelDepletedEvent.Invoke(Tank.Legs);
                }
                else if (value >= MaxLevel)
                    legLevel = MaxLevel;
                else
                    legLevel = value;
            }
        }
    
        public void ResetResource()
        {
            SetLevel(Tank.Arms | Tank.Legs | Tank.Propulsion, MaxLevel);
        }
    
        public float GetLevel(Tank tank)
        {
            switch (tank)
            {
                case Tank.Propulsion:
                    return PropulsionLevel;
                case Tank.Arms:
                    return ArmLevel;
                case Tank.Legs:
                    return LegLevel;
            }
    
            throw new ArgumentException("No such fuel tank");
        }
        
        public void SetLevel(Tank tank, float level)
        {
            if (tank.HasFlag(Tank.Propulsion))
                PropulsionLevel = level;
            if (tank.HasFlag(Tank.Arms))
                ArmLevel = level;
            if (tank.HasFlag(Tank.Legs))
                LegLevel = level;
        }
        
        public void AddLevel(Tank tank, float level)
        {
            if (tank.HasFlag(Tank.Propulsion))
                PropulsionLevel += level;
            if (tank.HasFlag(Tank.Arms))
                ArmLevel += level;
            if (tank.HasFlag(Tank.Legs))
                LegLevel += level;
        }
    }
}