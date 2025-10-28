using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Serialization;

namespace Software.Contraband.Inventory
{
    [
        RequireComponent(typeof(RectTransform)), 
        RequireComponent(typeof(CanvasGroup)),
        SelectionBase
    ]
    public sealed class Item :  MonoBehaviour, 
        IPointerDownHandler, IPointerUpHandler, 
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        //Settings
        [Header("Settings"), Space(10)]
        
        [Min(50), Tooltip(
             "(Immutable at runtime) The time in milliseconds to wait " +
             "between handling click events, lower means more instability")]
        [SerializeField] private int ClickLockingTime = 1000;
        
        [Range(0, 1), Tooltip(
             "The speed at which a floating (user has stopped dragging it) " +
             "item travels to its target slot. 1 is instant")]
        [SerializeField] private float PingTravelSpeed = 0.1f;
        
        [Min(1), Tooltip(
             "The distance between an item and its target slot at which it snaps " +
             "into the target slot and ceases floating")]
        [SerializeField] private float FlyingItemSnapThreshold = 6.0f;

        [Tooltip("Only allow the left click for dragging items, can sometimes get weird if disabled")]
        [SerializeField] private bool LimitDragClick = true;

        //Options
        [Serializable]
        public class OptionalAttr
        {
            public bool Stackable = false;
            public float MaximumAmount = 1;
        }
        // [Header("Game Options")]
        // [Tooltip("The type of the item, such as 'wooden-block'")]
        //public string ItemTypeIdentifier = "Default";
        //[Tooltip("If the item is to be stackable, it must have a defined unique identifier.")]
        //public OptionalAttr StackOptions;
        
        //Events
        [Header("Events"), Space(10)]
        
        [FormerlySerializedAs("event_Unslotted")]
        //the object has been removed from its slot
        public UnityEvent eventUnslotted = new UnityEvent();
        //The object was sent back to its original slot
        [FormerlySerializedAs("event_Reslotted")] public UnityEvent eventReslotted = new UnityEvent();
        //the object is in a new slot
        [FormerlySerializedAs("event_Slotted")] public UnityEvent eventSlotted = new UnityEvent();

        //State
        #region Public State
        public Canvas Canvas { get; internal set; }
        
        public Slot Slot { get; private set; } = null;
        public Slot PreviousSlot { get; private set; } = null;
        /// <summary>
        /// Only difference between this and GetPreviousSlot() is that it is set to GetCurrentSlot() after it
        /// has settled in a slot, useful for custom slot behaviours who want to check if an item was from the same
        /// container as that slot.
        /// </summary>
        /// <returns></returns>
        public Slot PreviousFloatSlot { get; private set; } = null;
        
        /// <summary>
        /// Returns if the item is visually set in its slot.
        /// </summary>
        public bool NotVisuallyInSlot => isBeingDragged | isFlying;
        #endregion
        
        private RectTransform rectTransform;
        private CanvasGroup cg;

        private Vector2 desiredPosition;

        private bool isBeingDragged = false;
        private bool isFlying = false;

        //Event locking
        private bool buttonLocked;
        private System.Timers.Timer timer;
        private void ClickLockResetCallback(object source, System.Timers.ElapsedEventArgs e)
        {
            buttonLocked = false;
            timer.Enabled = false;
        }

        #region Unity Callbacks
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            cg = GetComponent<CanvasGroup>();

            timer = new System.Timers.Timer(ClickLockingTime);
            timer.Elapsed += ClickLockResetCallback;
            gameObject.tag = "InventorySystemItem";

            desiredPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            Canvas = GetComponentInParent<InventoryContainersManager>().Canvas;
        }
        #endregion

        /// <summary>
        /// Just spawns an item in a slot
        /// </summary>
        /// <param name="newSlot"></param>
        internal void SpawnInSlot(Slot newSlot)
        {
            SetPreviousSlot(newSlot);
            Slot = newSlot;

            //MoveToPosition(newSlot.rectTransform.anchoredPosition);

            rectTransform.anchoredPosition = newSlot.RectTransform.anchoredPosition;
            desiredPosition = newSlot.RectTransform.anchoredPosition;
        }
        
        private void SetPreviousSlot(Slot newSlot)
        {
            PreviousSlot = newSlot;
            PreviousFloatSlot = newSlot;
        }

        /// <summary>
        /// Called when an item meets its slot
        /// </summary>
        /// <param name="newSlot"></param>
        internal void SetSlot(Slot newSlot)
        {
            Slot = newSlot;
            eventSlotted.Invoke();
            MoveToPosition(newSlot.RectTransform.anchoredPosition);

            //just in case, could be removed
            ToggleDrag(false);
            
            PreviousFloatSlot = newSlot;
        }

        //event handlers
        
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            //blocked due to click limiting
            if (buttonLocked)
            {
                eventData.pointerDrag = null;
                return;
            }
            
            //blocked due to click limiting
            if (isFlying)
            {
                eventData.pointerDrag = null;
                return;
            }

            isFlying = true;

            if (!isBeingDragged && (LimitDragClick) ? (eventData.button == PointerEventData.InputButton.Left) : true)
            {
                //locking to prevent bugs
                buttonLocked = true;
                timer.Enabled = true;

                ToggleDrag(true);

                if (Slot)
                {
                    Slot.UnsetItem();
                }

                //remember the slot we are departing from
                SetPreviousSlot(Slot);

                //we are currently not in a slot
                Slot = null;

                //fire events
                eventUnslotted.Invoke();
            }
            else
            {
                //Debug.Log("Drag Failed");
                //cancel the drag event
                eventData.pointerDrag = null;
            }
        }
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            //Move the item with respect to the canvas scaling (affects positioning)
            rectTransform.anchoredPosition += eventData.delta / Canvas.scaleFactor;
        }
        
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (Slot == null)
            {
                //return the item back to its original slot, as it has not been put into a valid new one
                PreviousSlot.TrySetItem(this.gameObject);

                Slot = PreviousSlot;

                //fire events
                eventReslotted.Invoke();
            }

            ToggleDrag(false);
        }
        
        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            //not captured by this script, captured by the ItemSlot script on the recieving end
            //the method still must exist even if it is empty as unity forces you to define it
        }
        
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            // Debug.Log("Pointer Down: " + mouseDown);
            //event_mouseDown.Invoke();
        }
        
        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            //Debug.Log("Pointer Up");
        }

        private void MoveToPosition(Vector2 pos)
        {
            isFlying = true;
            desiredPosition = pos;
        }

        private void Update()
        {
            //so the slot drop event can be fired even if it is occupied for swapping
            if (isBeingDragged) return;
            
            cg.blocksRaycasts = !Input.GetMouseButton(0);
                
            #region FLYING
            Vector2 diff = desiredPosition - rectTransform.anchoredPosition;
            if (diff.magnitude > FlyingItemSnapThreshold) {
                rectTransform.anchoredPosition += (diff) * PingTravelSpeed;
            } else
            {
                rectTransform.anchoredPosition = desiredPosition;
                isFlying = false;
            }
            #endregion
        }

        private void ToggleDrag(bool status)
        {
            if (status)
            {
                //allow elements behind it to detect events, such as the item slot detecting a drop
                cg.blocksRaycasts = false;
                isBeingDragged = true;

                //Debug.Log("Drag Started");
            }
            else
            {
                cg.blocksRaycasts = true;
                isBeingDragged = false;

                //Debug.Log("Drag Ended");
            }
        }
    }
}