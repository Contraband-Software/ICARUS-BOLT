using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SharedState
{
    public static class GameSettings
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

        public static GameSettingsData Data { get; private set; }
        public static GameSettingsData DataWorkingCopy { get; private set; } //For when youre in settings menu

        public static GameSettingsPreset defaults;

        public static void Initialize(GameSettingsPreset defaultSettings)
        {
            defaults = defaultSettings;

            Debug.Log("Settings Initialized, SavePath: " + SavePath.ToString());
            Debug.Log("Default X Sens: " + defaults.presetData.control.x_sensitivity);
        }

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
            File.WriteAllText(SavePath, json);
            Debug.Log("Settings saved to " + SavePath);
        }

        public static void Load()
        {
            if(File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                var loaded = JsonConvert.DeserializeObject<GameSettingsData>(json);
                Data = MergeWithDefaults(loaded);
            }
            else
            {
                Debug.Log("No Settings File found, using defaults");
                ResetToDefaults();
                Save();
            }
        }

        public static void ResetToDefaults()
        {
            Data = JsonConvert.DeserializeObject<GameSettingsData>(
                JsonConvert.SerializeObject(defaults.presetData));
            Data.version = GameSettingsData.CurrentVersion;
        }

        /// <summary>
        /// Initializes the working copy from the saved Data.
        /// Use this when you open the settings menu.
        /// </summary>
        public static void InitializeWorkingCopy()
        {
            ClearWorkingCopy();
            DataWorkingCopy = DeepCopy(Data);

            Debug.Log("Deep Copy X Sens: " + DataWorkingCopy.control.x_sensitivity);
        }

        /// <summary>
        /// Clears the working copy to a "blank" state.
        /// Usually called internally before reinitializing.
        /// </summary>
        public static void ClearWorkingCopy()
        {
            DataWorkingCopy = null;
        }

        /// <summary>
        /// Writes the working copy back into the live Data.
        /// Use this when the player presses "Apply" or "Save".
        /// </summary>
        public static void WriteWorkingCopy()
        {
            if(DataWorkingCopy == null) {
                Debug.LogWarning("Tried to write working copy, but it is null.");
                return;
            }
            Data = DeepCopy(DataWorkingCopy);
        }

        private static GameSettingsData MergeWithDefaults(GameSettingsData loaded)
        {
            // Start with defaults
            var merged = DeepCopy(defaults.presetData);

            // overlay loaded fields
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(loaded), merged);
            return merged;
        }

        private static GameSettingsData DeepCopy(GameSettingsData source)
        {
            return JsonConvert.DeserializeObject<GameSettingsData>(
                JsonConvert.SerializeObject(source));
        }

        private static bool AreEqual(GameSettingsData a, GameSettingsData b)
        {
            string jsonA = JsonConvert.SerializeObject(a);
            string jsonB = JsonConvert.SerializeObject(b);
            return jsonA.Equals(jsonB);
        }

        /// <summary>
        /// Check if the WorkingCopy differs from the current Data
        /// </summary>
        /// <returns></returns>
        public static bool CheckDirty()
        {
            bool isDirty = !(DataWorkingCopy != null && AreEqual(Data, DataWorkingCopy));
            return isDirty;
        }

        private static bool WorkingCopySet()
        {
            bool set = DataWorkingCopy != null;
            if(!set)
            {
                Debug.LogWarning("GameSettings Working Copy was not set. No changes will save.");
            }
            return set;
        }

        #region SETTINGS_API

        /// <summary>
        /// Set Horizontal Mouse Sensitivity. Expects a value between 0 and 1.
        /// </summary>
        /// <param name="percentOfMax"></param>
        public static void Set_X_Sensitivity(float percentOfMax)
        {
            if (!WorkingCopySet()) return;

            // Slider 0 -> 0.5 = 0.01 -> 1,  0.5 -> 1 = 1 -> 10
            float sliderRemap = percentOfMax < 0.5f
                ? Mathf.Lerp(0.0f, 1.0f, percentOfMax / 0.5f)
                : Mathf.Lerp(1.0f, 10.0f, (percentOfMax - 0.5f) / 0.5f);

            // Ensure the value doesn't go below 0.01 or above 10
            float newSens = Mathf.Clamp(sliderRemap, 0.01f, 10f);
            DataWorkingCopy.control.x_sensitivity = newSens;

            Debug.Log("New X Sens: " + DataWorkingCopy.control.x_sensitivity);
        }

        /// <summary>
        /// Get the sensitivity value but as a percent, for example to
        /// Set the slider to the correct position on loading the settings ui
        /// </summary>
        /// <returns></returns>
        public static float Get_X_Sensitivity_As_Percent()
        {
            if (!WorkingCopySet()) return 0f;
            float x_sens = DataWorkingCopy.control.x_sensitivity;
            float x_sens_percent = x_sens <= 1f
                ? Mathf.InverseLerp(0.01f, 1f, x_sens) * 0.5f
                : 0.5f + Mathf.InverseLerp(1f, 10f, x_sens) * 0.5f;
            return x_sens_percent;
        }

        #endregion
    }
}
