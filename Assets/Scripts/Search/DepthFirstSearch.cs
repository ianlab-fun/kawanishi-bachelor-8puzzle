using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

[Serializable]
public class DepthFirstSearch : SearchAlgorithmBase, ISearchAlgorithm
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
            Stack<Puzzle> stack = new Stack<Puzzle>();

            _puzzleDataMap[puzzle.State.CurrentValue] = new PuzzleNodeData();
            stack.Push(puzzle);

            while (stack.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();

                Puzzle currentPuzzle = stack.Pop();
                _progress?.Increment();

                if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
                    break;

                if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Down, stack, goalState)) break;
                if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Left, stack, goalState)) break;
                if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Up, stack, goalState)) break;
                if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Right, stack, goalState)) break;
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
        Stack<Puzzle> stack = new Stack<Puzzle>();

        // 初期状態を登録してスタックに積む
        _puzzleDataMap[puzzle.State.CurrentValue] = new PuzzleNodeData();
        stack.Push(puzzle);

        while (stack.Any())
        {
            Puzzle currentPuzzle = stack.Pop();

            // ゴールチェック: ゴール指定時のみチェックして即終了
            if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
            {
                return;
            }

            // 各方向への移動を試してスタックに積む（目標発見時は即終了）
            if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Down, stack, goalState)) return;
            if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Left, stack, goalState)) return;
            if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Up, stack, goalState)) return;
            if (TryPushMove(currentPuzzle, Puzzle.MoveDirection.Right, stack, goalState)) return;
        }

        // ループ終了後の処理
        if (goalState.HasValue)
        {
            // ゴール指定ありで到達できなかった = 失敗
            _puzzleDataMap.Clear();
        }
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したらノードデータを設定してスタックに積む
    /// 目標状態を発見した場合はtrueを返して探索終了を通知
    /// </summary>
    /// <returns>目標状態を発見した場合true</returns>
    private bool TryPushMove(Puzzle currentPuzzle, Puzzle.MoveDirection direction, Stack<Puzzle> stack, PuzzleState? goalState)
    {
        var nextPuzzle = CreateNextPuzzle(currentPuzzle, direction);
        if (nextPuzzle == null)
            return false;

        PuzzleState currentState = currentPuzzle.State.CurrentValue;
        PuzzleState nextState = nextPuzzle.State.CurrentValue;

        // 既に訪問済みの場合は隣接関係だけ追加してスキップ
        if (_puzzleDataMap.ContainsKey(nextState))
        {
            AddBidirectionalAdjacency(currentState, nextState);
            return false;
        }

        // 新規発見: 子ノードを作成
        PuzzleNodeData nextNodeData = CreateChildNodeData(currentState);
        _puzzleDataMap[nextState] = nextNodeData;
        AddBidirectionalAdjacency(currentState, nextState);

        // 目標発見時は即終了（スタックに追加せず）
        if (goalState.HasValue && nextState.Equals(goalState.Value))
        {
            return true;
        }

        stack.Push(nextPuzzle);
        return false;
    }
}

