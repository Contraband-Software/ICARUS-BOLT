using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using Sound;

namespace Player
{
    /// <summary>
    /// Defines the different types of surfaces a player can interact with.
    /// Used to determine which footstep or landing sound to play.
    /// </summary>
    public enum SurfaceType
    {
        Default,
        Dirt,
        Grass,
        Metal,
        Stone
    }

    /// <summary>
    /// A helper class to associate a surface type with an audio clip
    /// </summary>
    [System.Serializable]
    public class SurfaceTypeClip
    {
        [Tooltip("The audio clip to be played.")]
        public AudioClip clip;

        [Tooltip("The surface type this clip is played on.")]
        public SurfaceType surfaceType;
    }
    
    /// <summary>
    /// A helper class to associate a string tag with a SurfaceType enum.
    /// This allows for easy mapping in the Unity Inspector.
    /// </summary>
    [System.Serializable]
    public class TagToSurface
    {
        [Tooltip("The string of the tag used on ground GameObjects (e.g., 'Surface_Wood').")]
        public string tag;
        [Tooltip("The surface type that corresponds to this tag.")]
        public SurfaceType surfaceType;
    }


    /// <summary>
    /// Manages all sound effects for the player character.
    /// This component should be placed on the player GameObject.
    /// In the inspector, you need to create and assign separate AudioSource components for each sound type.
    /// </summary>
    public class PlayerSoundController : MonoBehaviour
    {
        public Transform surfaceTypeSensor;
        
        [Header("Audio Sources")] [Tooltip("The AudioSources for player sounds.")]
        public AudioSource playerSource;
        public AudioSource chargeSource;


        [Tooltip(
            "The AudioSource for a continuous sound, like gliding or a jetpack. Ensure 'Loop' is checked on the AudioSource itself.")]
        public AudioSource loopingSource;
        
        [Header("Tag To Surface Configuration")]
        [Tooltip("Map your GameObject tags to SurfaceType enums here.")]
        public TagToSurface[] surfaceMappings;
        
        [Header("launch Clips")]
        public AudioClip[] launchClips;
        
        [Header("Footstep servo Clips")]
        [Tooltip("An array of footstep servo sound clips. A random one will be played each time to add variety.")]
        public AudioClip[] servoClips;
        
        [Header("Footstep Clips")]
        [Tooltip("An array of footstep sound clips. A random one will be played each time to add variety.")]
        public SurfaceTypeClip[] footstepClips;
        
        // This dictionary will be built at startup for fast lookups
        private Dictionary<string, SurfaceType> _tagToSurfaceMap;
        private Dictionary<SurfaceType, List<AudioClip>> _footstepClipMap;

        public SurfaceType currentSurface;

        private void Awake()
        {
            // Build the dictionaries for fast lookups
            // This is more efficient than searching arrays every time a sound plays.
            _tagToSurfaceMap = new Dictionary<string, SurfaceType>();
            foreach (var mapping in surfaceMappings)
            {
                if (!_tagToSurfaceMap.ContainsKey(mapping.tag))
                {
                    _tagToSurfaceMap.Add(mapping.tag, mapping.surfaceType);
                }
            }
            
            // Build the footstep clip dictionary, grouping clips by surface type.
            _footstepClipMap = new Dictionary<SurfaceType, List<AudioClip>>();
            foreach (var sfc in footstepClips)
            {
                // If this surface type isn't in the dictionary yet, add it with a new list.
                if (!_footstepClipMap.ContainsKey(sfc.surfaceType))
                {
                    _footstepClipMap[sfc.surfaceType] = new List<AudioClip>();
                }
                // Add the clip to the list for the corresponding surface type.
                if (sfc.clip != null)
                {
                    _footstepClipMap[sfc.surfaceType].Add(sfc.clip);
                }
            }
        }

        private void Update()
        {
            currentSurface = GetCurrentSurfaceType();
        }

