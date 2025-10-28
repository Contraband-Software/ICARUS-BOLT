using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[
    RequireComponent(typeof(CanvasGroup))
]
public class MyItemBehaviour : MonoBehaviour
{
    public Enums.SlotType slotType = Enums.SlotType.A;

    public GameObject te;

    private CanvasGroup cg;

    private Software.Contraband.Inventory.Item II;

    private void startDrag()
    {
        cg.alpha = 0.5f;
    }
    private void endDrag()
    {
        cg.alpha = 1.0f;
    }

    private void Start()
    {
        cg = GetComponent<CanvasGroup>();
        te.GetComponent<TextMeshProUGUI>().text = slotType.ToString();

        II = GetComponent<Software.Contraband.Inventory.Item>();

        II.eventUnslotted.AddListener(startDrag);
        II.eventSlotted.AddListener(endDrag);

        //II.ItemTypeIdentifier = slotType.ToString();
    }
}
