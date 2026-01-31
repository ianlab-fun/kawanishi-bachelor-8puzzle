using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

public class ForceBasedVisualizer : IVisualizeStrategy, IDisposable
{
    private readonly ForceBasedVisualizerConfig _config;
    private SearchProgress _progress;

    // Native Arrays for job system
    private NativeArray<float3> positions;
    private NativeArray<float3> forces;
    private NativeArray<int> adjacencyData;
    private NativeArray<int> adjacencyOffsets;
    private NativeArray<NativeOctreeNode> octreeNodes;

    // Mapping system
    private Dictionary<PuzzleState, int> stateToIndex;
    private List<PuzzleState> indexToState;
    private bool isInitialized = false;

    public ForceBasedVisualizer(ForceBasedVisualizerConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Native-friendly な OctTree ノード構造体
    /// </summary>
    [System.Serializable]
    public struct NativeOctreeNode
    {
        public float3 centerOfMass;
        public float totalMass;
        public float3 boundsMin;
        public float3 boundsMax;
        public int childIndex;     // 子ノードのインデックス (-1 if leaf)
        public int nodeIndex;      // ノードのインデックス (-1 if internal)
        public bool isLeaf;

        public float3 boundsSize => boundsMax - boundsMin;
        public float maxDimension => math.max(math.max(boundsSize.x, boundsSize.y), boundsSize.z);
        public float3 center => (boundsMin + boundsMax) * 0.5f;
    }

    /// <summary>
    /// 斥力計算用の並列Job
    /// </summary>
    [BurstCompile]
    public struct RepulsiveForcesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<NativeOctreeNode> octreeNodes;
        [ReadOnly] public float k;
        [ReadOnly] public float theta;
        [ReadOnly] public bool isStub;
        [NativeDisableParallelForRestriction] 
        public NativeArray<float3> forces;
        
        public void Execute(int index)
        {
            if (isStub)
            {
                forces[index] = CalculateBarnesHutForce(positions[index], octreeNodes, k, theta);
            }
            else
            {
                // 単純な全対全計算
                forces[index] = CalculateNaiveRepulsiveForce(index, positions, k);
            }
        }
        
        private float3 CalculateBarnesHutForce(float3 targetPosition, NativeArray<NativeOctreeNode> nodes, float k, float theta)
        {
            float3 totalForce = float3.zero;
            
            // ルートノード（インデックス0）から開始
            if (nodes.Length > 0)
            {
                totalForce = CalculateForceRecursive(targetPosition, nodes, 0, k, theta);
            }
            
            return totalForce;
        }
        
        private float3 CalculateForceRecursive(float3 targetPosition, NativeArray<NativeOctreeNode> nodes, int nodeIndex, float k, float theta)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return float3.zero;
                
            var node = nodes[nodeIndex];
            
            if (node.totalMass == 0)
                return float3.zero;
                
            float3 direction = node.centerOfMass - targetPosition;
            float distance = math.length(direction);
            
            if (distance < 0.01f)
                return float3.zero;
                
            // リーフノードまたは十分に遠い場合は近似計算
            if (node.isLeaf || node.maxDimension / distance < theta)
            {
                float force = k * k * node.totalMass / (distance * distance);
                return -math.normalize(direction) * force;
            }
            
            // 子ノードの力を合計
            float3 totalForce = float3.zero;
            for (int i = 0; i < 8; i++)
            {
                int childIndex = node.childIndex + i;
                if (childIndex < nodes.Length)
                {
                    totalForce += CalculateForceRecursive(targetPosition, nodes, childIndex, k, theta);
                }
            }
            
            return totalForce;
        }
        
        private float3 CalculateNaiveRepulsiveForce(int targetIndex, NativeArray<float3> positions, float k)
        {
            float3 totalForce = float3.zero;
            float3 targetPos = positions[targetIndex];
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == targetIndex) continue;
                
                float3 delta = targetPos - positions[i];
                float distance = math.max(0.01f, math.length(delta));
                
