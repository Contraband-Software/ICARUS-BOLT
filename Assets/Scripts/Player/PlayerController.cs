using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resources;
using Resources.System;
using UnityEngine.InputSystem;
using Resources.Modules;
using UnityEngine.UI;
using Content.System.Modules;
using Camera;
using System.Linq;
using Software.Contraband.StateMachines;
using SharedState;
using ProgressionV2;

namespace Player
{
    [
        RequireComponent(typeof(PlayerStateHandler)),
        RequireComponent(typeof(UpgradeableStats)),
        RequireComponent(typeof(Fuel)),
        RequireComponent(typeof(MantleSensor)),
        RequireComponent(typeof(PlayerVFX)),
        SelectionBase
    ]

    public class PlayerController : MonoBehaviour
    {
        public bool isGrounded { get; private set; } = false;
        public bool canMantle { get; private set; } = false;
        private Vector3 prevPos;
        private Quaternion prevRot;

        [Header("Input")]
        public InputEvents inputEvents;

        [Header("Player Shared State")]
        public PlayerState playerStateShared;

        [Header("Game Shared State")]
        public GameState gameState;

        [Header("Component References")]
        public Rigidbody rb;
        public CapsuleCollider highestRadiusBodyCollider;
        public PlayerStateHandler StateHandler { get; private set; } 
        public UpgradeableStats Stats { get; private set; }
        public MantleSensor mantleSensor { get; private set; }
        public Fuel Fuel { get; private set; }
        public PlayerVFX vfxCon { get; private set; }

        [Header("Camera")]
        [SerializeField]
        private CameraStateHandler _cameraStateHandler;
        public CameraStateHandler CameraStateHandler => _cameraStateHandler;

        [Header("Movement GameSettingsPreset")]
        [Range(0f, 10f)] public float mouseSensitivityVertical = 1f;
        [SerializeField] private Slider mouseSensSliderHorizontal;
        [SerializeField] private InputActionReference lookAction;

        [Space(10)]
        [Header("Jump GameSettingsPreset")]
        public float minJumpingPower;
        public float downwardAccel;
        public float upwardDecel;
        public float airDecel; //higher value = more lopsided arc
        public float coyoteTime = 0.25f;
        [Header("Boost GameSettingsPreset")]
        public float velocityCut = 0f;
        public float downAccelBoost = 0f;
        public float upDecelBoost = 0f;
        public float boostGracePeriod = 0f;

        [Space(10)]
        [Header("Graple GameSettingsPreset")]
        public float graplePower;

        [Header("Collision")]
        public LayerMask terrainLayer;
        public Transform groundCheck;
        public CapsuleCollider bodyCollider;
        [SerializeField] Vector3 groundCheckCapsuleTop;
        [SerializeField] Vector3 groundCheckCapsuleBottom;
        [SerializeField] float groundCheckCapsuleRadius;
        [SerializeField] float freeMovementSlopeAngleMax = 24f;
        [SerializeField] float struggleSlopeAngleMax = 50f;
        [SerializeField] float slopeMultiplierSmoothing = 2.5f;
        [SerializeField] float maxSlopeDownwardMultiplier = 1.5f;
        [SerializeField] float minSlopeUpwardMultiplier = 0.2f;
        [SerializeField] float minSlopeDownwardMultiplier = 1f;
        public float coyoteTimeHeightThreshold { get; private set; } = 5f;
        public float horizontalInput { private get; set; } = 0f;
        public float forwardInput { private get; set; } = 0f;
        public Quaternion currentSurfaceQuaternion {get; private set;} = Quaternion.identity;
        public float currentSurfaceSlope { get; private set; } = 0f;
        public float currentSurfaceSlopeRelativeToPlayer { get; private set; } = 0f;

        [Header("Interaction")]
        public LayerMask interactableLayer;
        public float interactionDistance = 5f;
        public Interactable interactionTarget;

        [Header("Physics")]
        [SerializeField] private PhysicMaterial defaultPhysicMaterial;
        [SerializeField] private PhysicMaterial stationaryPhysicMaterial;

        [Space(20)]
        [Header("PSEUDO MODULES/FIRMWARES")]
        public bool hasGrasshopper = false;
        public bool hasRabbit = false;
        public bool hasSprint = false;
        public bool hasJetpack = false;
        public bool hasGlider = false;
        public bool hasGraple = false;

