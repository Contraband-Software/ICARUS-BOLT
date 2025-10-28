using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camera
{
    public class FreeLookOrbital2 : CameraBaseState
    {
        public FreeLookOrbital2(CameraStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.onFreeLookEntry.Invoke();
            stateHandler.freeLookOrbital2Camera.Priority = 100;
        }

        protected override void ExitState()
        {
            stateHandler.onFreeLookExit.Invoke();
            stateHandler.freeLookOrbital2Camera.Priority = stateHandler.freeLookOrbital2CameraDefaultPriority;
        }

        public override void UpdateState()
        {

        }

        public override void LateUpdateState()
        {

        }

        public override void FixedUpdateState()
        {
            return;
        }

        public override void HandleLook(Vector2 lookInput)
        {
            return;
        }

        public override void HandleFreeLookReleased()
        {
            stateHandler.SwitchState(stateHandler.States[typeof(FollowPlayer)]);
        }

        public override void HandleUnlockCameraTogglePressed()
        {
            stateHandler.SwitchState(stateHandler.States[typeof(Unlocked)]);
        }
    }
}
