using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using System.Linq;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements.Experimental;

namespace Player
{
    /// <summary>
    /// Script used to sense whether the player can mantle, and if so, providing the data
    /// required for the Mantle state and IK to execute properly
    /// </summary>
    public class MantleSensor : MonoBehaviour
    {
        [Header("Player Oriented Properties")]
        public Transform playerTransform;
        public Rigidbody playerRb;
        public CapsuleCollider widestPlayerCollider;
        public CapsuleCollider tallestPlayerCollider;
        public Transform playerFeet;
        public float shoulderDistance = 4f;

        [Header("  > Reaches")]
        public float forwardReach = 4f;
        public float sideReach = 4f;
        public float upwardsReach = 4f;
        public float baseDownwardReach = 4f;
        public float increaseDownReachAtYY = 15f;
        public float maxDownReachAtVY = 40f;
        private float downwardReach = 0f;
        public float maxDownReach = 10f;
        [Header("  > Reach Allowance")]
        public float forwardReach_addAllowance = 3f;

        [Header("Sensor Detail")]
        public LayerMask mantleableLayers;
        public float arcRadiusStep = 0.5f;
        public int mongolianWidthIncrementsPerSide = 8;
        public int mongolianScanWidthIncrements = 8;
        public float impossibleSlopeAngle = 50f;
        public float landingClearance_maxUpOffset = 4f;
        public int landingClearance_increments = 8;
        public int motionClearance_increments = 8;
        public float motionClearance_minYOffset = 1f;
        public float motionClearance_maxYOffset = 4f; // How high above landing point the mantle motion will go at peaks
        public float motionClearance_collider_offset_from_player = 2f;

        [Header("Debug")]
        public bool debug_broad_checks = false;
        public bool debug_downwardRaycastArcs = false;
        public bool debug_findVirtualMantleDirection = false;
        public bool debug_edgeFinding = false;
        public bool debug_edgeProcessing = false;
        public bool debug_handPointFinding = false;
        public bool debug_landingClearance = false;
        public bool debug_motionClearance = false;

        // Internal
        Vector3 origin_near_ = Vector3.zero;
        Vector3 origin_near_high_ = Vector3.zero;

        public struct MantleResult
        {
            public bool canMantle;
            public Vector3 leftHandPos;
            public Vector3 rightHandPos;
            public Vector3 landingPos;
            public float yClearancePoint;
            public Vector3 closestApproachPoint;

            public static readonly MantleResult Invalid = new MantleResult { canMantle = false };
            public override string ToString()
            {
                if (!canMantle)
                    return "MantleResult: Invalid";

                return $"MantleResult: canMantle = {canMantle}, " +
                       $"leftHandPos = {leftHandPos}, " +
                       $"rightHandPos = {rightHandPos}, " +
                       $"landingPos = {landingPos}" +
                       $"yClearance = {yClearancePoint}";
            }
        }

        public MantleResult CurrentMantleResult { get; private set; } = MantleResult.Invalid;

        internal void EvaluateMantle()
        {
            CurrentMantleResult = TryFindMantle();
        }

        private MantleResult TryFindMantle()
        {
            CalculateOrigins();

            ScaleDownwardReach();

            // Broad
            if (!BroadFrontCheck()) return MantleResult.Invalid;
            if (!BroadRadiusCheck()) return MantleResult.Invalid;

            // Narrow
            // Cast downward rays in decreasing radius arcs
            int numArcs = Mathf.FloorToInt((forwardReach + forwardReach_addAllowance) / arcRadiusStep);
            List<ValuedHitPoint> validArcRaycastSeries = new List<ValuedHitPoint>();
            for (int i = 0; i < numArcs; i++)
            {
                float currentRadius = (forwardReach + forwardReach_addAllowance) - i * arcRadiusStep;
                // Value of arcRaycasts is the angle they were shot at
                List<ValuedHitPoint> arcRaycasts = DownwardRaycastArc(currentRadius, 3f);
                int hits = 0;
                foreach(ValuedHitPoint ray in arcRaycasts)
                {
                    if (ray.hit.collider != null) hits++;
                }
                if (arcRaycasts.Count == 0) continue;
                if (hits == 0) continue;
                else
                {
                    validArcRaycastSeries = arcRaycasts;
                    break;
                }
            }
            if (validArcRaycastSeries.Count == 0) return MantleResult.Invalid;

            // Haitian groupings from arc hits
            List<HaitianGroup> haitianGroups = HaitianGroupings(validArcRaycastSeries);
            if (haitianGroups.Count == 0) return MantleResult.Invalid;

            // Find best virtual mantle direction
            Vector3 virtualMantleDirection;
            if (!FindBestVirtualMantleDirection(haitianGroups, out virtualMantleDirection)) return MantleResult.Invalid;

            // Find edges using virutal mantle direction
            List<ValuedPoint> leftHitPoints;
            List<ValuedPoint> rightHitPoints;
            MongolianEdgeFinding(virtualMantleDirection, shoulderDistance, out leftHitPoints, out rightHitPoints);
            if (leftHitPoints.Count == 0 && rightHitPoints.Count == 0) return MantleResult.Invalid;

            List<Edge> foundEdges = new List<Edge>();
            ProcessPointsIntoEdges(leftHitPoints, rightHitPoints, out foundEdges);
            if (foundEdges.Count == 0) return MantleResult.Invalid;

            Vector3 leftHandPoint;
            Vector3 rightHandPoint;
            if(!FindBestHandPoints_2(foundEdges, virtualMantleDirection, out leftHandPoint, out rightHandPoint))
                return MantleResult.Invalid;

            // Check landing clearance
            Vector3 landingPoint;
            Vector3 landingPointTop;
            if(!LandingClearance(leftHandPoint, rightHandPoint, out landingPoint, out landingPointTop)) return MantleResult.Invalid;
            Drawing.DrawCrossOnXZPlane(landingPoint, 0.05f, Color.cyan);

            // Check motion clearance
            float yClearancePoint;
            Vector3 closestApproachPoint = Vector3.zero;
            if(!MotionClearance(
                landingPoint,
                landingPointTop,
                out yClearancePoint,
                out closestApproachPoint)) return MantleResult.Invalid;

            return new MantleResult
            {
                canMantle = true,
                leftHandPos = leftHandPoint,
                rightHandPos = rightHandPoint,
                landingPos = landingPoint,
                yClearancePoint = yClearancePoint,
                closestApproachPoint = closestApproachPoint
            };
        }

        private void ScaleDownwardReach()
        {
            float yVel = playerRb.velocity.y;
            if(yVel < 0)
            {
                yVel = Mathf.Abs(yVel);
                float t = Mathf.Clamp01(Mathf.InverseLerp(increaseDownReachAtYY, maxDownReachAtVY, yVel));
                downwardReach = Mathf.Lerp(baseDownwardReach, maxDownReach, t);
            }
        }

        private void CalculateOrigins()
        {
            origin_near_ = playerTransform.position;
            origin_near_high_ = new Vector3(origin_near_.x, origin_near_.y + upwardsReach, origin_near_.z);
        }

        /// <summary>
        /// Generate overlap box in front of player
        /// </summary>
        private bool BroadFrontCheck()
        {
            float verticalOffset = (upwardsReach - downwardReach) / 2f;
            Vector3 origin = origin_near_
                + playerTransform.forward * (forwardReach / 2f)
                + playerTransform.up * verticalOffset;

            Vector3 size = new Vector3(sideReach, upwardsReach + downwardReach, forwardReach);

            Collider[] broad_hit_colliders = Physics.OverlapBox(origin, size / 2f, playerTransform.rotation, mantleableLayers);
            bool broad_hit = broad_hit_colliders.Length > 0;

            if (debug_broad_checks)
            {
                if(broad_hit) Drawing.DebugDrawBox(origin, size, playerTransform.forward, 0f, playerTransform.rotation, Color.green);
                else Drawing.DebugDrawBox(origin, size, playerTransform.forward, 0f, playerTransform.rotation, Color.red);
            }
            
            return broad_hit;
        }

        /// <summary>
        /// Generate overlap capsule around the player
        /// </summary>
        /// <returns></returns>
        private bool BroadRadiusCheck()
        {
            Vector3 topPoint = origin_near_ + new Vector3(0f, upwardsReach, 0f);
            Vector3 bottomPoint = origin_near_ + new Vector3(0f, -downwardReach, 0f);
            Collider[] broad_2_hit_colliders = Physics.OverlapCapsule(topPoint, bottomPoint, forwardReach, mantleableLayers);
            bool broad_2_hit = broad_2_hit_colliders.Length > 0;

            if (debug_broad_checks)
            {
                if (broad_2_hit) Drawing.DebugDrawCapsuleApprox(
                    bottomPoint, topPoint, forwardReach, playerTransform.rotation, new Color(0f, 1f, 0f, 0.4f));
                else Drawing.DebugDrawCapsuleApprox(
                    bottomPoint, topPoint, forwardReach, playerTransform.rotation, new Color(1f, 0f, 0f, 0.4f));
            }

            return broad_2_hit;
        }

