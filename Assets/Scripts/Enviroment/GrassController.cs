using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class GrassController : MonoBehaviour
{
    public Rigidbody rb;
    public Material grassMaterial;
    public float heightOffset = 0;
    public AnimationCurve velocityCurve;
    public float grassSpringStrength = 10;
    public float grassSpringDamping = 0.95f;
    private Vector3 springVelocity = Vector3.zero;
    private Vector3 springPos = Vector3.zero;
    void Start()
    {
        
    }

    void Update()
    {   
        springVelocity += rb.velocity * Time.deltaTime;
        springPos += springVelocity * Time.deltaTime;
        springVelocity -= springPos * grassSpringStrength * Time.deltaTime;
        springVelocity*= grassSpringDamping;
        grassMaterial.SetVector("_PlayerLoc", rb.position- new Vector3(0,heightOffset,0));
        grassMaterial.SetVector("_PlayerVel", springVelocity);
        grassMaterial.SetFloat("_PlayerBendFactor", velocityCurve.Evaluate(springVelocity.magnitude));
    }
}
