using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IVisualizeStrategy
{
    Dictionary<PuzzleState, Vector3> VisualizeSearchSpace(Dictionary<PuzzleState, PuzzleNodeData> searchDataMap, PuzzleState initialPuzzleState);

    UniTask<Dictionary<PuzzleState, Vector3>> VisualizeSearchSpaceAsync(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap,
        PuzzleState initialPuzzleState,
        SearchProgress progress = null,
        CancellationToken cancellationToken = default);
}
