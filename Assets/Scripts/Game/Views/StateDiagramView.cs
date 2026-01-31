using System;
using System.Collections.Generic;
using System.Linq;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 状態遷移図UIの表示制御
/// カーソル移動アニメーションで現在のモードを表現し、ボタンクリックで遷移
/// </summary>
public class StateDiagramView : MonoBehaviour
{
    private static StateDiagramView _instance;
    public static StateDiagramView Instance
    {
        get
        {
            // シーンリロード時に破棄済みオブジェクトを検出（UnityのObject.==nullで判定）
            if (_instance == null)
                _instance = FindAnyObjectByType<StateDiagramView>();
            _instance.EnsureInitialized();
            return _instance;
        }
    }

    // モードノード（7個）
    [Header("モードノード")]
    [SerializeField] private Button nodeGameSetup;
    [SerializeField] private Button nodeSearchProcess;
    [SerializeField] private Button nodePuzzleGame;
    [SerializeField] private Button nodePathSearch;
    [SerializeField] private Button nodeExplorationFromSearchProcess;
    [SerializeField] private Button nodeExplorationFromPuzzleGame;
    [SerializeField] private Button nodeExplorationFromPathSearch;

    [Header("カーソル")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private float moveDuration = 0.3f;

    private Dictionary<Type, Button> _nodeMap = null;
    private Dictionary<Type, Button> _explorationNodeMap = null;
    private Dictionary<Type, Type[]> _transitionMap = null;
    private MotionHandle _motionHandle;
    private Action<Type> _transitionAction;
    private ISearchSpacePositionReader _positionReader;

    [Inject]
    public void Construct(ISearchSpacePositionReader positionReader)
    {
        _positionReader = positionReader;
    }

    private void Awake()
    {
        _instance = this;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_nodeMap != null) return;

        _nodeMap = new Dictionary<Type, Button>
        {
            { typeof(GameSetupModeState), nodeGameSetup },
            { typeof(SearchProcessModeState), nodeSearchProcess },
            { typeof(PuzzleGameModeState), nodePuzzleGame },
            { typeof(PathSearchModeState), nodePathSearch },
        };

        _explorationNodeMap = new Dictionary<Type, Button>
        {
            { typeof(SearchProcessModeState), nodeExplorationFromSearchProcess },
            { typeof(PuzzleGameModeState), nodeExplorationFromPuzzleGame },
            { typeof(PathSearchModeState), nodeExplorationFromPathSearch },
        };

        // 各Stateから遷移可能なState
        _transitionMap = new Dictionary<Type, Type[]>
        {
            { typeof(GameSetupModeState), new[] { typeof(SearchProcessModeState), typeof(PuzzleGameModeState), typeof(PathSearchModeState) } },
            { typeof(SearchProcessModeState), new[] { typeof(GameSetupModeState), typeof(ExplorationModeState) } },
            { typeof(PuzzleGameModeState), new[] { typeof(GameSetupModeState), typeof(ExplorationModeState) } },
            { typeof(PathSearchModeState), new[] { typeof(GameSetupModeState), typeof(ExplorationModeState) } },
            { typeof(ExplorationModeState), new[] { typeof(GameSetupModeState) } },
        };

        // ボタンクリックイベント登録
        nodeGameSetup.onClick.AddListener(() => OnNodeClicked(typeof(GameSetupModeState)));
        nodeSearchProcess.onClick.AddListener(() => OnNodeClicked(typeof(SearchProcessModeState)));
        nodePuzzleGame.onClick.AddListener(() => OnNodeClicked(typeof(PuzzleGameModeState)));
        nodePathSearch.onClick.AddListener(() => OnNodeClicked(typeof(PathSearchModeState)));
        nodeExplorationFromSearchProcess.onClick.AddListener(() => OnNodeClicked(typeof(ExplorationModeState)));
        nodeExplorationFromPuzzleGame.onClick.AddListener(() => OnNodeClicked(typeof(ExplorationModeState)));
        nodeExplorationFromPathSearch.onClick.AddListener(() => OnNodeClicked(typeof(ExplorationModeState)));
    }

    public void SetTransitionAction(Action<Type> transitionAction)
    {
        _transitionAction = transitionAction;
    }

    private void OnNodeClicked(Type targetType)
    {
        _transitionAction(targetType);
    }

    /// <summary>
    /// 現在のモードに応じてカーソルを移動し、遷移可能なボタンを有効化
    /// </summary>
    public void SetCurrentMode(Type stateType, Type previousStateType)
    {
        if (_motionHandle.IsActive())
        {
            _motionHandle.Cancel();
        }

        Button targetNode = GetTargetNode(stateType, previousStateType);
        Vector2 startPosition = cursor.anchoredPosition;
        Vector2 targetPosition = ((RectTransform)targetNode.transform).anchoredPosition;

        _motionHandle = LMotion.Create(0f, 1f, moveDuration)
            .WithEase(Ease.OutQuad)
            .Bind(t => cursor.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t));

        UpdateButtonInteractable(stateType, previousStateType);
    }

    private void UpdateButtonInteractable(Type currentStateType, Type previousStateType)
    {
        Type[] allowedTransitions = _transitionMap[currentStateType];
        bool isExploration = currentStateType == typeof(ExplorationModeState);
        bool isInitialized = _positionReader.StatePositions.Any();

        // 通常ノード
        foreach (var (type, button) in _nodeMap)
        {
            bool isCurrent = type == currentStateType;
            bool canTransition = Array.Exists(allowedTransitions, t => t == type);
            bool canTransitionToPrevious = isExploration && type == previousStateType;

            // 未初期化ならGameSetupのみ遷移可能
            bool isGameSetup = type == typeof(GameSetupModeState);
            button.interactable = isCurrent || (isInitialized || isGameSetup) && (canTransition || canTransitionToPrevious);
        }

        // Explorationノード（未初期化なら全て無効）
        bool canTransitionToExploration = isInitialized && Array.Exists(allowedTransitions, t => t == typeof(ExplorationModeState));
        foreach (var (parentType, button) in _explorationNodeMap)
        {
            bool isCurrent = isExploration && parentType == previousStateType;
            bool canTransition = canTransitionToExploration && parentType == currentStateType;
            button.interactable = isCurrent || canTransition;
        }
    }

    private Button GetTargetNode(Type stateType, Type previousStateType)
    {
        return stateType == typeof(ExplorationModeState) ? _explorationNodeMap[previousStateType] : _nodeMap[stateType];
    }
}
