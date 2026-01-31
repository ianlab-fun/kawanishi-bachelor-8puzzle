using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BlockView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private int blockNumberValue;
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private Image blockImage;

    private BlockNumber _blockNumber;

    // インスタンスイベント：各BlockViewが独立してイベントを発火
    public event Action<BlockNumber> OnClicked;
    public event Action<Vector2> OnDragging;
    public event Action OnDragEnded;
    public bool IsDragging { get; private set; }

    public void SetBlockNumber(BlockNumber number)
    {
        _blockNumber = number;
        textMeshPro.text = number.ToString();
    } 

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(_blockNumber);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_blockNumber.IsZero()) return;

        IsDragging = true;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!_blockNumber.IsZero()) return;

        // 位置変更はイベント購読側（PuzzleDragger）で行う
        OnDragging?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_blockNumber.IsZero()) return;

        OnDragEnded?.Invoke();
        IsDragging = false;
    }

    /// <summary>
    /// ブロックの操作可能状態を設定（半透明化で表現）
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        Color color = blockImage.color;
        color.a = interactable ? 1.0f : 0.5f;
        blockImage.color = color;
    }
}
