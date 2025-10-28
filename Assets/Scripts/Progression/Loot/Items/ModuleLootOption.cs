using System;
using Resources.Modules;
using UnityEngine;

namespace Progression.Loot.Items
{
    [Serializable]
    public struct ModuleLootOption
    {
        public bool enabled;
        public bool randomized;
        [Header("Only applies if randomized is false")] public ModuleUpgradeAsset setObject;
    }
}