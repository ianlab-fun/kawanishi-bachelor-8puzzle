using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// パズル操作に関する入力Observableを提供するサービス
/// </summary>
/// <remarks>
/// - MonoBehaviourとして実装し、GameObject管理を明確化
/// - R3のObservable.EveryUpdate()とUI拡張メソッドを使用
/// - キーボード入力とUIボタン入力を統合したObservableを提供
/// - State側がメソッドを呼び出した時点で監視開始、Disposeで自動終了
/// - VContainerでRegisterComponentInHierarchy()として登録
/// </remarks>
public class PuzzleInputService : MonoBehaviour
{
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Toggle reverseToggle;
    /// <summary>
    /// パズル移動方向の入力Observableを取得
    /// </summary>
    /// <returns>キーが押された瞬間のみ発火するObservable</returns>
    /// <remarks>
    /// State側で購読した時点で監視開始、Disposeで自動終了します。
    /// WASD/矢印キーをサポートします。
    /// </remarks>
    public Observable<Puzzle.MoveDirection> GetPuzzleMoveObservable()
    {
        return Observable.EveryUpdate()
            .Select(_ => GetCurrentMoveDirection())
            .Where(dir => dir.HasValue)
            .Select(dir => dir.Value);
    }

    /// <summary>
    /// Undo入力Observableを取得（Zキー + undoButton統合）
    /// </summary>
    /// <returns>Zキーまたはundoボタンが押された瞬間のみ発火するObservable</returns>
    public Observable<Unit> GetUndoObservable()
    {
        // キー入力Observable
        var keyObservable = Observable.EveryUpdate()
            .Where(_ => Input.GetKeyDown(KeyCode.Z))
            .Select(_ => Unit.Default);

        // UIボタン入力Observable（R3のUI拡張メソッド使用）
        var buttonObservable = undoButton.OnClickAsObservable();

        // 統合（どちらかが発火したらイベント発火）
        return Observable.Merge(keyObservable, buttonObservable);
    }

    /// <summary>
    /// Redo入力Observableを取得（Yキー + redoButton統合）
    /// </summary>
    /// <returns>Yキーまたはredoボタンが押された瞬間のみ発火するObservable</returns>
    public Observable<Unit> GetRedoObservable()
    {
        // キー入力Observable
        var keyObservable = Observable.EveryUpdate()
            .Where(_ => Input.GetKeyDown(KeyCode.Y))
            .Select(_ => Unit.Default);

        // UIボタン入力Observable
        var buttonObservable = redoButton.OnClickAsObservable();

        // 統合（どちらかが発火したらイベント発火）
        return Observable.Merge(keyObservable, buttonObservable);
    }

    /// <summary>
    /// 初期状態に戻すボタンのObservableを取得
    /// </summary>
    public Observable<Unit> GetResetObservable()
    {
        return resetButton.OnClickAsObservable();
    }

    /// <summary>
    /// Reverse Toggle値変更Observableを取得
    /// </summary>
    /// <returns>Toggle値が変更された時のbool値を発火するObservable</returns>
    public Observable<bool> GetReverseToggleObservable()
    {
        // Toggleの値変更をObservable化
        return reverseToggle.OnValueChangedAsObservable();
    }

    /// <summary>
    /// 現在フレームでの移動方向を取得
    /// </summary>
    /// <returns>移動方向（入力がない場合はnull）</returns>
    private Puzzle.MoveDirection? GetCurrentMoveDirection()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            return Puzzle.MoveDirection.Up;
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            return Puzzle.MoveDirection.Down;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            return Puzzle.MoveDirection.Left;
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            return Puzzle.MoveDirection.Right;
        }

        return null;
    }
}
