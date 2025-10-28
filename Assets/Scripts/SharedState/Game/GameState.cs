using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedState
{
    [CreateAssetMenu(fileName = "GameState", menuName = "Game/Game State")]
    public class GameState : ScriptableObject
    {
        public enum State
        {
            PLAY,
            PAUSE
        }

        public State state;

        public enum PlaySubState
        {
            GAMEPLAY,
            MENUS
        }
        public PlaySubState playState;

        [System.Serializable]
        public class GameStateRequestEvents
        {
            public Action Pause;
            public Action Resume;
            public Action EnterInventory;
            public Action ExitInventory;
            public Action EnterDerelict;
            public Action ExitDerelict;
        }

        [System.Serializable]
        public class GameStateEvents
        {
            public Action OnPause;
            public Action OnResume;
            public Action OnEnterMenus;
            public Action OnEnterGameplay;
            public Action OnEnterInventory;
            public Action OnExitInventory;
            public Action OnEnterDerelict;
            public Action OnExitDerelict;
        }

        [NonSerialized] public GameStateRequestEvents Requests = new GameStateRequestEvents();
        [NonSerialized] public GameStateEvents Events = new GameStateEvents();

        private void OnEnable()
        {
            Debug.Log("Game State SO Enabled");
            Defaults();
        }

        private void OnDisable()
        {
            Defaults();
        }

        private void Defaults()
        {
            state = State.PLAY;
            playState = PlaySubState.GAMEPLAY;
        }
    }
}
