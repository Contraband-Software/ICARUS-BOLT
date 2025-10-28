using ProgressionV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressionV2
{
    // Defines data that will be serialized when firmware is saved
    public class FirmwareData : ItemData
    {
        public int firmwareId;          // To match the firmwareId of the Firmware ScriptableObject 
        public int tier;                // The tier at which the player has it upgraded to
        public bool isActive;           // whether the player is using the firmware as part of their active set
    }
}
