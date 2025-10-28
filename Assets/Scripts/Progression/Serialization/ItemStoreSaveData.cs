using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressionV2
{
    [System.Serializable]
    public class ItemStoreSaveData
    {
        public List<FirmwareData> firmwares = new();
        public List<ModuleData> modules = new();
    }
}
