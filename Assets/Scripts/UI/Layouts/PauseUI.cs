using SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    [SerializeField] GameState gameState;
    [SerializeField] InputEvents inputEvents;

    [Serializable]
    private class PauseUIComponents
    {
        public Canvas canvas;
    }

    [SerializeField] PauseUIComponents pauseUIComponents;

    [SerializeField] UnityEvent OnCancelPressed = new UnityEvent();
    [SerializeField] UnityEvent OnPause = new UnityEvent();
    [SerializeField] UnityEvent OnOpened = new UnityEvent();
    [SerializeField] UnityEvent OnClosed = new UnityEvent();

    private void Start()
    {
        Hide();
    }

    private void OnEnable()
    {
        inputEvents.ui.OnCancel += HandleCancel;
        gameState.Events.OnPause += _OnPause;
    }

    private void OnDisable()
    {
        inputEvents.ui.OnCancel -= HandleCancel;
        gameState.Events.OnPause -= _OnPause;
    }

    private void _OnPause()
    {
        OnPause?.Invoke();
    }

    public void Open()
    {
        Show();

        OnOpened?.Invoke();
    }

    public void Close()
    {
        Hide();
        ResumeGame();
        OnClosed?.Invoke();
    }


    public void Show()
    {
        pauseUIComponents.canvas.enabled = true;
    }

    public void Hide()
    {
        pauseUIComponents.canvas.enabled = false;
    }

    public void HandleCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCancelPressed?.Invoke();
        }
    }

    public void Button_MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void ResumeGame()
    {
        gameState.Requests?.Resume?.Invoke();
    }

}
