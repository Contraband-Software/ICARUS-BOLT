using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IKElementHasTarget : IKElement
{
    public Transform target;

    /// <summary>
    /// Sets the target based on its own local space (child of IKElement object)
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    public void SetTargetPositionByLocalDirection(Vector3 direction, float distance)
    {
        target.localPosition = direction * distance;
    }

    public void SetTargetPositionByLocalPosition(Vector3 position)
    {
        target.localPosition = position;
    }

    /// <summary>
    /// Sets the target by offsetting from its current world position, in a world direction
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public Vector3 SetTargetPositionByWorldDirection(Vector3 direction, float distance)
    {
        target.localPosition = Vector3.zero;
        Vector3 newWorldPosition = target.position + (direction.normalized * distance);
        target.position = newWorldPosition;
        return newWorldPosition;
    }

    /// <summary>
    /// Sets the target by providing a new world space position
    /// </summary>
    /// <param name="position"></param>
    public void SetTargetPositionByWorldPosition(Vector3 position)
    {
        target.position = position;
    }
}