        [Space(20)]
        [Header("MODULE TOGGLING")]
        public SkinnedMeshRenderer parachute;
        public SkinnedMeshRenderer parachuteBase;
        public SkinnedMeshRenderer jetpack;
        public SkinnedMeshRenderer left_leg;
        public SkinnedMeshRenderer right_leg;
        public Material grasshopperMaterial;
        public Material rabbitMaterial;
        public Material baseLegMaterial;

        [Space(20)]
        [Header("DEBUG")]
        public float forceGliderTilt = 0;
        public float fakeGliderHorizontalInput = 0;

        // INTERNALS
        [HideInInspector] public Vector3 lastVelocity = Vector3.zero;
        [HideInInspector] public Vector3 entryVelocity = Vector3.zero;
        [HideInInspector] public bool entryVelocityCompensated = false;
        // this variable determines how many frames the run state is allowed
        // to be "grounded" in without the players collider actually touching the floor.
        // This is so that if a velocity reset happens due to the collider hitting the ground,
        // the entry velocity is return to the player on that frame.
        [HideInInspector] public int entryBufferFrames = 4;
        [HideInInspector] public int currentStateFrame = 0;

        // TELEMTRY TRACKING
        private float highestRecentFallVelocity = 0f;
        public float HighestRecentFallVelocity => highestRecentFallVelocity;
        private int highestRecentFallVelocity_FramesAgo = 0;

        void Awake()
        {
            StateHandler = GetComponent<PlayerStateHandler>();
            Stats = GetComponent<UpgradeableStats>();
            mantleSensor = GetComponent<MantleSensor>();
            Fuel = GetComponent<Fuel>();
            vfxCon = GetComponent<PlayerVFX>();

            vfxCon.pCon = this;
        }

        private void OnEnable()
        {
            InventoryData.Inventory.OnModuleActivated += HandleModuleActivated;
            InventoryData.Inventory.OnModuleDeactivated += HandleModuleDeactivated;
        }

        private void OnDisable()
        {
            InventoryData.Inventory.OnModuleActivated -= HandleModuleActivated;
            InventoryData.Inventory.OnModuleDeactivated -= HandleModuleDeactivated;
        }

        private void Start()
        {
            Fuel.ResetResource();
            //ProcessHorizontalSensitivitySliderValue(mouseSensSliderHorizontal.value);
           

            ToggleJetpackModel(hasJetpack);
            ToggleGliderBaseModel(hasGlider);
            ToggleGliderModel(false);

            prevPos = rb.position;
            prevRot = rb.rotation;
        }

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            isGrounded = IsGrounded();
            if (isGrounded)
            {
                calculateOfSlopeBelowPlayer();
            }

            mantleSensor.EvaluateMantle();
            TrackFallVelocity();
        }

        private void LateUpdate()
        {      
        }

        public bool IsGrounded()
        {
            // Define the bottom and top points of the capsule
            float bodyColliderHalfHeight = bodyCollider.height * 0.5f;
            Vector3 bottomPoint = bodyCollider.bounds.center - Vector3.up * bodyColliderHalfHeight;
            Vector3 capsuleBottom = bottomPoint + groundCheckCapsuleBottom;
            Vector3 capsuleTop = bottomPoint + groundCheckCapsuleTop;
            // Perform capsule check

            Debug.DrawLine(capsuleTop, capsuleBottom, Color.blue);
            return 
                Physics.CheckCapsule(capsuleBottom, capsuleTop, groundCheckCapsuleRadius, terrainLayer);

        }

