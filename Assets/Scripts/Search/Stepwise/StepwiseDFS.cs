using System;
using System.Collections.Generic;
using System.Linq;
using R3;

/// <summary>
/// ステップ実行可能な深さ優先探索アルゴリズム
/// </summary>
[Serializable]
public class StepwiseDFS : SearchAlgorithmBase, IStepwiseSearchAlgorithm
{
    private enum SearchState
    {
        NotInitialized,  // 初期化前
        Running,         // 実行中
        Completed        // 完了
    }

    private Stack<Puzzle> _stack = new();
    private PuzzleState? _goalState;
    private SearchState _state = SearchState.NotInitialized;
    private Subject<SearchStepResult> _onStepExecutedSubject = new();
    public bool IsCompleted => _state == SearchState.Completed;
    public Observable<SearchStepResult> OnStepExecuted => _onStepExecutedSubject;

    public void Initialize(Puzzle initialPuzzle, PuzzleState? goalState = null)
    {
        _puzzleDataMap.Clear();
        _stack.Clear();
        _goalState = goalState;
        _state = SearchState.Running;

        // 初期状態を登録してスタックに積む
        _puzzleDataMap[initialPuzzle.State.CurrentValue] = new PuzzleNodeData();
        _stack.Push(initialPuzzle);
    }

    public SearchStepResult Step()
    {
        Puzzle currentPuzzle = _stack.Pop();
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

        // 各方向への移動を試してスタックに積む（LIFO逆順なので、視覚的順序の逆：下、左、上、右）
        TryPushMove(currentPuzzle, Puzzle.MoveDirection.Down);
        TryPushMove(currentPuzzle, Puzzle.MoveDirection.Left);
        TryPushMove(currentPuzzle, Puzzle.MoveDirection.Up);
        TryPushMove(currentPuzzle, Puzzle.MoveDirection.Right);

        // スタックが空になったら探索完了
        if (!_stack.Any())
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
        _stack.Clear();
        _state = SearchState.NotInitialized;
        _goalState = null;
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したらノードデータを完全に設定してスタックに積む
    /// 同時に双方向の隣接関係を構築する
    /// </summary>
    private void TryPushMove(Puzzle currentPuzzle, Puzzle.MoveDirection direction)
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

        // 新規発見: 子ノードを作成してスタックに積む
        PuzzleNodeData newNodeData = CreateChildNodeData(currentState);
        _puzzleDataMap[nextState] = newNodeData;
        _stack.Push(nextPuzzle);

        // 双方向の隣接関係を追加
        AddBidirectionalAdjacency(currentState, nextState);
    }
}
