using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class PuzzleGameBlockCreator : MonoBehaviour
{
    [SerializeField] private float spacing = 20.0f;
    [SerializeField] private float size = 100.0f;
    [SerializeField] private RectTransform gameNumberBlockPrefab;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private Toggle visibleEmptyToggle;
    
    private BlockView[] children = new BlockView[PuzzleState.TotalCells];
    private Vector2?[] positions = new Vector2?[PuzzleState.TotalCells];
    private IObjectResolver _resolver;

    // インスタンスイベント：このCreatorが管理するBlockViewからのイベントを統合
    private event Action<BlockNumber> OnBlockClicked;
    public event Action<Vector2> OnBlockDragging;
    public event Action OnBlockDragEnded;

    [Inject]
    public void Construct(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    public void Initialize(PuzzleState puzzleState)
    {
        // 全ブロック（0〜8）を事前に生成
        for (int i = 0; i < PuzzleState.TotalCells; i++)
        {
            var number = new BlockNumber(i);
            if (children[number] == null)
            {
                children[number] = _resolver.Instantiate(gameNumberBlockPrefab, transform).GetComponent<BlockView>();
                children[number].GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
                children[number].SetBlockNumber(number);

                // 各BlockViewのイベントをこのCreatorのイベントに転送
                children[number].OnClicked += (clickedNumber) => OnBlockClicked?.Invoke(clickedNumber);
                children[number].OnDragging += (position) => OnBlockDragging?.Invoke(position);
                children[number].OnDragEnded += () => OnBlockDragEnded?.Invoke();
            }
        }

        // 各ブロックを正しい位置に配置
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int column = 0; column < PuzzleState.ColumnCount; column++)
            {
                var blockPos = new BlockPosition(row, column);
                BlockNumber number = puzzleState[blockPos];
                GetBlockRect(number).anchoredPosition = GetLocalPosition(blockPos);
            }
        }
        visibleEmptyToggle.onValueChanged.AddListener(GetBlock(BlockNumber.Zero()).gameObject.SetActive);
    }

    public Vector2 GetLocalPosition(BlockPosition position)
    {
        if (!positions[position].HasValue)
        {
            float posX = (position.Column - 1) * (size + spacing);
            float posY = (1 - position.Row) * (size + spacing);
            positions[position] = new Vector2(posX, posY);
        }
        return positions[position].Value;
    }
    
    public BlockView GetBlock(BlockNumber number) => children[number];

    public RectTransform GetBlockRect(BlockNumber number) => GetBlock(number).GetComponent<RectTransform>();

    /// <summary>
    /// ブロッククリック入力Observable
    /// </summary>
    /// <returns>クリックされたBlockNumberを発火するObservable</returns>
    public Observable<BlockNumber> GetBlockClickObservable()
    {
        return Observable.FromEvent<BlockNumber>(
            h => OnBlockClicked += h,
            h => OnBlockClicked -= h)
            .Where(clickedNumber => !clickedNumber.IsZero()); // ゼロブロックは除外
    }

    /// <summary>
    /// 全ブロックの操作可能状態を更新
    /// 空きスペースと同じ行・列のブロックのみ操作可能
    /// </summary>
    public void UpdateBlockInteractivity(PuzzleState puzzleState)
    {
        BlockPosition emptyPos = puzzleState.EmptyBlockPosition;

        for (int i = 0; i < PuzzleState.TotalCells; i++)
        {
            var number = new BlockNumber(i);

            // 空ブロックは常に不透明
            if (number.IsZero())
            {
                children[number].SetInteractable(true);
                continue;
            }

            BlockPosition blockPos = puzzleState.FindNumberBlockPosition(number);
            bool canMove = blockPos.Row == emptyPos.Row || blockPos.Column == emptyPos.Column;
            children[number].SetInteractable(canMove);
        }
    }
}
