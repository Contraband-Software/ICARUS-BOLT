using ProgressionV2;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FirmwareItemUI : BaseItemUI
{
    [SerializeField] TextMeshProUGUI tierText;

    public override void UpdateUI()
    {
        base.UpdateUI();

        tierText.text = string.Empty;

        FirmwareData firmwareData = inSlot.itemHandlerUI.GetStore().GetFirmwareDataByItemId(GetItemId());
        if (firmwareData == null) return;

        if (firmwareData.tier < 2)
        {
            tierText.text = string.Empty;
        }
        else
        {
            tierText.text = RomanNumerals.ToRoman(firmwareData.tier);
        }
    }
}
