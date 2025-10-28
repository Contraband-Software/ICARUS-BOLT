using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SharedState
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        private PlayerInput playerInput;

        [Header("Event Broadcaster")]
        [SerializeField] private InputEvents inputEvents;

        [Header("Game State")]
        [SerializeField] private GameState gameState;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            // Have to do this bullshit because "default action map" doesnt fucking work
            // If i dont do this then all action maps recieve input
            SwitchActionMap("UI");
            SwitchActionMap("Player");
        }

        private Action _onPauseHandler;
        private Action _onResumeHandler;
        private Action _onEnterMenusHandler;
        private Action _OnEnterGameplayHandler;

        private void OnEnable()
        {
            _onEnterMenusHandler = () => SwitchActionMap("UI");
            _OnEnterGameplayHandler = () => SwitchActionMap("Player");

            gameState.Events.OnEnterMenus += _onEnterMenusHandler;
            gameState.Events.OnEnterGameplay += _OnEnterGameplayHandler;
        }

        private void OnDisable()
        {
            gameState.Events.OnEnterMenus -= _onEnterMenusHandler;
            gameState.Events.OnEnterGameplay -= _OnEnterGameplayHandler;
        }


        private void SwitchActionMap(string mapName)
        {
            Debug.Log("Switchint to input map: " + mapName);
            playerInput.SwitchCurrentActionMap(mapName);
        }

        public void OnMove(InputAction.CallbackContext context) => inputEvents.player.OnMove?.Invoke(context);
        public void OnJump(InputAction.CallbackContext context) => inputEvents.player.OnJump?.Invoke(context);
        public void OnLook(InputAction.CallbackContext context) => inputEvents.player.OnLook?.Invoke(context);
        public void OnPause(InputAction.CallbackContext context) => inputEvents.player.OnPause?.Invoke(context);
        public void OnBoost(InputAction.CallbackContext context) => inputEvents.player.OnBoost?.Invoke(context);
        public void OnInteract(InputAction.CallbackContext context) => inputEvents.player.OnInteract?.Invoke(context);
        public void OnSprint(InputAction.CallbackContext context) => inputEvents.player.OnSprint?.Invoke(context);
        public void OnJetpack(InputAction.CallbackContext context) => inputEvents.player.OnJetpack?.Invoke(context);
        public void OnGraple(InputAction.CallbackContext context) => inputEvents.player.OnGraple?.Invoke(context);
        public void OnFreeLook(InputAction.CallbackContext context) => inputEvents.player.OnFreeLook?.Invoke(context);
        public void OnUnlockCam(InputAction.CallbackContext context) => inputEvents.player.OnUnlockCam?.Invoke(context);
        public void OnUnlockCamMove(InputAction.CallbackContext context) => inputEvents.player.OnUnlockCamMove?.Invoke(context);
        public void OnInventory(InputAction.CallbackContext context) => inputEvents.player.OnInventory?.Invoke(context);

        public void TestFunc(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Test Func in Input Manager");
            }
        }


        public void UI_OnCancel(InputAction.CallbackContext context)
        {
            
            inputEvents.ui.OnCancel?.Invoke(context);
        }

        public void UI_OnInventory(InputAction.CallbackContext context) => inputEvents.ui.OnInventory?.Invoke(context);
        public void UI_OnClick(InputAction.CallbackContext context) => inputEvents.ui.OnClick?.Invoke(context);
        public void UI_OnPoint(InputAction.CallbackContext context) => inputEvents.ui.OnPoint?.Invoke(context);
    
    }
}
