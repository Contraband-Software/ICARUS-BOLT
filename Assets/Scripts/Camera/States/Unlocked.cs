using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Camera
{
    public class Unlocked : CameraBaseState
    {
        public Unlocked(CameraStateHandler stateHandler)
           : base(stateHandler) { }

        private float forwardInput;
        private float horizontalInput;

        protected override void EnterState()
        {
            stateHandler.onFreeLookEntry.Invoke();
            stateHandler.unlockedCamera.Priority = 100;
        }

        protected override void ExitState()
        {
            stateHandler.onFreeLookExit.Invoke();
            stateHandler.unlockedCamera.Priority = stateHandler.unlockedCameraDefaultPriority;
        }

        public override void UpdateState()
        {

        }

        public override void LateUpdateState()
        {

        }

        public override void FixedUpdateState()
        {
            MoveCamera();
        }

        public override void HandleLook(Vector2 lookInput)
        {
        }

        public override void HandleFreeLookReleased()
        {
        }

        public override void HandleUnlockCameraTogglePressed()
        {
            stateHandler.SwitchState(stateHandler.States[typeof(FollowPlayer)]);
        }

        public override void HandleUnlockedCameraMove(InputAction.CallbackContext context)
        {
            forwardInput = context.ReadValue<Vector2>().y;
            horizontalInput = context.ReadValue<Vector2>().x;
        }

        private void MoveCamera()
        {
            Vector3 camForward = UnityEngine.Camera.main.transform.forward;
            Vector3 camRight = UnityEngine.Camera.main.transform.right;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 forwardAspect = camForward * forwardInput;
            Vector3 horizontalAspect = camRight * horizontalInput;

            Vector3 moveDirection = forwardAspect + horizontalAspect;
            if(moveDirection.sqrMagnitude > 0 )
            {
                moveDirection.Normalize();
            }

            stateHandler.unlockedCameraDummyRb.velocity = moveDirection * stateHandler.unlockedCameraSpeed;
        }
    }
}
