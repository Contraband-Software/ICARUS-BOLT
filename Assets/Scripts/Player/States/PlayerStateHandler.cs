using UnityEngine;
using Software.Contraband.StateMachines;
using System;
using Resources.System;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using SharedState;
using Cinemachine;
using ProgressionV2;
using Content.System.Firmware;

namespace Player
{
    [
        RequireComponent(typeof(PlayerController)),
        RequireComponent(typeof(Animator)),
        RequireComponent(typeof(IKController))
    ]
    public class PlayerStateHandler : StateHandler<PlayerBaseState>
    {
        [Header("Mantle State")]
        public AnimationCurve mantle_pullupForce;
        public AnimationCurve mantle_fallBrakeForce;





        public PlayerController pCon { get; private set; }
        public Animator animator { get; private set; }
        public IKController ikCon { get; private set; }
        public PlayerAnimationController animCon { get; private set; }

        // Input 
        public float horizontalInput { get; private set; }
        public float forwardInput { get; private set; }

        // Internals
        //  Jump
        private float jumpStartHoldTime = 0f;
        [HideInInspector] public float jumpChargeTime = 0f; //length of time jump has been held down for
        private float jumpBufferTimer = 0f;
        private float jumpBufferDuration = 0.24f; // How long after jump release we will allow for auto rejump on grounding

        //  Boost
        [HideInInspector] public float lastBoostDuration { get; internal set; } = 0f;
        [HideInInspector] public bool allowBoost { get; internal set; } = true;

        // used to add to what is considered the maximum air speed

        public bool holdingJump { get; private set; } = false;
        private bool cancelledJump = false;

        // Inputs that require being held;
        [HideInInspector] public bool holdingSprint { get; private set; } = false;
        [HideInInspector] public bool holdingGlide { get; private set; } = false;
        [HideInInspector] public bool holdingJetpack { get; private set; } = false;


        // Camera Rotation
        private Vector3 cachedCameraForward = Vector3.zero;
        private readonly WaitForFixedUpdate _waitForFixedUpdate = new();



        // Events
        [System.Serializable]
        public class StateEvents
        {
            [System.Serializable]
            public class GliderEvents
            {
                public UnityEvent OnGliderEntry;
                public UnityEvent OnGliderExit;
                public UnityEvent OnGliderBankingEntry;
                public UnityEvent OnGliderBankingExit;
            }

            [System.Serializable]
            public class FallEvents
            {
                public UnityEvent OnLandingSoil;
            }

            public GliderEvents glider;
            public FallEvents fall;
        }
        [Header("Events")]
        public StateEvents events;

        protected override void Initialize()
        {
            pCon = GetComponent<PlayerController>();
            animator = GetComponent<Animator>();
            ikCon = GetComponent<IKController>();
            ikCon.Initialize();

            animCon = GetComponent<PlayerAnimationController>();

            animCon.Initialize(pCon, this, animator);

            SubscribeToInputEvents();

            StartCoroutine(LateFixedUpdate());

            Debug.Log("INVENTORY TESTS");
            Debug.Log("Base Run Speed: " + pCon.Stats.GetBaseStatInfo(Stat.RunSpeed).value);
            Debug.Log("Upgraded Run Speed: " + pCon.Stats.GetStat(Stat.RunSpeed));
            Debug.Log("Has Active Firmware Run Firmware? : " +
                InventoryData.Inventory.HasActiveFirmware<RunSpeedFirmware>());
        }

        // Subscribe to input events
        public void OnEnable()
        {
            SubscribeToInputEvents();
        }

        // unsubscribe from input events
        public void OnDisable()
        {
            UnsubscribeFromInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            if (pCon == null) return;
            if (pCon.inputEvents == null) return;
            if (pCon.inputEvents.player == null) return;

            pCon.inputEvents.player.OnMove += HandleMove;
            pCon.inputEvents.player.OnJump += HandleJump;
            pCon.inputEvents.player.OnSprint += HandleSprint;
            pCon.inputEvents.player.OnGraple += HandleGraple;
            pCon.inputEvents.player.OnLook += HandleLook;
            pCon.inputEvents.player.OnBoost += HandleBoost;
            pCon.inputEvents.player.OnJetpack += HandleJetpack;
            pCon.inputEvents.player.OnPause += HandlePause;
            pCon.inputEvents.player.OnInventory += HandleInventory;
            pCon.inputEvents.player.OnInteract += HandleInteract;
        }
        private void UnsubscribeFromInputEvents()
        {
            pCon.inputEvents.player.OnMove -= HandleMove;
            pCon.inputEvents.player.OnJump -= HandleJump;
            pCon.inputEvents.player.OnSprint -= HandleSprint;
            pCon.inputEvents.player.OnGraple -= HandleGraple;
            pCon.inputEvents.player.OnLook -= HandleLook;
            pCon.inputEvents.player.OnBoost -= HandleBoost;
            pCon.inputEvents.player.OnJetpack -= HandleJetpack;
            pCon.inputEvents.player.OnPause -= HandlePause;
            pCon.inputEvents.player.OnInventory -= HandleInventory;
            pCon.inputEvents.player.OnInteract -= HandleInteract;
        }

