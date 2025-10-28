using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Software.Contraband.Inventory
{
    public class InventoryInitializer : MonoBehaviour
    {
        [Serializable]
        public struct SlotItemPair
        {
            public Slot ItemSlot;
            public GameObject Item;
        }

        [SerializeField] List<SlotItemPair> Population = new();

        private void Start()
        {
            foreach (SlotItemPair pair in Population)
            {
#if UNITY_EDITOR
                if (!pair.Item.TryGetComponent<Item>(out _))
                    throw new InvalidOperationException("Tried to initialize slot with a non-item gameObject");
#endif
                if (!pair.ItemSlot.SpawnItem(pair.Item.GetComponent<Item>()))
                {
                    Debug.LogException(new Exception("Cannot init item to this slot"));
                }
            }
        }
    }
}