using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Resources.System;
using UnityEngine;
using UnityEngine.Events;

namespace Player
{
    public class PlayerGameOver : MonoBehaviour
    {
        
        [SerializeField, Min(0)] private float deathVelocity = 200f;
        [SerializeField, Min(0)] private float armourMultiplier = 2;
        [SerializeField, Min(0)] private float winDistanceThreshold = 20f;
        [SerializeField] private bool infiniteFuel = false;
        
        [field: SerializeField] public UnityEvent<string> OnPlayerDeath { get; private set; } = new();
        
        [Header("Game Over Messages")]
        [SerializeField] private string velocity = "You collided too fast with that surface, and blew yourself to pieces...";
        [SerializeField] private string fuel = "As you ran one tank empty, you doomed yourself to joining the Derelicts...";
        
        private GameObject winArea;
    
        private void Awake()
        {
            OnPlayerDeath.AddListener(cause =>
            {
                
            });

            winArea = GameObject.FindGameObjectWithTag("Finish");
        }

        private void Update()
        {
            if (winArea is not null)
                if (Vector3.Distance(winArea.transform.position, transform.position) < winDistanceThreshold)
                {

                }
                    
        }

        private void OnCollisionEnter(Collision collision)
        {
        }

        public void HandleFuelRanOut()
        {
            if (!infiniteFuel) OnPlayerDeath.Invoke(fuel);
        }
    }
}