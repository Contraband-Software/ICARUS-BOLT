using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resources.System;
using UnityEngine.UI;
using ProgressionV2;
using UnityEngine.InputSystem;
using SharedState;
using System;

[
    RequireComponent(typeof(RectTransform)),
    RequireComponent(typeof(CanvasGroup)),
    RequireComponent(typeof(Image))
]
public class BaseItemUI : MonoBehaviour
{
    int itemId;  //links to itemId in inventory
    public RectTransform rectTransform;
    public Image image_layer1;
    public Image image_layer2;
    CanvasGroup canvasGroup;
    
    Coroutine returnCoroutine;

    BaseSlot originalParent;

    public BaseSlot inSlot { get; private set; } = null;
    public float defaultFlySpeed = 2000f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    public void Initialize(int itemId)
    {
        Debug.Log("Initialized BaseItemUI of ID: " + itemId);
        this.itemId = itemId;
        UpdateUI();
    }

    public virtual void UpdateUI()
    {
        Sprite layer1 = inSlot.itemHandlerUI.GetStore().GetItemIconLayer_1(itemId);
        Sprite layer2 = inSlot.itemHandlerUI.GetStore().GetItemIconLayer_2(itemId);

        image_layer1.sprite = layer1;
        image_layer2.sprite = layer2;

        Debug.Log("layer1 valid: " + (layer1 != null));

        image_layer1.enabled = image_layer1.sprite != null;
        image_layer2.enabled = image_layer2.sprite != null;
    }

    public void DragStart(Canvas canvas)
    {
        originalParent = transform.parent.parent.GetComponent<BaseSlot>();
        if (originalParent == null) return;

        UnlockItem(canvas.transform);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
    }

    public void DragUpdate(Vector2 screenPosition, Canvas canvas)
    {
        Debug.Log("Dragging: " + gameObject.name);

        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            null,
            out Vector2 localPos))
        {
            Debug.Log("Cursor Pos: " + screenPosition + " localPos: " + localPos);
            rectTransform.localPosition = localPos;
        }
    }

    public void DragCancel()
    {
        if (returnCoroutine != null)
            StopCoroutine(returnCoroutine);

        canvasGroup.alpha = 1f;
        FlyToSlot(originalParent, defaultFlySpeed);
    }

    public void DragEnd()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        originalParent = null;
    }

    public void UnlockItem(Transform newParent)
    {
        Debug.Log("Unlocking item");
        transform.SetParent(newParent, true);
    }

    public void FlyToSlot(BaseSlot targetSlot, float speed = 2000f, Action onComplete = null)
    {
        if (targetSlot == null) return;
        if (returnCoroutine != null)
            StopCoroutine(returnCoroutine);

        returnCoroutine = StartCoroutine(FlyToSlotCoroutine(targetSlot, speed, onComplete));
    }

    private IEnumerator FlyToSlotCoroutine(BaseSlot targetSlot, float speed, Action onComplete)
    {
        canvasGroup.blocksRaycasts = false;

        Vector3 startPos = rectTransform.position;
        Vector3 targetPos = targetSlot.GetCenterWorld();

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / speed;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // smoothstep

            rectTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Reparent and snap
        targetSlot.PlaceItemInSlot(this);

        DragEnd();
        returnCoroutine = null;

        // invoke callback if provided
        onComplete?.Invoke();
    }

    public void PlacedInSlot(BaseSlot newSlot)
    {
        inSlot = newSlot;
    }

    public int GetItemId()
    {
        return itemId;
    }

}
