using SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [SerializeField] GameState gameState;
    [SerializeField] PlayerState playerState;

    [Serializable]
    private class HUDUIElements
    {
        public Image generalChargeMeter;
    }

    [SerializeField] HUDUIElements gameUIComponents;

    private void OnEnable()
    {

        playerState.GeneralChargePercent.OnValueChanged += UpdateChargeMeter;

        UpdateChargeMeter(playerState.GeneralChargePercent.v);
    }

    private void OnDisable()
    {
        playerState.GeneralChargePercent.OnValueChanged -= UpdateChargeMeter;
    }

    private void UpdateChargeMeter(float chargePercent_v)
    {
        gameUIComponents.generalChargeMeter.fillAmount = chargePercent_v;
    }
}
