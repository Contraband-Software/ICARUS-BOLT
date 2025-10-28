using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedState
{
    public class SettingsInitializer : MonoBehaviour
    {
        [SerializeField] GameSettingsPreset defaultSettings;

        public void Initialize()
        {
            GameSettings.Initialize(defaultSettings);
        }

        public void ApplySettings()
        {

        }
    }
}
