using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FellOutOfMap : MonoBehaviour
{
    public UnityEvent FellOutOfMapEvent = new();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("OutOfBounds"))
            FellOutOfMapEvent.Invoke();
    }
}
