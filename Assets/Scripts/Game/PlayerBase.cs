using R3;

/// <summary>
/// 再生プレイヤーの基底クラス
/// 解法再生・ステップ実行の共通インターフェースを提供
/// </summary>
public abstract class PlayerBase
{
    public enum State { Idle, Playing, Paused, Completed }

    protected readonly ReactiveProperty<State> _state = new(State.Idle);

    public ReadOnlyReactiveProperty<State> CurrentState => _state;
    public abstract bool CanStepForward { get; }
    public abstract bool CanStepBack { get; }

    public void Play()
    {
        if (_state.CurrentValue != State.Paused) return;
        _state.Value = State.Playing;
    }

    public void Pause()
    {
        if (_state.CurrentValue != State.Playing) return;
        _state.Value = State.Paused;
    }

    /// <summary>
    /// 1ステップ前進（Template Method）
    /// </summary>
    public void StepForward()
    {
        if (!CanStepForward) return;

        ExecuteStepForward();
        CheckCompletion();
    }

    /// <summary>
    /// 1ステップ後退（Template Method）
    /// </summary>
    public void StepBack()
    {
        if (!CanStepBack) return;

        ExecuteStepBack();

        // Completedから戻ったらPausedに
        if (_state.CurrentValue == State.Completed)
        {
            _state.Value = State.Paused;
        }
    }

    /// <summary>
    /// 初期状態にリセット（Template Method）
    /// </summary>
    public void Reset()
    {
        ExecuteReset();
        _state.Value = State.Idle;
    }

    /// <summary>
    /// 派生クラスで実装する実際の処理
    /// </summary>
    protected abstract void ExecuteStepForward();
    protected abstract void ExecuteStepBack();
    protected abstract void ExecuteReset();

    private void CheckCompletion()
    {
        if (!CanStepForward)
        {
            _state.Value = State.Completed;
        }
    }
}