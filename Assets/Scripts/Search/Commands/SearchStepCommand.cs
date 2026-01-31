using System;

/// <summary>
/// 探索アルゴリズムの1ステップ実行を表すコマンド
/// InvokeCommandで履歴管理される
/// </summary>
public class SearchStepCommand : ICommand
{
    private readonly IStepwiseSearchAlgorithm _algorithm;
    private SearchStepResult _result;

    public SearchStepCommand(IStepwiseSearchAlgorithm algorithm)
    {
        _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
    }

    public void Execute()
    {
        _result = _algorithm.Step();
    }

    public PuzzleState GetBoardState()
    {
        if (_result == null)
        {
            throw new InvalidOperationException("Execute() を先に呼び出してください。");
        }
        return _result.ExpandedState;
    }

    /// <summary>
    /// Undo機能は未実装
    /// 将来的にMemento Patternでアルゴリズムの内部状態を復元する必要がある
    /// </summary>
    public void Undo()
    {
        throw new NotImplementedException("Undo機能はMemento Patternの実装が必要です。");
    }
}
