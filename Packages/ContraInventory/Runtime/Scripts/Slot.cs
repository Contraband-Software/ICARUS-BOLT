using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Software.Contraband.Inventory
{
    [
        RequireComponent(typeof(RectTransform)),
        SelectionBase
    ]
    public sealed class Slot : MonoBehaviour, IDropHandler, IDragHandler
    {

        //Settings
        //[Serializable]
        //public class StackSettings
        //{
        //    public bool AllowStacking = false;
        //    public int MaximumItemAmount = 1;
        //}

        //custom behaviour hooking
        [Serializable]
        public class OptionalScript
        {
            public bool Enabled = false;
            public AbstractSlotBehaviour script = null;
        }

        [Header("Settings"), Space(10)]
        [Tooltip(
            "Custom script object that inherits from InventoryManager.SlotBehaviour " +
            "to define checks on how this slot should accept items.")]
        [field: SerializeField] private OptionalScript CustomSlotBehaviour;
        //public StackSettings stackSettings;
        
        //Events
        [Header("Events"), Space(10)]
        public UnityEvent eventSlotted = new();
        public UnityEvent eventUnslotted = new();

        //State
        public Container Container { get; internal set; } = null;
        public Item SlotItem { get; private set; } = null;
        public RectTransform RectTransform { get; private set; }

        private Action<Item> lostItemHandler;

        #region Unity Callbacks
        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            
            gameObject.tag = "InventorySystemSlot";
            
#if UNITY_EDITOR
            // ReSharper disable once Unity.NoNullPropagation
            if (Container?.Manager is null)
            {
                throw new InvalidOperationException(
                    "A container hierarchy may only be one level deep, and only composed of slots.");
            }
#endif
            lostItemHandler = Container.Manager.LostItemHandler;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Spawns an item in a slot
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SpawnItem(Item item)
        {
            if (SlotItem != null || !CheckCustomBehaviour(item)) return false;
            
            SetItem(item);
            item.SpawnInSlot(this);
            
            return true;
        }

        public void DestroyItem()
        {
            if (!SlotItem) return;
            Destroy(SlotItem.gameObject);
            SlotItem = null;
        }
        #endregion

        //for moving an item from another slot
        internal bool TrySetItem(GameObject item)
        {
            Item itemScript = item.GetComponent<Item>();
            Slot itemsPreviousSlot = itemScript.PreviousSlot;

#if UNITY_EDITOR
            if (itemsPreviousSlot == null)
                throw new InvalidOperationException("itemScript.GetPreviousSlot() == null");
#endif
            
            //isolation
            Container previousSlotContainer = itemsPreviousSlot.Container;
            if (Container.IsolationSettings.Enabled || previousSlotContainer.IsolationSettings.Enabled)
            {
                //if either are isolated, compare identifiers
                if (previousSlotContainer.IsolationSettings.Identifier != 
                    Container.IsolationSettings.Identifier)
                {
                    return false;
                }
            }
            
            if (!CheckCustomBehaviour(itemScript)) return false;

            // empty slot
            if (SlotItem == null)
            {
                itemScript.SetSlot(this);
                SetItem(itemScript);
                return true;
            }
            
            // empty source slot means we can swap the items
            if (itemsPreviousSlot.SlotItem == null)
            {
                // try to swap the items
                if (!itemsPreviousSlot.TrySetItem(SlotItem.gameObject)) return false;
                
                itemScript.SetSlot(this);
                SetItem(itemScript);
                return true;
            }

            //Deal with item
            lostItemHandler?.Invoke(itemScript);

            return false;
        }

        private bool CheckCustomBehaviour(Item item)
        {
            //custom logic
            if (!CustomSlotBehaviour.Enabled)
                return true;

            return CustomSlotBehaviour.script.CanItemSlot(this, item);
        }

        //core logic that applies to both initialising and moving
        private void SetItem(Item item)
        {
            SlotItem = item;
            eventSlotted.Invoke();
        }

        //for when an item is removed from a slot
        internal void UnsetItem()
        {
            //slotItem.GetComponent<InventorySystemItem>().SetSlot(null);
            SlotItem = null;
            
            eventUnslotted.Invoke();
        }

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            //fire a custom event saying that a slot has been updated within the inventory system
            //this needs to check if the pointer drag item is a compatible inventory system item,
            //use tags instead of trygetcomponent
            if (eventData.pointerDrag != null && eventData.pointerDrag.CompareTag("InventorySystemItem"))
            {
                //Debug.Log("Tried to drop in slot");
                TrySetItem(eventData.pointerDrag);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData) { }
    }
}

