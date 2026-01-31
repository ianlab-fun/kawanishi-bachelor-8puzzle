using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// A*探索アルゴリズム
/// マンハッタン距離をヒューリスティック関数として使用し、最適経路を探索
/// </summary>
[Serializable]
public class AStarSearch : SearchAlgorithmBase, ISearchAlgorithm
{
    public async UniTask SearchAsync(
        Puzzle puzzle,
        PuzzleState? goalState,
        SearchProgress progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        await UniTask.SwitchToThreadPool();

        try
        {
            _puzzleDataMap.Clear();
            var priorityQueue = new Utils.PriorityQueue<Puzzle, int>();

            // 初期状態の設定
            var initialNodeData = new PuzzleNodeData();
            if (goalState.HasValue)
            {
                initialNodeData.HCost = CalculateManhattanDistance(puzzle.State.CurrentValue, goalState.Value);
            }
            _puzzleDataMap[puzzle.State.CurrentValue] = initialNodeData;
            priorityQueue.Enqueue(puzzle, initialNodeData.FCost);

            // 探索ループ
            while (priorityQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Puzzle currentPuzzle = priorityQueue.Dequeue();
                _progress?.Increment();

                if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
                    break;

                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Right, priorityQueue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Up, priorityQueue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Left, priorityQueue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Down, priorityQueue, goalState)) break;
            }

            // ゴール指定ありで到達できなかった場合
            if (goalState.HasValue && !_puzzleDataMap.ContainsKey(goalState.Value))
            {
                _puzzleDataMap.Clear();
            }
        }
        finally
        {
            _progress = null;
            await UniTask.SwitchToMainThread();
        }
    }

    public void Search(Puzzle puzzle, PuzzleState? goalState = null)
    {
        _puzzleDataMap.Clear();
        var priorityQueue = new Utils.PriorityQueue<Puzzle, int>();

        // 初期状態の設定
        var initialNodeData = new PuzzleNodeData();
        if (goalState.HasValue)
        {
            initialNodeData.HCost = CalculateManhattanDistance(puzzle.State.CurrentValue, goalState.Value);
        }
        _puzzleDataMap[puzzle.State.CurrentValue] = initialNodeData;
        priorityQueue.Enqueue(puzzle, initialNodeData.FCost);

        // 探索ループ
        while (priorityQueue.Count > 0)
        {
            Puzzle currentPuzzle = priorityQueue.Dequeue();

            // ゴールチェック: ゴール指定時のみチェックして即終了
            if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
            {
                return;
            }

            // 4方向への移動を試行（目標発見時は即終了）
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Right, priorityQueue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Up, priorityQueue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Left, priorityQueue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Down, priorityQueue, goalState)) return;
        }

        // ループ終了後の処理
        if (goalState.HasValue)
        {
            // ゴール指定ありで到達できなかった = 失敗
            _puzzleDataMap.Clear();
        }
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したらノードデータを設定して優先度付きキューに積む
    /// 目標状態を発見した場合はtrueを返して探索終了を通知
    /// </summary>
    /// <returns>目標状態を発見した場合true</returns>
    private bool TryEnqueueMove(
        Puzzle currentPuzzle,
        Puzzle.MoveDirection direction,
        Utils.PriorityQueue<Puzzle, int> queue,
        PuzzleState? goalPuzzle)
    {
        // 移動を試行
        var nextPuzzle = CreateNextPuzzle(currentPuzzle, direction);
        if (nextPuzzle == null) return false;

        PuzzleState currentState = currentPuzzle.State.CurrentValue;
        PuzzleState nextState = nextPuzzle.State.CurrentValue;

        // 既に訪問済みの場合は隣接関係だけ追加してスキップ
        if (_puzzleDataMap.ContainsKey(nextState))
        {
            AddBidirectionalAdjacency(currentState, nextState);
            return false;
        }

        // 子ノードの作成とコスト計算
        PuzzleNodeData nextNodeData = CreateChildNodeData(currentState);
        if (goalPuzzle.HasValue)
        {
            nextNodeData.HCost = CalculateManhattanDistance(nextState, goalPuzzle.Value);
        }

        _puzzleDataMap[nextState] = nextNodeData;
        AddBidirectionalAdjacency(currentState, nextState);

        // 目標発見時は即終了（キューに追加せず）
        if (goalPuzzle.HasValue && nextState.Equals(goalPuzzle.Value))
        {
            return true;
        }

        queue.Enqueue(nextPuzzle, nextNodeData.FCost);
        return false;
    }

    /// <summary>
    /// マンハッタン距離を計算
    /// 各ブロック（空白以外）が目標位置までに必要な最小移動回数の合計
    /// </summary>
    private int CalculateManhattanDistance(PuzzleState current, PuzzleState goal)
    {
        int distance = 0;

        // 各ブロック（空白以外）のマンハッタン距離を計算
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                BlockPosition currentPos = new BlockPosition(row, col);
                BlockNumber blockNumber = current[currentPos];

                // 空白ブロック（0）はスキップ
                if (blockNumber.IsZero()) continue;

                // 目標位置を探索
                BlockPosition goalPos = goal.FindNumberBlockPosition(blockNumber);

                // マンハッタン距離を加算
                distance += Math.Abs(currentPos.Row - goalPos.Row) +
                           Math.Abs(currentPos.Column - goalPos.Column);
            }
        }

        return distance;
    }
}