        public void calculateOfSlopeBelowPlayer()
        {
            float bodyColliderHalfHeight = bodyCollider.height * 0.5f;
            float bodyColliderRadius = bodyCollider.radius;

            // Get the bottom center of the collider
            Vector3 bottomPoint = bodyCollider.bounds.center - Vector3.up * bodyColliderHalfHeight
                + new Vector3(0, 1f, 0);

            // Define surrounding points based on radius distance relative to the player's transform
            Vector3 bottomForward = bottomPoint + transform.forward * bodyColliderRadius;
            Vector3 bottomBehind = bottomPoint + -transform.forward * bodyColliderRadius; // Using negative forward for "behind"
            Vector3 bottomRight = bottomPoint + transform.right * bodyColliderRadius;
            Vector3 bottomLeft = bottomPoint + -transform.right * bodyColliderRadius; // Using negative right for "left"

            // Define corner points with normalized diagonal vectors relative to the player's transform
            Vector3 bottomForwardRight = bottomPoint + Vector3.Normalize(transform.forward + transform.right) * bodyColliderRadius;
            Vector3 bottomForwardLeft = bottomPoint + Vector3.Normalize(transform.forward + -transform.right) * bodyColliderRadius; // Using negative right
            Vector3 bottomBehindRight = bottomPoint + Vector3.Normalize(-transform.forward + transform.right) * bodyColliderRadius; // Using negative forward
            Vector3 bottomBehindLeft = bottomPoint + Vector3.Normalize(-transform.forward + -transform.right) * bodyColliderRadius; // Both negative

            // bias forward motion
            Vector3 bottomForward2 = bottomPoint + transform.forward * 0.1f;
            Vector3 bottomRight2 = bottomRight + transform.forward * 0.1f;
            Vector3 bottomLeft2 = bottomLeft + transform.forward * 0.1f;

            Vector3[] rays = new Vector3[]
            {
                bottomPoint,
                bottomForward,
                bottomBehind,
                bottomRight,
                bottomLeft,
                bottomForwardRight,
                bottomForwardLeft,
                bottomBehindRight,
                bottomBehindLeft,
                bottomForward2,
                bottomRight2,
                bottomLeft2
            };

            Debug.DrawLine(bodyCollider.bounds.center, bottomPoint, Color.red);
            Debug.DrawLine(bottomPoint, bottomPoint + Vector3.down, Color.blue);
            Debug.DrawLine(bottomForward, bottomForward + Vector3.down, Color.blue);
            Debug.DrawLine(bottomBehind, bottomBehind + Vector3.down, Color.blue);
            Debug.DrawLine(bottomRight, bottomRight + Vector3.down, Color.blue);
            Debug.DrawLine(bottomLeft, bottomLeft + Vector3.down, Color.blue);
            Debug.DrawLine(bottomForwardRight, bottomForwardRight + Vector3.down, Color.blue);
            Debug.DrawLine(bottomForwardLeft, bottomForwardLeft + Vector3.down, Color.blue);
            Debug.DrawLine(bottomBehindRight, bottomBehindRight + Vector3.down, Color.blue);
            Debug.DrawLine(bottomBehindLeft, bottomBehindLeft + Vector3.down, Color.blue);
            Debug.DrawLine(bottomForward2, bottomForward2 + Vector3.down, Color.blue);
            Debug.DrawLine(bottomRight2, bottomRight2 + Vector3.down, Color.blue);
            Debug.DrawLine(bottomLeft2, bottomLeft2 + Vector3.down, Color.blue);

            // find the direction player intends to move
            Vector3 playerIntentMotion = MathF.Ceiling(forwardInput) * transform.forward
            +MathF.Ceiling(horizontalInput) * transform.right;
            // If player does not intend to move, use facing direction
            if (MathF.Ceiling(forwardInput) == 0f)
            {
                playerIntentMotion = transform.forward;
            }

            // Normalize the direction vector to ensure unit length
            playerIntentMotion = Vector3.Normalize(playerIntentMotion);
            //Debug.Log(playerIntentMotion.ToString());

            // choose closest raycast hit to bottomPoint
            // Dictionary from Normal -> [distances to bottom of collider]
            Dictionary<Vector3, List<float>> normalDistances = new Dictionary<Vector3, List<float>>();

            foreach(Vector3 v in rays)
            {
                var ray = new Ray(v, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 2f, terrainLayer))
                {

                    // ignore the surface if its normal is point down
                    if(Vector3.Angle(hitInfo.normal, Vector3.up) < 0)
                    {
                        continue;
                    }

                    float distance = (hitInfo.point - bottomPoint).sqrMagnitude;
                    if (!normalDistances.ContainsKey(hitInfo.normal))
                    {
                        normalDistances[hitInfo.normal] = new List<float>();
                    }
                    normalDistances[hitInfo.normal].Add(distance);
                    //Debug.Log("Slope Relative: " + currentSurfaceSlopeRelativeToPlayer);
                    //Debug.Log("Slope: " + currentSurfaceSlope);
                    Debug.DrawLine(hitInfo.point, hitInfo.point + Vector3.down * 0.2f, Color.green);
                }
            }

