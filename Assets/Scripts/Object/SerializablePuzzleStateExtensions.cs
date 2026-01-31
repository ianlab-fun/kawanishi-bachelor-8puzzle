/// <summary>
/// SerializablePuzzleStateの拡張メソッド
/// </summary>
public static class SerializablePuzzleStateExtensions
{
    /// <summary>
    /// SerializablePuzzleStateが有効な場合はPuzzleStateに変換し、
    /// それ以外（null または IsEnabled が false）の場合は null を返す
    /// </summary>
    public static PuzzleState? ToPuzzleStateOrNull(this SerializablePuzzleState? self)
    {
        return self != null && self.IsEnabled ? self.ToPuzzleState() : null;
    }
}
