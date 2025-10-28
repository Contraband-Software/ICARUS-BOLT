using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        public GameObject player;

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
