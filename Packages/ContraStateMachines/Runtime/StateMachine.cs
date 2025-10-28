using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Software.Contraband.StateMachines
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class StateMachine<T> where T : BaseState
    {
        public Dictionary<Type, T> States { get; internal set; } = new();
        
        internal T DefaultState => States.Values.First(
            s => s.GetType()
                .GetCustomAttributes(typeof(DefaultStateAttribute), true).Length > 0);
        
        public Type StartState => DefaultState.GetType();
        
        internal T CurrentState { get; private set; }
        
        public void Start()
        {
            Initialize();

            // Starting state
            CurrentState = DefaultState;
            CurrentState.EnterState();
        }
        
        public Type GetCurrentState() => CurrentState.GetType();

        protected virtual void Initialize() { }

        public void SwitchState(T newState)
        {
            SwitchStateImpl(newState);
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual void SwitchStateImpl(T newState)
        {
            CurrentState.ExitState();
            CurrentState = newState;
            CurrentState.EnterState();
        }
    }
}