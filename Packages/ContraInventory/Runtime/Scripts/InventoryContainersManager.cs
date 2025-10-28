using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Software.Contraband.Inventory
{
    public sealed class InventoryContainersManager : MonoBehaviour
    {
        #region Public Fields and State
        [field: Header("References")]
        [field: Tooltip("The UI canvas this inventory system operates on, " +
                        "should be a direct parent for a clean project")]
        [field: FormerlySerializedAs("canvas"), SerializeField] 
        public Canvas Canvas { get; private set; }

        [Header("Inventory Object Containers")]
        [SerializeField, FormerlySerializedAs("ContainerContainer")]
        private GameObject containerContainer;
        [field: SerializeField] public GameObject ItemContainer { get; private set; }

        public Action<Item> LostItemHandler { internal get; set; } = (Item item) =>
        {
            Debug.LogWarning("Lost Item: " + item.name + ", Destroying it...");
            Destroy(item.gameObject);
        };
        #endregion

        // State
        private readonly Dictionary<string, Container> containerNameMap = new ();

        #region Unity Callbacks
        private void Awake()
        {
#if UNITY_EDITOR
            if (Canvas == null)
                throw new InvalidOperationException("Inventory system canvas not assigned");
#endif
            RegisterChildContainers();
        }

        private void OnTransformChildrenChanged()
        {
            RegisterChildContainers();
        }
        #endregion

        #region Public API
        public Container GetContainer(string containerName)
        {
            return containerNameMap[containerName];
        }

        public IReadOnlyDictionary<string, Slot> GetContainerMap(string containerName)
        {
            return containerNameMap[containerName].SlotNameMap;
        }

        /// <summary>
        /// Takes in an item and adds it to the given slot of the given container,
        /// as well as parenting the object to the item container Object.
        /// The item gameObject must already be parented to the same canvas as the target inventory system.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="slotName"></param>
        /// <param name="item"></param>
        /// <returns>Whether the slotting was successful or not</returns>
        public bool AddItem(string containerName, string slotName, Item item)
        {
            item.transform.SetParent(ItemContainer.transform);
            item.GetComponent<Item>().Canvas = Canvas;

            if (!containerNameMap.TryGetValue(containerName, out Container ic)) return false;
            
            return ic.AddItemToSlot(slotName, item);
        }
        #endregion

        private void RegisterChildContainers()
        {
            containerNameMap.Clear();
            foreach (Container cont in containerContainer.GetComponentsInChildren<Container>(true))
                RegisterContainer(cont);
        }

        private void RegisterContainer(Container t)
        {
            t.Manager = this;
            containerNameMap.Add(t.gameObject.name, t);
        }
    }
}
