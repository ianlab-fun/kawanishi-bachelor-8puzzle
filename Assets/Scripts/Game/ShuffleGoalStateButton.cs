using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// ゴール状態をランダムにシャッフルするボタンのPresenter
/// プレハブ内で処理を完結させる独立したコンポーネント
/// </summary>
public class ShuffleGoalStateButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private PuzzleFinderView finderView;

    private ISearchSpacePositionReader _positionReader;

    [Inject]
    public void Construct(ISearchSpacePositionReader positionReader)
    {
        _positionReader = positionReader;
    }

    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var statePositions = _positionReader.StatePositions;

        PuzzleState randomState;
        if (statePositions.Any())
        {
            // 状態空間が生成済みの場合: 状態空間内からランダムに1つ選択
            var randomIndex = Random.Range(0, statePositions.Count);
            randomState = statePositions.Keys.ElementAt(randomIndex);
        }
        else
        {
            // 状態空間が未生成の場合: 362,880通りの全パズル状態からランダムに生成
            randomState = PuzzleState.CreateRandom();
        }

        finderView.SetPuzzle(randomState);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}