                float3 force = math.normalize(delta) * (k * k / distance);
                totalForce += force;
            }
            
            return totalForce;
        }
    }

    /// <summary>
    /// 引力計算用の並列Job
    /// </summary>
    [BurstCompile]
    public struct AttractiveForcesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<int> adjacencyData;
        [ReadOnly] public NativeArray<int> adjacencyOffsets;
        [ReadOnly] public float k;
        [NativeDisableParallelForRestriction] 
        public NativeArray<float3> forces;
        
        public void Execute(int index)
        {
            if (index >= adjacencyOffsets.Length - 1)
                return;
                
            int startOffset = adjacencyOffsets[index];
            int endOffset = adjacencyOffsets[index + 1];
            
            float3 nodePos = positions[index];
            float3 totalForce = float3.zero;
            
            for (int i = startOffset; i < endOffset; i++)
            {
                if (i >= adjacencyData.Length)
                    break;
                    
                int adjacentIndex = adjacencyData[i];
                if (adjacentIndex < 0 || adjacentIndex >= positions.Length)
                    continue;
                    
                float3 adjacentPos = positions[adjacentIndex];
                float3 delta = nodePos - adjacentPos;
                float distance = math.max(0.01f, math.length(delta));
                
                float3 force = math.normalize(delta) * (distance * distance / k);
                totalForce -= force * 0.5f; // 0.5倍して重複を避ける
            }
            
            forces[index] += totalForce;
        }
    }

    /// <summary>
    /// 位置更新用の並列Job
    /// </summary>
    [BurstCompile]
    public struct UpdatePositionsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> forces;
        [ReadOnly] public float temperature;
        public NativeArray<float3> positions;
        
        public void Execute(int index)
        {
            float3 displacement = forces[index];
            float displacementMagnitude = math.length(displacement);
            
            if (displacementMagnitude > 0)
            {
                displacement = math.normalize(displacement) * math.min(displacementMagnitude, temperature);
                positions[index] += displacement;
            }
        }
    }

    public Dictionary<PuzzleState, Vector3> VisualizeSearchSpace(Dictionary<PuzzleState, PuzzleNodeData> searchDataMap, PuzzleState initialPuzzleState)
    {
        var result = new Dictionary<PuzzleState, Vector3>();
        var nodes = searchDataMap.Keys.ToList();
        
        if (nodes.Count == 0)
            return result;

        try
        {
            // データの初期化
            InitializeData(nodes, searchDataMap, initialPuzzleState);
            
            // 繰り返し処理
            ExecuteIterations();
            
            // 結果の変換
            ConvertResultsToUnityVectors(result);
        }
        finally
        {
            // リソースの解放
            DisposeNativeArrays();
        }
        
        return result;
    }

    public async UniTask<Dictionary<PuzzleState, Vector3>> VisualizeSearchSpaceAsync(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap,
        PuzzleState initialPuzzleState,
        SearchProgress progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        await UniTask.SwitchToThreadPool();

        try
        {
            return VisualizeSearchSpace(searchDataMap, initialPuzzleState);
        }
        finally
        {
            _progress = null;
            await UniTask.SwitchToMainThread();
        }
    }

    private void InitializeData(List<PuzzleState> nodes, Dictionary<PuzzleState, PuzzleNodeData> searchDataMap, PuzzleState initialPuzzleState)
    {
        int nodeCount = nodes.Count;
        
        // Native Arrayの初期化
        positions = new NativeArray<float3>(nodeCount, Allocator.TempJob);
        forces = new NativeArray<float3>(nodeCount, Allocator.TempJob);
        
        // マッピングシステムの初期化
        stateToIndex = new Dictionary<PuzzleState, int>();
        indexToState = new List<PuzzleState>();
        
        for (int i = 0; i < nodeCount; i++)
        {
            var state = nodes[i];
            stateToIndex[state] = i;
            indexToState.Add(state);
        }
        
        // 初期位置の設定
        InitializePositions(nodes, initialPuzzleState);
        
        // 隣接関係データの構築
        BuildAdjacencyData(searchDataMap);
        
        if (_config.IsStub)
        {
            BuildOctree();
        }
        
        isInitialized = true;
    }

    private void InitializePositions(List<PuzzleState> nodes, PuzzleState initialPuzzleState)
    {
        // 初期パズルを中央に配置
        if (_config.CenterInitialPuzzle && nodes.Contains(initialPuzzleState))
        {
            int initialIndex = stateToIndex[initialPuzzleState];
            positions[initialIndex] = float3.zero;
        }

        // 他のノードを3D空間にランダムに配置
        var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);

        for (int i = 0; i < nodes.Count; i++)
        {
            var state = nodes[i];
            int index = stateToIndex[state];

            if (_config.CenterInitialPuzzle && state.Equals(initialPuzzleState))
                continue;

            float3 randomPos = new float3(
                random.NextFloat(-_config.InitialAreaSize * 0.5f, _config.InitialAreaSize * 0.5f),
                random.NextFloat(-_config.InitialAreaSize * 0.5f, _config.InitialAreaSize * 0.5f),
                random.NextFloat(-_config.InitialAreaSize * 0.5f, _config.InitialAreaSize * 0.5f)
            );

            positions[index] = randomPos;
        }
    }

    private void BuildAdjacencyData(Dictionary<PuzzleState, PuzzleNodeData> searchDataMap)
    {
        var adjList = new List<int>();
        var offsets = new List<int>();
        
        foreach (var state in indexToState)
        {
            offsets.Add(adjList.Count);
            
            if (searchDataMap.TryGetValue(state, out PuzzleNodeData nodeData) && nodeData.AdjacentStates != null)
            {
                foreach (var adjacent in nodeData.AdjacentStates)
                {
                    if (stateToIndex.TryGetValue(adjacent, out int adjIndex))
                    {
                        adjList.Add(adjIndex);
                    }
                }
            }
        }
        
        offsets.Add(adjList.Count); // 終端オフセット
        
        adjacencyData = new NativeArray<int>(adjList.ToArray(), Allocator.TempJob);
        adjacencyOffsets = new NativeArray<int>(offsets.ToArray(), Allocator.TempJob);
    }

    private void BuildOctree()
    {
        // 簡単なOctTreeの実装（詳細な実装は複雑なため、基本的な構造のみ）
        // 実際の実装では、より効率的なバイナリツリー構築が必要
        octreeNodes = new NativeArray<NativeOctreeNode>(1, Allocator.TempJob);
        
        // ルートノードの作成
        var rootNode = new NativeOctreeNode
        {
            centerOfMass = float3.zero,
            totalMass = positions.Length,
            boundsMin = new float3(-_config.InitialAreaSize, -_config.InitialAreaSize, -_config.InitialAreaSize),
            boundsMax = new float3(_config.InitialAreaSize, _config.InitialAreaSize, _config.InitialAreaSize),
            childIndex = -1,
            nodeIndex = -1,
            isLeaf = true
        };
        
        octreeNodes[0] = rootNode;
    }

    private void ExecuteIterations()
    {
        float temperature = _config.InitialTemperature;

        for (int iteration = 0; iteration < _config.MaxIterations; iteration++)
        {
            // 力を初期化
            for (int i = 0; i < forces.Length; i++)
            {
                forces[i] = float3.zero;
            }
            
            // Job のスケジューリング
            JobHandle repulsiveHandle = new RepulsiveForcesJob
            {
                positions = positions,
                octreeNodes = octreeNodes,
                k = _config.K,
                theta = _config.Theta,
                isStub = _config.IsStub,
                forces = forces
            }.Schedule(positions.Length, _config.BatchSize);

            JobHandle attractiveHandle = new AttractiveForcesJob
            {
                positions = positions,
                adjacencyData = adjacencyData,
                adjacencyOffsets = adjacencyOffsets,
                k = _config.K,
                forces = forces
            }.Schedule(positions.Length, _config.BatchSize, repulsiveHandle);
            
            JobHandle updateHandle = new UpdatePositionsJob
            {
                forces = forces,
                temperature = temperature,
                positions = positions
            }.Schedule(positions.Length, _config.BatchSize, attractiveHandle);
            
            // 完了待機
            updateHandle.Complete();
            
            // 温度更新
            temperature *= _config.CoolingRate;
            if (temperature < _config.MinTemperature)
                break;
        }
    }

    private void ConvertResultsToUnityVectors(Dictionary<PuzzleState, Vector3> result)
    {
        for (int i = 0; i < indexToState.Count; i++)
        {
            var state = indexToState[i];
            var pos = positions[i];
            result[state] = new Vector3(pos.x, pos.y, pos.z);
            _progress?.Increment();
        }
    }

    private void DisposeNativeArrays()
    {
        if (positions.IsCreated) positions.Dispose();
        if (forces.IsCreated) forces.Dispose();
        if (adjacencyData.IsCreated) adjacencyData.Dispose();
        if (adjacencyOffsets.IsCreated) adjacencyOffsets.Dispose();
        if (octreeNodes.IsCreated) octreeNodes.Dispose();
        
        isInitialized = false;
    }

    public void Dispose()
    {
        DisposeNativeArrays();
    }

}