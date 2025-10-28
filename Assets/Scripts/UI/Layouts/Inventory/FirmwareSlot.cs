using ProgressionV2;
using Resources.Firmware;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirmwareSlot : InventorySlot
{
    public override bool CanSlotItem(ItemData item, SlotMode mode, out SlottingCondition condition)
    {
        condition = SlottingCondition.NONE;
        if(!base.CanSlotItem(item, mode, out condition)) return false;

        // should only be allowed to slot firmwares
        if (item.GetType() != typeof(FirmwareData)) return false;

        // Is there already an active firmware of the same type that isnt the same item? => return false
        if(condition != SlottingCondition.SWAP)
        {
            FirmwareData firmwareData = (FirmwareData)item;
            FirmwareUpgradeAsset firmwareAsset = ItemStore.GetFirmwareAsset(firmwareData.firmwareId);
            if (firmwareAsset == null) return false;
            if (itemHandlerUI.GetStore()
                .HasActiveFirmware(firmwareAsset.GetType(), firmwareData.itemId)) return false;
        }

        return true;
    }

    public override void OnItemSlotted(int itemId)
    {
        Debug.Log("OnItemSlotted Firmware");
        itemHandlerUI.GetStore().SetFirmwareActive(itemId, true);
    }
}
