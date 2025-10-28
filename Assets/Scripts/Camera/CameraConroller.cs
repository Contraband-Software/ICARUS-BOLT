using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camera
{
    public class CameraConroller : MonoBehaviour
    {
        [Header("Focal Point References")]
        public GameObject player;

        [Header("Player Camera Settings")]
        public Vector3 baseOffset = new Vector3(0f, 5.5f, -7.5f);
        public Vector3 maxLookDownOffset;
        public Vector3 maxLookUpOffset;
        public float maxTilt;
        public float minTilt;
        [Range(0, 20)] public float positionSmoothFactor = 10f;
        [Range(0, 20)] public float rotationSmoothFactor = 10f;

        [Header("State")]
        [SerializeField] private CameraStateHandler stateHandler;
        void Start()
        {

        }

        void Update()
        {

        }
    }
}
