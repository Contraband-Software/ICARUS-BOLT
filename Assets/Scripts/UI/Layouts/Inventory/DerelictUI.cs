using ProgressionV2;
using SharedState;
using Software.Contraband.Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DerelictUI : ItemHandlerUI
{
    public override ItemStore GetStore()
    {
        return DerelictData.GetDerelictLoot(DerelictData.activeDerelict);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        gameState.Events.OnEnterDerelict += Open;
        gameState.Events.OnExitDerelict += Close;
        gameState.Events.OnExitInventory += Close;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        gameState.Events.OnEnterDerelict -= Open;
        gameState.Events.OnExitDerelict -= Close;
        gameState.Events.OnExitInventory -= Close;

    }
    private void Start()
    {
        Hide();
        Initialize();
    }

    private void GenerateItems()
    {
        foreach(BaseSlot slot in slots)
        {
            slot.DestroyOwnItemObject();
        }

        if (GetStore() == null) return;

        foreach(ItemData item in GetStore().Items.Values)
        {
            BaseSlot itemSlot = GetSlotById(item.slotId);
            if(itemSlot != null)
            {
                if (!itemSlot.CanSlotItem(item, BaseSlot.SlotMode.SYSTEM, out _)) continue;
                GenerateItemInSlot(item, itemSlot);
                continue;
            }

            foreach(BaseSlot slot in slots)
            {
                if (!slot.CanSlotItem(item, BaseSlot.SlotMode.SYSTEM, out _)) continue;
                GenerateItemInSlot(item, slot);
                break;
            }
        }
    }

    private void Open()
    {
        GenerateItems();
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
}
