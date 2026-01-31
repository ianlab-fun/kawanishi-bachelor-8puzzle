using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PuzzleVisualizer : MonoBehaviour
{
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");

    // 8-puzzleの全状態空間容量設定（9!/2 = 181,440状態）
    private const int MaxStates = 181440;

    [SerializeField] private TMP_Text numberTextPrefab;
    [SerializeField] private InstancedMeshInfo puzzleBlockInfo;
    [SerializeField] private InstancedMeshInfo puzzleQuadInfo;
    [SerializeField] private InstancedMeshInfo edgeInfo;
    [SerializeField] private InstancedMeshInfo edgeRouteInfo;
    [SerializeField] private InstancedMeshInfo edgeExploredInfo; // 探索済み（無駄な探索）エッジ用

    [SerializeField] private Toggle lightModelToggle;
    [SerializeField] private Toggle edgeToggle;
    [SerializeField] private Toggle routeEdgeToggle;
    [SerializeField] private float blockSpacing = 2.1f;

    private InstancedMeshRenderer puzzleBlockInstancedRenderer;
    private InstancedMeshRenderer puzzleQuadInstancedRenderer;
    private InstancedMeshRenderer[] numberTextInstancedRenderer = new InstancedMeshRenderer[PuzzleState.TotalCells];
    private InstancedMeshRenderer edgeBlockInstancedRenderer;
    private InstancedMeshRenderer edgeRouteBlockInstancedRenderer;
    private InstancedMeshRenderer edgeExploredBlockInstancedRenderer; // 探索済みエッジ用

    /// <summary>
    /// StateManager用：状態配置の可視化
    /// 頂点、パズルブロック、数字を追加する
    /// </summary>
    /// <param name="statePositions">状態とその3D空間上の位置のマッピング</param>
    /// <param name="edgeThickness">エッジの太さ（パズルブロックの厚みを自動調整するために使用）</param>
    public void AddStates(IReadOnlyDictionary<PuzzleState, Vector3> statePositions, float edgeThickness)
    {
        // エッジの太さに応じてパズルブロックの厚みを計算（エッジより常に0.4f厚くする）
        float puzzleBlockDepth = edgeThickness + 0.4f;
        float numberTextOffset = puzzleBlockDepth / 2f + 0.01f;

        // パズルブロックと数字を追加
        foreach (var puzzleVisualize in statePositions)
        {
            // 2Dクアッド用：1つのQuadをパズル状態の中心に配置
            Matrix4x4 quadMatrix = Matrix4x4.TRS(puzzleVisualize.Value + Vector3.forward * -puzzleBlockDepth / 2, Quaternion.identity, new Vector3(6, 6, 1));
            puzzleQuadInstancedRenderer.AddMatrix(quadMatrix);

            for (int row = 0; row < PuzzleState.RowCount; row++)
            {
                for (int col = 0; col < PuzzleState.ColumnCount; col++)
                {
                    Vector3 position = new Vector3(
                        (col - 1) * blockSpacing,
                        (1 - row) * blockSpacing,
                        0
                    );
                    Matrix4x4 blockMatrix = Matrix4x4.TRS(puzzleVisualize.Value + position, Quaternion.identity, new Vector3(2, 2, puzzleBlockDepth));
                    puzzleBlockInstancedRenderer.AddMatrix(blockMatrix);

                    numberTextInstancedRenderer[puzzleVisualize.Key[new BlockPosition(row, col)]].AddMatrix(Matrix4x4.TRS(puzzleVisualize.Value + position + Vector3.back * numberTextOffset, Quaternion.identity, Vector3.one));
                }
            }
        }

        ApplyStateMatrixData();
    }

    /// <summary>
    /// 状態の描画をすべてクリアする（ステップ実行のリセット用）
    /// </summary>
    public void ClearStates()
    {
        // パズルブロックと数字をクリア
        puzzleBlockInstancedRenderer.ClearMatrices();
        puzzleQuadInstancedRenderer.ClearMatrices();
        for (int i = 0; i < numberTextInstancedRenderer.Length; i++)
        {
            numberTextInstancedRenderer[i].ClearMatrices();
        }

        ApplyStateMatrixData();
    }

    /// <summary>
    /// EdgeManager用：エッジの可視化
    /// 全エッジを通常エッジとして追加する
    /// </summary>
    public void InitializeEdges(Matrix4x4[] allEdges)
    {
        // 全エッジを通常エッジとして追加
        edgeBlockInstancedRenderer.AddMatrices(allEdges);

        ApplyEdgeMatrixData();
    }

    void Awake()
    {
        puzzleBlockInstancedRenderer = new InstancedMeshRenderer(puzzleBlockInfo, MaxStates * PuzzleState.TotalCells);
        puzzleQuadInstancedRenderer = new InstancedMeshRenderer(puzzleQuadInfo, MaxStates);
        for (int i = 0; i < numberTextInstancedRenderer.Length; i++)
        {
            var tmp = Instantiate(numberTextPrefab);
            tmp.text = i.ToString();
            tmp.ForceMeshUpdate();
            var tmpMesh = tmp.textInfo.meshInfo[0].mesh;
            var numberTextMaterial = tmp.fontMaterial;

            Mesh numberTextMesh = new Mesh();

            numberTextMesh.vertices = tmpMesh.vertices;
            numberTextMesh.triangles = tmpMesh.triangles;
            numberTextMesh.uv = tmpMesh.uv;
            numberTextMesh.normals = tmpMesh.normals;
            numberTextMesh.colors32 = tmpMesh.colors32;
            numberTextMesh.tangents = tmpMesh.tangents;

            DestroyImmediate(tmp.gameObject);
            InstancedMeshInfo numberTextMeshInfo = new InstancedMeshInfo
            {
                mesh = numberTextMesh,
                mat = numberTextMaterial
            };
            numberTextInstancedRenderer[i] = new InstancedMeshRenderer(numberTextMeshInfo, MaxStates);
        }
        edgeBlockInstancedRenderer = new InstancedMeshRenderer(edgeInfo, MaxStates * 4);
        edgeRouteBlockInstancedRenderer = new InstancedMeshRenderer(edgeRouteInfo, MaxStates * 4);
        edgeExploredBlockInstancedRenderer = new InstancedMeshRenderer(edgeExploredInfo, MaxStates * 4);
    }

    private void Update()
    {
        // Lキーでライトモデルの切り替え
        if (Input.GetKeyDown(KeyCode.M))
        {
            lightModelToggle.isOn = !lightModelToggle.isOn;
        }

        // Eキーでエッジの切り替え
        if (Input.GetKeyDown(KeyCode.E))
        {
            edgeToggle.isOn = !edgeToggle.isOn;
        }

        if (lightModelToggle.isOn)
        {
            puzzleQuadInstancedRenderer?.Render();
        }
        else
        {
            puzzleBlockInstancedRenderer?.Render();
        }

        for (int i = 1; i < numberTextInstancedRenderer.Length; i++)
        {
            numberTextInstancedRenderer[i]?.Render();
        }

        if (edgeToggle.isOn)
        {
            edgeBlockInstancedRenderer?.Render();
            // 経路エッジはrouteEdgeToggleで制御（nullなら従来通り描画）
            if (routeEdgeToggle == null || routeEdgeToggle.isOn)
            {
                edgeRouteBlockInstancedRenderer?.Render();
                edgeExploredBlockInstancedRenderer?.Render();
            }
        }
    }

    /// <summary>
    /// 状態関連のMatrixDataを適用する
    /// </summary>
    private void ApplyStateMatrixData()
    {
        for (int i = 0; i < numberTextInstancedRenderer.Length; i++)
        {
            numberTextInstancedRenderer[i].ApplyMatrixData();
        }
        puzzleBlockInstancedRenderer.ApplyMatrixData();
        puzzleQuadInstancedRenderer.ApplyMatrixData();
    }

    /// <summary>
    /// エッジ関連のMatrixDataを適用する
    /// </summary>
    private void ApplyEdgeMatrixData()
    {
        edgeBlockInstancedRenderer.ApplyMatrixData();
        edgeRouteBlockInstancedRenderer.ApplyMatrixData();
        edgeExploredBlockInstancedRenderer.ApplyMatrixData();
    }

    public void SetEdges(IEnumerable<Matrix4x4> normalEdges, IEnumerable<Matrix4x4> routeEdges)
    {
        edgeBlockInstancedRenderer.ClearMatrices();
        edgeBlockInstancedRenderer.AddMatrices(normalEdges);

        edgeRouteBlockInstancedRenderer.ClearMatrices();
        edgeRouteBlockInstancedRenderer.AddMatrices(routeEdges);

        ApplyEdgeMatrixData();
    }

    /// <summary>
    /// 3種類のエッジを設定する（探索経路ハイライト用）
    /// </summary>
    /// <param name="normalEdges">通常エッジ（全エッジ、背景）</param>
    /// <param name="exploredEdges">探索済みエッジ（無駄な探索）</param>
    /// <param name="routeEdges">解答経路エッジ</param>
    public void SetEdgesWithExplored(
        IEnumerable<Matrix4x4> normalEdges,
        IEnumerable<Matrix4x4> exploredEdges,
        IEnumerable<Matrix4x4> routeEdges)
    {
        edgeBlockInstancedRenderer.ClearMatrices();
        edgeBlockInstancedRenderer.AddMatrices(normalEdges);

        edgeExploredBlockInstancedRenderer.ClearMatrices();
        edgeExploredBlockInstancedRenderer.AddMatrices(exploredEdges);

        edgeRouteBlockInstancedRenderer.ClearMatrices();
        edgeRouteBlockInstancedRenderer.AddMatrices(routeEdges);

        ApplyEdgeMatrixData();
    }

    /// <summary>
    /// normalEdgeだけを増分追加する（前回のrouteEdgeをnormalに移動する用）
    /// </summary>
    public void AddNormalEdges(IEnumerable<Matrix4x4> newNormalEdges)
    {
        edgeBlockInstancedRenderer.AddMatrices(newNormalEdges);
        edgeBlockInstancedRenderer.ApplyMatrixData();
    }

    /// <summary>
    /// routeEdgeを設定する（ハイライト表示用、normalEdgeには追加しない）
    /// </summary>
    public void SetRouteEdges(IEnumerable<Matrix4x4> newRouteEdges)
    {
        edgeRouteBlockInstancedRenderer.ClearMatrices();
        edgeRouteBlockInstancedRenderer.AddMatrices(newRouteEdges);
        edgeRouteBlockInstancedRenderer.ApplyMatrixData();
    }

    /// <summary>
    /// エッジをクリアする（ステップ実行のリセット用）
    /// </summary>
    public void ClearEdges()
    {
        edgeBlockInstancedRenderer?.ClearMatrices();
        edgeRouteBlockInstancedRenderer?.ClearMatrices();
        edgeExploredBlockInstancedRenderer?.ClearMatrices();
    }

    /// <summary>
    /// RouteEdgeのfillAmountを設定する
    /// </summary>
    public void SetRouteEdgeFillAmount(float value)
    {
        edgeRouteBlockInstancedRenderer.SetFloat(FillAmount, value);
    }

    void OnDestroy()
    {
        DisposeInstances();
    }

    void DisposeInstances()
    {
        puzzleBlockInstancedRenderer?.Dispose();
        puzzleQuadInstancedRenderer?.Dispose();
        for (int i = 0; i < numberTextInstancedRenderer.Length; i++)
        {
            numberTextInstancedRenderer[i]?.Dispose();
        }
        edgeBlockInstancedRenderer?.Dispose();
        edgeRouteBlockInstancedRenderer?.Dispose();
        edgeExploredBlockInstancedRenderer?.Dispose();
    }
} 