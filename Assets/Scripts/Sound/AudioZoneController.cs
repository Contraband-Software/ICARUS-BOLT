// AudioZoneController.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Audio;
using Vector3 = UnityEngine.Vector3;

namespace Sound
{

    /// <summary>
    /// A data structure to hold the results of the occlusion calculation.
    /// </summary>
    public struct OcclusionInfo
    {
        public float occlusionFactor; // 0-1, how much is occluded
        public float averageDistance; // The average length of the cast rays
    }

    /// <summary>
    /// Manages which AudioZone is currently active based on the player's position.
    /// Uses a List as a stack to handle nested/overlapping zones correctly.
    /// This component should be on the player or the main camera.
    /// </summary>
    public class AudioZoneController : MonoBehaviour
    {
        public AudioMixerGroup interiorMixer;
        public AudioMixerGroup exteriorMixer;
        public AudioMixerGroup sfxMixer;

        public List<AudioSource> ambientSources = new List<AudioSource>();
        public List<AudioZone> activeZones = new List<AudioZone>();
        public List<float> ambientWeights = new List<float>();
        public List<float> targetAmbientWeights = new List<float>();

        // The raw occlusion value calculated each frame.
        public OcclusionInfo currentOcclusionInfo;

        // A smoothed version of the occlusion value to prevent abrupt audio changes.
        public float smoothedOcclusion = 0f;
        public float smoothedAverageDistance = 0f;

        [Header("Occlusion Settings")]
        [Tooltip("Number of rays to cast upwards to check for occlusion.")]
        [SerializeField]
        [Range(1, 256)]
        private int occlusionRays = 64;

        [Tooltip("Maximum distance for the occlusion rays.")] [SerializeField]
        private float occlusionRayMaxDistance = 50f;

        [SerializeField] private float occlusionReferenceDistace = 10f;

        [Tooltip("Layers that will block the occlusion rays (e.g., 'Default', 'Walls').")] [SerializeField]
        private LayerMask occlusionLayerMask;

        [Header("Reverb Control")] [Tooltip("The master audio mixer that has the reverb effects.")] [SerializeField]
        private AudioMixer masterMixer;

        [Tooltip("The name of the exposed parameter for the reverb's wet level.")] [SerializeField]
        private string reverbRoomParameter = "Reverb_Room";

        [Tooltip("The name of the exposed parameter for a size-related reverb property (e.g., Room Size, Decay Time).")]
        [SerializeField]
        private string reverbSizeParameter = "Reverb_DecayT";


        private void Start()
        {
            GameObject[] ambientObjects = GameObject.FindGameObjectsWithTag("Ambient");
            foreach (GameObject obj in ambientObjects)
            {
                if (obj.TryGetComponent<AudioSource>(out AudioSource source))
                {
                    ambientSources.Add(source);
                    ambientWeights.Add(0f);
                    targetAmbientWeights.Add(0f);
                }
            }
        }