            // see which slope, on average, is closest to the bottom point of the collider
            if(normalDistances.Count != 0)
            {
                /*Vector3 closestNormal = normalDistances.Keys.First();
                float closestDistance = float.PositiveInfinity;
                foreach (Vector3 key in normalDistances.Keys)
                {
                    float avgDistance = normalDistances[key].Average();
                    if(avgDistance < closestDistance)
                    {
                        closestDistance = avgDistance;
                        closestNormal = key;
                    }
                }*/

                Vector3 bestNormal = normalDistances.Keys.First();
                int count = 0;
                foreach(Vector3 key in normalDistances.Keys)
                {
                    int c = normalDistances[key].Count;
                    if(c > count)
                    {
                        count = c;
                        bestNormal = key;
                    }

                }

                currentSurfaceQuaternion = Quaternion.FromToRotation(Vector3.up, bestNormal);
                currentSurfaceSlopeRelativeToPlayer = Vector3.Angle(bestNormal, playerIntentMotion) - 90;
                currentSurfaceSlope = Vector3.Angle(bestNormal, Vector3.up);
                //Debug.Log("Slope: " + currentSurfaceSlopeRelativeToPlayer);
            }
            else
            {
                currentSurfaceQuaternion = Quaternion.identity;
                currentSurfaceSlopeRelativeToPlayer = 0f;
                currentSurfaceSlope = 0f;
            }
        }

        /// <summary>
        /// 0 to 24 degrees: Move normally
        /// 24 to 50 degrees: increased resistance upwards, faster fall
        /// 50+ : impossible to climb, but can descend very fast
        /// </summary>
        /// <param name="slopeAngle">Angle of slope relative to players intended motion direction</param>
        /// <returns>The slope multiplier</returns>
        public float GetSlopeMultiplier(float slopeAngle, float slopeAngleRelative)
        {
            float absSlopeAngle = MathF.Abs(slopeAngle);
            //Debug.Log("Slop Angle: " + slopeAngle + " Rel Slop Angle: " + slopeAngleRelative);
            if(absSlopeAngle < freeMovementSlopeAngleMax)
            {
                return 1f;
            }
            if(absSlopeAngle < struggleSlopeAngleMax)
            {
                // dToMax = (Z - min) / (max - min)

                //slopeAngleRelative *= -1f;
                // scale input motion by slopeAngleRelative
                if(slopeAngleRelative >= 0)
                {
                    // player moving upwards
                    if (slopeAngleRelative < freeMovementSlopeAngleMax)
                    {
                        return 1f;
                    }
                    float dToMax = (slopeAngleRelative - freeMovementSlopeAngleMax)
                        / (struggleSlopeAngleMax - freeMovementSlopeAngleMax);
                    float min = minSlopeUpwardMultiplier;
                    float k = slopeMultiplierSmoothing;
                    float mult = min + (1 - min) * MathF.Pow((1 - dToMax), k);
                    return mult;
                }
                else
                {
                    //player moving downwards
                    float absSlopeAngleRelative = MathF.Abs(slopeAngleRelative);
                    if (absSlopeAngleRelative < freeMovementSlopeAngleMax)
                    {
                        return 1f;
                    }
                    float dToMax = (absSlopeAngleRelative - freeMovementSlopeAngleMax)
                        / (struggleSlopeAngleMax - freeMovementSlopeAngleMax);
                    float min = minSlopeDownwardMultiplier;
                    float k = slopeMultiplierSmoothing;
                    float mult = min - (maxSlopeDownwardMultiplier - min) * MathF.Pow(1 - dToMax, k);
                    return mult;
                }
            }

            // otherwise we restrict movement, letting player fall
            Debug.Log("SLOPE MULTIPLIER ZERO");
            return 0f;
        }
        /// <summary>
        /// On entering a grounded state, returns velocity if it abruptly
        /// turns to near zero (caused by collider hitting floor after "grounded" logically)
        /// </summary>
        public void CompensateGroundEntryVelocity()
        {
            if (entryVelocityCompensated || currentStateFrame > entryBufferFrames)
            {
                return;
            }
            if (entryVelocity.magnitude < 1)
            {
                entryVelocityCompensated = true;
                return;
            }
            if (MathF.Abs(rb.velocity.x) < 0.1
                || MathF.Abs(rb.velocity.z) < 0.1)
            {
                Debug.Log("COMPENSTATE VELOCITY");
                Vector3 compDirection = Vector3.Normalize(lastVelocity);
                compDirection.y = 0f;
                float compMagnitude = Vector3.Magnitude(lastVelocity);
                Vector3 compVelocity = compDirection * compMagnitude;
                rb.velocity = compVelocity;
                entryVelocityCompensated = true;

            }
            currentStateFrame++;
            lastVelocity = rb.velocity;
            //Debug.Log("new velocity: " + lastVelocity);
        }

        /// <summary>
        /// Judge what material was landed on, activate correct
        /// event in StateEvents in StateHandler
        /// </summary>
        public void RespondToLanding()
        {
            StateHandler.events.fall.OnLandingSoil.Invoke();
        }

        public MantleSensor.MantleResult GetLastMantleSensorResult()
        {
            return mantleSensor.CurrentMantleResult;
        }
        

        public Vector2 ApplyMouseSensitivity(Vector2 rawInput)
        {
            float mouseX = rawInput.x * GameSettings.Data.control.x_sensitivity;
            float mouseY = rawInput.y * mouseSensitivityVertical;
            return new Vector2 (mouseX, mouseY);
        }
        public Vector2 ApplyMouseSensitivityVertical(Vector2 rawInput)
        {
            float mouseY = rawInput.y * mouseSensitivityVertical;
            return new Vector2(rawInput.x, mouseY);
        }

        public void ToggleStationaryPhysicMaterial(bool toggle)
        {
            if (toggle)
            {
                if(bodyCollider.material != stationaryPhysicMaterial)
                {
                    bodyCollider.material = stationaryPhysicMaterial;
                }
            }
            else
            {
                if (bodyCollider.material != defaultPhysicMaterial)
                {
                    bodyCollider.material = defaultPhysicMaterial;
                }
            }
        }

        public void ToggleCollision(bool collisionON)
        {
            if(collisionON)
            {
                gameObject.layer = LayerMask.NameToLayer("Player");
                bodyCollider.gameObject.layer = LayerMask.NameToLayer("Player");
                highestRadiusBodyCollider.gameObject.layer = LayerMask.NameToLayer("Player");
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("PlayerNoCollision");
                bodyCollider.gameObject.layer = LayerMask.NameToLayer("PlayerNoCollision");
                highestRadiusBodyCollider.gameObject.layer = LayerMask.NameToLayer("PlayerNoCollision");
            }
        }

        private void TrackFallVelocity()
        {
            if (isGrounded) return;
            float fallVelocity = rb.velocity.y;
            if(fallVelocity < highestRecentFallVelocity)
            {
                highestRecentFallVelocity = Mathf.Abs(fallVelocity);
                highestRecentFallVelocity_FramesAgo = 0;
            }
            else
            {
                highestRecentFallVelocity_FramesAgo++;
            }

            if(highestRecentFallVelocity_FramesAgo > 10)
            {
                highestRecentFallVelocity = 0f;
                highestRecentFallVelocity_FramesAgo = 0;
            }
        }

        private void HandleModuleActivated(ModuleUpgradeAsset moduleType)
        {
            Debug.Log("A module was activated");
            if(moduleType is JetpackModule)
            {
                ToggleJetpackModel(true);
                return;
            }
            if(moduleType is GliderModule)
            {
                ToggleGliderBaseModel(true);
                return;
            }
        }

        private void HandleModuleDeactivated(ModuleUpgradeAsset moduleType)
        {
            if (moduleType is JetpackModule)
            {
                ToggleJetpackModel(false);
                return;
            }
            if (moduleType is GliderModule)
            {
                ToggleGliderBaseModel(false);
                return;
            }
        }

        public void ToggleGliderModel(bool active)
        {
            if(!parachute.enabled == active) parachute.enabled = active;
        }
        public void ToggleGliderBaseModel(bool active)
        {
            if(!parachuteBase.enabled == active) parachuteBase.enabled = active;
        }
        public void ToggleJetpackModel(bool active)
        {
            if(!jetpack.enabled == active) jetpack.enabled = active;
        }
    }
}

