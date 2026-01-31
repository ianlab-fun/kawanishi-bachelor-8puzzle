using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LitMotion;
using VContainer;

/// <summary>
/// 検索空間におけるエッジ（状態間の遷移）の管理を担当するクラス
/// エッジの計算・保持・色分け表示を行う
/// </summary>
public class SearchSpaceEdgeManager : MonoBehaviour
{
    [SerializeField] private PuzzleVisualizer puzzleVisualizer;
    [SerializeField] private float edgeAnimationDuration = 0.4f;
    [SerializeField] private float edgeThickness = 0.1f; // 通常エッジの太さ
    [SerializeField] private float routeEdgeThickness = 0.1f; // ルートエッジの太さ

    private ISearchSpacePositionReader _positionReader;
    private ISearchSpaceEdgeCalculator _edgeCalculator;
    private Dictionary<PuzzleState, PuzzleNodeData> _nodeData;
    private Dictionary<PuzzleState, List<Matrix4x4>> _allEdges;
    private Puzzle _puzzle;

    [Inject]
    public void Construct(ISearchSpacePositionReader positionReader, ISearchSpaceEdgeCalculator edgeCalculator)
    {
        _positionReader = positionReader;
        _edgeCalculator = edgeCalculator;
    }

    /// <summary>
    /// 通常エッジの太さを取得する（ステップ実行のエッジ計算用）
    /// </summary>
    public float NormalEdgeThickness => edgeThickness;

    /// <summary>
    /// ルートエッジの太さを取得する（パズルブロックの厚み計算用）
    /// </summary>
    public float EdgeThickness => routeEdgeThickness;

    /// <summary>
    /// エッジ管理の初期化
    /// 全エッジを計算し、PuzzleVisualizerに設定する
    /// </summary>
    /// <param name="nodeData">検索結果のノードデータ</param>
    /// <param name="puzzle">パズルインスタンス</param>
    public void Initialize(
        Dictionary<PuzzleState, PuzzleNodeData> nodeData,
        Puzzle puzzle)
    {
        _nodeData = nodeData;
        _puzzle = puzzle;

        // 全エッジを計算（一度だけ実行）
        _allEdges = CalculateAllEdges();

        // 自分の担当部分（エッジ）をVisualizerに設定
        var allEdgesFlat = _allEdges.Values.SelectMany(list => list).ToArray();
        puzzleVisualizer.InitializeEdges(allEdgesFlat);
    }

    /// <summary>
    /// 指定された経路リストを可視化する
    /// searchResultを指定すると、探索済みエッジも色分けして表示
    /// </summary>
    /// <param name="routeList">可視化したい状態リスト（解答経路）</param>
    /// <param name="searchResult">探索結果（nullの場合は解答経路のみ表示）</param>
    public void VisualizeRoutes(List<PuzzleState> routeList, Dictionary<PuzzleState, PuzzleNodeData> searchResult = null)
    {
        var solutionStates = new HashSet<PuzzleState>(routeList);

        // 解答経路のエッジを計算
        List<Matrix4x4> routeEdges = new List<Matrix4x4>();
        for (int i = 0; i < routeList.Count - 1; i++)
        {
            routeEdges.Add(_edgeCalculator.CalculateEdgeMatrix(routeList[i], routeList[i + 1], routeEdgeThickness));
        }

        // 通常エッジ（全エッジ）
        var normalEdges = _allEdges.Values.SelectMany(list => list);

        if (searchResult != null)
        {
            // 探索済みエッジ = searchResultの親子関係から、解答経路を除いたもの
            List<Matrix4x4> exploredEdges = new List<Matrix4x4>();
            foreach (var (state, nodeData) in searchResult)
            {
                if (!nodeData.Parent.HasValue) continue;
                var parent = nodeData.Parent.Value;
                if (solutionStates.Contains(state) && solutionStates.Contains(parent)) continue;

                exploredEdges.Add(_edgeCalculator.CalculateEdgeMatrix(parent, state, routeEdgeThickness));
            }
            puzzleVisualizer.SetEdgesWithExplored(normalEdges, exploredEdges, routeEdges);
        }
        else
        {
            puzzleVisualizer.SetEdges(normalEdges, routeEdges);
        }
        puzzleVisualizer.SetRouteEdgeFillAmount(1);
    }

