using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

[Serializable]
public class BreadthFirstSearch : SearchAlgorithmBase, ISearchAlgorithm
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
            Queue<Puzzle> queue = new Queue<Puzzle>();

            _puzzleDataMap[puzzle.State.CurrentValue] = new PuzzleNodeData();
            queue.Enqueue(puzzle);

            while (queue.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();

                Puzzle currentPuzzle = queue.Dequeue();
                _progress?.Increment();

                if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
                    break;

                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Right, queue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Up, queue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Left, queue, goalState)) break;
                if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Down, queue, goalState)) break;
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
        Queue<Puzzle> queue = new Queue<Puzzle>();

        // 初期状態を登録してキューに積む
        _puzzleDataMap[puzzle.State.CurrentValue] = new PuzzleNodeData();
        queue.Enqueue(puzzle);

        while (queue.Any())
        {
            Puzzle currentPuzzle = queue.Dequeue();

            //*
            // ゴールチェック: ゴール指定時のみチェックして即終了
            if (goalState.HasValue && currentPuzzle.State.CurrentValue.Equals(goalState.Value))
            {
                return;
            }
            /*/
            PuzzleNodeData currentNodeData = _puzzleDataMap[currentPuzzle.State.CurrentValue];
            if (currentNodeData.Depth.Equals(16))
            {
                UnityEngine.Debug.Log("ゴールに到達しました！ (DFS)");
                return;
            }
            //*/

            // 各方向への移動を試してキューに積む（目標発見時は即終了）
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Right, queue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Up, queue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Left, queue, goalState)) return;
            if (TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Down, queue, goalState)) return;
        }

        // ループ終了後の処理
        // 隣接関係は探索中に増分的に構築済み
        if (goalState.HasValue)
        {
            // ゴール指定ありで到達できなかった = 失敗
            _puzzleDataMap.Clear();
        }
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したらノードデータを設定してキューに積む
    /// 目標状態を発見した場合はtrueを返して探索終了を通知
    /// </summary>
    /// <returns>目標状態を発見した場合true</returns>
    private bool TryEnqueueMove(Puzzle currentPuzzle, Puzzle.MoveDirection direction, Queue<Puzzle> queue, PuzzleState? goalState)
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

        // 目標発見時は即終了（キューに追加せず）
        if (goalState.HasValue && nextState.Equals(goalState.Value))
        {
            return true;
        }

        queue.Enqueue(nextPuzzle);
        return false;
    }
} 