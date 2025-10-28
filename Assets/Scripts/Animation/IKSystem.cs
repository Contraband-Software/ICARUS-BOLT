using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls IK calculation and execution order for its given IKGroups.
/// 
/// NOTE: IK Targets must be children of the transform with this component
/// 
/// IK Calculations and Applications are done Root Bone first, Leaf bones last
///  -> This is so a parent transform can properly update before its child
///     calculates how it should move. Otherwise, if a child rotates to an IK target,
///     then the parent rotates to its own IK target, the child will no longer be aligned.
/// </summary>
public class IKSystem : MonoBehaviour
{

    // List of IKElements that all target the same Transform,
    // sorted by where the IKElement belonging to the 1st IKGroup
    // is last in the Buckets List, so it has precedence
    private class Bucket
    {
        public List<IKElement> ikElements;
        public Transform targettingTransform;

        public Bucket()
        {
            ikElements = new List<IKElement>();
            targettingTransform = null;
        }
    }

    private class Layer
    {
        public List<Bucket> bucketsInLayer;
        public HashSet<IKElement> allIKElements;
        // these are different because:
        // Buckets in the layer can have an IKElement in two+ buckets
        // (as an IKelement could effect two+ different transforms)

        public Layer()
        {
            bucketsInLayer = new List<Bucket>();
            allIKElements = new HashSet<IKElement>();
        }
    }

    [SerializeField] private List<IKGroup> IKGroups = new List<IKGroup>();
    private List<Layer> IKElementExecutionOrder = new List<Layer>();
    //      Layer<Buckets<Bucket<IKElement>>>
    //      Layer: Seperates out IKElements based off of their assigned layer number
    //      Buckets: Groups of IKElements that affect the same transform.
    //              Buckets are sorted by order of their targetting Transform in the heirarchy.
    //      Bucket: List of IKElements, sorted by where the IKElement belonging to the 1st IKGroup
    //              is last in the Bucket, so it has precedence.

    private HashSet<Transform> allIKAffectedTransforms = new HashSet<Transform>();
    private List<Transform> allIKAffectedTransformsList = new List<Transform>(); // the above but as a list
    private Dictionary<Transform, Pose> defaultPoses = new Dictionary<Transform, Pose>();
    private Dictionary<Transform, AnimationDeltaLocal> animationDeltas = new Dictionary<Transform, AnimationDeltaLocal>();

    [SerializeField]
    private List<UnityEvent> postIKCallbacks = new List<UnityEvent>();

    private void Start()
    {
        // Create the Execution Order
        List<IKElement> allIKElements = CollectAllIKElements();

        CallIKElementInitialization(allIKElements);

        List<List<IKElement>> ikElementsInLayers = SortAndGroupByLayer(allIKElements);

        IKElementExecutionOrder = BucketAndSortIKElementsOfLayers(ikElementsInLayers);

        allIKAffectedTransforms = FindAllIKAffectedTransforms(allIKElements);
        allIKAffectedTransformsList = new List<Transform>(allIKAffectedTransforms);
        CacheDefaultPoses(allIKAffectedTransformsList);
    }
    
    private void Update()
    {
        //Reset All Bones pre-animation loop
        // (this is to avoid non-animated bones keeping their IK'd transforms
        //  between frames)
        HardResetBonePoses(allIKAffectedTransformsList);
    }

    private void LateUpdate()
    {
        Execute();
    }

    public void Execute()
    {
        // Alternative:
        // Reset bone poses and cache animation deltas ONCE
        // Then layer bones can IK off of their new positions
        // afterwards re-add animation to all required transforms

        CacheBoneAnimationDeltas(allIKAffectedTransformsList);

        foreach (Layer layer in IKElementExecutionOrder)
        {
            ResetBonePosesToDefault(layer.bucketsInLayer);
        }


        foreach (Layer layer in IKElementExecutionOrder)
        {
            //Any delta from the base pose at the start of a layer executions is the new animation delta.
            //CacheBoneAnimationDeltas(allIKAffectedTransformsList);

            //ResetBonePosesToDefault(layer.bucketsInLayer); // Reset bones of GIVEN LAYER to default.

            ExecuteIKs(layer);  // Execute the IK's of the GIVEN LAYER

            ApplyAdditiveIKAnimationLayer(layer); // Add animation lost FOR THIS LAYER

            //Release cached calculation afterwards
            foreach (IKElement ikElement in layer.allIKElements)
            {
                ikElement.ReleaseCachedCalculation();
            }
        }

        // Invoke Post IK Callbacks
        foreach (var c in postIKCallbacks)
        {
            if (c == null) continue;
            c.Invoke();
        }
    }

    // Collect all IKElement components references in IKGroups linked to the IKSystem
    List<IKElement> CollectAllIKElements()
    {
        List<IKElement> finalList = new List<IKElement>();
        foreach (IKGroup ikgroup in IKGroups)
        {
            finalList.AddRange(ikgroup.GetIKElements());
        }
        return finalList;
    }