        private void Update()
        {

            if(holdingJump)
                UpdateGeneralChargePercent();

            CurrentState.UpdateState();
            PlayerDetectInteractable();

            animCon.AnimationControl();
        }

        private void FixedUpdate()
        {
            DecayJumpBuffer();

            CurrentState.FixedUpdateState();

            Vector3 velocity = pCon.rb.velocity;
            Vector3 start = pCon.transform.position;
            Vector3 end = start + velocity;

            Debug.DrawLine(start, end, Color.cyan, 0f, false);
        }

        private void LateUpdate()
        {
            CacheCameraForward();
            CurrentState.LateUpdateState();

        }

        private IEnumerator LateFixedUpdate()
        {
            while (true)
            {
                yield return _waitForFixedUpdate;
                PlayerLookInCameraDirection();
            }
        }

        private void CacheCameraForward()
        {
            if (pCon?.CameraStateHandler?.brain == null) return;
            CinemachineBrain brain = pCon?.CameraStateHandler.brain;

            if (!brain.IsBlending &&
                brain.ActiveVirtualCamera != null &&
                pCon.CameraStateHandler.brain.ActiveVirtualCamera.VirtualCameraGameObject
                == pCon.CameraStateHandler.standard3rdPersonCamera.VirtualCameraGameObject)
            {
                cachedCameraForward = UnityEngine.Camera.main.transform.forward;
            }
        }

        private void PlayerLookInCameraDirection()
        {
            if (pCon?.CameraStateHandler?.brain == null) return;
            CinemachineBrain brain = pCon?.CameraStateHandler.brain;

            // Player looks in camera direction only if the active camera is 
            // the 3rd person camera
            if (!brain.IsBlending &&
                brain.ActiveVirtualCamera != null &&
                pCon.CameraStateHandler.brain.ActiveVirtualCamera.VirtualCameraGameObject 
                == pCon.CameraStateHandler.standard3rdPersonCamera.VirtualCameraGameObject)
            {
                CurrentState.LookInCameraDirection(cachedCameraForward);
            }
        }

        private void PlayerDetectInteractable()
        {
            if (pCon?.CameraStateHandler?.brain == null) return;
            CinemachineBrain brain = pCon?.CameraStateHandler.brain;

            // We only check for interactables if in 3rd person camera
            if (!brain.IsBlending &&
                brain.ActiveVirtualCamera != null &&
                pCon.CameraStateHandler.brain.ActiveVirtualCamera.VirtualCameraGameObject
                == pCon.CameraStateHandler.standard3rdPersonCamera.VirtualCameraGameObject)
            {

                Ray ray = new Ray(pCon.transform.position, cachedCameraForward);
                Debug.DrawRay(ray.origin, ray.direction, Color.yellow);
                if(Physics.Raycast(ray, out RaycastHit hit, pCon.interactionDistance, pCon.interactableLayer))
                {
                    Interactable interactable = hit.collider.GetComponent<Interactable>();
                    if(interactable != null)
                    {
                        if(interactable != pCon.interactionTarget)
                        {
                            // new object found
                            pCon.interactionTarget = interactable;
                            Debug.Log("Looking at: " + pCon.interactionTarget.name);
                        }
                        return;
                    }
                }
                if (pCon.interactionTarget != null)
                {
                    pCon.interactionTarget = null;
                }
            }
        }

        #region CONTROL

        private void DecayJumpBuffer()
        {
            if(jumpBufferTimer > 0f)
            {
                float newT = jumpBufferTimer - Time.deltaTime;
                jumpBufferTimer = Mathf.Max( 0f, newT );
            }
        }
        public void SetJumpBuffer()
        {
            jumpBufferTimer = jumpBufferDuration;
        }
        public void ClearJumpBuffer()
        {
            jumpBufferTimer = 0f;
            jumpChargeTime = 0f;
        }
        public bool JumpBufferActive()
        {
            return jumpBufferTimer > 0f;
        }

        public void CancelJump()
        {
            ClearJumpBuffer();
            pCon.playerStateShared.GeneralChargePercent.v = 0f;
            cancelledJump = true;
            holdingJump = false;
        }
        #endregion

        // virtual method now
        // protected override void SwitchStateImpl(PlayerBaseState newState)
        // {
        //     CurrentState.ExitState();
        //     CurrentState = newState;
        //     CurrentState.EnterState();
        // }

        #region PLAYER_CONTROL_INTERFACE
        public void HandleMove(InputAction.CallbackContext context)
        {
            horizontalInput = context.ReadValue<Vector2>().x;
            forwardInput = context.ReadValue<Vector2>().y;

            pCon.horizontalInput = horizontalInput;
            pCon.forwardInput = forwardInput;
        }

