using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// ゲームモード状態管理の抽象基底クラス
/// テンプレートメソッドパターンを使用し、状態遷移ロジックを共通化
/// </summary>
public abstract class GameModeStateBase : MonoBehaviour
{
    // 状態管理を static で保持
    private static GameModeStateBase _currentState;
    private static GameModeStateBase _previousState;  // 前のステートを記憶
    private static Dictionary<Type, GameModeStateBase> _states = new Dictionary<Type, GameModeStateBase>();

    /// <summary>
    /// モードのローカライズ表示名（サブクラスでオーバーライド）
    /// </summary>
    protected virtual LocalizedString DisplayName => null;

    /// <summary>
    /// 前のモードの表示名を取得
    /// </summary>
    protected static LocalizedString PreviousStateDisplayName => _previousState?.DisplayName;

    private void Awake()
    {
        _currentState = null;
        _previousState = null;

        StateDiagramView.Instance.SetTransitionAction(TransitionTo);

        // 各Stateが起動時に自分を登録
        _states[GetType()] = this;

        // サブクラスの初期化処理を呼ぶ
        OnAwake();
    }

    /// <summary>
    /// サブクラスでAwake時の処理が必要な場合にオーバーライド
    /// </summary>
    protected virtual void OnAwake() { }

    /// <summary>
    /// 指定されたゲームモードへ遷移します（テンプレートメソッド）
    /// </summary>
    /// <typeparam name="T">遷移先のState型</typeparam>
    /// <remarks>
    /// 状態遷移のロジックを一箇所に集約し、以下を保証します:
    /// - 現在のStateの終了処理（OnExit）
    /// - 新しいStateの開始処理（OnEnter）
    /// </remarks>
    protected static void TransitionTo<T>() where T : GameModeStateBase
    {
        TransitionTo(typeof(T));
    }

    /// <summary>
    /// 指定されたゲームモードへ遷移します（Type引数版）
    /// </summary>
    /// <param name="stateType">遷移先のState型</param>
    private static void TransitionTo(Type stateType)
    {
        // 現在のStateから抜ける
        _currentState?.OnExit();

        // 前のStateを記録
        _previousState = _currentState;

        // 新しいStateに遷移 - DictionaryでO(1)アクセス
        _currentState = _states[stateType];
        Debug.Log($"Transitioning to {_currentState.GetType().Name}");
        // 新しいStateに入る
        _currentState.OnEnter();

        // 状態遷移図を更新
        StateDiagramView.Instance.SetCurrentMode(stateType, _previousState?.GetType());
    }

    /// <summary>
    /// 前のゲームモードへ戻ります
    /// </summary>
    /// <remarks>
    /// 前のStateが存在しない場合は警告を出して何もしません
    /// </remarks>
    protected void TransitionToPrevious()
    {
        if (_previousState == null)
        {
            Debug.LogWarning("No previous state to transition to");
            return;
        }

        TransitionTo(_previousState.GetType());
    }

    /// <summary>
    /// State開始時の処理（サブクラスで実装）
    /// </summary>
    protected abstract void OnEnter();

    /// <summary>
    /// State終了時の処理（サブクラスで実装）
    /// </summary>
    protected abstract void OnExit();
}
