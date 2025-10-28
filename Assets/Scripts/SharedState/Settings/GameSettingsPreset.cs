using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedState
{
    [CreateAssetMenu(fileName = "NewGameSettingsPreset", menuName = "Game/GameSettings Preset")]
    public class GameSettingsPreset : ScriptableObject
    {
        public GameSettingsData presetData = new GameSettingsData();
    }
}
