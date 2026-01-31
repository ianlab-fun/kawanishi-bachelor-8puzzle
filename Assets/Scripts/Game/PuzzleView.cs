using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public class PuzzleView : MonoBehaviour
{
    [SerializeField] private PuzzleGameBlockCreator blockCreator;
    [SerializeField] private float tweenDuration = 0.2f;
    
    /// <summary>
    /// アニメーションなしで即座にパズルの状態を反映する
    /// </summary>
    public void SetPositionImmediate(PuzzleState puzzleState)
    {
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                var position = new BlockPosition(row, col);
                var blockNumber = puzzleState[position];

                var rect = blockCreator.GetBlockRect(blockNumber);
                Vector2 targetPos = blockCreator.GetLocalPosition(position);
                rect.anchoredPosition = targetPos;
            }
        }
    }

    public void AnimateMove(PuzzleState puzzleState, PuzzleState pastPuzzleState)
    {
        BlockPosition from = puzzleState.EmptyBlockPosition;
        BlockPosition to = pastPuzzleState.EmptyBlockPosition;

        int distance = Math.Abs(to.Row - from.Row) + Math.Abs(to.Column - from.Column);

        // 空位置の距離が1 かつ スワップされたブロックが一致する場合のみアニメーション
        if (!(distance == 1 && pastPuzzleState[from].Equals(puzzleState[to])))
        {
            SetPositionImmediate(puzzleState);
            return;
        }

        int stepRow = Math.Sign(to.Row - from.Row);
        int stepCol = Math.Sign(to.Column - from.Column);

        for (int i = 0; i < distance; i++)
        {
            var panelCurrentPos = new BlockPosition(from.Row + i * stepRow, from.Column + i * stepCol);
            var panelTargetPos = new BlockPosition(from.Row + (i + 1) * stepRow, from.Column + (i + 1) * stepCol);

            var pieceNumber = pastPuzzleState[panelCurrentPos];

            if (pieceNumber.IsZero()) continue;

            try
            {
                TweenAsync(pieceNumber, panelCurrentPos, panelTargetPos);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
    
    public async UniTask TweenAsync(BlockNumber num, BlockPosition from, BlockPosition to)
    {
        var rect = blockCreator.GetBlockRect(num);
        var zeroBlockView = blockCreator.GetBlock(BlockNumber.Zero());
        var zeroRect = blockCreator.GetBlockRect(BlockNumber.Zero());
        Vector2 fromPos = blockCreator.GetLocalPosition(from);
        Vector2 toPos = blockCreator.GetLocalPosition(to);

        await LMotion.Create(0f, 1f, tweenDuration)
            .WithEase(Ease.OutQuad)
            .Bind(t =>
            {
                rect.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
                if (!zeroBlockView.IsDragging)
                {
                    zeroRect.anchoredPosition = Vector2.Lerp(toPos, fromPos, t);
                }
            })
            .ToUniTask();

        // 最終位置を確定
        rect.anchoredPosition = toPos;
        if (!zeroBlockView.IsDragging)
        {
            zeroRect.anchoredPosition = fromPos;
        }
    }
    
    /// <summary>
    /// ドラッグ中の空ブロック位置を更新
    /// </summary>
    /// <param name="screenPosition">スクリーン座標</param>
    public void UpdateEmptyBlockPosition(Vector2 screenPosition)
    {
        RectTransform emptyBlockRect = blockCreator.GetBlockRect(BlockNumber.Zero());
        RectTransform canvasRect = blockCreator.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out Vector3 mouseWorldPosition
        );

        Vector3 centerToPivotOffset = emptyBlockRect.position
                                      - emptyBlockRect.TransformPoint(emptyBlockRect.rect.center);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            null,
            mouseWorldPosition + centerToPivotOffset
        );
        emptyBlockRect.position = screenPoint;
    }

    /// <summary>
    /// 空ブロックの位置をリセット
    /// </summary>
    public void ResetEmptyBlockPosition(PuzzleState puzzleState)
    {
        BlockPosition emptyPosition = puzzleState.EmptyBlockPosition;
        blockCreator.GetBlockRect(BlockNumber.Zero()).anchoredPosition
            = blockCreator.GetLocalPosition(emptyPosition);
    }
}
