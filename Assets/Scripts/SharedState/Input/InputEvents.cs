using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SharedState
{
    [CreateAssetMenu(fileName = "InputEvents", menuName = "Game/Input Events")]
    public class InputEvents : ScriptableObject
    {
        public class PlayerInputEvents
        {
            public Action<InputAction.CallbackContext> OnMove;
            public Action<InputAction.CallbackContext> OnJump;
            public Action<InputAction.CallbackContext> OnLook;
            public Action<InputAction.CallbackContext> OnPause;
            public Action<InputAction.CallbackContext> OnBoost;
            public Action<InputAction.CallbackContext> OnInteract;
            public Action<InputAction.CallbackContext> OnSprint;
            public Action<InputAction.CallbackContext> OnJetpack;
            public Action<InputAction.CallbackContext> OnGraple;
            public Action<InputAction.CallbackContext> OnFreeLook;
            public Action<InputAction.CallbackContext> OnUnlockCam;
            public Action<InputAction.CallbackContext> OnUnlockCamMove;
            public Action<InputAction.CallbackContext> OnInventory;

            // Method to clear all events in this class
            public void ClearAllEvents()
            {
                var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(Action<InputAction.CallbackContext>))
                    {
                        field.SetValue(this, null);
                    }
                }
            }
        }

        public class UIInputEvents
        {
            public Action<InputAction.CallbackContext> OnCancel;
            public Action<InputAction.CallbackContext> OnInventory;
            public Action<InputAction.CallbackContext> OnClick;
            public Action<InputAction.CallbackContext> OnPoint;
        }

        [NonSerialized] public PlayerInputEvents player = new PlayerInputEvents();
        [NonSerialized] public UIInputEvents ui = new UIInputEvents();

        private void OnEnable()
        {
            
        }
    }
}
