using ProgressionV2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[
    RequireComponent(typeof(RectTransform)),
    SelectionBase
]
public class InventorySlot : BaseSlot
{

    protected override void OnHoverEnter()
    {
        base.OnHoverEnter();
        BaseElements.FrameColorBlender.TransitionToColor("_HoverColor",
            InteractiveMatBlender.TransitionDirection.IN);
    }

    protected override void OnHoverExit()
    {
        base.OnHoverExit();
        BaseElements.FrameColorBlender.TransitionToColor("_DefaultColor",
            InteractiveMatBlender.TransitionDirection.OUT);
    }
}
