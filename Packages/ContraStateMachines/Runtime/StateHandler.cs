using UnityEngine;
using System;
using Assembly = System.Reflection.Assembly;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Software.Contraband.StateMachines
{
    /// <summary>
    /// A StateHandler that handles states of a certain type inherited from BaseState
    /// </summary>
    public abstract class StateHandler<T> : MonoBehaviour where T : BaseState
    {
        [field: SerializeField] public bool DebugStates { get; private set; } = false;

        private readonly StateMachine<T> stateMachine = new();

        protected T CurrentState => stateMachine.CurrentState;

        public Dictionary<Type, T> States
        {
            get => stateMachine.States;
            protected set => stateMachine.States = value;
        }

        public void LoadStateTypes()
        {
            stateMachine.States.Clear();
            
            Assembly statesAssembly = Assembly.GetAssembly(typeof(T));
            var states = statesAssembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(T)));
            
            foreach (var type in states)
            {
                T s = Activator.CreateInstance(type, new object[] { this }) as T;
                stateMachine.States.Add(s.GetType(), s);
            }
        }

        private void Awake()
        {
            LoadStateTypes();
        }

        private void Start()
        {
            Initialize();
            DebugOutput(stateMachine.StartState);
            stateMachine.Start();
        }

        protected virtual void Initialize() { }

        public void SwitchState(T newState)
        {
            DebugOutput(newState.GetType());
            
            //CurrentState.ExitState();
            SwitchStateImpl(newState);
            //CurrentState.EnterState();
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual void SwitchStateImpl(T newState) =>
            stateMachine.SwitchState(newState);

        private void DebugOutput(Type newState)
        {
#if UNITY_EDITOR
            if (DebugStates)
                Debug.Log(
                    $"{GetType().Name} => {newState.Name}" + 
                    $"{(stateMachine.DefaultState.GetType() == newState ? " [Default]" : "")}");
#endif
        }
    }
}