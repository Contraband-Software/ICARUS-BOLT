// using Codice.Client.BaseCommands;
using Software.Contraband.StateMachines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedState;

namespace Camera
{
    /// <summary>
    /// In this state, the Camera is in the default 3rd person camera mode.
    /// </summary>
    [DefaultState]
    public class FollowPlayer : CameraBaseState
    {
        public FollowPlayer(CameraStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            // insta re-centre on entry
            stateHandler.ForceRecenter3rdPersonCameraX();
            //stateHandler.Toggle3rdPersonCameraRecentering(true);
            stateHandler.standard3rdPersonCamera.Priority = 100;
        }

        protected override void ExitState()
        {
            stateHandler.lastCameraPositionOnExitState = stateHandler.standard3rdPersonCamera.State.FinalPosition;
            stateHandler.lastCameraQuaternionOnExitState = stateHandler.standard3rdPersonCamera.State.FinalOrientation;
            stateHandler.lastCameraOffsetOnExitState = stateHandler.standard3rdPersonCamera.Follow.position;
            stateHandler.standard3rdPersonCamera.Priority = stateHandler.standard3rdPersonCameraDefaultPriority;
        }

        public override void UpdateState()
        {
            stateHandler.standard3rdPersonCamera.m_XAxis.m_MaxSpeed 
                = stateHandler.standardXSpeed3rdPerson * GameSettings.Data.control.x_sensitivity;
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

        public override void HandleFreeLookPressed()
        {
            stateHandler.SwitchState(stateHandler.States[typeof(FreeLookOrbital2)]);
        }

        public override void HandleUnlockCameraTogglePressed()
        {
            stateHandler.SwitchState(stateHandler.States[typeof(Unlocked)]);
        }
    }
}
