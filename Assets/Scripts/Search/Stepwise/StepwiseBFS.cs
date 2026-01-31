using System;
using System.Collections.Generic;
using System.Linq;
using R3;

/// <summary>
/// ステップ実行可能な幅優先探索アルゴリズム
/// </summary>
[Serializable]
public class StepwiseBFS : SearchAlgorithmBase, IStepwiseSearchAlgorithm
{
    private enum SearchState
    {
        NotInitialized,  // 初期化前
        Running,         // 実行中
        Completed        // 完了
    }

    private Queue<Puzzle> _queue = new();
    private PuzzleState? _goalState;
    private SearchState _state = SearchState.NotInitialized;
    private Subject<SearchStepResult> _onStepExecutedSubject = new();
    public bool IsCompleted => _state == SearchState.Completed;
    public Observable<SearchStepResult> OnStepExecuted => _onStepExecutedSubject;

    public void Initialize(Puzzle initialPuzzle, PuzzleState? goalState = null)
    {
        _puzzleDataMap.Clear();
        _queue.Clear();
        _goalState = goalState;
        _state = SearchState.Running;

        // 初期状態を登録してキューに積む
        _puzzleDataMap[initialPuzzle.State.CurrentValue] = new PuzzleNodeData();
        _queue.Enqueue(initialPuzzle);
    }

    public SearchStepResult Step()
    {
        Puzzle currentPuzzle = _queue.Dequeue();
        PuzzleState expandedState = currentPuzzle.State.CurrentValue;
        PuzzleNodeData expandedNodeData = _puzzleDataMap[expandedState];

        // ゴールチェック: ゴール指定時のみチェックして即終了
        if (_goalState.HasValue && expandedState.Equals(_goalState.Value))
        {
            _state = SearchState.Completed;
            SearchStepResult goalResult = new SearchStepResult(expandedState, expandedNodeData);
            _onStepExecutedSubject.OnNext(goalResult);
            return goalResult;
        }

        // 各方向への移動を試してキューに積む（視覚的に直感的な順序：右、上、左、下）
        TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Right);
        TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Up);
        TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Left);
        TryEnqueueMove(currentPuzzle, Puzzle.MoveDirection.Down);

        // キューが空になったら探索完了
        if (!_queue.Any())
        {
            _state = SearchState.Completed;
            SearchStepResult completedResult = new SearchStepResult(expandedState, expandedNodeData);
            _onStepExecutedSubject.OnNext(completedResult);
            return completedResult;
        }

        SearchStepResult result = new SearchStepResult(expandedState, expandedNodeData);
        _onStepExecutedSubject.OnNext(result);
        return result;
    }

    public void Reset()
    {
        _puzzleDataMap.Clear();
        _queue.Clear();
        _state = SearchState.NotInitialized;
        _goalState = null;
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したらノードデータを完全に設定してキューに積む
    /// 同時に双方向の隣接関係を構築する
    /// </summary>
    private void TryEnqueueMove(Puzzle currentPuzzle, Puzzle.MoveDirection direction)
    {
        var nextPuzzle = CreateNextPuzzle(currentPuzzle, direction);
        if (nextPuzzle == null)
            return;

        PuzzleState currentState = currentPuzzle.State.CurrentValue;
        PuzzleState nextState = nextPuzzle.State.CurrentValue;

        // 既に訪問済みの場合は隣接関係だけ追加してスキップ
        if (_puzzleDataMap.ContainsKey(nextState))
        {
            AddBidirectionalAdjacency(currentState, nextState);
            return;
        }

        // 新規発見: 子ノードを作成してキューに積む
        PuzzleNodeData newNodeData = CreateChildNodeData(currentState);
        _puzzleDataMap[nextState] = newNodeData;
        _queue.Enqueue(nextPuzzle);

        // 双方向の隣接関係を追加
        AddBidirectionalAdjacency(currentState, nextState);
    }
}