        public void HandleJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpStartHoldTime = Time.time;
                holdingJump = true;
                cancelledJump = false;
                CurrentState.HandleJumpPressed();
            }

            else if (context.canceled 
                && pCon.gameState.state == GameState.State.PLAY)
            {
                // Dont do any of the standard code if we forced the jump to cancel
                if (cancelledJump)
                {
                    cancelledJump = false;
                    return;
                }

                jumpChargeTime = Time.time - jumpStartHoldTime;
                SetJumpBuffer();
                CurrentState.HandleJumpReleased();
                //bufferedJumpChargeTime = jumpChargeTime;
                //jumpChargeTime = 0f;
                pCon.playerStateShared.GeneralChargePercent.v = 0f;
                holdingJump = false;
            }
        }

        public void HandleBoost(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CurrentState.HandleBoostPressed();
            }
            else if (context.canceled
                && pCon.gameState.state == GameState.State.PLAY)
            {
                CurrentState.HandleBoostReleased();
            }
        }

        public void HandleGraple(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CurrentState.HandleGraplePressed();
            }
            else if (context.canceled
                && pCon.gameState.state == GameState.State.PLAY)
            {
                CurrentState.HandleGrapleReleased();
            }
        }

        public void HandleSlide(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Slide Pressed");
            }
            else if (context.canceled)
            {
                Debug.Log("Slide Released");
            }
        }

        public void HandleLook(InputAction.CallbackContext context)
        {
        }

        public void HandleJetpack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                holdingJetpack = true;
                holdingGlide = true;
            }
            else if (context.canceled)
            {
                holdingJetpack = false;
                holdingGlide = false;
            }
        }

        public void HandleSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                holdingSprint = true;
            }
            else if (context.canceled)
            {
                holdingSprint = false;
            }
        }

        public void HandlePause(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Player Requesting Pause");
                pCon.gameState.Requests?.Pause?.Invoke();
            }
        }

        public void HandleInventory(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Player Requesting Enter Inventory");
                pCon.gameState.Requests?.EnterInventory?.Invoke();
            }
        }

        public void HandleInteract(InputAction.CallbackContext context)
        {
            if (pCon.interactionTarget == null) return;
            if(context.performed)
            {
                pCon.interactionTarget.OnInteractedWith();
            }
        }

        public void HandleTriggerEnter(Collider2D collision)
        {
            Debug.Log("Trigger Enter");
        }

        public void HandleCollisionEnter(Collision2D collision)
        {
            Debug.Log("Collision Enter");
        }

        #endregion

        #region ABILITIES
 
        public float CalculateJumpPower(float chargeTime)
        {
            float maxPower = pCon.Stats.GetStat(Stat.BoostPower);
            float timeToMaxCharge = pCon.Stats.GetStat(Stat.TimeToMaxChargeJump);

            Debug.Log("charge time: " + chargeTime);
            float powerPercentage = Mathf.Clamp(
                chargeTime / timeToMaxCharge,
                0f,
                1f);
            float power = pCon.minJumpingPower
                + ((maxPower - pCon.minJumpingPower) * powerPercentage);
            return power;
        }

        public float CalculateBoostPower()
        {
            float maxPower = pCon.Stats.GetStat(Stat.BoostPower);
            float timeToMaxCharge = pCon.Stats.GetStat(Stat.TimeToMaxChargeJump);

            Debug.Log("charge time: " + jumpChargeTime);
            float powerPercentage = Mathf.Clamp(
                lastBoostDuration / timeToMaxCharge,
                0f,
                1f);
            float power = pCon.minJumpingPower
                + ((maxPower - pCon.minJumpingPower) * powerPercentage);
            return power;
        }

        public float GetPlaneVelocity()
        {
            float x = pCon.rb.velocity.x;
            float z = pCon.rb.velocity.z;
            return Mathf.Sqrt(x * x + z * z);
        }
        public float GetVerticalVelocity()
        {
            return pCon.rb.velocity.y;
        }

        private void UpdateGeneralChargePercent()
        {
            float timeToMaxCharge = pCon.Stats.GetStat(Stat.TimeToMaxChargeJump);
            float jumpChargeDuration = Time.time - jumpStartHoldTime;
            float powerPercentage = Mathf.Clamp(
                jumpChargeDuration / timeToMaxCharge,
                0f,
                1f);
            pCon.playerStateShared.GeneralChargePercent.v = powerPercentage;
        }

        public float CalculateMaxRunSpeed()
        {
            return 0f;
        }

        public bool CheckCanMantle()
        {
            return (pCon.GetLastMantleSensorResult().canMantle && holdingJump);
        }
        #endregion

        #region TIMINGS_AND_COOLDOWNS
        #endregion
    }
}
