using R3;
using UnityEngine;

/// <summary>
/// ゲームモード切り替えに関する入力Observableを提供するサービス
/// </summary>
public class GameModeInputService : MonoBehaviour
{
    /// <summary>
    /// モード切り替え入力Observableを取得（右クリック）
    /// </summary>
    public Observable<Unit> GetModeChangeObservable()
    {
        return Observable.EveryUpdate()
            .Where(_ => Input.GetMouseButtonDown(1)) // 右クリック
            .Select(_ => Unit.Default);
    }

    /// <summary>
    /// キャンセルキー(Esc)入力Observableを取得
    /// </summary>
    public Observable<Unit> GetCancelKeyObservable()
    {
        return Observable.EveryUpdate()
            .Where(_ => Input.GetKeyDown(KeyCode.Q))
            .Select(_ => Unit.Default);
    }
}