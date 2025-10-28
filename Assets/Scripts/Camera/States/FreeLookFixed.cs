using Cinemachine;
using Software.Contraband.StateMachines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camera
{
    public class FreeLookFixed : CameraBaseState
    {
        // Camera moves freely moves from fixed position

        // This is an abandoned state

        public FreeLookFixed(CameraStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            // IMPORTANT
            //CinemachineTransposer transposer = stateHandler.freeLookFixedCamera.GetCinemachineComponent<CinemachineTransposer>();
            /*if (transposer != null)
            {
                Vector3 currentOffset = stateHandler.lastCameraPositionOnExitState
                    - stateHandler.lastCameraOffsetOnExitState;
                transposer.m_FollowOffset = currentOffset;
            }

            stateHandler.onFreeLookEntry.Invoke();
            stateHandler.Restrict3rdPersonCameraLookX(false);
            stateHandler.Toggle3rdPersonCameraRecentering(false);*/
            //stateHandler.freeLookFixedCamera.Priority = 100;
        }

        protected override void ExitState()
        {
            stateHandler.onFreeLookExit.Invoke();
            //stateHandler.freeLookFixedCamera.Priority = stateHandler.freeLookFixedCameraDefaultPriority;
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