        private void Update()
        {
            currentOcclusionInfo = CalculateOcclusion(transform.position);

            // Smooth the values to prevent abrupt audio changes.
            smoothedOcclusion =
                Mathf.Lerp(smoothedOcclusion, currentOcclusionInfo.occlusionFactor, Time.deltaTime * 5f);
            smoothedAverageDistance = Mathf.Lerp(smoothedAverageDistance, currentOcclusionInfo.averageDistance,
                Time.deltaTime * 5f);
            
            UpdateMixers();

            targetAmbientWeights.Clear();
            for (int i = 0; i < ambientSources.Count; i++)
            {
                targetAmbientWeights.Add(0f);
            }

            foreach (AudioZone zone in activeZones)
            {
                foreach (WeightedAudioSource weightedSource in zone.controlledSources)
                {
                    int index = ambientSources.IndexOf(weightedSource.source);
                    if (index != -1)
                    {
                        float weightMultiplier = 1;
                        if (zone.distFade)
                        {
                            if (zone.TryGetComponent<Collider>(out var zoneCollider))
                            {
                                float distanceToCenter = Vector3.Distance(transform.position, zone.transform.position);
                                // Use the magnitude of the collider's extents as an effective "radius".
                                float zoneRadius = zoneCollider.bounds.extents.magnitude;

                                if (zoneRadius > 0)
                                {
                                    // This will create a smooth fade from 1 (at the center) to 0 (at the edge).
                                    weightMultiplier = Mathf.InverseLerp(zoneRadius, 0f, distanceToCenter);
                                }
                            }
                        }

                        targetAmbientWeights[index] += weightedSource.weight * weightMultiplier;
                    }
                }
            }

            for (int i = 0; i < ambientSources.Count; i++)
            {
                ambientWeights[i] += (Mathf.Clamp(targetAmbientWeights[i], 0, 1) - ambientWeights[i]) * Time.deltaTime;
            }

            // Every frame, apply the master weights to the actual AudioSource volumes.
            for (int i = 0; i < ambientSources.Count; i++)
            {
                AudioSource source = ambientSources[i];
                float currentVolume = ambientWeights[i];

                // Apply the volume.
                source.volume = currentVolume;

                // Automatically play or stop the source based on its volume.
                if (currentVolume > 0.001f && !source.isPlaying)
                {
                    source.Play();
                }
                else if (currentVolume <= 0.001f && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        private void UpdateMixers()
        {
            // Interior volume is directly proportional to occlusion.
            float linearInteriorVol = smoothedOcclusion;
            // Exterior volume is inversely proportional to occlusion.
            float linearExteriorVol = 1f - smoothedOcclusion;

            // Clamp to a small value to avoid log10(0) which is -infinity.
            linearInteriorVol = Mathf.Clamp(linearInteriorVol, 0.0001f, 1f);
            linearExteriorVol = Mathf.Clamp(linearExteriorVol, 0.0001f, 1f);

            // Convert linear values to decibels.
            float dbInteriorVol = Mathf.Log10(linearInteriorVol) * 20f;
            float dbExteriorVol = Mathf.Log10(linearExteriorVol) * 20f;

            if (interiorMixer != null)
            {
                interiorMixer.audioMixer.SetFloat("Volume_In", dbInteriorVol);
            }

            if (exteriorMixer != null)
            {
                exteriorMixer.audioMixer.SetFloat("Volume_Out", dbExteriorVol);
            }
            

            float normalizedDistance = Mathf.InverseLerp(0, occlusionRayMaxDistance, smoothedAverageDistance);
            
            float targetRoomLevel = Mathf.Lerp(-3000, -100, smoothedOcclusion);
            sfxMixer.audioMixer.SetFloat(reverbRoomParameter, targetRoomLevel);

            float targetDecayTime = Mathf.Lerp(0.4f, 3.5f, normalizedDistance);
            sfxMixer.audioMixer.SetFloat(reverbSizeParameter, targetDecayTime);

            // float d1, d2;
            // sfxMixer.audioMixer.GetFloat(reverbRoomParameter, out d1);
            // sfxMixer.audioMixer.GetFloat(reverbSizeParameter, out d2);
            // Debug.Log("room param: " + d1);
            // Debug.Log("size param: " + d2);

        }


        /// <summary>
        /// Calculates occlusion and average ray distance from a given position.
        /// </summary>
        /// <param name="position">The world position to cast rays from.</param>
        /// <returns>An OcclusionInfo struct containing the results.</returns>
        public OcclusionInfo CalculateOcclusion(Vector3 position)
        {
            float hits = 0;
            float totalDistance = 0f;
            float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));

            for (int i = 0; i < occlusionRays; i++)
            {
                float y = 1 - (i / (float)(occlusionRays - 1));
                float radius = Mathf.Sqrt(1 - y * y);
                float theta = phi * i;
                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;
                Vector3 direction = new Vector3(x, y, z);

                RaycastHit hit;
                if (Physics.Raycast(position, direction, out hit, occlusionRayMaxDistance, occlusionLayerMask))
                {
                    hits += Mathf.Min(1, occlusionReferenceDistace / hit.distance);
                    totalDistance += hit.distance;
                    Debug.DrawRay(position, direction * hit.distance, Color.red);
                }
                else
                {
                    totalDistance += occlusionRayMaxDistance;
                    Debug.DrawRay(position, direction * occlusionRayMaxDistance, Color.green);
                }
            }

            return new OcclusionInfo
            {
                occlusionFactor = hits / occlusionRays,
                averageDistance = totalDistance / occlusionRays
            };
        }




        /// <summary>
        /// Starts the coroutines to fade the weights for a given zone.
        /// </summary>
        // private void AdjustWeightsForZone(AudioZone zone, float direction)
        // {
        //     foreach (var weightedSource in zone.controlledSources)
        //     {
        //         int index = ambientSources.IndexOf(weightedSource.source);
        //         if (index != -1)
        //         {
        //             float distanceFactor = 1f;
        //             if (zone.distFade)
        //             {
        //                 float distance = Vector3.Distance(transform.position, zone.transform.position);
        //                 Debug.Log(distance);
        //                 distanceFactor = Mathf.Max(1f, distance);
        //             }
        //             targetAmbientWeights[index] = targetAmbientWeights[index] + (direction * weightedSource.weight) / (distanceFactor/8);
        //         }
        //     }
        // }

        /// <summary>
        /// Called by an AudioZone to add its weights to the master list.
        /// </summary>
        public void EnterZone(AudioZone zone)
        {
            Debug.Log($"Entering Zone: {zone.gameObject.name}");
            // AdjustWeightsForZone(zone, 1f); // Use a direction of 1 to add weights.
            activeZones.Add(zone);
        }

        /// <summary>
        /// Called by an AudioZone to subtract its weights from the master list.
        /// </summary>
        public void ExitZone(AudioZone zone)
        {
            Debug.Log($"Exiting Zone: {zone.gameObject.name}");
            // AdjustWeightsForZone(zone, -1f); // Use a direction of -1 to subtract weights.
            activeZones.Remove(zone);
        }
    }
}