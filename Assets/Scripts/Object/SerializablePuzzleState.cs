using System;
using UnityEngine;

/// <summary>
/// PuzzleStateをInspectorでシリアライズ可能にするラッパークラス
/// Inspector上でNxNグリッドとして表示される
/// </summary>
[Serializable]
public class SerializablePuzzleState
{
    // ゴール状態を使用するかどうか
    [SerializeField]
    [Tooltip("チェックを外すとnull（ゴール状態なし）として扱われます")]
    private bool _isEnabled = true;

    // 1次元配列として保存（Custom PropertyDrawerでNxNグリッドとして表示）
    [SerializeField]
    [Tooltip("パズルの状態を表すNxNグリッド（0-8の数字、0は空きマス）")]
    private int[] _blocks;

    /// <summary>
    /// 有効かどうか
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// 2次元配列から作成するコンストラクタ
    /// </summary>
    public SerializablePuzzleState(int[,] blocks)
    {
        if (blocks == null)
            throw new ArgumentNullException(nameof(blocks));

        if (blocks.GetLength(0) != PuzzleState.RowCount || blocks.GetLength(1) != PuzzleState.ColumnCount)
            throw new ArgumentException(
                $"配列のサイズは{PuzzleState.RowCount}x{PuzzleState.ColumnCount}である必要があります。",
                nameof(blocks));

        _blocks = new int[PuzzleState.TotalCells];
        int index = 0;
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                _blocks[index++] = blocks[row, col];
            }
        }
    }

    /// <summary>
    /// PuzzleStateから作成するコンストラクタ
    /// </summary>
    public SerializablePuzzleState(PuzzleState puzzleState)
    {
        _blocks = new int[PuzzleState.TotalCells];
        int index = 0;
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                _blocks[index++] = puzzleState[new BlockPosition(row, col)];
            }
        }
    }

    /// <summary>
    /// PuzzleStateに変換
    /// バリデーションはPuzzleState.Createに任せる
    /// </summary>
    public PuzzleState ToPuzzleState()
    {
        var numbers = new int[PuzzleState.RowCount, PuzzleState.ColumnCount];
        int index = 0;
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                numbers[row, col] = _blocks[index++];
            }
        }

        return PuzzleState.Create(numbers);
    }
}
