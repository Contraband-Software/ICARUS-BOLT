using System;
using Resources.Firmware;
using Resources.System;
using UnityEngine;

namespace Progression.Loot.Items
{
    [Serializable]
    public struct FirmwareLootOption
    {
        public bool enabled;
        public bool randomized;
        [Header("Only applies if randomized is false")] public FirmwareUpgradeAsset setObject;
    }
}