        /// <summary>
        /// Create an arc of downward raycasts at a radius away from our main collider
        /// </summary>
        /// <returns></returns>
        private List<ValuedHitPoint> DownwardRaycastArc(float maxDistance, float angleStep)
        {
            float bodyColliderRadius = widestPlayerCollider.bounds.extents.x;
            float arcMax = Mathf.Asin(bodyColliderRadius / maxDistance) * Mathf.Rad2Deg * 2f;
            float rayDistance = upwardsReach + downwardReach;

            // We test the ground below the player, getting the position on the surface directly below the
            // player as well as the surface normal
            // if theres no surface in proximity, we just allow the process to continue

            bool surfaceBelowPlayerFound = false;
            Vector3 playerPointOnSurface = Vector3.zero;
            RaycastHit playerSurfaceHit;

            if (Physics.Raycast(playerFeet.position + Vector3.up * 0.135f,
                Vector3.down, out playerSurfaceHit,
                downwardReach + 0.54f,
                mantleableLayers)){

                surfaceBelowPlayerFound = true;
                playerPointOnSurface = playerSurfaceHit.point;

                if(debug_downwardRaycastArcs)
                {
                    Debug.DrawLine(playerPointOnSurface, playerFeet.position + Vector3.up * 0.135f, Color.yellow);
                }
                //playerPointOnSurface += Vector3.up * 0.02f;
            }

            List<ValuedHitPoint> arcRaycasts = new List<ValuedHitPoint>();
            for (float angle = -arcMax / 2f; angle <= arcMax / 2f; angle += angleStep)
            {
                // Calculate direction
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 direction = rotation * transform.forward;
                // Perform downward raycast
                Vector3 rayOrigin = origin_near_high_ + direction * maxDistance;
                RaycastHit hit;
                bool reject = false;

                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, mantleableLayers))
                {
                    // Draw green line to hit point
                    //reject if it hits an impossible slope 
                    if (GetSlopeAngleFromNormal(hit.normal) >= impossibleSlopeAngle) {
                        reject = true;
                    }


                    float verticality = Vector3.Dot(hit.normal, Vector3.up);

                    // If the surface is too vertical (i.e., surface normal is almost sideways)
                    if (!reject && Mathf.Abs(verticality) < 0.01f)
                    {
                        reject = true; // Surface is too vertical to walk along
                    }

                    // walk the surface towards the players feet.
                    // if ~0 difference (relative to surface) then player is on the same surface
                    if (!reject && surfaceBelowPlayerFound)
                    {

                        // define surface as a plane
                        Plane surfacePlane = new Plane(hit.normal, hit.point);
                        // cast a vertical ray from the players feet straight down
                        Ray verticalRay = new Ray(playerPointOnSurface + (Vector3.up * 0.01f), Vector3.down);

                        float tolerance = 0.15f;
                        float enter;
                        if (surfacePlane.Raycast(verticalRay, out enter))
                        {
                            Debug.DrawRay(playerPointOnSurface, Vector3.down, Color.magenta);

                            // get the exact surface point directly under/above the player's XZ
                            Vector3 walkedPoint = verticalRay.GetPoint(enter);

                            // compare its Y to the player’s surface point Y
                            float yDiff = Mathf.Abs(playerPointOnSurface.y - walkedPoint.y);

                            if (debug_downwardRaycastArcs)
                            {
                                Debug.DrawLine(hit.point, walkedPoint, Color.magenta);
                                Debug.DrawLine(hit.point, playerPointOnSurface, Color.yellow);
                            }

                            // reject if they're too close in height (same or similar surface)
                            if (yDiff < tolerance)
                            {
                                reject = true;
                            }
                        }
                      
                        // Alternatively and simply, if the hit is within 0.5 Y of our feet point, 
                        // its also invalid
                        if (Mathf.Abs(playerPointOnSurface.y - hit.point.y) < tolerance)
                        {
                            reject = true;
                        }
                    }

                    if (debug_downwardRaycastArcs)
                    {
                        Debug.DrawLine(rayOrigin, hit.point, reject ? Color.red : Color.green);
                    }
                }
                else
                {
                    // Draw red line to maximum distance
                    Vector3 endPoint = rayOrigin + Vector3.down * rayDistance;
                    //Debug.DrawLine(rayOrigin, endPoint, Color.red);
                }
                if(!reject) arcRaycasts.Add(new ValuedHitPoint(angle, rayOrigin, hit));
            }
            return arcRaycasts;
        }

        /// <summary>
        /// Scans list of ValuedHitPoints left to right, creating groups where there is a streak of hits
        /// </summary>
        /// <returns></returns>
        private List<HaitianGroup> HaitianGroupings(List<ValuedHitPoint> arcRaycasts)
        {
            List<HaitianGroup> haitian_groups = new List<HaitianGroup>();
            int reds = 0;
            HaitianGroup c_group = new HaitianGroup { hits = new List<ValuedHitPoint>() };
            foreach (ValuedHitPoint raycast in arcRaycasts)
            {
                if (raycast.hit.collider != null)
                {
                    if (c_group.hits.Count == 0)
                    {
                        // Start a new group on the first green
                        c_group = new HaitianGroup { hits = new List<ValuedHitPoint>() };
                    }
                    c_group.hits.Add(raycast);
                    reds = 0; // Reset reds since we got a green
                }
                else
                {
                    reds++;
                    if (reds == 2 && c_group.hits.Count > 0)
                    {
                        // End the current group after 2 consecutive reds
                        c_group.angleAverage = CalculateAverageValue(c_group.hits);
                        haitian_groups.Add(c_group);
                        c_group = new HaitianGroup { hits = new List<ValuedHitPoint>() };
                    }
                }
            }
            // Handle last group if it was still accumulating (wont add an empty group)
            if (c_group.hits.Count > 0)
            {
                c_group.angleAverage = CalculateAverageValue(c_group.hits);
                haitian_groups.Add(c_group);
            }
            return haitian_groups;
        }

        /// <summary>
        /// Uses Haitian groups to find vertical surface normals, the uses the average
        /// of those normals, as well as the average hit direction, weighting by the number of elements
        /// in the Haitian groups, to produce a virtual mantle direction
        /// </summary>
        private bool FindBestVirtualMantleDirection(List<HaitianGroup> haitianGroups, out Vector3 directionOutput)
        {
            Vector3 weightedSumDirection = Vector3.zero;
            int totalElements = 0;

            // Iterate through each Haitian group
            foreach (HaitianGroup group in haitianGroups)
            {
                // Debug draw the group as an arc
                List<Vector3> points = new List<Vector3>();
                foreach(ValuedHitPoint hit in group.hits)
                {
                    points.Add(hit.rayOrigin);
                }
                //Drawing.DrawLineFromPoints(points, Color.green);

                // Collect normals and hit points
                List<Vector3> haitian_normals = new List<Vector3>();
                List<Vector3> haitian_hits = new List<Vector3>();

                foreach (ValuedHitPoint hitPoint in group.hits)
                {
                    haitian_hits.Add(new Vector3(hitPoint.hit.point.x, hitPoint.hit.point.y, hitPoint.hit.point.z));

                    float recast_y = hitPoint.hit.point.y - 0.0135f;
                    Vector3 recastOrigin = new Vector3(origin_near_.x, recast_y, origin_near_.z);

                    Vector3 flatDirection = new Vector3(
                        hitPoint.hit.point.x - recastOrigin.x,
                        0f,
                        hitPoint.hit.point.z - recastOrigin.z
                    ).normalized;

                    RaycastHit recastHit;
                    float rayDistance = (forwardReach + forwardReach_addAllowance);
                    if (Physics.Raycast(recastOrigin, flatDirection, out recastHit, rayDistance, mantleableLayers))
                    {
                        //Debug.DrawLine(recastOrigin, recastHit.point, Color.green);
                        haitian_normals.Add(recastHit.normal);
                    }
                    else
                    {
                        //Debug.DrawLine(recastOrigin, recastOrigin + flatDirection * rayDistance, Color.red);
                    }
                }
                if (haitian_normals.Count == 0) continue;

                // Calculate average normal and hit direction for this group
                Vector3 averageNormalXZ = CalculateAverageDirectionXZ(haitian_normals);
                Vector3 averageNormalXZ_flipped = -averageNormalXZ;
                Vector3 averageHitPoint = CalculateAveragePosXZ(haitian_hits);
                Vector3 dirToAverageHitPoint = (averageHitPoint - new Vector3(origin_near_high_.x, 0f, origin_near_high_.z)).normalized;

                // Calculate the best direction for this group
                Vector3 groupDirection = (averageNormalXZ_flipped + dirToAverageHitPoint).normalized;

                // Weight the group direction by the number of elements
                int groupSize = group.hits.Count;
                weightedSumDirection += groupDirection * groupSize;
                totalElements += groupSize;
            }

            // If no valid groups, reject
            directionOutput = playerTransform.forward;
            if (totalElements == 0) return false;

            // Calculate the weighted average direction
            Vector3 bestNextTestDirection_fromGroups = weightedSumDirection.normalized;
            Vector3 forwardFlat = transform.forward;
            forwardFlat.y = 0f;
            forwardFlat.Normalize();
            Vector3 bestNextTestDirection = (bestNextTestDirection_fromGroups + forwardFlat).normalized;

            directionOutput = bestNextTestDirection;
            Debug.DrawLine(origin_near_high_, origin_near_high_ + bestNextTestDirection * forwardReach, Color.cyan);
            return true;
        }

        /// <summary>
        /// Casts rays along line of virtual mantle direction, offset by a scaled right vector.
        /// Finds the closest hit along the raycast series. Returns as the list of closest hits on the left and right side
        /// of the mantle direction.
        /// </summary>
        private void MongolianEdgeFinding(
            Vector3 virtualMantleDirection,
            float maxWidth,
            out List<ValuedPoint> leftHitPoints,
            out List<ValuedPoint> rightHitPoints)
        {
            float halfWidth = maxWidth / 2f;
            float maxDistance = forwardReach + forwardReach_addAllowance;

            // Dictionary to map offset to closest hit point
            SortedDictionary<float, ValuedPoint> leftWidthHitMap = new SortedDictionary<float, ValuedPoint>();
            // First, scan leftwards (negative offsets)
            int numWidthCasts = mongolianWidthIncrementsPerSide;
            float stepDistance = (halfWidth - 0.025f) / (numWidthCasts - 1);
            for (float offsetDistance = -0.025f; offsetDistance >= -halfWidth; offsetDistance -= stepDistance)
            {
                ScanWidth(
                    offsetDistance,
                    origin_near_,
                    origin_near_high_,
                    virtualMantleDirection,
                    0.025f,
                    maxDistance,
                    upwardsReach + downwardReach,
                    mongolianScanWidthIncrements,
                    mantleableLayers,
                    leftWidthHitMap);
            }
            SortedDictionary<float, ValuedPoint> rightWidthHitMap = new SortedDictionary<float, ValuedPoint>();
            //Secondly, scan right
            for (float offsetDistance = 0.025f; offsetDistance <= halfWidth; offsetDistance += stepDistance)
            {
                ScanWidth(
                    offsetDistance,
                    origin_near_,
                    origin_near_high_,
                    virtualMantleDirection, 
                    0.025f, 
                    maxDistance,
                    upwardsReach + downwardReach,
                    mongolianScanWidthIncrements,
                    mantleableLayers,
                    rightWidthHitMap);
            }

            // return as lists
            leftHitPoints = leftWidthHitMap.Values.ToList();
            rightHitPoints = rightWidthHitMap.Values.ToList();

            void ScanWidth(
                float offsetDistance,
                Vector3 origin_near,
                Vector3 origin_near_high,
                Vector3 mongolianDirection,
                float minDistance,
                float maxDistance,
                float downraycastDistance,
                int numCasts,
                LayerMask terrainLayer,
                SortedDictionary<float, ValuedPoint> widthHitMap)
            {
                Vector3 rightOffset = -Vector3.Cross(mongolianDirection, Vector3.up).normalized * offsetDistance;
                Vector3 offsetOrigin = origin_near + rightOffset;

                // Overlap Box
                Vector3 boxSize = new Vector3(0.05f, downraycastDistance, maxDistance);
                Vector3 boxCenter = offsetOrigin + mongolianDirection * (maxDistance / 2f);
                Quaternion boxRotation = Quaternion.LookRotation(mongolianDirection, Vector3.up);

                Collider[] overlaps = Physics.OverlapBox(boxCenter, boxSize / 2f, boxRotation, terrainLayer);
                Color boxColor = overlaps.Length > 0 ? new Color(0f, 1f, 0f, 0.2f) : new Color(1f, 0f, 0f, 0.2f);
                //Drawing.DebugDrawBox(boxCenter, boxSize, mongolianDirection, 0f, boxRotation, boxColor);

                if (overlaps.Length == 0)
                {
                    return; // No hit at this width
                }

                Vector3 shoulderPoint;
                if(offsetDistance > 0)
                {
                    shoulderPoint = playerTransform.position + (playerTransform.right * shoulderDistance / 2f); 
                }
                else
                {
                    shoulderPoint = playerTransform.position + (-playerTransform.right * shoulderDistance / 2f);
                }

                // Raycast search at this width
                float stepDistance = (maxDistance - minDistance) / (numCasts - 1);
                float closestHitDistance = float.MaxValue;
                Vector3 closestHitPoint = Vector3.zero;
                float closestHitNormalAngle = 0f;
                bool hitFound = false;

                // This says how far a new best grip point is allowed to be considered.
                // This will scale downwards with how high above origin_near it is
                // And also scale downwards when the the y velocity is super low to where we'd fall to grab
                //  the ledge.
                int maxStepsFromClosest = 3;
                float baseAllowedDistanceFromClosest = stepDistance * maxStepsFromClosest;

                SortedDictionary<float, RaycastHit> distanceHitMap = new SortedDictionary<float, RaycastHit>();

                for (int i = 0; i < numCasts; i++)
                {
                    float distance = minDistance + stepDistance * i;
                    Vector3 rayOrigin = origin_near_high + rightOffset + mongolianDirection * distance;

                    // Stop further casting if we go some distance further than our closest point.
                    if (distance - closestHitDistance > baseAllowedDistanceFromClosest)
                    {
                        break;
                    }

                    // Check if inside any geometry. If we are, we need to break out from further casts..
                    if (Physics.CheckSphere(rayOrigin, 0.01f, terrainLayer, QueryTriggerInteraction.Ignore))
                    {
                        Debug.DrawLine(rayOrigin, rayOrigin + Vector3.up * 0.5f, new Color(1f, 0.5f, 0f, 0.5f));
                        break;
                    }

                    RaycastHit hit;

                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, downraycastDistance, terrainLayer))
                    {
                        // reject the hit if it hit too steep a plane
                        float surfaceAngle = GetSlopeAngleFromNormal(hit.normal);
                        bool reject = false;
                        if (surfaceAngle > 70) reject = true;

                        // reject the hit if its not in range
                        //if (Vector3.Distance(hit.point, shoulderPoint) > maxDistance) reject = true;


                        if (!reject) Debug.DrawLine(rayOrigin, hit.point, new Color(0f, 1f, 0f, 0.2f));
                        //else Debug.DrawLine(rayOrigin, hit.point, new Color(1f, 0f, 0f, 0.2f));

                        if (reject) continue;

                        hitFound = true;
                        distanceHitMap.Add(distance, hit);

                        if (distance < closestHitDistance)
                        {
                            closestHitDistance = distance;
                            closestHitPoint = hit.point;
                            closestHitNormalAngle = GetSlopeAngleFromNormal_RelativeToDirection(hit.normal, mongolianDirection);
                        }
                    }
                    else
                    {
                        Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * downraycastDistance, new Color(1f, 0f, 0f, 0.2f));
                    }
                }

                if (!hitFound) return;

                // max distance should depend on distance to close hit point
                float maxReversalSearchDistance = maxDistance;

                bool reversalFound = false;

                Vector3 bestPoint = closestHitPoint;
                float bestPointDistance = float.MaxValue;
                float bestPointSlope = float.MaxValue;

                Vector3 reversalPoint = Vector3.zero;
                float reversalPointDistance = float.MaxValue;

                int j = 0;

                float origin_near_to_high_y_dist = origin_near_high.y - origin_near.y;
                float yThresholdForDecreasingAllowedDistance = 0f;
                float highestY = closestHitPoint.y;

                //Debug.Log("for raycast at width: " + offsetDistance);
                foreach (KeyValuePair<float, RaycastHit> pair in distanceHitMap)
                {
                    bool isLast = j == distanceHitMap.Count - 1;
                    float distance = pair.Key;
                    RaycastHit hit = pair.Value;


                    // Calculate how far we are allowed to consider a grip point based off of Y
                    float yOffset = highestY - origin_near.y;
                    float maxYOffset = origin_near_to_high_y_dist;
                    float threshold = yThresholdForDecreasingAllowedDistance;
                    float t = 0f;
                    if (yOffset > threshold)
                    {
                        yOffset = Mathf.Clamp(yOffset, 0f, maxYOffset);
                        float heightAboveThreshold = yOffset - threshold;
                        float interpRange = maxYOffset - threshold;
                        // Prevent divide by zero
                        if (interpRange > 0f)
                            t = heightAboveThreshold / interpRange;
                        else
                            t = 1f;
                    }
                    float allowedDistanceFromClosest = Mathf.Lerp(baseAllowedDistanceFromClosest, 0f, t);

                    //allowedDistanceFromClosest = baseAllowedDistanceFromClosest;

                    //if highest point higher than origin_near, then proceeding
                    // points must be at least Y distance higher than the highest point.
                    bool willSkip = false;
                    if (highestY > origin_near.y)
                    {
                        willSkip = hit.point.y - highestY < 1f;
                    }
                    if (hit.point.y > highestY)
                    {
                        highestY = hit.point.y;
                    }
                    if (willSkip) continue;


                    //Debug.Log("yOffset: " + yOffset + " t: " + t + " Allowed D from closest: " +  allowedDistanceFromClosest);
                    //Debug.Log("this Dist from closest: " + (distance - closestHitDistance));

                    // Check Normal of slope hit relative to player
                    float slopeAngle = GetSlopeAngleFromNormal_RelativeToDirection(hit.normal, mongolianDirection);
                    //Debug.Log("reversal code - slope angle: " + slopeAngle);

                    //Debug.Log("BestPointSlope: " + bestPointSlope + " this Slope: " + slopeAngle);

                    // we should really only consider grip points that are higher than our current

                    // if slope becomes OPTIMAL, thats our reversal
                    if (slopeAngle < 20f 
                        && slopeAngle > -60f
                        && !reversalFound
                        && distance - closestHitDistance < (allowedDistanceFromClosest) + 0.025f)
                    {

                        //TODO: reversal point should be raised to match the height of the previous point
                        reversalFound = true;
                        reversalPoint = hit.point;
                        reversalPointDistance = distance;
                    }

                    // if slope valid and less than our best one, set it to best one
                    if (slopeAngle < impossibleSlopeAngle 
                        && slopeAngle < bestPointSlope
                        && distance - closestHitDistance < (allowedDistanceFromClosest) + 0.025f)
                    {
                        bestPoint = hit.point;
                        bestPointDistance = distance;
                        bestPointSlope = slopeAngle;
                    }

                    // if higher  and not too far off from closest, set to best
                    if (slopeAngle < impossibleSlopeAngle
                        && hit.point.y - bestPoint.y > 2f
                        && distance - closestHitDistance < (allowedDistanceFromClosest) + 0.025f)
                    {
                        bestPoint = hit.point;
                        bestPointDistance = distance;
                        bestPointSlope = slopeAngle;
                    }

/*                    if (isLast)
                    {
                        // if last hit point slope same as closest, use closest
                        // they cant be the same hit
                        float closestSlopeAngle = closestHitNormalAngle;
                        if (
                            distance != closestHitDistance &&
                            Mathf.Abs(closestSlopeAngle - slopeAngle) < 5f)
                        {
                            //reversalFound = true;
                            bestPoint = closestHitPoint;
                            bestPointDistance = closestHitDistance;
                        }
                    }*/

                    j++;
                }

                // if reversal closer than best, swap around

                Vector3 maxPoint = reversalPoint;
                float maxPointDist = reversalPointDistance;
                Vector3 midPoint = bestPoint;
                float midPointDist = bestPointDistance;

                if(reversalPointDistance < bestPointDistance)
                {
                    maxPoint = bestPoint;
                    maxPointDist = bestPointDistance;
                    midPoint = reversalPoint;
                    midPointDist = reversalPointDistance;
                }

                Vector3 selectedPoint = maxPoint;
                float selectedPointDist = maxPointDist;

                float interpointAllowance = stepDistance * 3f;

                // check distance closest -> reversal, if too large:
                // check distance closest -> best, if too large:
                // use closest
                //Debug.Log("interpointAllowance: " + interpointAllowance);
                //Debug.Log("maxPoint_D: " + maxPointDist + " midPoint_D: " + midPointDist + " clPoint_D: " + closestHitDistance);
                if (maxPointDist == float.MaxValue || maxPointDist - closestHitDistance > interpointAllowance)
                {
                    //Debug.Log("Reversal too far or non existant");
                    selectedPoint = midPoint;
                    selectedPointDist = midPointDist;
                }
                if(midPointDist == float.MaxValue || midPointDist -  closestHitDistance > interpointAllowance)
                {
                    //Debug.Log("bestpoint too far or non existant");
                    selectedPoint = closestHitPoint;
                    selectedPointDist = closestHitDistance;
                }


                // Find edge
                RaycastHit hit_;
                if (Physics.Raycast(selectedPoint + Vector3.up * 0.0135f, Vector3.down, out hit_, downraycastDistance, terrainLayer))
                {
                    Vector3 hitNormal = hit_.normal;
                    float slopeAngle = GetSlopeAngleFromNormal_RelativeToDirection(hitNormal, mongolianDirection);
                    float recastDistance = stepDistance * 0.35f;

                    Vector3 slopeDirection = Vector3.Cross(Vector3.Cross(hitNormal, Vector3.down), hitNormal);
                    slopeDirection.Normalize();
                    Vector3 recastDir = Vector3.ProjectOnPlane(mongolianDirection, hitNormal).normalized;
                    Vector3 selectedPointRecastYDiff = (-Vector3.up * 0.0135f);
                    Vector3 recastPos = selectedPoint + selectedPointRecastYDiff - (recastDir * recastDistance);
                    Ray recastRay = new Ray(recastPos, recastDir);
                    RaycastHit[] recastHits = Physics.RaycastAll(recastRay, recastDistance, terrainLayer);

                    if (recastHits.Length > 0)
                    {
                        RaycastHit? lastMatchingHit = null;

                        foreach (var hit in recastHits)
                        {
                            if (hit.collider == hit_.collider)
                            {
                                lastMatchingHit = hit;
                            }
                        }

                        if (lastMatchingHit.HasValue)
                        {


                            Vector3 recastHit = lastMatchingHit.Value.point;

                            Debug.DrawLine(recastPos, recastHit, new Color(1f, 1f, 0f, 0.2f));

                            Vector3 dirToHit = (recastHit - (selectedPoint + selectedPointRecastYDiff));
                            Vector3 dToHit = dirToHit + (dirToHit.normalized * -0.0135f);
                            recastHit = selectedPoint + dToHit;

                            Debug.DrawLine(selectedPoint, recastHit, new Color(1f, 1f, 0f, 0.2f ));
                            // find change in delta according to mongolian direction

                            selectedPoint = recastHit;
                            //selectedPointDist = recastPointDepth;
                        }
                    }
                }


                widthHitMap.Add(offsetDistance, new ValuedPoint(selectedPointDist, selectedPoint));

                if(!reversalFound) Debug.DrawLine(selectedPoint, selectedPoint + Vector3.up * 1f, new Color(1f, 0f, 1f, 0.4f));
                else Debug.DrawLine(selectedPoint, selectedPoint + Vector3.up * 1f, new Color(1f, 1f, 0f, 0.4f));



            }
        }

        private void ProcessPointsIntoEdges(
            List<ValuedPoint> leftHitPoints,
            List<ValuedPoint> rightHitPoints,
            out List<Edge> foundEdges)
        {
            List<ValuedPoint> allPoints = new List<ValuedPoint>();
            allPoints.AddRange(leftHitPoints);
            allPoints.AddRange(rightHitPoints);

            List<Edge> edges = new List<Edge>();
            foundEdges = edges;

            if (allPoints.Count < 2) return;
            edges = new List<Edge>();

            // STEP 1: Build surface normals map
            List<Vector3> surfaceNormals = new List<Vector3>();

            foreach (var vp in allPoints)
            {
                Vector3 origin = vp.point + Vector3.up * 0.0135f;
                Ray ray = new Ray(origin, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 0.025f))
                {
                    surfaceNormals.Add(hitInfo.normal);
                }
                else
                {
                    surfaceNormals.Add(Vector3.up); // fallback normal if not hit
                }
            }

            // STEP 2: Edge creation
            // Start with first point
            // Begin with a degenerate edge starting at the first point
            Edge currentEdge = new Edge
            {
                startPoint = allPoints[0].point,
                endPoint = allPoints[0].point,
                generalDirection = Vector3.zero,
                midPoint = allPoints[0].point,
                debugCol = new Color(1f, 1f, 0f, 0.8f)
            };

            for (int i = 1; i < allPoints.Count; i++)
            {
                Vector3 candidate = allPoints[i].point;
                bool isSinglePointEdge = currentEdge.startPoint == currentEdge.endPoint;

                if (isSinglePointEdge)
                {
                    Vector3 referencePoint = currentEdge.startPoint;
                    Vector3 referenceNormal = surfaceNormals[i - 1]; // currentEdge point
                    Vector3 candidateNormal = surfaceNormals[i];

                    float normalAngle = Vector3.Angle(referenceNormal, candidateNormal);
                    float pointToPlaneDist = Mathf.Abs(Vector3.Dot(candidate - referencePoint, referenceNormal));
                    float planarDistance = Vector3.ProjectOnPlane(candidate - referencePoint, referenceNormal).magnitude;

                    bool isSameSurface =
                        normalAngle < 15f &&
                        pointToPlaneDist < 0.0135f &&  // candidate lies close to the plane
                        planarDistance < 2f;         // not too far along surface

                    if (isSameSurface)
                    {
                        currentEdge.endPoint = candidate;
                        currentEdge.generalDirection = (candidate - currentEdge.startPoint).normalized;
                        currentEdge.midPoint = Vector3.Lerp(currentEdge.startPoint, currentEdge.endPoint, 0.5f);
                        continue;
                    }
                }
                else
                {
                    Vector3 stepDir = (candidate - currentEdge.endPoint).normalized;
                    float angle = Vector3.Angle(currentEdge.generalDirection, stepDir);

                    if (angle <= 10f)
                    {
                        currentEdge.endPoint = candidate;
                        currentEdge.generalDirection = (currentEdge.endPoint - currentEdge.startPoint).normalized;
                        currentEdge.midPoint = Vector3.Lerp(currentEdge.startPoint, currentEdge.endPoint, 0.5f);
                        continue;
                    }
                }

                // Finalize and start new edge
                edges.Add(currentEdge);
                currentEdge = new Edge
                {
                    startPoint = candidate,
                    endPoint = candidate,
                    generalDirection = Vector3.zero,
                    midPoint = candidate,
                    debugCol = new Color(1f, 1f, 0f, 0.8f) // Your preferred color
                };
            }

            // Finalize last edge
            edges.Add(currentEdge);

            // Remove edges that are just 1 point
            int j = 0;
            foreach(Edge edge in edges)
            {
                if(Vector3.Distance(edge.startPoint, edge.endPoint) > 0.0135f)
                {
                    foundEdges.Add(new Edge
                    {
                        startPoint = edge.startPoint,
                        endPoint = edge.endPoint,
                        generalDirection = edge.generalDirection,
                        midPoint = edge.midPoint,
                        debugCol = edge.debugCol,
                        edgeNum = j
                    });
                    j++;
                }
            }

            // Debug draw
            foreach (var edge in foundEdges)
            {
                //Debug.DrawLine(edge.startPoint, edge.endPoint, edge.debugCol);
            }
        }

        private bool FindBestHandPoints_2(
            List<Edge> allEdges, 
            Vector3 heading,
            out Vector3 leftHandOutput, 
            out Vector3 rightHandOutput)
        {
            leftHandOutput = Vector3.zero;
            rightHandOutput = Vector3.zero;

            Vector3.Normalize(heading);
            Vector3 playerPos = playerTransform.position;
            float colliderWidth = widestPlayerCollider.bounds.extents.x * 2f;
            Vector3 heading_flat = FlattenDirection(heading);
            Vector3 heading_flat_right = Vector3.Cross(Vector3.up, heading_flat);

            Vector3 left_line_origin = origin_near_high_ - heading_flat_right * (colliderWidth / 2f);
            Vector3 right_line_origin = origin_near_high_ + heading_flat_right * (colliderWidth / 2f);

            foreach(Edge edge in allEdges)
            {
                Debug.DrawLine(edge.startPoint, edge.endPoint, new Color(1f, 0f, 0f, 0.4f));
            }


            Debug.DrawLine(origin_near_high_, origin_near_high_ + heading * 6f, new Color(1f, 0.5f, 0f, 1f));
            Debug.DrawLine(left_line_origin, left_line_origin + heading * 6f, new Color(1f, 0.5f, 0f, 1f));
            Debug.DrawLine(right_line_origin, right_line_origin + heading * 6f, new Color(1f, 0.5f, 0f, 1f));

            // Step 1: Find Edges in-between corridor in heading direction, of collider width
            List<Edge> validEdges = new List<Edge>();

            float Cross2D(Vector3 a, Vector3 b) => Vector3.Cross(a, b).y;

            int SideOfLine(Vector3 point, Vector3 lineOrigin, Vector3 lineDir)
            {
                Vector3 toPoint = Flatten(point) - Flatten(lineOrigin);
                float cross = Cross2D(lineDir, toPoint);
                if (cross > 0f) return 1; // Left Side
                if (cross < 0f) return -1; // Right Side
                return 0;
            }

            bool IsBetweenParallelLines(Vector3 point, Vector3 left_line_origin, Vector3 right_line_origin, Vector3 heading)
            {
                left_line_origin = Flatten(left_line_origin);
                right_line_origin = Flatten(right_line_origin);
                int sideToLeft = SideOfLine(point, left_line_origin, heading);
                int sideToRight = SideOfLine(point, right_line_origin, heading);
                return sideToLeft != 0 && sideToRight != 0 && sideToLeft != sideToRight;
            }

            bool IsEdgeInsideCorridor(Edge edge)
            {
                Vector3 sp = Flatten(edge.startPoint);
                Vector3 mp = Flatten(edge.midPoint);
                Vector3 ep = Flatten(edge.endPoint);
                return IsBetweenParallelLines(sp, left_line_origin, right_line_origin, heading) ||
                       IsBetweenParallelLines(mp, left_line_origin, right_line_origin, heading) ||
                       IsBetweenParallelLines(ep, left_line_origin, right_line_origin, heading);
            }

            foreach(Edge edge in allEdges)
            {
                if (!IsEdgeInsideCorridor(edge))
                {
                    Debug.DrawLine(edge.startPoint, edge.endPoint, Color.red);
                }
                else
                {
                    validEdges.Add(edge);
                }
            }

            if (validEdges.Count == 0) return false;

            // Find highest edge
            Edge highestEdge = validEdges[0];
            float highestEdgePointY = int.MinValue;
            foreach(Edge edge in validEdges)
            {
                if(edge.midPoint.y > highestEdgePointY)
                {
                    highestEdge = edge;
                    highestEdgePointY = edge.midPoint.y;
                }
            }

            // Keep edges that fall within Y distance of highest edge
            // Instead of binning off semi-passing edges, cut them off at Y limit.
            List<Edge> validEdgesCopy = new List<Edge>(validEdges);
            validEdges.Clear();
            float yTolerance = 0.3f;
            float minAllowedY = highestEdgePointY - yTolerance;

            foreach(Edge edge in validEdgesCopy)
            {
                Vector3 start = edge.startPoint;
                Vector3 end = edge.endPoint;

                bool startInTolerance = start.y >= minAllowedY;
                bool endInTolerance = end.y >= minAllowedY;

                if(startInTolerance && endInTolerance)
                {
                    validEdges.Add(edge); continue;
                }
                else if (!startInTolerance && !endInTolerance) continue;

                else
                {
                    // One point below tolerance => trim the lower point
                    Vector3 edgeDir = (end - start).normalized;
                    float edgeLength = (end - start).magnitude;

                    // Find the required vertical difference
                    float requiredDeltaY = minAllowedY - (startInTolerance ? end.y : start.y);

                    // Find t on edge such that point.y == minAllowedY
                    float verticalDelta = (end - start).y;
                    float t = requiredDeltaY / verticalDelta;

                    t = Mathf.Clamp01(t);

                    Vector3 newPoint = startInTolerance
                        ? Vector3.Lerp(end, start, t) // move endpoint towards start
                        : Vector3.Lerp(start, end, t); // move startpoint toward end
                    
                    Edge trimmedEdge = new Edge();
                    if (startInTolerance)
                    {
                        trimmedEdge.startPoint = edge.startPoint;
                        trimmedEdge.endPoint = newPoint;
                    }
                    else
                    {
                        trimmedEdge.startPoint = newPoint;
                        trimmedEdge.endPoint = edge.endPoint;
                    }
                    // copy over other fields
                    trimmedEdge.edgeNum = edge.edgeNum;
                    trimmedEdge.midPoint = (trimmedEdge.endPoint + trimmedEdge.startPoint) * 0.5f;
                    trimmedEdge.generalDirection = (trimmedEdge.endPoint - trimmedEdge.startPoint).normalized;
                    validEdges.Add(trimmedEdge);
                }
            }

            foreach(Edge edge in validEdges)
            {
                Debug.DrawLine(edge.startPoint, edge.endPoint, new Color(1f, 1f, 0f));
            }

            Debug.DrawLine(highestEdge.startPoint, highestEdge.endPoint, Color.green);


            bool EdgeIntersectsCorridorLine(Edge edge, Vector3 line_origin, Vector3 line_heading)
            {
                Vector3 sp = Flatten(edge.startPoint);
                Vector3 ep = Flatten(edge.endPoint);
                line_origin = Flatten(line_origin); 
                int sp_side = SideOfLine(sp, line_origin, line_heading);
                int ep_side = SideOfLine(ep, line_origin, line_heading);
                return sp_side != 0 && ep_side != 0 && sp_side != ep_side;
            }

            Vector3 SlideAlongEdge(Edge edge, Vector3 startPointOnEdge, 
                Vector3 slideDir, float slideDist,
                Vector3 limitDir, float maxDistInLimitDir, Vector3 limitAnchorPoint,
                out float distTravelled,
                out bool endReached)
            {
                distTravelled = 0f;
                endReached = false;
                float edgeLength = (edge.endPoint - edge.startPoint).magnitude;
                if (edgeLength < 0.0001f)return startPointOnEdge; // degenerate edge
                Vector3 flat_dir = Flatten(slideDir).normalized;
                Vector3 flatLimitDir = Flatten(limitDir).normalized;
                Vector3 edgeDirFlat = Flatten(edge.generalDirection).normalized;
                // project edge direction onto flat direction
                float projection = Vector3.Dot(edgeDirFlat, flat_dir);
                if (Mathf.Abs(projection) < 0.0001f)return startPointOnEdge; // cant slide, lines perpendicular
                //calculate actual 3D distance to move along the edge
                float edgeTravelDistance = slideDist / projection;
                
                // clamp to stay within the edge bounds
                float dFromStart = Vector3.Dot(startPointOnEdge - edge.startPoint, edge.generalDirection);
                float maxTravelForward = edgeLength - dFromStart;
                float maxTravelBackward = -dFromStart;

                edgeTravelDistance = Mathf.Clamp(edgeTravelDistance, maxTravelBackward, maxTravelForward);



                // Get the tentative moved point
                Vector3 candidatePoint = startPointOnEdge + edge.generalDirection * edgeTravelDistance;

                // Check how far this candidate point has moved in the limit direction
                Vector3 flatDelta = Flatten(candidatePoint - limitAnchorPoint);
                float projectedDeltaInLimitDir = Vector3.Dot(flatDelta, flatLimitDir);

                // Clamp if it exceeds allowed distance in the limiting direction
                if (Mathf.Abs(projectedDeltaInLimitDir) > maxDistInLimitDir)
                {
                    // Clamp projected distance
                    float clampedProj = Mathf.Sign(projectedDeltaInLimitDir) * maxDistInLimitDir;

                    // We need to find how far along the edge we'd travel to get this amount of movement in limitDir
                    float limitProjection = Vector3.Dot(edgeDirFlat, flatLimitDir);
                    if (Mathf.Abs(limitProjection) < 0.0001f) return startPointOnEdge; // Can't move in limitDir at all

                    float clampedEdgeTravel = clampedProj / limitProjection;

                    // Apply final clamp based on edge bounds
                    clampedEdgeTravel = Mathf.Clamp(clampedEdgeTravel, maxTravelBackward, maxTravelForward);

                    candidatePoint = startPointOnEdge + edge.generalDirection * clampedEdgeTravel;
                }
                distTravelled = Mathf.Abs(Vector3.Dot(candidatePoint - startPointOnEdge, Flatten(slideDir).normalized));
                // Check if we've reached either end of the edge
                const float epsilon = 0.001f;

                if ((candidatePoint - edge.startPoint).sqrMagnitude < epsilon * epsilon ||
                    (candidatePoint - edge.endPoint).sqrMagnitude < epsilon * epsilon)
                {
                    endReached = true;
                }

                return candidatePoint;
            }

            bool TryGetEdgeLineIntersection(Edge edge, Vector3 line_origin, Vector3 line_dir, out Vector3 intersection)
            {
                Vector3 edgeVec_flat = Flatten(edge.endPoint - edge.startPoint);
                Vector3 lineDir_flat = Flatten(line_dir);
                Vector3 edge_s_flat = Flatten(edge.startPoint);
                Vector3 edge_e_flat = Flatten(edge.endPoint);
                line_origin = Flatten(line_origin);
                line_dir = Vector3.Normalize(Flatten(line_dir));

                float cross = line_dir.x * edgeVec_flat.z - line_dir.z * edgeVec_flat.x;
                if(Mathf.Abs(cross) < 0.0001f)
                {
                    intersection = Vector3.zero;
                    return false;
                }

                // Vector from line origin to edge start
                Vector3 qMinusP = edge_s_flat - line_origin;
                // solve for t: scalar along line direction to get to intersection
                float t = (qMinusP.x * edgeVec_flat.z - qMinusP.z * edgeVec_flat.x) / cross;
                // solve for u: scalar along edge direction (0= edge start, 1 = edge end)
                float u = (qMinusP.x * line_dir.z - qMinusP.z * line_dir.x) / cross;
                // If u is outside [0,1], the intersection point is not on the edge segment
                if (u < 0f || u > 1f)
                {
                    intersection = Vector3.zero;
                    return false;
                }
                // Compute full 3D intersection point on the infinite line
                Vector3 pointOnLine = line_origin + line_dir * t;

                // Interpolate the Y value using u along the original 3D edge
                float y = Mathf.Lerp(edge.startPoint.y, edge.endPoint.y, u);

                // Final intersection point, with accurate Y
                intersection = new Vector3(pointOnLine.x, y, pointOnLine.z);
                return true;
            }

            //Debug.Log("Edge Count: " + validEdges.Count);

            // Step 2: Case for when only 1 valid edge found 

            float optHandFromCenterDist = shoulderDistance / 2f;
            if(validEdges.Count == 1)
            {
                Edge edge = validEdges[0];
                bool intersectsLeftLine = EdgeIntersectsCorridorLine(edge, left_line_origin, heading);
                bool intersectsRightLine = EdgeIntersectsCorridorLine(edge, right_line_origin, heading);
                bool intersectsCenterLine = EdgeIntersectsCorridorLine(edge, Flatten(origin_near_), heading);

                Vector3 leftHandPoint = edge.startPoint;
                Vector3 rightHandPoint = edge.endPoint;

                // We take a point that will stay stable, and points will slide to adjust around it
                Vector3 stablePoint = (leftHandPoint + rightHandPoint) / 2f;

                if (intersectsLeftLine && intersectsRightLine)
                {
                    TryGetEdgeLineIntersection(edge, origin_near_high_, heading_flat, out stablePoint);
                }

                else if (intersectsLeftLine && !intersectsRightLine)
                {
                    if(!TryGetEdgeLineIntersection(edge, left_line_origin, heading_flat, out stablePoint))
                    {
                        stablePoint = edge.endPoint;
                    }
                }

                else if (!intersectsLeftLine && intersectsRightLine)
                {
                    if(!TryGetEdgeLineIntersection(edge, right_line_origin, heading_flat, out stablePoint))
                    {
                        stablePoint = edge.startPoint;
                    }
                }

                if (intersectsCenterLine)
                {
                    TryGetEdgeLineIntersection(edge, origin_near_high_, heading_flat, out stablePoint);
                }

                //float edgeMultiplier = 1f - Mathf.Abs(Vector3.Dot(heading_flat.normalized, Flatten(edge.generalDirection).normalized));

                float edgeMultiplier_left = CalculateEdgeMultiplier(edge, -heading_flat_right, heading_flat);
                float edgeMultipler_right = CalculateEdgeMultiplier(edge, heading_flat_right, heading_flat);

                float stablePoint_offcenter = Vector3.Dot(Flatten(stablePoint) - Flatten(origin_near_), heading_flat_right);
                leftHandPoint = SlideAlongEdge(edge, stablePoint, -heading_flat_right,
                   (optHandFromCenterDist + stablePoint_offcenter) * edgeMultiplier_left, heading_flat, 1.5f, stablePoint,
                   out _, out _);
                rightHandPoint = SlideAlongEdge(edge, stablePoint, heading_flat_right,
                   (optHandFromCenterDist - stablePoint_offcenter) * edgeMultipler_right, heading_flat, 1.5f, stablePoint,
                   out _, out _);

                Drawing.DrawCrossOnXZPlane(leftHandPoint, 0.1f, Color.cyan);
                Drawing.DrawCrossOnXZPlane(rightHandPoint, 0.1f, Color.magenta);
                Drawing.DrawCrossOnXZPlane(stablePoint, 0.05f, Color.yellow);

                leftHandOutput = leftHandPoint;
                rightHandOutput = rightHandPoint;

            }

            bool GetAdjacentEdge(List<Edge> edges, Edge startEdge, int direction, out Edge adjacentEdge)
            {
                adjacentEdge = startEdge;
                Edge lowestAbove = startEdge;
                Edge highestBelow = startEdge;
                int lowestAbove_edgeNum = int.MaxValue;
                int highestBelow_edgeNum = int.MinValue;
                foreach(Edge edge in edges)
                {
                    if (edge.edgeNum == startEdge.edgeNum) continue;
                    if (edge.edgeNum < startEdge.edgeNum && edge.edgeNum > highestBelow_edgeNum)
                    {
                        highestBelow_edgeNum = edge.edgeNum;
                        highestBelow = edge;
                    }
                    if (edge.edgeNum > startEdge.edgeNum && edge.edgeNum < lowestAbove_edgeNum)
                    {
                        lowestAbove_edgeNum = edge.edgeNum;
                        lowestAbove = edge;
                    }
                }

                if(direction < 0)
                {
                    if (highestBelow.edgeNum != startEdge.edgeNum)
                    {
                        adjacentEdge = highestBelow;
                        return true;
                    }
                }
                if(direction >= 0)
                {
                    if(lowestAbove.edgeNum != startEdge.edgeNum)
                    {
                        adjacentEdge = lowestAbove;
                        return true;
                    }
                }

                return false;
            }

            Vector3 GetClosestEdgeEnd(Edge edge, Vector3 fromPoint, Vector3 distDirection, out float dist)
            {
                Vector3 startPoint_flat = Flatten(edge.startPoint);
                Vector3 endPoint_flat = Flatten(edge.endPoint);
                Vector3 fromPoint_flat = Flatten(fromPoint);
                Vector3 distDirection_flat = Flatten(distDirection).normalized;

                float sp_d = Mathf.Abs(Vector3.Dot(startPoint_flat - fromPoint_flat, distDirection_flat));
                float ep_d = Mathf.Abs(Vector3.Dot(endPoint_flat - fromPoint_flat, distDirection_flat));

               /* Debug.Log("GetClosestEdgeEnd: " +
                    "D to StartPoint: " + sp_d
                    + " D to EndPoint: " + ep_d);*/

                if (sp_d < ep_d)
                {
                    dist = sp_d;
                    return edge.startPoint;
                }
                dist = ep_d;
                return edge.endPoint;
            }

            float CalculateEdgeMultiplier(
                Edge edge,
                Vector3 slideDir, Vector3 limitDir
                )
            {
                Vector3 edgeFlatDir = Flatten(edge.endPoint - edge.startPoint).normalized;
                Vector3 slideFlatDir = Flatten(slideDir).normalized;
                Vector3 limitFlatDir = Flatten(limitDir).normalized;

                // Determine travel direction along edge: could be forward or backward
                float slideProjection = Vector3.Dot(slideFlatDir, edgeFlatDir);
                Vector3 slideEdgeDir = slideProjection >= 0f ? edgeFlatDir : -edgeFlatDir;

                // Check if edge travel goes against the limit direction (i.e. toward the player)
                float travelVsLimitDot = Vector3.Dot(slideEdgeDir, limitFlatDir);

                // How aligned is this edge direction with the heading (limitDir)?
                float alignmentDot = Mathf.Abs(Vector3.Dot(limitFlatDir, slideEdgeDir));

                float edgeMultiplier;

                if (travelVsLimitDot < 0f)
                {
                    edgeMultiplier = 1f - alignmentDot;
                    edgeMultiplier = Mathf.Clamp(edgeMultiplier, 0.3f, 1f);
                }
                else
                {
                    // Away from player: apply full penalty
                    edgeMultiplier = 1f - alignmentDot;
                }

                return Mathf.Clamp01(edgeMultiplier);
            }

            Vector3 RecursiveSlideAlongEdge(
                List<Edge> edges, Edge startEdge, Vector3 startPoint, int dir,
                float slideDist, Vector3 slideDir,
                float limitDist, Vector3 limitDir
                )
            {
                int iterMax = edges.Count;
                int iter = 0;

                dir = Mathf.Clamp(dir, -1, 1);
                slideDir = Flatten(slideDir).normalized;
                limitDir = Flatten(limitDir).normalized;
                Vector3 outputPoint = startPoint;
                Edge currentEdge = startEdge;
                Vector3 slideStartPoint = startPoint;
                float remainingSlideDist = slideDist;
                bool canSlideFurther = true;

                while (canSlideFurther)
                {
                    if (iter > iterMax) break; //failsafe

                    //Debug.Log("Sliding on Edge: " + currentEdge.edgeNum + " Dir: " + dir);

                    float edgeMultiplier = CalculateEdgeMultiplier(
                        currentEdge, slideDir, limitDir);

                    //Debug.Log("Edge Multiplier: " + edgeMultiplier);

                    float distSlid = 0f; 
                    bool endReached = false;

                    outputPoint = SlideAlongEdge(
                        currentEdge, slideStartPoint, slideDir,
                        remainingSlideDist * edgeMultiplier, limitDir, limitDist, startPoint,
                        out distSlid, out endReached);

                    float deltaInLimitDir = Mathf.Abs(Vector3.Dot(
                        Flatten(outputPoint) - Flatten(startPoint), limitDir));
                    remainingSlideDist -= distSlid;

                    // We can recurse to slide onto adjacent edge if:
                    // -we have slide distance remaining
                    // -we arent too far in heading direction from the stablepoint

                    canSlideFurther =
                        remainingSlideDist > 0 &&
                        deltaInLimitDir < limitDist;

                    if (!canSlideFurther) break;

                    if (!GetAdjacentEdge(validEdges, currentEdge, dir, out currentEdge))
                    {
                        break;
                    }

                    // When we jump to the next edge, we check the distance to the point we would be starting
                    // at on that and remove that distance from our remaining slide distance.
                    // if this causes remaining slide distance to fall below 0, we stop the recursive slide.
                    float dToNextEdgeStartPoint = 0f;
                    slideStartPoint = GetClosestEdgeEnd(currentEdge, outputPoint, slideDir, out dToNextEdgeStartPoint);
                    remainingSlideDist -= dToNextEdgeStartPoint;
                    deltaInLimitDir = Mathf.Abs(Vector3.Dot(
                        Flatten(startPoint - slideStartPoint), limitDir));

                    //Debug.Log("Distance to next edge start:  " + dToNextEdgeStartPoint);
                    //Debug.Log("Delta in Limit Dir: " + deltaInLimitDir);

                    if (remainingSlideDist < 0 || deltaInLimitDir > limitDist) break;

                    iter++;
                }
                return outputPoint;
            }

            // Step 3: Case for 2+ valid edges found
            if(validEdges.Count > 1)
            {
                // Collect all points that are within the corridor and find minima.
                // Link points to what edge they belong to
                Dictionary<Vector3, Edge> pointToEdgeMap = new Dictionary<Vector3, Edge>();
                foreach (Edge edge in validEdges)
                {
                    if(IsBetweenParallelLines(edge.startPoint, left_line_origin, right_line_origin, heading_flat))
                    {
                        pointToEdgeMap[edge.startPoint] = edge;
                    }
                    if(IsBetweenParallelLines(edge.endPoint,left_line_origin, right_line_origin,heading_flat))
                    {
                        pointToEdgeMap[edge.endPoint] = edge;
                    }
                }
                Vector3 minima = Vector3.zero;
                float minimaDist = float.MaxValue;
                Vector3 midPoint = Vector3.zero;
                float midPointDist = float.MaxValue;
                foreach(KeyValuePair<Vector3, Edge> kvp in pointToEdgeMap)
                {
                    Vector3 point = kvp.Key;

                    Vector3 point_flat = Flatten(point);
                    Vector3 origin_flat = Flatten(origin_near_);

                    Vector3 toPoint = point_flat - origin_flat;
                    float distanceInHeadingDirection = Vector3.Dot(toPoint, heading_flat);

                    if(distanceInHeadingDirection < minimaDist)
                    {
                        minimaDist = distanceInHeadingDirection;
                        minima = point;
                    }

                    float distanceInHeadingRightDirection = Mathf.Abs(Vector3.Dot(toPoint, heading_flat_right));
                    if(distanceInHeadingRightDirection < midPointDist)
                    {
                        midPointDist = distanceInHeadingRightDirection;
                        midPoint = point;
                    }
                }

                // Find provisional left/right hand points

                // First try to see if our center line intersects any edge
                // otherwise, use middle-most point

                Vector3 stablePoint = midPoint;
                Edge startEdge = pointToEdgeMap[midPoint];

                foreach (Edge edge in validEdges)
                {
                    if (EdgeIntersectsCorridorLine(edge, origin_near_, heading)
                        && TryGetEdgeLineIntersection(edge, origin_near_, heading, out stablePoint))
                    {
                        startEdge = edge;
                        break;
                    }
                }
                float stablePoint_offcenter = Vector3.Dot(Flatten(stablePoint) - Flatten(origin_near_), heading_flat_right);

                // Left Provisional
                Vector3 leftHandPoint_prov = RecursiveSlideAlongEdge(
                    validEdges, startEdge, stablePoint, -1,
                    optHandFromCenterDist + stablePoint_offcenter, -heading_flat_right,
                    0.35f, heading_flat);

                // Right Provisional
                Vector3 rightHandPoint_prov = RecursiveSlideAlongEdge(
                    validEdges, startEdge, stablePoint, 1,
                    optHandFromCenterDist + stablePoint_offcenter, heading_flat_right,
                    0.35f, heading_flat);

                // do minima based recursion if:
                // - minima close enough to center
                // - minima edge is substantially large
                // - diff between furthest out handpoint and minima is large

                float minimaFromCenterDist = Mathf.Abs(Vector3.Dot(
                    Flatten(origin_near_ - minima), heading_flat_right));
                Edge minimaEdge = pointToEdgeMap[minima];
                float minimaEdgeLength = Vector3.Distance(
                    Flatten(minimaEdge.startPoint), Flatten(minimaEdge.endPoint));
                float minimaToLeftHandDist = Vector3.Dot(
                    Flatten(leftHandPoint_prov - minima), heading_flat);
                float minimaToRightHandDist = Vector3.Dot(
                    Flatten(rightHandPoint_prov - minima), heading_flat);
                float minimaToFurthestHandDist = Mathf.Max(minimaToLeftHandDist, minimaToRightHandDist);

                Vector3 leftHandPoint = leftHandPoint_prov;
                Vector3 rightHandPoint = rightHandPoint_prov;
/*
                Debug.Log("Minima Priority Stats: "
                    + " From Center Dist: " + minimaFromCenterDist
                    + " Edge Length : " + minimaEdgeLength
                    + " ToFurtherHandDist: " + minimaToFurthestHandDist);*/

                if(minimaFromCenterDist < 0.25f 
                    && minimaToFurthestHandDist > 0.35f)
                {
                    //Debug.Log("Basing Handpoint on Minima");
                    float minima_offcenter = Vector3.Dot(Flatten(minima - origin_near_), heading_flat_right);

                    leftHandPoint = RecursiveSlideAlongEdge(validEdges, minimaEdge, minima, -1,
                        optHandFromCenterDist + minima_offcenter, -heading_flat_right,
                        0.35f, heading_flat);


                    rightHandPoint = RecursiveSlideAlongEdge(validEdges, minimaEdge, minima, 1,
                        optHandFromCenterDist + minima_offcenter, heading_flat_right,
                        0.35f, heading_flat);
                }

                Drawing.DrawCrossOnXZPlane(leftHandPoint, 0.1f, Color.cyan);
                Drawing.DrawCrossOnXZPlane(rightHandPoint, 0.1f, Color.magenta);

                Drawing.DrawCrossOnXZPlane(stablePoint, 0.05f, new Color(1f, 0.5f, 0f, 0.7f));

                Drawing.DrawCrossOnXZPlane(minima, 0.05f, Color.yellow);

                leftHandOutput = leftHandPoint;
                rightHandOutput = rightHandPoint;
            }

            if(Vector3.Distance(leftHandOutput, origin_near_) < 5.4f && 
                Vector3.Distance(rightHandOutput, origin_near_) < 5.4f)
            {
                return true;
            }
            return false;
        }

        private bool LandingClearance(Vector3 leftHandPoint, Vector3 rightHandPoint, out Vector3 landingPoint_, out Vector3 landingPointTop_)
        {
            Vector3 handsMidpoint = (leftHandPoint + rightHandPoint) / 2f;
            Vector3 mantleDir = (handsMidpoint - playerTransform.position).normalized;
            mantleDir.y = 0f;
            mantleDir.Normalize();
            Vector3 forwardPoint = handsMidpoint + (mantleDir * 0.3f);

            if (!Physics.Raycast(forwardPoint + (Vector3.up * 0.3f), Vector3.down, out _, 0.8f, mantleableLayers))
            {
                forwardPoint = handsMidpoint + (mantleDir * 0.54f);
            }

            float minUpOffset = 0.05f;
            float maxUpOffset = landingClearance_maxUpOffset;
            float stepSize = (maxUpOffset - minUpOffset) / landingClearance_increments;
            for(int i = 0; i < landingClearance_increments; i++)
            {
                float upOffset = minUpOffset + (stepSize * i);

                Vector3 landingPoint = forwardPoint + Vector3.up * upOffset;
                Drawing.DrawCrossOnXZPlane(landingPoint, 0.2f, new Color(1f, 1f, 1f, 0.4f));

                Vector3 tall_c_world_center = tallestPlayerCollider.transform.TransformPoint(tallestPlayerCollider.center);
                Vector3 wide_c_world_center = widestPlayerCollider.transform.TransformPoint(widestPlayerCollider.center);
                float wide_c_center_y_offset_from_tall_c = wide_c_world_center.y - tall_c_world_center.y;

                // Tall Collider

                // We ADD to the bottom points, as the OverlapCapsule top and bottom points
                // disregard the additional height added by the radius

                float tall_c_radius = tallestPlayerCollider.bounds.extents.x;
                float tall_c_height = tallestPlayerCollider.bounds.size.y;
                float tall_c_top_point_y = landingPoint.y + tall_c_height;
                Vector3 tall_c_bottom_point = landingPoint + (Vector3.up * tall_c_radius);
                Vector3 tall_c_top_point = landingPoint + (Vector3.up * tall_c_height) - (Vector3.up * tall_c_radius);

                // Wide Collider
                float wide_c_radius = widestPlayerCollider.bounds.extents.x;
                float wide_c_height = widestPlayerCollider.bounds.size.y;
                float wide_c_bottom_point_y = (landingPoint.y + tall_c_height / 2f) + wide_c_center_y_offset_from_tall_c - wide_c_height / 2f;
                float wide_c_top_point_y = wide_c_bottom_point_y + wide_c_height;
                Vector3 wide_c_bottom_point = new Vector3(landingPoint.x, wide_c_bottom_point_y + wide_c_radius, landingPoint.z);
                Vector3 wide_c_top_point = new Vector3(landingPoint.x, wide_c_top_point_y - wide_c_radius, landingPoint.z);

                bool tall_hit = Physics.OverlapCapsule(
                    tall_c_bottom_point,
                    tall_c_top_point,
                    tall_c_radius,
                    mantleableLayers).Length > 0;

                bool wide_hit = Physics.OverlapCapsule(
                    wide_c_bottom_point,
                    wide_c_top_point,
                    wide_c_radius,
                    mantleableLayers).Length > 0;
                bool hasLandingClearance = (!tall_hit && !wide_hit);

                // Debug Drawings
/*                Drawing.DebugDrawCapsuleApprox(
                    tall_c_bottom_point,
                    tall_c_top_point,
                    tall_c_radius,
                    Quaternion.LookRotation(mantleDir),
                    tall_hit ? new Color(1f, 0f, 0f, 0.2f) : new Color(0f, 1f, 0f, 0.2f));
                Drawing.DebugDrawCapsuleApprox(
                    wide_c_bottom_point,
                    wide_c_top_point,
                    wide_c_radius,
                    Quaternion.LookRotation(mantleDir),
                     wide_hit ? new Color(1f, 0f, 0f, 0.2f) : new Color(0f, 1f, 0f, 0.2f));*/

                if (hasLandingClearance)
                {
                    landingPoint_ = landingPoint;
                    landingPointTop_ = (tall_c_top_point.y > wide_c_top_point.y) ? tall_c_top_point : wide_c_top_point;
                    return true;
                }
            }

            landingPoint_ = Vector3.zero;
            landingPointTop_ = Vector3.zero;
            return false;
        }


        private bool MotionClearance(
            Vector3 landingPointBottom,
            Vector3 landingPointTop,
            out float yClearancePoint,
            out Vector3 bestApproachPoint_)
        {
            yClearancePoint = 0f;
            bestApproachPoint_ = Vector3.zero;

            if (FindClosestMantleApproachPoint(landingPointBottom, landingPointTop, out Vector3 bestApproachPoint, out float localYClearance))
            {
                yClearancePoint = localYClearance;
                bestApproachPoint_ = bestApproachPoint;

                // Optional: draw the final chosen point
                Drawing.DrawCrossOnXZPlane(bestApproachPoint, 0.2f, Color.green);
                Debug.DrawLine(bestApproachPoint, bestApproachPoint + Vector3.up * 2f, Color.green, 1f);

                return true;
            }

            return false;
        }

        private bool FindClosestMantleApproachPoint(
            Vector3 landingPointBottom,
            Vector3 landingPointTop,
            out Vector3 bestApproachPoint,
            out float yClearancePoint)
        {
            bestApproachPoint = Vector3.zero;
            yClearancePoint = 0f;

            Vector3 directionToLanding_flat = Flatten(landingPointBottom - playerTransform.position);
            float totalDistance = directionToLanding_flat.magnitude;
            directionToLanding_flat.Normalize();

            int steps = motionClearance_increments * 2;
            float stepSize = totalDistance / steps;

            Vector3 currentTestPoint = playerTransform.position - (directionToLanding_flat * widestPlayerCollider.bounds.extents.x);
            currentTestPoint.y = 0f;

            Vector3 tall_c_center = tallestPlayerCollider.bounds.center;

            float wide_c_radius = widestPlayerCollider.bounds.extents.x * 0.8f;
            float tall_c_height = tallestPlayerCollider.bounds.size.y;

            Vector3 lastValidApproach = Vector3.zero;
            float lastClearanceY = 0f;
            bool foundValid = false;

            for (int i = 0; i <= steps; i++)
            {

                // Capsule bounds based on test point
                Vector3 capsuleBottom = currentTestPoint + (Vector3.up * (tall_c_center.y - tall_c_height / 2f))
                    + (Vector3.up * wide_c_radius) + (Vector3.up * 0.3f);
                Vector3 capsuleTop = currentTestPoint + (Vector3.up * (tall_c_center.y + tall_c_height / 2f))
                    - (Vector3.up * wide_c_radius);

                Collider[] capsuleHits = Physics.OverlapCapsule(
                    capsuleBottom,
                    capsuleTop,
                    wide_c_radius,
                    mantleableLayers
                );

                Drawing.DebugDrawCapsuleApprox(
                    capsuleBottom,
                    capsuleTop,
                    wide_c_radius,
                    Quaternion.LookRotation(directionToLanding_flat),
                    capsuleHits.Length > 0 ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f)
                );

                // Skip if capsule collides
                if (capsuleHits.Length > 0)
                    break;

                if (CheckVerticalClearanceAt(
                    landingPointBottom,
                    landingPointTop,
                    capsuleTop,
                    directionToLanding_flat,
                    wide_c_radius,
                    tall_c_height,
                    out float localYClearancePoint))
                {
                    lastValidApproach = currentTestPoint;
                    lastClearanceY = localYClearancePoint;
                    foundValid = true;
                }
                else
                {
                    // We've gone too far; return last successful point.
                    break;
                }

                currentTestPoint += directionToLanding_flat * stepSize;
            }

            if (foundValid)
            {
                bestApproachPoint = lastValidApproach;
                bestApproachPoint.y = playerTransform.position.y;
                yClearancePoint = lastClearanceY;
                return true;
            }

            return false;
        }

        private bool CheckVerticalClearanceAt(
            Vector3 landingPointBottom,
            Vector3 landingPointTop,
            Vector3 approachPointTop,
            Vector3 directionToPlayer_flat,
            float wide_c_radius,
            float tall_c_height,
            out float yClearancePoint)
        {
            float stepSize = (motionClearance_maxYOffset - motionClearance_minYOffset) / motionClearance_increments;
            yClearancePoint = 0f;
            float highClearanceTopY = 0f;

            Vector3 approachPoint_XZ = approachPointTop;
            approachPoint_XZ.y = 0f;

            Vector3 directionToApproachPoint_XZ = (approachPoint_XZ - landingPointBottom);
            directionToApproachPoint_XZ.y = 0f;
            float distanceToApproachPoint_XZ = directionToApproachPoint_XZ.magnitude;
            directionToApproachPoint_XZ.Normalize();

            float landingPointCenter_Y = ((landingPointBottom + landingPointTop) / 2f).y;
            Vector3 landingToApproachPointMidpoint_XZ = (landingPointBottom + approachPoint_XZ) / 2f;
            landingToApproachPointMidpoint_XZ.y = 0f;

            Vector3 box_center_base = new Vector3(
                landingToApproachPointMidpoint_XZ.x,
                landingPointCenter_Y,
                landingToApproachPointMidpoint_XZ.z);
            box_center_base += directionToPlayer_flat * ((motionClearance_collider_offset_from_player / 2f));

            Vector3 box_size = new Vector3(
                wide_c_radius * 2f,
                tall_c_height,
                distanceToApproachPoint_XZ + motionClearance_collider_offset_from_player);

            Quaternion box_rot = Quaternion.LookRotation(directionToPlayer_flat);

            for (int i = 0; i < motionClearance_increments; i++)
            {
                Vector3 yOffset = Vector3.up * (motionClearance_minYOffset + i * stepSize);
                Vector3 box_center = box_center_base + yOffset;

                Collider[] hits = Physics.OverlapBox(box_center, box_size / 2f, box_rot, mantleableLayers);
                Color boxColor = hits.Length == 0 ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
                Drawing.DebugDrawBox(box_center, box_size, Vector3.zero, 0f, box_rot, boxColor);

                if (hits.Length == 0)
                {
                    yClearancePoint = box_center.y - box_size.y / 2f;
                    highClearanceTopY = box_center.y + box_size.y / 2f;

                    // Capsule check
                    float pColliderHighestYPoint = approachPointTop.y;
                    float capsule_col_height = highClearanceTopY - pColliderHighestYPoint;
                    if (capsule_col_height < 0f) return true;

                    float capsule_col_r = wide_c_radius;
                    Vector3 capsule_col_bottom_point = approachPointTop;
                    capsule_col_bottom_point.y = pColliderHighestYPoint + capsule_col_r;
                    Vector3 capsule_col_top_point = capsule_col_bottom_point + Vector3.up * (capsule_col_height - capsule_col_r * 2f);

                    Collider[] capsule_hits = Physics.OverlapCapsule(capsule_col_bottom_point, capsule_col_top_point, capsule_col_r, mantleableLayers);


                    Vector3 debug_yClearancePointVec3 = box_center;
                    debug_yClearancePointVec3.y = yClearancePoint;
                    Drawing.DrawCrossOnXZPlane(debug_yClearancePointVec3, 0.1f, Color.blue);

                    //Drawing.DrawCrossOnXZPlane(capsule_col_bottom_point, 0.7f, Color.blue);
                    //Drawing.DrawCrossOnXZPlane(capsule_col_top_point, 0.7f, Color.blue);

                    Color capsuleColor = capsule_hits.Length > 0 ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
                    Drawing.DebugDrawCapsuleApprox(capsule_col_bottom_point, capsule_col_top_point, capsule_col_r, Quaternion.LookRotation(directionToPlayer_flat), capsuleColor);

                    return capsule_hits.Length == 0;
                }
            }

            return false;
        }

        private float GetSlopeAngleFromNormal(Vector3 normal)
        {
            return Vector3.Angle(normal, Vector3.up);
        }
        private float GetSlopeAngleFromNormal_RelativeToDirection(Vector3 normal, Vector3 direction)
        {
            return Vector3.Angle(normal, direction) - 90;
        }

        /// <summary>
        /// Hit Point with some float value attached to it
        /// </summary>
        private struct ValuedHitPoint
        {
            public float v;
            public Vector3 rayOrigin;
            public RaycastHit hit;
            public ValuedHitPoint(float v, Vector3 rayOrigin, RaycastHit hit)
            {
                this.v = v;
                this.rayOrigin = rayOrigin;
                this.hit = hit;
            }
        }

        private struct ValuedPoint
        {
            public float v;
            public Vector3 point;
            public ValuedPoint(float v, Vector3 point)
            {
                this.v = v;
                this.point = point;
            }
        }

        private struct Edge
        {
            public int edgeNum;
            public Vector3 startPoint;
            public Vector3 midPoint;
            public Vector3 endPoint;
            public Vector3 generalDirection;
            public Color debugCol;

        }

        private Color GetRandomVibrantColor()
        {
            return new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f)
            );
        }

        private float CalculateAverageValue(List<ValuedHitPoint> points)
        {
            if (points.Count == 0) return 0f;
            float sum = 0f;
            foreach (ValuedHitPoint point in points){
                sum += point.v;
            }
            return sum / points.Count;
        }
        private float CalculateAverageValue(List<ValuedPoint> points)
        {
            if (points.Count == 0) return 0f;
            float sum = 0f;
            foreach (ValuedPoint point in points){
                sum += point.v;
            }
            return sum / points.Count;
        }

        private float CalculateAverageY(List<ValuedPoint> points)
        {
            if (points.Count == 0) return 0f;
            float sum = 0f;
            foreach(ValuedPoint point in points){
                sum += point.point.y;
            }
            return sum / points.Count;
        }

        private struct HaitianGroup
        {
            public List<ValuedHitPoint> hits;
            public float angleAverage;
        }
        private Vector3 CalculateAveragePosXZ(List<Vector3> vectors)
        {
            if (vectors.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;

            foreach (Vector3 normal in vectors)
            {
                sum += new Vector3(normal.x, 0f, normal.z); // Ignore Y component
            }

            Vector3 average = sum / vectors.Count;
            return average;
        }
        private Vector3 CalculateAverageDirectionXZ(List<Vector3> vectors)
        {
            Vector3 average = CalculateAveragePosXZ(vectors);
            return average.normalized; // Return as a normalized vector
        }

        private Vector3 Flatten(Vector3 v)
        {
            return new Vector3 (v.x, 0f, v.z);
        }
        private Vector3 FlattenDirection(Vector3 v)
        {
            return Flatten(v).normalized;
        }
    }
}
