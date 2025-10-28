using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    public float speed = 1f;
    public float magnitude = 1f;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.position += transform.up * (float)Math.Sin(speed * Time.fixedTime)*magnitude;
    }

    void Update()
    {
        
    }
}
