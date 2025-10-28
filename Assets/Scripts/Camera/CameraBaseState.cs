using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Software.Contraband.StateMachines;

namespace Camera
{
    public abstract class CameraBaseState : BaseState
    {
        protected CameraStateHandler stateHandler;
        protected Vector3 targetPosition = Vector3.zero;
        protected Quaternion targetRotation = Quaternion.identity;
        //angles the camera will maintain
        protected Quaternion independentRotationTarget = Quaternion.identity;

        protected CameraBaseState(CameraStateHandler stateHandler)
        {
            this.stateHandler = stateHandler;
        }

        public abstract void UpdateState();

        public abstract void FixedUpdateState();

        public virtual void LateUpdateState() { }

        public virtual void HandleLook(Vector2 input) { }

        public virtual void HandleFreeLookPressed() { }

        public virtual void HandleFreeLookReleased() { }

        public virtual void HandleUnlockCameraTogglePressed() { }

        public virtual void HandleUnlockedCameraMove(InputAction.CallbackContext context) { }

    }
}
