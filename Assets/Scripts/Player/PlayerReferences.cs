
using System;
using Player;
using Resources;
using Resources.Modules;
using Resources.System;
using UnityEngine;

public static class PlayerReferences
{
    // reference cache for player systems
    
    private static GameObject player;
    public static GameObject GetGameObject()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
#if UNITY_EDITOR
        if (player is null)
            throw new InvalidOperationException("No GameObject with tag 'Player'");
#endif
        return player;
    }
    
    private static PlayerController controller;
    public static PlayerController GetControllerInstance()
    {
        return GetGameObject().GetComponent<PlayerController>();
    }
    
    private static Fuel fuel;
    public static Fuel GetFuelInstance()
    {
        return GetGameObject().GetComponent<Fuel>();
    }
}