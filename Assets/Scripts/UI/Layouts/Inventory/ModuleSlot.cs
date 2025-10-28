using ProgressionV2;
using Resources;
using Resources.Firmware;
using Resources.Modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleSlot : InventorySlot
{
    [SerializeField] ModuleType.Type type;
    public override bool CanSlotItem(ItemData item, SlotMode mode, out SlottingCondition condition)
    {
        condition = SlottingCondition.NONE;
        if (!base.CanSlotItem(item, mode, out condition)) return false;

        bool canSlot = true;

        // should only be allowed to slot modules
        if (item.GetType() != typeof(ModuleData)) return false;

        ModuleData moduleData = (ModuleData)item;

        ModuleUpgradeAsset moduleAsset = ItemStore.GetModuleAsset(moduleData.moduleId);
        if (moduleAsset == null) return false;

        // Must only allow module types of the slot type
        if (moduleAsset.type != type) return false;

        // Is there already an active module of the same type that isnt the same item? => return false
        if (itemHandlerUI.GetStore().HasActiveModule(moduleAsset.GetType(), moduleData.itemId)) return false;

        return canSlot;
    }


    public override void OnItemSlotted(int itemId)
    {
        Debug.Log("OnItemSlotted Module");
        itemHandlerUI.GetStore().SetModuleActive(itemId, true);
    }
}
