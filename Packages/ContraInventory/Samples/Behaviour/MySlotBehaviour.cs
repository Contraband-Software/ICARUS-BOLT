using System.Collections;
using System.Collections.Generic;
using Software.Contraband.Inventory;
using UnityEngine;
using TMPro;

public class MySlotBehaviour : Software.Contraband.Inventory.AbstractSlotBehaviour
{
    public Enums.SlotType slotType = Enums.SlotType.A;

    public GameObject te;
    private void Start()
    {
        te.GetComponent<TextMeshProUGUI>().text = slotType.ToString();
    }
    public override bool CanItemSlot(Slot _, Item item)
    {
        return slotType == item.GetComponent<MyItemBehaviour>().slotType;
    }
}
