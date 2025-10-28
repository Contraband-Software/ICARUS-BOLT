using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IKElementHasValue : IKElement
{
    protected enum ValueMode
    {
        NONE,
        NEW_WORLDSPACE,
        OFFSET_WORLDSPACE,
        NEW_LOCALSPACE,
        OFFSET_LOCALSPACE
    }

    protected ValueMode m_position = ValueMode.NONE;
    protected ValueMode m_rotation = ValueMode.NONE;

    protected Vector3 v_position;
    protected Quaternion v_rotation;

    private void SetValuePosition(Vector3 p)
    {
        v_position = p;
    }
    private void SetValueRotation(Quaternion r)
    {
        v_rotation = r;
    }
    private void SetModePosition(ValueMode mode_p)
    {
        m_position = mode_p;
    }
    private void SetModeRotation(ValueMode mode_r)
    {
        m_rotation = mode_r;
    }

    public Vector3 GetPositionValue()
    {
        return v_position;
    }
    public Quaternion GetRotationValue()
    {
        return v_rotation;
    }

    public void DisregardPositionValue()
    {
        SetModePosition(ValueMode.NONE);
    }
    public void DisregardRotationValue()
    {
        SetModeRotation(ValueMode.NONE);
    }
    //-- WORLDSPACE OFFSET

    /// <summary>
    /// Set positional repose by providing an offset in worldspace
    /// </summary>
    /// <param name="worldspaceOffset_p"></param>
    public void SetValuePositionAsWorldspaceOffset(Vector3 worldspaceOffset_p)
    {
        SetModePosition(ValueMode.OFFSET_WORLDSPACE);
        SetValuePosition(worldspaceOffset_p);
    }
    /// <summary>
    /// Set rotational repose by providing an offset in worldspace
    /// </summary>
    /// <param name="worldspaceOffset_r"></param>
    public void SetValueRotationAsWorldspaceOffset(Quaternion worldspaceOffset_r)
    {
        SetModeRotation(ValueMode.OFFSET_WORLDSPACE);
        SetValueRotation(worldspaceOffset_r);
    }

    //-- NEW WORLDSPACE

    /// <summary>
    /// Set positional repose by providing a new world space position
    /// </summary>
    /// <param name="newWorldspace_p"></param>
    public void SetValuePositionAsNewWorldspace(Vector3 newWorldspace_p)
    {
        SetModePosition(ValueMode.NEW_WORLDSPACE);
        SetValuePosition(newWorldspace_p);
    }
    /// <summary>
    /// Set rotational repose by providing a new world space position
    /// </summary>
    /// <param name="newWorldspace_r"></param>
    public void SetValueRotationAsNewWorldspace(Quaternion newWorldspace_r)
    {
        SetModeRotation(ValueMode.NEW_WORLDSPACE);
        SetValueRotation(newWorldspace_r);
    }

    //-- LOCAL OFFSET

    /// <summary>
    /// Set positional repose by providing an offset in localspace
    /// </summary>
    /// <param name="localOffset_p"></param>
    public void SetValuePositionAsLocalOffset(Vector3 localOffset_p)
    {
        SetModePosition(ValueMode.OFFSET_LOCALSPACE);
        SetValuePosition(localOffset_p);
    }
    /// <summary>
    /// Set rotational repose by providing an offset in localspace
    /// </summary>
    /// <param name="localOffset_r"></param>
    public void SetValueRotationAsLocalOffset(Quaternion localOffset_r)
    {
        SetModeRotation(ValueMode.OFFSET_LOCALSPACE);
        SetValueRotation(localOffset_r);
    }

    //-- NEW LOCAL

    /// <summary>
    /// Set positional repose by providing a new local space position
    /// </summary>
    /// <param name="newLocal_p"></param>
    public void SetValuePositionAsNewLocal(Vector3 newLocal_p)
    {
        SetModePosition(ValueMode.NEW_LOCALSPACE);
        SetValuePosition(newLocal_p);
    }
    /// <summary>
    /// Set rotational repose by providing a new local space position
    /// </summary>
    /// <param name="newLocal_r"></param>
    public void SetValueRotationAsNewLocal(Quaternion newLocal_r)
    {
        SetModeRotation(ValueMode.NEW_LOCALSPACE);
        SetValueRotation(newLocal_r);
    }
}
