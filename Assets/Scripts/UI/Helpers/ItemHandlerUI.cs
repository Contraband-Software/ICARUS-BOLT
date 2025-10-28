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

    protected BaseItemUI draggingItem;

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
        inputEvents.ui.OnClick += HandleClick;
        inputEvents.ui.OnPoint += HandlePointerMove;
    }

    protected virtual void OnDisable()
    {
        inputEvents.ui.OnClick -= HandleClick;
        inputEvents.ui.OnPoint -= HandlePointerMove;
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

    protected void HandleClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("InventoryUI: Click Start");
            OnClickStarted(context);
        }
        else if (context.canceled)
        {
            Debug.Log("InventoryUI: Click Release");
            OnClickReleased(context);
        }
    }

    protected void OnClickStarted(InputAction.CallbackContext context)
    {
        // Raycast UI to see if we clicked an item
        Vector2 pointer = pointAction.action.ReadValue<Vector2>();
        var data = new PointerEventData(EventSystem.current) { position = pointer };
        var results = new List<RaycastResult>();
        itemHandlerUIElements.raycaster.Raycast(data, results);
        foreach (var hit in results)
        {
            BaseItemUI item = hit.gameObject.GetComponent<BaseItemUI>();
            if (item != null)
            {
                Debug.Log("Clicked Item of id: " + item.GetItemId());
                draggingItem = item;

                Canvas dragCanvas = itemHandlerUIElements.dragCanvas 
                    ? itemHandlerUIElements.dragCanvas 
                    : itemHandlerUIElements.canvas;
                draggingItem.DragStart(dragCanvas);
                draggingItem.DragUpdate(pointer, dragCanvas);
                break;
            }
        }
    }

    protected void HandlePointerMove(InputAction.CallbackContext context)
    {
        if (draggingItem == null) return;

        Vector2 pointer = context.ReadValue<Vector2>();

        Debug.Log("ItemHandlerUI HandlePointerMove: " +  pointer);
        Canvas dragCanvas = itemHandlerUIElements.dragCanvas
            ? itemHandlerUIElements.dragCanvas
            : itemHandlerUIElements.canvas;

        draggingItem.DragUpdate(pointer, dragCanvas);
    }

    protected void OnClickReleased(InputAction.CallbackContext context)
    {
        if (draggingItem == null) return;

        // Raycast under mouse
        Vector2 pointer = pointAction.action.ReadValue<Vector2>();
        var data = new PointerEventData(EventSystem.current) { position = pointer };

        var results = new List<RaycastResult>();

        // Raycast across all active & enabled canvases
        foreach (var handler in FindObjectsOfType<ItemHandlerUI>())
        {
            Canvas canvas = handler.itemHandlerUIElements.canvas;
            GraphicRaycaster raycaster = handler.itemHandlerUIElements.raycaster;

            if (canvas != null && canvas.enabled && canvas.gameObject.activeInHierarchy &&
                raycaster != null && raycaster.enabled)
            {
                raycaster.Raycast(data, results);
            }
        }
        // Sort hits by visual priority — higher sortingOrder & depth first
        results.Sort((a, b) =>
        {
            int orderCompare = b.sortingOrder.CompareTo(a.sortingOrder);
            if (orderCompare != 0)
                return orderCompare;

            // If same sortingOrder, compare by depth
            return b.depth.CompareTo(a.depth);
        });

        BaseSlot targetSlot = null;

        foreach (var hit in results)
        {
            // look for Slot on the root object hit
            targetSlot = hit.gameObject.GetComponent<BaseSlot>();
            if (targetSlot != null)
                break;
        }

        void FunctionExit()
        {
            draggingItem.DragCancel();
            draggingItem = null;
        }

        if (targetSlot == null)
        {
            Debug.Log("No target slot found");
            FunctionExit();
            return;
        }

        ItemData itemData = draggingItem.inSlot.itemHandlerUI.GetStore().GetItemById(draggingItem.GetItemId());
        if (itemData == null)
        {
            FunctionExit();
            return;
        }

        BaseSlot currentSlot = draggingItem.inSlot;

        if (targetSlot.TrySlotItem(
            draggingItem,
            itemData,
            BaseSlot.SlotMode.USER,
            currentSlot))
        {
            // success
            draggingItem.DragEnd();
            targetSlot.OnItemSlotted(itemData.itemId);
        }
        else
        {
            draggingItem.DragCancel();
        }

        draggingItem = null;
    }
}
