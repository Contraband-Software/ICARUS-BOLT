using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camera
{
    public class FreeLookOrbital : CameraBaseState
    {
        // Camera moves in an orbit around player freely

        public FreeLookOrbital(CameraStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.onFreeLookEntry.Invoke();
            stateHandler.standard3rdPersonCamera.Priority = 100;
        }

        protected override void ExitState()
        {
            stateHandler.onFreeLookExit.Invoke();
            stateHandler.standard3rdPersonCamera.Priority = stateHandler.standard3rdPersonCameraDefaultPriority;
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
    }
}
