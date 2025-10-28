using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressionV2
{
    [System.Serializable]
    public class DerelictSaveData
    {
        public Dictionary<string, ItemStoreSaveData> derelicts = new();
    }
}
