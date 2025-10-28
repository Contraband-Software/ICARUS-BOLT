using Software.Contraband.StateMachines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Cinemachine;

namespace Camera
{
    [
        RequireComponent(typeof(UnityEngine.Camera)),
        RequireComponent(typeof(CameraController))
    ]
    public class CameraStateHandler : StateHandler<CameraBaseState>
    {

        public CameraController camCon { get; private set; }
        [Header("References")]
        public Cinemachine.CinemachineFreeLook standard3rdPersonCamera;
        public Cinemachine.CinemachineFreeLook freeLookOrbital2Camera;
        public Cinemachine.CinemachineVirtualCamera unlockedCamera;
        public Cinemachine.CinemachineBrain brain;

        [Header("Camera Settings")]
        public float standardYSpeed3rdPerson = 0.0005f;
        public float standardXSpeed3rdPerson = 0.02f;
        public int standard3rdPersonCameraDefaultPriority = 10;
        public int freeLookOrbital2CameraDefaultPriority = 8;
        public int unlockedCameraDefaultPriority = 7;

        [Header("Events")]
        public UnityEvent onFreeLookEntry;
        public UnityEvent onFreeLookExit;

        public Vector3 lastCameraPositionOnExitState = Vector3.zero;
        public Quaternion lastCameraQuaternionOnExitState = Quaternion.identity;
        public Vector3 lastCameraOffsetOnExitState = Vector3.zero;

        [Header("Unlocked Camera Settings")]
        public GameObject unlockedCameraDummy;
        public Rigidbody unlockedCameraDummyRb;
        public float unlockedCameraSpeed = 5f;

        protected override void Initialize()
        {
            camCon = GetComponent<CameraController>();
        }

        private void Update()
        {
            //standard3rdPersonCamera.m_YAxis.m_MaxSpeed = standardYSpeed3rdPerson * camCon.pCon.mouseSensitivityVertical;

            // the commented out bit above will be changed. It will get settings from global settings, decoupled
            // from player.

            //standard3rdPersonCamera.m_YAxis.m_MaxSpeed = standardYSpeed3rdPerson;
            CurrentState.UpdateState();
        }
        private void FixedUpdate() => CurrentState.FixedUpdateState();
        private void LateUpdate() => CurrentState.LateUpdateState();

        public void HandleLook(InputAction.CallbackContext context)
        {
            CurrentState.HandleLook(context.ReadValue<Vector2>());
        }

        // virtual method now
        protected override void SwitchStateImpl(CameraBaseState newState)
        {
            base.SwitchStateImpl(newState);
        }

        public void Set3rdPersonHorizontalInputVal(float delta)
        {
            standard3rdPersonCamera.m_XAxis.Value += delta;
        }

        public void Set3rdPersonVerticalInputVal(float delta)
        {
            standard3rdPersonCamera.m_YAxis.Value += delta;
        }

        public void ForceRecenter3rdPersonCameraX()
        {
            float lookAtObjectYaw = standard3rdPersonCamera.LookAt.eulerAngles.y;
            float normalizedYaw = lookAtObjectYaw > 180f ? lookAtObjectYaw - 360f : lookAtObjectYaw;
            standard3rdPersonCamera.m_XAxis.Value = normalizedYaw;
        }

        public void UpdateCamera()
        {
            brain.ManualUpdate();
        }

        public void HandleFreeLook(InputAction.CallbackContext context)
        {
            if(context.performed)
            {
                CurrentState.HandleFreeLookPressed();
            }
            else if(context.canceled)
            {
                CurrentState.HandleFreeLookReleased();
            }
        }

        public void HandleUnlockedCameraToggle(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CurrentState.HandleUnlockCameraTogglePressed();
            }
        }

        public void HandleUnlockedCameraMove(InputAction.CallbackContext context)
        {
            CurrentState.HandleUnlockedCameraMove(context);
        }

        #region UNLOCKED_CAMERA

        #endregion

    }
}
