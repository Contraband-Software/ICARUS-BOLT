using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Software.Contraband.Control;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SharedState;
using ProgressionV2;

public class GameController : MonoBehaviour
{

    public static GameController Instance { get; private set; }

    public GameState gameState;
    public InputEvents inputEvents;

    public SettingsInitializer settingsInitializer;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(Instance != this)
        {
            Debug.LogWarning("Duplicate GameController found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        Debug.Log("Game Controller Loaded");
    }

    private void Start()
    {
        Debug.Log("Game Controller Start");
        LoadGameData();

        //LootGen.SimulateLootDrops(1, 3, 100, "C:\\Users\\jakub\\Downloads\\SimulatedDrops.csv", 4, 1, 5);

        SubscribeEvents();

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void LoadGameData()
    {
        settingsInitializer.Initialize();
        GameSettings.Load();
        InventoryData.Initialize();
        InventoryData.Load();
        DerelictData.Initialize();
        DerelictData.Load();
    }

    private void SaveGameData()
    {
        InventoryData.Save();
        DerelictData.Save();
    }

    private void OnDestroy()
    {
       UnsubscribeEvents();
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }

    private void SubscribeEvents()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        gameState.Requests.Pause += HandleRequest_PauseGame;
        gameState.Requests.Resume += HandleRequest_ResumeGame;
        gameState.Requests.EnterInventory += HandleRequest_EnterInventory;
        gameState.Requests.ExitInventory += HandleRequest_ExitInventory;
        gameState.Requests.EnterDerelict += HandleRequest_EnterDerelict;
        gameState.Requests.ExitDerelict += HandleRequest_ExitDerelict;
    }

    private void UnsubscribeEvents()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        gameState.Requests.Pause -= HandleRequest_PauseGame;
        gameState.Requests.Resume -= HandleRequest_ResumeGame;
        gameState.Requests.EnterInventory -= HandleRequest_EnterInventory;
        gameState.Requests.ExitInventory -= HandleRequest_ExitInventory;
        gameState.Requests.EnterDerelict -= HandleRequest_EnterDerelict;
        gameState.Requests.ExitDerelict -= HandleRequest_ExitDerelict;
    }

    #region REQUEST_HANDLING
    private void HandleRequest_PauseGame()
    {
        Debug.Log("Game Request: Pause");
        if (gameState.state == GameState.State.PAUSE) return;
        PauseGame();
    }

    private void HandleRequest_ResumeGame()
    {
        Debug.Log("Game Request: Resume");
        if (gameState.state == GameState.State.PLAY) return;
        ResumeGame();
    }

    private void HandleRequest_EnterInventory()
    {
        Debug.Log("Game Request: Enter Inventory");
        // deny request to enter inventory if we are paused
        if (gameState.state == GameState.State.PAUSE) return;
        gameState.playState = GameState.PlaySubState.MENUS;
        UnlockCursor();
        gameState.Events.OnEnterMenus?.Invoke();
        gameState.Events.OnEnterInventory?.Invoke();
    }

    private void HandleRequest_ExitInventory()
    {
        Debug.Log("Game Request: Exit Inventory");
        // deny request to exit inventory if we are paused
        if (gameState.state == GameState.State.PAUSE) return;
        gameState.playState = GameState.PlaySubState.GAMEPLAY;
        LockCursor();
        gameState.Events.OnEnterGameplay?.Invoke();
        gameState.Events.OnExitInventory?.Invoke();
    }

    private void HandleRequest_EnterDerelict()
    {
        if (gameState.state == GameState.State.PAUSE) return;
        gameState.playState = GameState.PlaySubState.MENUS;
        UnlockCursor();
        gameState.Events.OnEnterMenus?.Invoke();
        gameState.Events.OnEnterInventory?.Invoke();
        gameState.Events.OnEnterDerelict?.Invoke();
    }

    private void HandleRequest_ExitDerelict()
    {
        if(gameState.state == GameState.State.PLAY) return;
        gameState.playState = GameState.PlaySubState.GAMEPLAY;
        LockCursor();
        gameState.Events.OnEnterGameplay?.Invoke();
        gameState.Events.OnExitDerelict?.Invoke();
        gameState.Events.OnExitInventory?.Invoke();
    }
    #endregion


    #region SCENE_CONTROL
    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        Debug.Log("Scene Loaded: " + scene.name);
        if(scene.name == "MainMenu")
        {
            UnlockCursor();
            gameState.Events.OnEnterMenus?.Invoke();
        }

        else if(scene.name == "VSlice")
        {
            LockCursor();
            gameState.playState = GameState.PlaySubState.GAMEPLAY;
            gameState.Events.OnEnterGameplay?.Invoke();
        }

        else
        {
            ResumeGame();
        }
    }

    #endregion


    #region ACTIONS
    private void PauseGame()
    {
        Debug.Log("PAUSING GAME");
        gameState.state = GameState.State.PAUSE;
        Time.timeScale = 0f;
        UnlockCursor();

        gameState.Events.OnEnterMenus?.Invoke();
        gameState.Events.OnPause?.Invoke();
    }

    private void ResumeGame()
    {
        Debug.Log("RESUMING GAME");
        gameState.state = GameState.State.PLAY;
        Time.timeScale = 1f;

        gameState.Events.OnResume?.Invoke();

        // We only call to re-enter gameplay if we were actually playing before we paused.
        // if we were in a UI for example, we should go back to that.
        if (gameState.playState == GameState.PlaySubState.GAMEPLAY)
        {
            LockCursor();
            gameState.Events.OnEnterGameplay?.Invoke();
        }
    }


    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    #endregion
}