    void CallIKElementInitialization(List<IKElement> iKElements)
    {
        foreach(IKElement ikElement in iKElements)
        {
            ikElement.Initialize();
        }
    }

    // Find all IK affected Transforms
    HashSet<Transform> FindAllIKAffectedTransforms(List<IKElement> ikElements)
    {
        HashSet<Transform> allIKAffectedTransforms = new HashSet<Transform>();
        foreach(IKElement ikElement in ikElements)
        {
            allIKAffectedTransforms.Add(ikElement.IKObject);
            allIKAffectedTransforms.UnionWith(ikElement.GetSideEffectedIKObjects());
        }
        return allIKAffectedTransforms;
    }

    // Divides list of IKElements into groups of the same layer, sorting them by layer number
    List<List<IKElement>> SortAndGroupByLayer(List<IKElement> ikElements)
    {
        Dictionary<int, List<IKElement>> layers = new Dictionary<int, List<IKElement>>();
        int lowestLayer = int.MaxValue;
        int highestLayer = int.MinValue;

        foreach(IKElement ikElement in ikElements)
        {
            int layerNum = ikElement.GetLayer();
            if(layerNum < lowestLayer) { lowestLayer = layerNum; }
            if(layerNum > highestLayer) { highestLayer = layerNum; }
            if (!layers.ContainsKey(layerNum))
            {
                layers[layerNum] = new List<IKElement>();
            }
            layers[layerNum].Add(ikElement);
        }

        List<List<IKElement>> sortedLayers = new List<List<IKElement>>();
        for(int i = lowestLayer; i <= highestLayer; i++)
        {
            sortedLayers.Add(layers[i]);
        }

        return sortedLayers;
    }

    // Take all the IKElements that have been placed into layers, and for each layer,
    // do the bucketing method for all IKElements in the layer.
    List<Layer> BucketAndSortIKElementsOfLayers(List<List<IKElement>> ikElementsInLayers)
    {
        List<Layer> ikElementsInBucketsInLayers = new List<Layer>();
        foreach(List<IKElement> ikElementsInLayer in ikElementsInLayers)
        {
            Layer layer = new Layer();
            layer.allIKElements = new HashSet<IKElement>(ikElementsInLayer);
            layer.bucketsInLayer = BucketAndSortIKElementsByTargetTransform(ikElementsInLayer);
            ikElementsInBucketsInLayers.Add(layer);
        }
        return ikElementsInBucketsInLayers;
    } 

    // Get all IKElements, create buckets based of IKElements based off of which transform
    // they influence, sort the bucket order so that the first bucket holds IKElements which
    // influence the bone that is highest in the heirarchy
    List<Bucket> BucketAndSortIKElementsByTargetTransform(List<IKElement> unsortedIKElements)
    {
        // Step 1: Group IKElements by their target transforms
        Dictionary<Transform, Bucket> transformToBucket = new Dictionary<Transform, Bucket>();

        foreach (IKElement ikElement in unsortedIKElements)
        {
            // Combine the targetTransfrom and sideEffectTransforms of the IKElement into one list
            List<Transform> targettedTransforms = new List<Transform> { ikElement.IKObject };
            targettedTransforms.AddRange(ikElement.GetSideEffectedIKObjects());

            foreach(Transform targetTransform in targettedTransforms)
            {
                // Add IKElement to the corresponding bucket
                if (!transformToBucket.ContainsKey(targetTransform))
                {
                    Bucket newBucket = new Bucket();
                    newBucket.targettingTransform = targetTransform;
                    transformToBucket[targetTransform] = newBucket;
                }
                transformToBucket[targetTransform].ikElements.Add(ikElement);
            }
        }

        // Step 2: Sort the target transforms by their depth in the hierarchy
        List<Transform> sortedTransforms = new List<Transform>(transformToBucket.Keys);
        sortedTransforms.Sort((a, b) =>
        {
            if (a == b) return 0;
            if (a.IsChildOf(b)) return 1; // a is deeper in hierarchy
            if (b.IsChildOf(a)) return -1; // b is deeper in hierarchy
            return 0; // Siblings remain in original order
        });

        // Step 3: Generate the final list of buckets in sorted order of their target transform heirarchy depth
        List<Bucket> sortedBuckets = new List<Bucket>();

        foreach (Transform transform in sortedTransforms)
        {
            // Get the bucket of IKElements for this transform
            Bucket bucket = transformToBucket[transform];

            // Step 4: Sort IKElements in this bucket based on their IKGroup precedence
            bucket.ikElements.Sort((ikElementA, ikElementB) =>
            {
                IKGroup groupA = ikElementA.GetOwningIKGroup();
                IKGroup groupB = ikElementB.GetOwningIKGroup();

                int indexA = IKGroups.IndexOf(groupA);
                int indexB = IKGroups.IndexOf(groupB);

                // Higher precedence means later in the execution order
                return indexA.CompareTo(indexB);
            });

            sortedBuckets.Add(bucket);
        }

        return sortedBuckets;
    }

