using Resources.System;
using UnityEngine;

namespace Resources.Modules
{
    public abstract class ModuleUpgradeAsset : AbstractUpgradeAsset, IMechanicalUpgrade, IStatChange
    {
        [field: SerializeField] public ModuleType.Type type { get; private set; }

        [field: SerializeField] public float Rarity { get; private set; }
        [field: SerializeField] public float PointValue { get; private set; }

        public abstract float GetStatChange(Stat stat, int onTier);
        public abstract float GetStatMultiplier(Stat stat, int onTier);

        public virtual void OnAdd()
        {
            
        }

        public virtual void OnRemove()
        {
            
        }

        public override float GetRarity() => Rarity;

        public override float GetPointValue() => PointValue;

        public override float GetRarity(int tier) => Rarity;

        public override float GetPointValue(int tier) => PointValue;
    }
}