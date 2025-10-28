using UnityEngine;
using System;

namespace Software.Contraband.StateMachines
{
    [Flags]
    public enum StateType
    {
        Generic = 0,
        Default = 1
    }

    public abstract class BaseState
    {
        // protected internal virtual StateType GetStateInfo => StateType.Generic;

        /// <summary>
        /// What you do when you first transition to this state
        /// </summary>
        /// <param name="stateHandler"></param>
        protected internal virtual void EnterState() { }

        /// <summary>
        /// What you do right before switching to another state
        /// </summary>
        /// <param name="stateHandler"></param>
        protected internal virtual void ExitState() { }
    }
}