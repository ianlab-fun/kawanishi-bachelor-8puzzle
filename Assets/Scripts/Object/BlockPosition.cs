using System;

public readonly struct BlockPosition : IEquatable<BlockPosition>
{
    public int Row { get; }
    public int Column { get; }

    public BlockPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
    
    public static BlockPosition CreateFromIndex(int index) => new BlockPosition(index / PuzzleState.RowCount, index % PuzzleState.ColumnCount);

    public bool Equals(BlockPosition other) => Row == other.Row && Column == other.Column;
    public override bool Equals(object obj) => obj is BlockPosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Row, Column);

    public static bool operator ==(BlockPosition left, BlockPosition right) => left.Equals(right);
    public static bool operator !=(BlockPosition left, BlockPosition right) => !left.Equals(right);
    
    public static (int Row, int Column) operator -(BlockPosition left, BlockPosition right) => (left.Row - right.Row, left.Column - right.Column);
    
    public static implicit operator int(BlockPosition position) => position.Row * PuzzleState.ColumnCount + position.Column;

    public BlockPosition Up() => new BlockPosition(Row - 1, Column);
    public BlockPosition Down() => new BlockPosition(Row + 1, Column);
    public BlockPosition Left() => new BlockPosition(Row, Column - 1);
    public BlockPosition Right() => new BlockPosition(Row, Column + 1);
} 