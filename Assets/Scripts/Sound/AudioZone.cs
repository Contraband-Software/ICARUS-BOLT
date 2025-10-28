// AudioZone.cs

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sound
{

    /// <summary>
    /// A helper class to associate a weight with an AudioSource.
    /// </summary>
    [System.Serializable]
    public class WeightedAudioSource
    {
        [Tooltip("The audio source to be controlled.")]
        public AudioSource source;

        [Tooltip("The target volume for this source when the zone is active. Defaults to 1.")] [Range(-1f, 1f)]
        public float weight = 1f;
    }


    /// <summary>
    /// Defines a spatial zone that controls a set of AudioSources.
    /// This component should be on a GameObject with a trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AudioZone : MonoBehaviour
    {
        [Tooltip("The audio sources that will play when the player is inside this zone.")]
        public List<WeightedAudioSource> controlledSources;

        [HideInInspector] public List<float> weights;

        [Tooltip("Determines if the influence of the zone should fade in based on proximity to zone center.")]
        public Boolean distFade = false;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object entering has an AudioZoneController
            if (other.TryGetComponent<AudioZoneController>(out var controller))
            {
                controller.EnterZone(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the object exiting has an AudioZoneController
            if (other.TryGetComponent<AudioZoneController>(out var controller))
            {
                controller.ExitZone(this);
            }
        }

        private void OnDrawGizmos()
        {
            // Ensure the script can find the collider component.
            if (TryGetComponent<Collider>(out var collider))
            {
                // Set the color for the gizmo.
                Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f); // A nice light blue with transparency

                // Draw a wireframe cube that matches the collider's size and position.
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }

        // Optional: You can use OnDrawGizmosSelected to change the color when the object is selected.
        private void OnDrawGizmosSelected()
        {
            if (TryGetComponent<Collider>(out var collider))
            {
                // Set a different, more prominent color for when the zone is selected.
                Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // A bright yellow

                // Draw the same wireframe cube.
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }

        /// <summary>
        /// Fades in all controlled audio sources for this zone.
        /// </summary>
        // public void ActivateZone()
        // {
        //     StopAllCoroutines();
        //     foreach (var source in controlledSources)
        //     {
        //         if (!source.source.isPlaying)
        //         {
        //             source.source.Play();
        //         }
        //         StartCoroutine(FadeSource(source.source, source.source.volume, 1f, fadeDuration));
        //     }
        // }
        //
        // /// <summary>
        // /// Fades out all controlled audio sources for this zone.
        // /// </summary>
        // public void DeactivateZone()
        // {
        //     StopAllCoroutines();
        //     foreach (var source in controlledSources)
        //     {
        //         StartCoroutine(FadeSource(source.source, source.source.volume, 0f, fadeDuration));
        //     }
        // }
        //
        // /// <summary>
        // /// A coroutine to smoothly fade an AudioSource's volume.
        // /// </summary>
        // private IEnumerator FadeSource(AudioSource source, float startVolume, float endVolume, float duration)
        // {
        //     float time = 0f;
        //     
        //     // We temporarily set the volume to the startVolume in case a previous fade was interrupted.
        //     source.volume = startVolume;
        //
        //     while (time < duration)
        //     {
        //         source.volume = Mathf.Lerp(startVolume, endVolume, time / duration);
        //         time += Time.deltaTime;
        //         yield return null;
        //     }
        //
        //     source.volume = endVolume;
        //
        //     if (endVolume == 0f)
        //     {
        //         source.Stop();
        //     }
        // }
    }
}