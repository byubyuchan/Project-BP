using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public static ItemSlotUI Instance { get; private set; }

    [Header("UI Components")]
    public Image itemIconImage;

    private void Awake()
    {
        Instance = this;
        ClearSlot();
    }

    public void SetItem(Sprite icon)
    {
        itemIconImage.sprite = icon;
        itemIconImage.enabled = true; // 아이콘이 있을 때 이미지 활성화
    }

    public void ClearSlot()
    {
        itemIconImage.sprite = null;
        itemIconImage.enabled = false; // 아이콘이 없을 때 이미지 비활성화
    }
}
