using System.Collections.Generic;

public class PuzzleNodeData
{
    public List<PuzzleState> AdjacentStates { get; }
    public PuzzleState? Parent { get; private set; }
    public int Depth { get; set; }
    public int HCost { get; set; } = 0;
    public int FCost => Depth + HCost;

    public PuzzleNodeData()
    {
        AdjacentStates = new List<PuzzleState>(4); // Ensure capacity
        Parent = null;
        Depth = 0;
    }

    public void AddAdjacentState(PuzzleState puzzle)
    {
        AdjacentStates.Add(puzzle);
    }
    
    public void SetParent(PuzzleState parent)
    {
        Parent = parent;
    }

    public void IncrementDepth()
    {
        Depth++;
    }
}