    // Cache the default pose of each given transform
    private void CacheDefaultPoses(List<Transform> IKObjects)
    {
        foreach(Transform IKObject in IKObjects)
        {
            if (!defaultPoses.ContainsKey(IKObject))
            {
                defaultPoses[IKObject] = new Pose(IKObject.localPosition, IKObject.localRotation);
            }
        }
    }

    // Cache how each IK'd transform has been manipulated by animation
    // using the bones default pose as reference
    private void CacheBoneAnimationDeltas(List<Transform> IKObjects)
    {
        foreach(Transform IKObject in IKObjects)
        {
            // calculate animation delta change from base pose
            // (position and rotation stored in Pose are local)
            Pose defaultPose = defaultPoses[IKObject];
            Quaternion animationRotationDelta = Quaternion.Inverse(defaultPose.rotation) * IKObject.localRotation;
            Vector3 animationTranslationDelta = IKObject.localPosition - defaultPose.position;
            animationDeltas[IKObject] = new AnimationDeltaLocal(animationTranslationDelta, animationRotationDelta);
        }
    }

    // forceful reset of all IK affected bones
    private void HardResetBonePoses(List<Transform> IKObjects)
    {
        foreach(Transform IKObject in IKObjects)
        {
            Pose defaultPose = defaultPoses[IKObject];
            // Reset to the cached pose
            IKObject.localPosition = defaultPose.position;
            IKObject.localRotation = defaultPose.rotation;
        }
    }

    // Set each IK'd transform to its original pose 
    private void ResetBonePosesToDefault(List<Bucket> ikBucketsInLayer)
    {
        // only reset poses on elements where an IK will actually act on them
        foreach (Bucket bucket in ikBucketsInLayer)
        {
            if (bucket.ikElements.Count == 0){
                continue;
            }
            
            Transform IKObject = bucket.targettingTransform;
            bool resetBonePose = false;
            foreach (IKElement ikElement in bucket.ikElements)
            {
                float finalWeight = ikElement.GetTotalWeight();
                if( (ikElement.ForceBonePoseReset || ikElement.IsAdditive) && finalWeight > 0)
                {
                    resetBonePose = true;
                    break;
                }
            }
            if (resetBonePose)
            {
                Pose defaultPose = defaultPoses[IKObject];
                // Reset to the cached pose
                IKObject.localPosition = defaultPose.position;
                IKObject.localRotation = defaultPose.rotation;
            }
        }
    }

    private void ExecuteIKs(Layer layer)
    {
        // Go through each bucket now as they are sorted in order of transform heirarchy
        foreach(Bucket bucket in layer.bucketsInLayer)
        {
            if (bucket.ikElements.Count == 0) continue;
            // Assume all IKElements in the bucket act on the same transform (AS THEY SHOULD)
            Transform targetTransform = bucket.targettingTransform;

            // Initialize accumulated transformation for the buckets target transform
            IKTransformationWorld accumulatedIKTW = new IKTransformationWorld(Vector3.zero, Quaternion.identity);

            foreach(IKElement ikElement in bucket.ikElements)
            {
                float totalWeight = ikElement.GetTotalWeight();
                if (totalWeight <= 0) continue;

                // if we havent calculated the IK for this IKElement yet,
                // calculate and cache the result
                if (!ikElement.CalculationCached)
                {
                    Dictionary<Transform, IKTransformationWorld> ikR = ikElement.CalculateIK();
                    ikElement.SetCachedCalculation(ikR);
                }

                // Find the transformation that is calculated for THIS transform by the ikelement
                IKTransformationWorld ikTW = ikElement.GetCachedCalculation()[targetTransform];
                // accumulate
                accumulatedIKTW.translation += ikTW.translation;
                accumulatedIKTW.rotation *= ikTW.rotation;
            }
            // Apply the accumulated IK to the transform
            ApplyIK(targetTransform, accumulatedIKTW);
        }
    }

    private void ApplyIK(Transform IKObject, IKTransformationWorld ikWorld)
    {
        // Apply this rotation to the current rotation in world space
        Quaternion targetRotationWorld = ikWorld.rotation * IKObject.rotation;

        // Convert the target world rotation into local space
        Quaternion targetRotationLocal = IKObject.parent != null
            ? Quaternion.Inverse(IKObject.parent.rotation) * targetRotationWorld
            : targetRotationWorld;

        // Apply the calculated rotation in local space
        IKObject.localRotation = targetRotationLocal;

        // --- Apply translation

        // Get the current world position of the IKObject
        Vector3 currentPositionWorld = IKObject.position;

        // Add the ikWorld.translation (which is in world space) to the current world position
        Vector3 targetPositionWorld = currentPositionWorld + ikWorld.translation;

        // Convert the target world position back to local space relative to the parent
        Vector3 targetPositionLocal = IKObject.parent != null
            ? IKObject.parent.InverseTransformPoint(targetPositionWorld)
            : targetPositionWorld;

        // Set the local position of the IKObject
        IKObject.localPosition = targetPositionLocal;
    }

