using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEngine.Serialization;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Software.Contraband.Inventory
{
    public enum SlotAction
    {
        Added,
        Removed
    }
    
    public sealed class Container : MonoBehaviour
    {
        //events
        [FormerlySerializedAs("event_Refresh")]
        public readonly UnityEvent<SlotAction, Item> EventRefresh = new();

        //settings
        [Serializable]
        public class OptionalIsolationSettings
        {
            public bool Enabled = false;
            public string Identifier = "Default";
        }

        [Tooltip("Limit item transfer only to containers with this identifier")]
        [field: SerializeField] public OptionalIsolationSettings IsolationSettings { get; private set; }

        //state
        [SerializeField]
        private List<Item> items = new();
        private readonly Dictionary<string, Slot> slotNameMap = new();
        
        public InventoryContainersManager Manager { get; internal set; } = null;
        public IReadOnlyDictionary<string, Slot> SlotNameMap { get; private set; }
        public ReadOnlyCollection<Item> Items { get; private set; }

        #region Unity Callbacks
        private void Awake()
        {
            Items = items.AsReadOnly();
            SlotNameMap = slotNameMap;

            RegisterChildSlots();
        }

        private void OnTransformChildrenChanged()
        {
            RegisterChildSlots();
        }

        private void OnEnable()
        {
            RegisterChildSlots();
        }

        #endregion

        /// <summary>
        /// Programatically adding item to slot
        /// </summary>
        /// <param name="slotName"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool AddItemToSlot(string slotName, Item item)
        {
            if (!slotNameMap.TryGetValue(slotName, out Slot slot)) return false;
            
            bool res = slot.SpawnItem(item);

            ContainerRefreshEvent(SlotAction.Added, item);

            return res;
        }

        private void RegisterChildSlots()
        {
            slotNameMap.Clear();
            
            //grab a reference to the item slot script for all the item slots in this container,
            //as well as initialising the item cache, and adding its own reference and event handlers
            foreach (Slot slot in GetComponentsInChildren<Slot>(true))
                RegisterSlot(slot);
        }
        
        private void RegisterSlot(Slot slot)
        {
            slotNameMap.Add(slot.gameObject.name, slot);

            slot.Container = this;

            slot.eventSlotted.AddListener(() => ContainerRefreshEvent(SlotAction.Added, slot.SlotItem));
            slot.eventUnslotted.AddListener(() => ContainerRefreshEvent(SlotAction.Removed, slot.SlotItem));

            if (slot.SlotItem != null)
                items.Add(slot.SlotItem);
        }
        
        private void ContainerRefreshEvent(SlotAction action, Item item)
        {
            items.Clear();

            foreach (Item slotItem in 
                     slotNameMap
                         .Select(child => child.Value.SlotItem)
                         .Where(slotItem => slotItem != null))
                items.Add(slotItem);
            
            EventRefresh.Invoke(action, item);
        }
    }
}