using ProgressionV2;
using SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public abstract class ItemHandlerUI : MonoBehaviour
{
    [SerializeField] protected InputEvents inputEvents;
    [SerializeField] protected GameState gameState;
    [SerializeField] protected InputActionReference pointAction;

    [SerializeField] protected GameObject inventoryItemPrefab;
    [SerializeField] protected GameObject firmwareItemPrefab;
    [SerializeField] protected GameObject moduleItemPrefab;

    public static BaseItemUI DraggingItem;
    public static bool HasDraggingItem => DraggingItem != null;

    [Serializable]
    public class ItemHandlerUIElements
    {
        public Canvas canvas;
        public Canvas dragCanvas;
        public GraphicRaycaster raycaster;
    }
    public ItemHandlerUIElements itemHandlerUIElements;

    [SerializeField] protected List<BaseSlot> slots = new List<BaseSlot>();

    /// <summary>
    /// Get the item store that this UI interfaces for
    /// </summary>
    /// <returns></returns>
    public abstract ItemStore GetStore();
    protected virtual void OnEnable()
    {
        gameState.Events.OnPause += CancelDraggingAction;
    }

    protected virtual void OnDisable()
    {
        gameState.Events.OnPause -= CancelDraggingAction;
    }

    protected virtual void Initialize()
    {
        foreach(BaseSlot slot in slots)
        {
            slot.SetItemHandlerUI(this);
        }
    }

    protected BaseSlot GetSlotById(int id)
    {
        return slots.FirstOrDefault(s => s.SlotId == id);
    }

    /// <summary>
    /// Instantiates an itemData inside the slot under assumption it is fully allowed
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="slot"></param>
    protected void GenerateItemInSlot(ItemData itemData, BaseSlot slot)
    {
        if (itemData == null || slot == null)
        {
            Debug.LogError("Tried to generate itemData but itemData or slot is null");
            return;
        }
        if (!slot.CanSlotItem(itemData, BaseSlot.SlotMode.SYSTEM, out _))
        {
            Debug.LogError("Cannot slot generated item of ID " + itemData.itemId + " into slot id: " + slot.SlotId);
            return;
        }
        GameObject slotObject = slot.gameObject;

        // Select correct prefab type to instantiate
        GameObject itemPrefab = inventoryItemPrefab;
        if (itemData.GetType() == typeof(FirmwareData))
        {
            itemPrefab = firmwareItemPrefab;
        }
        else if (itemData.GetType() == typeof(ModuleData))
        {
            itemPrefab = moduleItemPrefab;
        }

        var instance = Instantiate(itemPrefab, slotObject.transform);
        BaseItemUI item = instance.GetComponent<BaseItemUI>();
        if (item == null)
        {
            Debug.LogError($"Prefab {itemPrefab.name} is missing BaseItemUI!");
            Destroy(instance);
            return;
        }
        SetItemToSlot(itemData.itemId, slot.SlotId);
        slot.PlaceItemInSlot(item);
        item.Initialize(itemData.itemId);
    }

    protected void SetItemToSlot(int itemId, int slotId)
    {
        BaseSlot slot = GetSlotById(slotId);
        if (slot == null)
        {
            Debug.LogError("cant do RegisterItemToSlot as slot doesnt exist of id: " + slotId);
            return;
        }
        GetStore().RegisterItemToSlot(itemId, slotId);
        slot.OnItemSlotted(itemId);

    }

    /// <summary>
    /// Finds a slot of given type where a given item has permission to reside
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemData"></param>
    /// <returns></returns>
    protected BaseSlot FindEmptySlotOfType<T>(ItemData itemData) where T : BaseSlot
    {
        foreach (BaseSlot slot in slots)
        {
            if (slot.GetType() == typeof(T) && slot.CanSlotItem(itemData, BaseSlot.SlotMode.SYSTEM, out _))
            {
                return slot;
            }
        }
        return null;
    }

    public void OnDraggingItemReleased(PointerEventData eventData)
    {
        if (DraggingItem == null) return;

        Debug.Log("POINTER UP");

        var data = new PointerEventData(EventSystem.current) { position = eventData.position };

        // 1. Raycast on ALL canvases
        List<RaycastResult> hits = new();
        foreach (var handler in FindObjectsOfType<ItemHandlerUI>())
        {
            if (!handler.itemHandlerUIElements.canvas.enabled)
                continue;

            handler.itemHandlerUIElements.raycaster.Raycast(data, hits);
        }

        // 2. Sort the hits like Unity does (topmost first)
        hits.Sort((a, b) =>
        {
            int order = b.sortingOrder.CompareTo(a.sortingOrder);
            if (order != 0) return order;
            return b.depth.CompareTo(a.depth);
        });

        // 3. Find the FIRST slot hit
        BaseSlot targetSlot = null;
        foreach (var hit in hits)
        {
            targetSlot = hit.gameObject.GetComponentInParent<BaseSlot>();
            if (targetSlot != null)
                break;
        }

        void FunctionExit()
        {
            DraggingItem.DragCancel();
            DraggingItem = null;
        }

        if (targetSlot == null)
        {
            Debug.Log("No target slot found");
            FunctionExit();
            return;
        }

        ItemData itemData = DraggingItem.inSlot.itemHandlerUI.GetStore().GetItemById(DraggingItem.GetItemId());
        if (itemData == null)
        {
            FunctionExit();
            return;
        }

        BaseSlot currentSlot = DraggingItem.inSlot;


        targetSlot.HandleItemDrop();

        DraggingItem = null;
    }

    public void CancelDraggingAction()
    {
        if (DraggingItem == null) return;
        DraggingItem.DragCancel();
        DraggingItem = null;
    }
}
