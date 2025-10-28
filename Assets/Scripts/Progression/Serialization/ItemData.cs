using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace ProgressionV2
{
    // Defines data that will be serialized when any item is saved
    public class ItemData
    {
        public int itemId;     // Unique identifier for the item
        public int slotId;     // to which slot the item belongs to in its container


        // - UNSERIALIZED
        //Shared ID registry
        private static HashSet<int> usedIds = new();
        private static int nextId = 1;

        public ItemData()
        {
            AssignUniqueId();
        }

        private void AssignUniqueId()
        {
            // Find the next free ID
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            itemId = nextId;
            usedIds.Add(itemId);
            nextId++;
        }

        private static bool TrySetId(int id)
        {
            if (usedIds.Contains(id))
            {
                return false; // already taken
            }

            usedIds.Add(id);
            return true;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!TrySetId(itemId))
            {
                Debug.LogWarning($"Duplicate itemId {itemId} detected after load, assigning a new one.");
                AssignUniqueId();
            }
        }
    }
}
