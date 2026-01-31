using System;

public readonly struct BlockNumber : IEquatable<BlockNumber>
{
    private static readonly int MinValue = 0;
    private static int MaxValue => PuzzleState.TotalCells - 1;
    
    private readonly int _value;

    public BlockNumber(int value)
    {
        ValidateValue(value);
        _value = value;
    }

    private static void ValidateValue(int value)
    {
        if (value < MinValue || MaxValue < value)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                actualValue: value,
                message: $"ブロックの番号が有効範囲外です。\n" +
                        $"有効範囲: {MinValue}～{MaxValue}\n" +
                        $"指定された値: {value}"
            );
        }
    }

    public static implicit operator int(BlockNumber blockNumber) => blockNumber._value;
    public static BlockNumber Zero() => new BlockNumber(0);
    public bool IsZero() => _value == 0;
    public override string ToString() => IsZero() ? string.Empty : _value.ToString();
    public static int Parse(string value){
        if (value == "")
        {
            return 0;
        }
        if (value.Length == 1 && '0' <= value[0] && value[0] <= '8')
        {
            return int.Parse(value);
        }
        throw new ArgumentOutOfRangeException($"ブロックの番号が有効範囲外です。有効範囲: 0～8\n指定された値: {value}");
    }

    public bool Equals(BlockNumber other) => _value == other._value;
    public override bool Equals(object obj) => obj is BlockNumber other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(BlockNumber left, BlockNumber right) => left.Equals(right);
    public static bool operator !=(BlockNumber left, BlockNumber right) => !left.Equals(right);
}