using ProgressionV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressionV2
{
    // Defines data that will be serialized when module is saved
    public class ModuleData : ItemData
    {
        public int moduleId;            // To match the firmwareId of the Firmware ScriptableObject 
        public bool isActive;           // whether the player is using the firmware as part of their active set
    }
}
