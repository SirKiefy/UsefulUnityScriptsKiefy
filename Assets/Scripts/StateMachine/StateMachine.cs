using System;
using System.Collections.Generic;

namespace UsefulScripts.StateMachine
{
    /// <summary>
    /// Generic finite state machine implementation.
    /// </summary>
    /// <typeparam name="T">The context type (usually the class that owns this state machine)</typeparam>
    public class StateMachine<T>
    {
        private T context;
        private IState<T> currentState;
        private IState<T> previousState;
        private Dictionary<Type, IState<T>> states = new Dictionary<Type, IState<T>>();
        private float stateTime;
        private bool isInitialized;

        // Events
        public event Action<IState<T>, IState<T>> OnStateChanged;

        // Properties
        public IState<T> CurrentState => currentState;
        public IState<T> PreviousState => previousState;
        public float StateTime => stateTime;
        public bool IsInitialized => isInitialized;

        public StateMachine(T context)
        {
            this.context = context;
        }

        /// <summary>
        /// Add a state to the state machine
        /// </summary>
        public void AddState(IState<T> state)
        {
            var type = state.GetType();
            if (!states.ContainsKey(type))
            {
                states[type] = state;
                state.Initialize(context);
            }
        }

        /// <summary>
        /// Add multiple states
        /// </summary>
        public void AddStates(params IState<T>[] newStates)
        {
            foreach (var state in newStates)
            {
                AddState(state);
            }
        }

        /// <summary>
        /// Set the initial state
        /// </summary>
        public void SetInitialState<TState>() where TState : IState<T>
        {
            if (states.TryGetValue(typeof(TState), out var state))
            {
                currentState = state;
                currentState.Enter();
                stateTime = 0;
                isInitialized = true;
            }
            else
            {
                throw new Exception($"State {typeof(TState).Name} not found in state machine!");
            }
        }

        /// <summary>
        /// Change to a new state
        /// </summary>
        public void ChangeState<TState>() where TState : IState<T>
        {
            if (!isInitialized)
            {
                SetInitialState<TState>();
                return;
            }

            if (states.TryGetValue(typeof(TState), out var newState))
            {
                if (currentState == newState) return;

                previousState = currentState;
                currentState?.Exit();
                
                currentState = newState;
                stateTime = 0;
                currentState.Enter();

                OnStateChanged?.Invoke(previousState, currentState);
            }
            else
            {
                throw new Exception($"State {typeof(TState).Name} not found in state machine!");
            }
        }

        /// <summary>
        /// Return to the previous state
        /// </summary>
        public void RevertToPreviousState()
        {
            if (previousState != null)
            {
                var temp = currentState;
                currentState?.Exit();
                currentState = previousState;
                previousState = temp;
                stateTime = 0;
                currentState.Enter();

                OnStateChanged?.Invoke(previousState, currentState);
            }
        }

        /// <summary>
        /// Update the current state (call in Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!isInitialized || currentState == null) return;

            stateTime += deltaTime;
            currentState.Update(deltaTime);
        }

        /// <summary>
        /// Fixed update the current state (call in FixedUpdate)
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!isInitialized || currentState == null) return;

            currentState.FixedUpdate(fixedDeltaTime);
        }

        /// <summary>
        /// Check if currently in a specific state
        /// </summary>
        public bool IsInState<TState>() where TState : IState<T>
        {
            return currentState != null && currentState.GetType() == typeof(TState);
        }

        /// <summary>
        /// Get a state by type
        /// </summary>
        public TState GetState<TState>() where TState : IState<T>
        {
            if (states.TryGetValue(typeof(TState), out var state))
            {
                return (TState)state;
            }
            return default;
        }
    }

    /// <summary>
    /// State interface
    /// </summary>
    public interface IState<T>
    {
        void Initialize(T context);
        void Enter();
        void Update(float deltaTime);
        void FixedUpdate(float fixedDeltaTime);
        void Exit();
    }

    /// <summary>
    /// Base state class with virtual methods
    /// </summary>
    public abstract class State<T> : IState<T>
    {
        protected T context;

        public virtual void Initialize(T context)
        {
            this.context = context;
        }

        public virtual void Enter() { }
        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate(float fixedDeltaTime) { }
        public virtual void Exit() { }
    }
}
