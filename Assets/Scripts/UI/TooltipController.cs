using System;
using System.Collections;
using System.Collections.Generic;
using Software.Contraband.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[
    RequireComponent(typeof(RectTransform))
]
public class TooltipController : MonoBehaviour
{
    [SerializeField] private GameObject toolTipContainer;
    [FormerlySerializedAs("enabled")] [SerializeField] private bool show;
    
    [Header("Optional GameSettingsPreset")]
    [SerializeField] private Item item;
    
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos)
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition) &&
            (item is null || !item.Slot))
            toolTipContainer.SetActive(true);
        else
            toolTipContainer.SetActive(false);
    }
}