    // This is to check if layers following the current one will try to re-add animation
    // to the given transform. 
    // Yeah its a N^2 search but who gives a shit
    private bool ProceedingLayerAddsAnimationsToTransform(Layer thisLayer, Transform targetTransform)
    {
        int c_index = IKElementExecutionOrder.IndexOf(thisLayer);
        if (c_index == -1)
        {
            throw new InvalidOperationException("Provided layer was not found in the layers list.");
        }
        for(int i = c_index + 1; i < IKElementExecutionOrder.Count; i++)
        {
            // try locate target transform in layer
            Layer layer = IKElementExecutionOrder[i];
            foreach (Bucket bucket in layer.bucketsInLayer)
            {
                if (bucket.ikElements.Count == 0) continue;
                if(bucket.targettingTransform != targetTransform) continue;
                foreach (IKElement ikElement in bucket.ikElements)
                {
                    // If the below is true, shows that some layer in front will
                    // readd animation. 
                    float finalWeight = ikElement.GetTotalWeight();
                    if (ikElement.IsAdditive && finalWeight > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;

    }

    // Re-add animations onto bones that IK'd by IKElements
    // that declare that they are Additive
    // This could be optimized  
    private void ApplyAdditiveIKAnimationLayer(Layer layer)
    {

        foreach (Bucket bucket in layer.bucketsInLayer)
        {
            if (bucket.ikElements.Count == 0)
            {
                continue;
            }
            // if any one IKElement declares it is additive and its active on some
            // bone, always re-add all animation to said bone.
            bool reapplyAnimation = false;
            foreach (IKElement ikElement in bucket.ikElements)
            {
                // reapply animation delta if IKElement is Additive and In-Effect
                float finalWeight = ikElement.GetTotalWeight();
                if (ikElement.IsAdditive && finalWeight > 0)
                {
                    reapplyAnimation = true;
                    break;
                }
            }

            if (reapplyAnimation)
            {
                Transform IKObject = bucket.targettingTransform;
                // First check to see if a later layer wont do this. We shouldnt do this twice.
                if (ProceedingLayerAddsAnimationsToTransform(layer, IKObject))
                {
                    continue;
                }

                AnimationDeltaLocal animationDelta = animationDeltas[IKObject];
                IKObject.localPosition += animationDelta.translation;
                IKObject.localRotation *= animationDelta.rotation;
            }
        }
    }

    #region TYPES
    public enum Axis
    {
        X,
        NegX,
        Y,
        NegY,
        Z,
        NegZ
    }

    // I named this one WORLD so that whenever its used you can easily tell its meant to store WORLD
    // transformations as opposed to some arbitrary translation and rotation
    public struct IKTransformationWorld
    {
        public Vector3 translation;
        public Quaternion rotation;
        public IKTransformationWorld(Vector3 translation, Quaternion rotation)
        {
            this.translation = translation;
            this.rotation = rotation;
        }
        public IKTransformationWorld(Vector3 translation)
        {
            this.translation = translation;
            this.rotation = Quaternion.identity;
        }
        public IKTransformationWorld(Quaternion rotation)
        {
            this.translation = Vector3.zero;
            this.rotation = rotation;
        }
    }

    private struct AnimationDeltaLocal
    {
        public Vector3 translation;
        public Quaternion rotation;
        public AnimationDeltaLocal(Vector3 t, Quaternion r)
        {
            translation = t;
            rotation = r;
        }
    }
    #endregion

    #region STATIC_FUNCIONS
    public static Vector3 GetDirectionVectorFromAxis(Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return Vector3.right;
            case Axis.Y:
                return Vector3.up;
            case Axis.Z:
                return Vector3.forward;
            case Axis.NegX:
                return Vector3.left;
            case Axis.NegY:
                return Vector3.down;
            case Axis.NegZ:
                return Vector3.back;
        }
        return Vector3.zero;
    }
    public static Vector3 GetTransformDirectionVectorFromAxis(Transform transform, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return transform.right;
            case Axis.Y:
                return transform.up;
            case Axis.Z:
                return transform.forward;
            case Axis.NegX:
                return -transform.right;
            case Axis.NegY:
                return -transform.up;
            case Axis.NegZ:
                return -transform.forward;
        }
        return Vector3.zero;
    }
    #endregion
}
