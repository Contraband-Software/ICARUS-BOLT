using ProgressionV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSlot : MonoBehaviour
{
    [SerializeField] int slotId;
    public int SlotId => slotId;

    public enum SlotMode
    {
        SYSTEM,
        USER
    }

    public enum SlottingCondition
    {
        NONE,
        SWAP,       // Items must be swapped
        UPGRADE    // One item must be deleted
    }

    [SerializeField] public Transform containerRoot;
    [SerializeField] public Transform itemFrame;
    public ItemHandlerUI itemHandlerUI { get; private set; }

    public void SetItemHandlerUI(ItemHandlerUI itemHandlerUI) => this.itemHandlerUI = itemHandlerUI;

    public bool IsOccupied() => GetItem() != null;
    protected ItemData GetItem() => itemHandlerUI.GetStore().GetItemFromSlot(slotId);

    public BaseItemUI GetItemObject()
    {
        return GetComponentInChildren<BaseItemUI>(includeInactive: false);
    }

    public virtual bool CanSlotItem(ItemData itemData, SlotMode mode, out SlottingCondition condition)
    {
        condition = SlottingCondition.NONE;

        ItemData currentItem = GetItem();

        // If slot already occupied by an item that isnt the one youre trying to slot :
        if (currentItem != null && currentItem.itemId != itemData.itemId)
        {
            if (mode == SlotMode.SYSTEM) return false;
            if (mode == SlotMode.USER)
            {
                condition = SlottingCondition.SWAP;
            }
        }

        return true;
    }

    public bool TrySlotItem(
        BaseItemUI item,
        ItemData itemData,
        SlotMode mode,
        BaseSlot itemSourceSlot = null)
    {
        if (item == null) return false;
        if (itemData == null) return false;

        if (mode == SlotMode.SYSTEM)
        {
            if (!CanSlotItem(itemData, mode, out _)) return false;
            RegisterItemToSlot(item);
            PlaceItemInSlot(item);
        }
        else if (mode == SlotMode.USER)
        {
            SlottingCondition condition = SlottingCondition.NONE;
            bool itemCanSlot = CanSlotItem(itemData, mode, out condition);

            // check if the item itself can be upgraded by the incoming item
            // if so, that immediately takes precendence
            ItemData currentSlotItem = GetItem();
            if (currentSlotItem != null &&
                itemHandlerUI.GetStore().CanUpgadeItem(currentSlotItem.itemId) &&
                itemHandlerUI.GetStore().ValidUpgradeCandidate(currentSlotItem.itemId, itemData))
            {
                itemCanSlot = true;
                condition = SlottingCondition.UPGRADE;
            }

            if (!itemCanSlot) return false;

            Debug.Log("InventorySlot CanSlotItem Result: " + itemCanSlot + " condition: " + condition.ToString());

            if (condition == SlottingCondition.NONE)
            {
                RegisterItemToSlot(item);
                PlaceItemInSlot(item);
            }

            else if (condition == SlottingCondition.SWAP)
            {
                if (itemSourceSlot == null) return false;
                if (!itemSourceSlot.CanSlotItem(currentSlotItem, SlotMode.USER, out _)) return false;

                RegisterItemToSlot(item);
                PlaceItemInSlot(item);
                // Swap this slots item into the incoming items slot
                Canvas dragCanvas = itemHandlerUI.itemHandlerUIElements.dragCanvas
                    ? itemHandlerUI.itemHandlerUIElements.dragCanvas
                    : itemHandlerUI.itemHandlerUIElements.canvas;
                MigrateSlotItemOnSwap(currentSlotItem, itemSourceSlot, dragCanvas);
            }

            else if (condition == SlottingCondition.UPGRADE)
            {
                BaseItemUI thisItemObject = GetItemObject();
                if (thisItemObject == null) return false;
                itemSourceSlot.DestroyItem(item);
                itemHandlerUI.GetStore().UpgradeFirmware(GetItem().itemId, 1);
                thisItemObject.UpdateUI();
            }
        }

        item.UpdateUI();
        return true;
    }
    /// <summary>
    /// Visually place item in the slot
    /// </summary>
    /// <param name="item"></param>
    public void PlaceItemInSlot(BaseItemUI item)
    {
        item.transform.SetParent(itemFrame, false);
        item.rectTransform.anchoredPosition = Vector2.zero;
        item.rectTransform.anchorMin = Vector2.zero;
        item.rectTransform.anchorMax = Vector2.one;
        item.rectTransform.offsetMin = Vector2.zero;
        item.rectTransform.offsetMax = Vector2.zero;

        item.PlacedInSlot(this);
    }

    /// <summary>
    /// Backend update
    /// </summary>
    /// <param name="item"></param>
    public void RegisterItemToSlot(BaseItemUI item)
    {
        // If the item is being moved to a different store, it must be removed from its
        // current store and added to the new one before being registered to a slot.
        ItemStore item_currentStore = item.inSlot.itemHandlerUI.GetStore();
        ItemStore thisStore = itemHandlerUI.GetStore();

        if (thisStore == null)
        {
            throw new MissingReferenceException("Slot of name " + gameObject.name + " isnt tied to a store");
        }

        if(item_currentStore != null)
        {
            // remove from its current store
           
        }

        if(!ReferenceEquals(item_currentStore, thisStore))
        {
            ItemData itemData = item_currentStore.GetItemById(item.GetItemId());
            // remove from its current store
            item_currentStore.RemoveItem(itemData.itemId);
            // add to new store
            thisStore.AddItem(itemData);
        }

        thisStore.RegisterItemToSlot(item.GetItemId(), SlotId);
    }

    /// <summary>
    /// Moves this slots item to a different slot.
    /// IMPORTANT: This assumes that the item is allowed to move there. Check this beforehand.
    /// </summary>
    /// <param name="item">The item data of the item in this slot to be moved</param>
    /// <param name="targetSlot">the slot that the item will be moved to</param>
    private void MigrateSlotItemOnSwap(ItemData currentItem, BaseSlot targetSlot, Canvas dragCanvas)
    {
        BaseItemUI currentSlotItemObject = GetItemObject();
        if (currentSlotItemObject == null) return;

        currentSlotItemObject.UnlockItem(dragCanvas.transform);
        targetSlot.RegisterItemToSlot(currentSlotItemObject);
        targetSlot.OnItemSlotted(currentItem.itemId);

        // Visual: fly to slot
        currentSlotItemObject.FlyToSlot(targetSlot, currentSlotItemObject.defaultFlySpeed);
    }

    public virtual void DestroyItem(BaseItemUI itemObject)
    {
        // delete the item object and associated store entry
        itemHandlerUI.GetStore().RemoveItem(itemObject.GetItemId());
        Destroy(itemObject.gameObject);
    }

    public virtual void DestroyOwnItemObject()
    {
        BaseItemUI itemObject = GetItemObject();
        if(itemObject != null) Destroy(GetItemObject().gameObject);
    }

    /// <summary>
    /// Get the slots center in world space
    /// </summary>
    /// <returns></returns>
    public Vector3 GetCenterWorld()
    {
        RectTransform slotRect = transform as RectTransform;

        // Local offset to the center, regardless of pivot
        Vector2 slotLocalCenter = new Vector2(
            (0.5f - slotRect.pivot.x) * slotRect.rect.width,
            (0.5f - slotRect.pivot.y) * slotRect.rect.height
        );

        return slotRect.TransformPoint(slotLocalCenter);
    }


    public virtual void OnItemSlotted(int itemId) {
        itemHandlerUI.GetStore().SetFirmwareActive(itemId, false);
        itemHandlerUI.GetStore().SetModuleActive(itemId, false);
    }
}