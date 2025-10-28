using UnityEngine;

public interface IStateCollisionHandler
{
    void OnTriggerEnter2D(Collider2D collision);

    void OnCollisionEnter2D(Collision2D collision);
}
