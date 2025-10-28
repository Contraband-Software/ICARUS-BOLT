using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedState
{
    [System.Serializable]
    public class GameSettingsData
    {
        public int version = CurrentVersion; // DO NOT TOUCH
        public const int CurrentVersion = 1; // FOR SETTINGS MIGRATIONS. BUMP WHEN ADD/REMOVE FIELDS

        [System.Serializable]
        public class Control
        {
            public float x_sensitivity;
        }

        public Control control = new Control();
    }
}
