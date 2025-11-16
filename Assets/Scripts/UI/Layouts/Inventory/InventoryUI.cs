using SharedState;
using Software.Contraband.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ProgressionV2;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : ItemHandlerUI
{

    protected override void OnEnable()
    {
        base.OnEnable();
        gameState.Events.OnEnterInventory += Open;
        gameState.Events.OnExitInventory += Close;

        inputEvents.ui.OnInventory += HandleInventoryKey;
        inputEvents.ui.OnCancel += HandleCancel;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        gameState.Events.OnEnterInventory -= Open;
        gameState.Events.OnExitInventory -= Close;

        inputEvents.ui.OnInventory -= HandleInventoryKey;
        inputEvents.ui.OnCancel -= HandleCancel;
    }

    private void Start()
    {
        Hide();
        Initialize();
        RePopulateInventory();
    }

    public override ItemStore GetStore()
    {
        return InventoryData.Inventory;
    }


    /// <summary>
    /// Load in all inventory items for the first time
    /// </summary>
    public void RePopulateInventory()
    {
        GenerateItems();
    }

    private void GenerateItems()
    {
        Debug.Log("Generating Items");
        if (slots.Count == 0)
        {
            Debug.LogError("Cannot Generate Items. No slots initialized");
            return;
        }

        List<ItemData> slotConflictItems = new List<ItemData>();

        foreach(ItemData itemData in InventoryData.Inventory.Items.Values)
        {
            BaseSlot slot = GetSlotById(itemData.slotId);

            // Slot of ID may not exist
            if(slot == null)
            {
                slotConflictItems.Add(itemData);
                continue;
            }
            // Slot may be of incompatible type
            // Slot may be already taken
            if(!slot.CanSlotItem(itemData, BaseSlot.SlotMode.SYSTEM, out _))
            {
                slotConflictItems.Add(itemData);
                continue;
            }

            GenerateItemInSlot(itemData, slot);
        }

        // Either generate rehomed items or delete the orphans
        foreach(ItemData itemData in slotConflictItems)
        {
            bool rehomeSuccesful = RehomeSlotConflictingItem(itemData);
            if (!rehomeSuccesful)
            {
                Debug.LogWarning($"Deleting itemData of ID {itemData.itemId} from inventory (could not be rehomed)");
                InventoryData.Inventory.RemoveItem(itemData.itemId);
            }
            else
            {
                BaseSlot slot = GetSlotById(itemData.slotId);
                GenerateItemInSlot(itemData, slot);
            }
        }
    }

    private bool RehomeSlotConflictingItem(ItemData item)
    {
        if (item.GetType() == typeof(FirmwareData))
        {
            // Try to slot it into firmware slot
            BaseSlot firmwareSlot = FindEmptySlotOfType<FirmwareSlot>(item);
            if(firmwareSlot != null)
            {
                SetItemToSlot(item.itemId, firmwareSlot.SlotId);
                return true;
            }
        }

        if(item.GetType() == typeof(ModuleData))
        {
            // Try to slot it into Module slot
            BaseSlot moduleSlot = FindEmptySlotOfType<ModuleSlot>(item);
            if(moduleSlot != null)
            {
                SetItemToSlot(item.itemId, moduleSlot.SlotId);
                return true;
            }
        }

        // final catch-all
        // Are there inventory slots left?
        BaseSlot slot = FindEmptySlotOfType<InventorySlot>(item);
        if (slot == null) return false;
        SetItemToSlot(item.itemId, slot.SlotId);
        return true;
    }

    private void Open()
    {
        Show();
    }

    private void Close()
    {
        CancelDraggingAction();
        Hide();
    }

    private void Show()
    {
        itemHandlerUIElements.canvas.enabled = true;
    }

    private void Hide()
    {
        itemHandlerUIElements.canvas.enabled = false;
    }

    private void HandleInventoryKey(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            Debug.Log("InventoryUI Requesting Exit Inventory");
            gameState.Requests.ExitInventory?.Invoke();
        }
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            gameState.Requests.Pause?.Invoke();
        }
    }
}