        /// <summary>
        /// Fires a raycast downwards to determine the type of surface the player is currently over.
        /// </summary>
        /// <returns>The SurfaceType of the ground, or SurfaceType.Default if nothing is detected.</returns>
        private SurfaceType GetCurrentSurfaceType()
        {
            if (surfaceTypeSensor == null) return SurfaceType.Default;

            // Fire a raycast down from the sensor's position.
            if (Physics.Raycast(surfaceTypeSensor.position, Vector3.down, out RaycastHit hit, 10, ~LayerMask.GetMask("Player")))
            {
                Debug.DrawRay(surfaceTypeSensor.position, Vector3.down * 10, Color.green);
                // Debug.Log($"Raycast hit: {hit.collider.name} with tag: {hit.collider.tag}");
                // If we hit a collider, look up its tag in our dictionary.
                return GetSurfaceTypeFromTag(hit.collider.tag);
            }

            Debug.DrawRay(surfaceTypeSensor.position, Vector3.down * 10, Color.red);
            // If the raycast doesn't hit anything within the distance/layer, it's like being in the air.
            return SurfaceType.Default; 
        }
        private SurfaceType GetSurfaceTypeFromTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && _tagToSurfaceMap.TryGetValue(tag, out SurfaceType surfaceType))
            {
                return surfaceType;
            }
            // If the tag doesn't exist in our dictionary, return the default type.
            return SurfaceType.Default;
        }




        /// <summary>
        /// Detects the current surface and plays a random footstep sound from the appropriate clip group.
        /// </summary>
        public void PlayFootstepSound()
        {
            AudioClip servoClip = servoClips[UnityEngine.Random.Range(0, servoClips.Length)];
            playerSource.PlayOneShot(servoClip,0.14f);
            SurfaceType surface = GetCurrentSurfaceType();
            // Try to get the list of clips for the detected surface.
            if (_footstepClipMap.TryGetValue(surface, out List<AudioClip> clips) && clips.Count > 0)
            {
                // Pick a random clip from the list and play it.
                AudioClip clipToPlay = clips[UnityEngine.Random.Range(0, clips.Count)];
                StartCoroutine(PlayDelayed(clipToPlay,0.1f,0.5f));
            }
            // If no specific clip is found, fall back to the 'Default' clips.
            else if (_footstepClipMap.TryGetValue(SurfaceType.Default, out List<AudioClip> defaultClips) && defaultClips.Count > 0)
            {
                // Pick a random clip from the default list.
                AudioClip clipToPlay = defaultClips[UnityEngine.Random.Range(0, defaultClips.Count)];
                StartCoroutine(PlayDelayed(clipToPlay,0.1f,0.5f));
            }
        }

        /// <summary>
        /// Coroutine that waits for a delay and then plays a one-shot sound.
        /// </summary>
        private IEnumerator PlayDelayed(AudioClip clip, float delay, float volumeScale)
        {
            yield return new WaitForSeconds(delay);
            playerSource.PlayOneShot(clip,volumeScale);
        }


        /// <summary>
        /// Plays the jump sound.
        /// Call this when the player jumps.
        /// </summary>
        public void PlayJumpSound()
        {
            if (playerSource != null && playerSource.clip != null)
            {
                playerSource.Play();
            }
            else
            {
                Debug.LogWarning("Jump source or its clip is not assigned in PlayerSoundController.");
            }
        }

        /// <summary>
        /// Plays the landing sound.
        /// Call this when the player lands on the ground.
        /// </summary>
        public void PlayLandSound()
        {
            if (playerSource != null && playerSource.clip != null)
            {
                playerSource.Play();
            }
            else
            {
                Debug.LogWarning("Land source or its clip is not assigned in PlayerSoundController.");
            }
        }

        public void startChageSound()
        {
            chargeSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            chargeSource.Play();
        }
        public void stopChageSound(float charge)
        {
            chargeSource.Stop();
            AudioClip launchClip = launchClips[UnityEngine.Random.Range(0, servoClips.Length)];
            chargeSource.pitch = 3f - charge/0.7f;
            chargeSource.PlayOneShot(launchClip,0.2f+ 1f*Mathf.Pow(charge,0.7f));
        }

        /// <summary>
        /// Starts playing a continuous looping sound (e.g., for gliding).
        /// Ensure the looping source has a clip assigned and 'Loop' is checked in the inspector.
        /// </summary>
        public void StartLoopingSound()
        {
            if (loopingSource != null && !loopingSource.isPlaying)
            {
                loopingSource.Play();
            }
        }

        /// <summary>
        /// Stops the continuous looping sound.
        /// </summary>
        public void StopLoopingSound()
        {
            if (loopingSource != null && loopingSource.isPlaying)
            {
                loopingSource.Stop();
            }
        }
    }
}