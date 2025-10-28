using System;
using System.Collections;
using System.Collections.Generic;
using Resources;
using TMPro;
using UnityEngine;

[
    RequireComponent(typeof(TextMeshProUGUI))
]
public class FuelLevelReadout : MonoBehaviour
{
    [SerializeField] private Fuel playerFuel;
    [SerializeField] private Fuel.Tank fuelType;
    
    private TextMeshProUGUI readout;
    
    void Start()
    {
        readout = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        readout.text = "Fuel Level: " + ((int)playerFuel.GetLevel(fuelType)) + "%";
    }
}