    /// <summary>
    /// 訪問済みルートを可視化する
    /// 通常エッジとルートエッジを色分けして表示
    /// </summary>
    public void VisualizeRoutes()
    {
        var visitedRouteList = _puzzle.GetVisitedRouteList();
        Debug.Log($"Total states: {_positionReader.StatePositions.Count}");
        Debug.Log($"Visited routes: {visitedRouteList.Count}");

        // オーバーロードに委譲
        VisualizeRoutes(visitedRouteList);
    }

    /// <summary>
    /// ステップ実行用：前回のエッジをnormalに追加してから、新エッジをrouteに表示
    /// </summary>
    /// <param name="previousStepEdges">前回のステップで発見されたエッジ（normalに追加）</param>
    /// <param name="currentStepEdges">今回のステップで発見されたエッジ（routeに表示）</param>
    public void VisualizeStepEdges(List<Matrix4x4> previousStepEdges, List<Matrix4x4> currentStepEdges)
    {
        // 前回のrouteEdgeをnormalEdgeに追加（元の細さのまま）
        if (previousStepEdges.Any())
        {
            puzzleVisualizer.AddNormalEdges(previousStepEdges);
        }

        // 今回の新エッジをrouteEdge（ハイライト）として表示（太く変更）
        var thickRouteEdges = currentStepEdges.Select(edge => RescaleEdgeThickness(edge, edgeThickness, routeEdgeThickness)).ToList();
        puzzleVisualizer.SetRouteEdges(thickRouteEdges);
    }

    /// <summary>
    /// エッジをクリアする（ステップ実行のリセット用）
    /// </summary>
    public void ClearEdges()
    {
        puzzleVisualizer.ClearEdges();
    }

    /// <summary>
    /// RouteEdgeをfillAmountアニメーションで表示する
    /// </summary>
    public void AnimateRouteEdges()
    {
        LMotion.Create(0f, 1f, edgeAnimationDuration)
            .Bind(value => puzzleVisualizer.SetRouteEdgeFillAmount(value))
            .AddTo(this);
    }

    /// <summary>
    /// 全エッジを計算する
    /// 完全な状態空間グラフを親→子方向で描画（重複回避、O(N×D) D=平均次数）
    /// 親子関係を優先し、そうでない場合はハッシュ値で方向決定
    /// </summary>
    private Dictionary<PuzzleState, List<Matrix4x4>> CalculateAllEdges()
    {
        Dictionary<PuzzleState, List<Matrix4x4>> edges = new Dictionary<PuzzleState, List<Matrix4x4>>();

        foreach (var (currentState, nodeData) in _nodeData)
        {
            foreach (PuzzleState adjacentState in nodeData.AdjacentStates)
            {
                // 自分の親が隣接状態 → 隣接状態が親なので、相手側で描画される
                bool isParentEdge = nodeData.Parent?.Equals(adjacentState) == true;

                // 隣接状態の親が自分 → 自分が親なので描画
                bool isChildEdge = _nodeData[adjacentState].Parent?.Equals(currentState) == true;

                // 親子関係なし（横方向エッジ）→ ハッシュ値で決定
                bool isHorizontalEdge = currentState.GetHashCode() < adjacentState.GetHashCode();

                // スキップ条件: 親エッジ、または（子でも横でもない）
                if (isParentEdge || !(isChildEdge || isHorizontalEdge))
                    continue;

                Matrix4x4 matrix = _edgeCalculator.CalculateEdgeMatrix(currentState, adjacentState, edgeThickness);

                if (!edges.ContainsKey(currentState))
                {
                    edges[currentState] = new List<Matrix4x4>();
                }
                edges[currentState].Add(matrix);
            }
        }

        return edges;
    }

    /// <summary>
    /// エッジのスケール（太さ）を変更する
    /// normalEdgeをrouteEdge用に太くする際に使用
    /// </summary>
    /// <param name="edgeMatrix">元のエッジ行列</param>
    /// <param name="fromThickness">元の太さ</param>
    /// <param name="toThickness">新しい太さ</param>
    /// <returns>スケール変更後のエッジ行列</returns>
    private static Matrix4x4 RescaleEdgeThickness(Matrix4x4 edgeMatrix, float fromThickness, float toThickness)
    {
        // スケール比率を計算
        float scaleRatio = toThickness / fromThickness;

        // 行列を分解
        Vector3 position = edgeMatrix.GetPosition();
        Quaternion rotation = edgeMatrix.rotation;
        Vector3 scale = edgeMatrix.lossyScale;

        // X軸とZ軸のスケール（太さ）だけを変更、Y軸（長さ）はそのまま
        scale.x *= scaleRatio;
        scale.z *= scaleRatio;

        return Matrix4x4.TRS(position, rotation, scale);
    }
}
