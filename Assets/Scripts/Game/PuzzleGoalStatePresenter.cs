using R3;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// PuzzleFinderViewとGoalStateHolderモデル間の調整役（MVPパターンのPresenter）
/// ゴール状態（Goal State）を管理し、Viewからの状態更新をリアクティブに購読してモデルへ反映
/// </summary>
public class PuzzleGoalStatePresenter : MonoBehaviour
{
    [SerializeField] private PuzzleFinderView finderView;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PuzzleGameBlockCreator puzzleGameBlockCreator;
    [SerializeField] private PuzzleView puzzleView;
    [SerializeField] private Button editButton;
    
    private GoalStateHolder _goalStateHolder;
    private Puzzle _puzzle;
    private ISearchSpacePositionReader _positionReader;

    [Inject]
    public void Construct(GoalStateHolder goalStateHolder, Puzzle puzzle, ISearchSpacePositionReader positionReader)
    {
        _goalStateHolder = goalStateHolder;
        _puzzle = puzzle;
        _positionReader = positionReader;
    }

    private void Awake()
    {
        // Viewからの状態更新をリアクティブに購読
        finderView.AppliedState
            .Where(state => state.HasValue)
            .Select(state => state.Value)
            .Subscribe(state =>
            {
                if (!_positionReader.StatePositions.TryGetValue(state, out Vector3 _)) 
                    return;

                _goalStateHolder.SetPuzzle(state);
                puzzleView.SetPositionImmediate(state);
            })
            .AddTo(this);

        puzzleGameBlockCreator.Initialize(_goalStateHolder.GoalState.Value);
    }

    /// <summary>
    /// ゴール状態をFinderビューに反映
    /// </summary>
    public void StateToFinder()
    {
        finderView.SetPuzzle(_goalStateHolder.GoalState.Value);
    }

    /// <summary>
    /// ゴール状態のUIを更新
    /// </summary>
    public void UpdateView()
    {
        puzzleView.SetPositionImmediate(_goalStateHolder.GoalState.Value);
    }

    /// <summary>
    /// ゴール状態を検索し、カメラを移動
    /// </summary>
    public void FindState()
    {
        Vector3 position = _positionReader.StatePositions[_goalStateHolder.GoalState ?? _puzzle.State.CurrentValue];
        cameraFollow.MoveToPosition(position);
    }
}
