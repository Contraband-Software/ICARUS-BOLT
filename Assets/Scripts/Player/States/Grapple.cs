using Software.Contraband.StateMachines;
using UnityEngine;
using Resources;
using Resources.System;
using Resources.Modules;
using Resources.Firmware;
using Content.System.Firmware;
using Content.System.Modules;
using ProgressionV2;

namespace Player
{
    public class Grapple : PlayerBaseState
    {
        // protected override StateType GetStateInfo => StateType.Generic;

        // This state is broken and shit

        private bool grappleHit;
        private Vector3 grappleHitLocation;

       /* private readonly Camera mainCamera = Camera.main;*/

        public Grapple(PlayerStateHandler stateHandler)
            : base(stateHandler) { }

        protected override void EnterState()
        {
            stateHandler.animator.SetBool("isFalling", true);
            //stateHandler.pCon.transform.position += new Vector3(0,1f,0);
            //stateHandler.pCon.rb.AddForce(new Vector3(0,1000,0));
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            
            //Ray ray = mainCamera.ScreenPointToRay(screenCenter);
          
            RaycastHit hit;

/*            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit point: " + hit.point);
                Vector3 grapleDir = hit.point - stateHandler.pCon.transform.position;
                stateHandler.pCon.rb.AddForce(grapleDir.normalized *stateHandler.pCon.graplePower);
            }
            else
            {
                Debug.Log("No hit");
            }
            grappleHit = Physics.Raycast(ray, out hit);
            grappleHitLocation = hit.point;*/


        }
        protected override void ExitState()
        {
            stateHandler.animator.SetBool("isFalling", false);
        }

        public override void UpdateState()
        {
        }

        public override void FixedUpdateState()
        {
            //TRANSITION TO Idle
            if (stateHandler.pCon.IsGrounded())
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Idle)]);
            }

            //TRANSITION TO FALLING
            //if(!stateHandler.pCon.IsGrounded() && stateHandler.pCon.rb.velocity.y < 0)
            //{
            //    stateHandler.SwitchState(stateHandler.States[typeof(Falling)]);
            //}



            if (grappleHit)
            {
                Vector3 grapleDir = grappleHitLocation - stateHandler.pCon.transform.position;
                stateHandler.pCon.rb.AddForce(grapleDir.normalized *stateHandler.pCon.graplePower);
            }


            stateHandler.pCon.Fuel.AddLevel(Fuel.Tank.Arms, -0.1f);
            XZVelocity_Air();
            VerticalVelocity();
        }

        public override void HandleJumpPressed()
        {
            // avoid default
            return;
        }
        public override void HandleJumpReleased()
        {
            // avoid default
            return;
        }

        public override void HandleJetpackPressed()
        {
            if (InventoryData.Inventory.HasActiveModule<JetpackModule>()
                || stateHandler.pCon.hasJetpack)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(Jetpack)]);
            }
        }

        public override void HandleBoostPressed()
        {
            //TRANSITION TO BOOST CHARGE
            if (stateHandler.allowBoost)
            {
                stateHandler.SwitchState(stateHandler.States[typeof(BoostCharge)]);
            }
        }

        #region MOVEMENT_MANIPULATION
        #endregion
    }
}