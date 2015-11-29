﻿using Hedgehog.Core.Actors;
using UnityEngine;
using UnityEngine.Events;

namespace Hedgehog.Core.Moves
{
    public class Move : MonoBehaviour
    {
        /// <summary>
        /// Reference to the attached controller.
        /// </summary>
        protected HedgehogController Controller;

        /// <summary>
        /// Reference to the associated move manager.
        /// </summary>
        protected MoveManager Manager;

        /// <summary>
        /// Reference to the attached animator.
        /// </summary>
        protected Animator Animator;

        /// <summary>
        /// The move's current state.
        /// </summary>
        public State CurrentState;
        public enum State   
        {
            Unavailable,    // Can't be performed unless forced to.
            Available,      // Can be performed through player input.
            Active,         // Currently being performed.
        }

        /// <summary>
        /// Whether the move is currently active.
        /// </summary>
        public bool Active
        {
            get { return CurrentState == State.Active; }
            set { ChangeState(value ? State.Active : CurrentState == State.Active ? State.Available : CurrentState); }

        }

        /// <summary>
        /// If the move is active, whether it was activated through player input.
        /// </summary>
        public bool InputActivated;

        /// <summary>
        /// Whether the move can be activated through input.
        /// </summary>
        [Tooltip("Whether the move can be activated through input.")]
        public bool InputEnabled;
        #region Events
        /// <summary>
        /// Invoked when the move is performed.
        /// </summary>
        public UnityEvent OnActive;

        /// <summary>
        /// Invoked when the move is ended.
        /// </summary>
        public UnityEvent OnEnd;

        /// <summary>
        /// Invoked when the move becomes available.
        /// </summary>
        public UnityEvent OnAvailable;

        /// <summary>
        /// Invoked when the move becomes unavailable.
        /// </summary>
        public UnityEvent OnUnavailable;
        #endregion
        #region Animation
        /// <summary>
        /// Name of an Animator trigger set when the move is activated.
        /// </summary>
        [Tooltip("Name of an Animator trigger set when the move is activated.")]
        public string ActiveTrigger;
        protected int ActiveTriggerHash;

        /// <summary>
        /// Name of an Animator bool set to whether the move is active.
        /// </summary>
        [Tooltip("Name of an Animator bool set to whether the move is active.")]
        public string ActiveBool;
        protected int ActiveBoolHash;

        /// <summary>
        /// Name of an Animator bool set to whether the move is available.
        /// </summary>
        [Tooltip("Name of an Animator bool set to whether the move is available.")]
        public string AvailableBool;
        protected int AvailableBoolHash;
        #endregion

        public virtual void Reset()
        {
            ActiveTrigger = ActiveBool = AvailableBool = "";
        }

        public virtual void Awake()
        {
            Controller = Controller ?? GetComponentInParent<HedgehogController>();
            Animator = Controller.Animator;
            Manager = Controller.MoveManager;

            CurrentState = State.Unavailable;
            InputActivated = false;
            InputEnabled = true;

            ActiveTriggerHash = ActiveTrigger == null ? 0 : Animator.StringToHash(ActiveTrigger);
            ActiveBoolHash = ActiveBool == null ? 0 : Animator.StringToHash(ActiveBool);
            AvailableBoolHash = AvailableBool == null ? 0 : Animator.StringToHash(AvailableBool);

            OnActive = OnActive ?? new UnityEvent();
            OnEnd = OnEnd ?? new UnityEvent();
            OnAvailable = OnAvailable ?? new UnityEvent();
            OnUnavailable = OnUnavailable ?? new UnityEvent();
        }

        public virtual void OnEnable()
        {
            // I'm here for consistency!
        }

        public virtual void OnDisable()
        {
            End();
        }

        public virtual void Start()
        {
            Animator = Controller.Animator;
        }

        public virtual void Update()
        {
            if(Animator != null)
                SetAnimatorParameters();
        }

        /// <summary>
        /// Set animator parameters here. Called only if the object has an Animator component.
        /// </summary>
        public virtual void SetAnimatorParameters()
        {
            if(ActiveBoolHash != 0)
                Animator.SetBool(ActiveBoolHash, CurrentState == State.Active);

            if(AvailableBoolHash != 0)
                Animator.SetBool(AvailableBoolHash, CurrentState == State.Available);
        }

        /// <summary>
        /// Changes the move's state to the one specified.
        /// </summary>
        /// <param name="nextState">The specified state.</param>
        /// <returns>Whether the move's state is different from its previous.</returns>
        public bool ChangeState(State nextState)
        {
            if (nextState == CurrentState) return false;

            var prevState = CurrentState;
            CurrentState = nextState;
            OnStateChanged(prevState);

            if (prevState == State.Active)
            {
                OnActiveExit();
                OnEnd.Invoke();
            }
            else if (CurrentState == State.Active)
            {
                OnActiveEnter(prevState);
                OnActive.Invoke();
            }
            else if (CurrentState == State.Available)
            {
                OnAvailable.Invoke();
            }
            else if (CurrentState == State.Unavailable)
            {
                OnUnavailable.Invoke();
            }

            if (Animator == null)
                return true;

            if (CurrentState == State.Active && ActiveTriggerHash != 0)
                Animator.SetTrigger(ActiveTriggerHash);

            return true;
        }

        /// <summary>
        /// Calls on the controller to perform the move. Works only if the move is available.
        /// </summary>
        /// <returns>Whether the move was performed.</returns>
        public bool Perform(bool force = false)
        {
            return Manager.Perform(this, force);
        }

        /// <summary>
        /// Calls on the controller to end the move. Only works if the move is active.
        /// </summary>
        /// <returns>Whether the move was ended.</returns>
        public bool End()
        {
            return Manager.End(this);
        }

        /// <summary>
        /// Let the controller know your move can be triggered through input here.
        /// </summary>
        /// <returns></returns>
        public virtual bool Available()
        {
            return true;
        }

        /// <summary>
        /// Let the controller know your move should be activated based on the current input here.
        /// </summary>
        /// <returns></returns>
        public virtual bool InputActivate()
        {
            return false;
        }

        /// <summary>
        /// Let the controller know your move should be deactivated based on the current conditions here.
        /// </summary>
        /// <returns></returns>
        public virtual bool InputDeactivate()
        {
            return false;
        }
        
        /// <summary>
        /// Called when the move's state changes.
        /// </summary>
        /// <param name="previousState">The move's previous state.</param>
        public virtual void OnStateChanged(State previousState)
        {

        }

        /// <summary>
        /// Called when the move is activated.
        /// </summary>
        public virtual void OnActiveEnter(State previousState)
        {

        }

        /// <summary>
        /// Called on Update while the move is activated.
        /// </summary>
        public virtual void OnActiveUpdate()
        {

        }

        /// <summary>
        /// Called on FixedUpdate while the move is activated.
        /// </summary>
        public virtual void OnActiveFixedUpdate()
        {

        }

        /// <summary>
        /// Called when the move is deactivated.
        /// </summary>
        public virtual void OnActiveExit()
        {

        }
    }
}