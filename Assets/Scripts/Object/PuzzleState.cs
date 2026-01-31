using System;
using System.Linq;

public readonly struct PuzzleState : IEquatable<PuzzleState>
{
      public static readonly int GridSize = 3;
      public static int RowCount => GridSize;
      public static int ColumnCount => GridSize;
      public static int TotalCells => RowCount * ColumnCount;
      public BlockPosition EmptyBlockPosition => _emptyPosition;
      private readonly BlockPosition _emptyPosition;
      private readonly BlockNumber[,] _blockNumbers;
      
      public PuzzleState(BlockNumber[,] blockNumbers)
      {
            if (blockNumbers == null)
                  throw new ArgumentNullException(nameof(blockNumbers));

            if (blockNumbers.GetLength(0) != RowCount || blockNumbers.GetLength(1) != ColumnCount)
            {
                  throw new ArgumentException(
                        $"パズルは{RowCount}x{ColumnCount}のグリッドである必要があります。",
                        nameof(blockNumbers)
                  );
            }

            // 配列をコピーして保持（参照の共有を防ぐ）
            _blockNumbers = new BlockNumber[RowCount, ColumnCount];
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        _blockNumbers[row, col] = blockNumbers[row, col];
                  }
            }
        
            // ビットマスクによるブロック番号の検証と空きブロック位置の検索
            int bitmask = 0;
            int expectedBitmask = (1 << TotalCells) - 1; // 0からTotalCells-1までのすべてのビットがセットされたマスク
            BlockPosition emptyPosition = default;

            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        BlockNumber blockNumber = blockNumbers[row, col];
                        int number = blockNumber;
                        int currentBit = 1 << number;

                        if ((bitmask & currentBit) != 0)
                        {
                              throw new ArgumentException(
                                    $"ブロック番号{number}が重複しています。",
                                    nameof(blockNumbers)
                              );
                        }
                        bitmask |= currentBit;

                        // 空きブロック（0）を見つけたら位置をキャッシュ
                        if (blockNumber.IsZero())
                        {
                              emptyPosition = new BlockPosition(row, col);
                        }
                  }
            }

            _emptyPosition = emptyPosition;

            if (bitmask != expectedBitmask)
            {
                  throw new ArgumentException(
                        $"パズルは0から{TotalCells - 1}までのすべての番号を1つずつ含む必要があります。",
                        nameof(blockNumbers)
                  );
            }
      }
      
      /// <summary>
      /// 有効なPuzzleStateを生成できるか試みる
      /// </summary>
      public static bool TryCreate(BlockNumber[,] blockNumbers, out PuzzleState state)
      {
            state = default;

            if (blockNumbers == null ||
                blockNumbers.GetLength(0) != RowCount ||
                blockNumbers.GetLength(1) != ColumnCount)
                  return false;

            int bitmask = 0;
            int expectedBitmask = (1 << TotalCells) - 1;

            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        int number = blockNumbers[row, col];
                        int currentBit = 1 << number;

                        if ((bitmask & currentBit) != 0)
                              return false;

                        bitmask |= currentBit;
                  }
            }

            if (bitmask != expectedBitmask)
                  return false;

            state = new PuzzleState(blockNumbers);
            return true;
      }

      public static PuzzleState Create(int[,] numbers)
      {
            if (numbers == null)
                  throw new ArgumentNullException(nameof(numbers));

            if (numbers.GetLength(0) != RowCount || numbers.GetLength(1) != ColumnCount)
            {
                  throw new ArgumentException(
                        $"配列は{RowCount}x{ColumnCount}のサイズである必要があります。",
                        nameof(numbers)
                  );
            }

            var blockNumbers = new BlockNumber[RowCount, ColumnCount];

            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        blockNumbers[row, col] = new BlockNumber(numbers[row, col]);
                  }
            }

            return new PuzzleState(blockNumbers);
      }

      /// <summary>
      /// ランダムなパズル状態を生成します
      /// </summary>
      public static PuzzleState CreateRandom()
      {
            var numbers = Enumerable.Range(0, TotalCells)
                  .OrderBy(_ => UnityEngine.Random.value)
                  .ToArray();

            var blockNumbers = new BlockNumber[RowCount, ColumnCount];
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        blockNumbers[row, col] = new BlockNumber(numbers[row * ColumnCount + col]);
                  }
            }

            return new PuzzleState(blockNumbers);
      }
      
      public BlockNumber this[BlockPosition position]
      {
            get
            {
                  ValidatePosition(position);
                  return _blockNumbers[position.Row, position.Column];
            }
      }

      private void ValidatePosition(BlockPosition position)
      {
            if (position.Row < 0 || position.Row >= RowCount)
                  throw new ArgumentOutOfRangeException(nameof(position.Row), position.Row, $"行は0から{RowCount - 1}の範囲である必要があります。");
            
            if (position.Column < 0 || position.Column >= ColumnCount)
                  throw new ArgumentOutOfRangeException(nameof(position.Column), position.Column, $"列は0から{ColumnCount - 1}の範囲である必要があります。");

      }

      private bool IsValidPosition(BlockPosition position)
      {
            return position.Row >= 0 && position.Row < RowCount && 
                   position.Column >= 0 && position.Column < ColumnCount;
      }

      public bool Equals(PuzzleState other)
      {
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        BlockPosition blockPos = new BlockPosition(row, col);
                        if (this[blockPos] != other[blockPos])
                        {
                              return false;
                        }
                  }
            }
            return true;
      }
      
      public BlockPosition FindNumberBlockPosition(BlockNumber findNumber)
      {
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        if (_blockNumbers[row, col] == findNumber)
                        {
                              return new BlockPosition(row, col);
                        }
                  }
            }
            // そもそもPuzzleは各数字が配置されるようになっているので、エラー消し
            return default;
      }
      
      public static bool operator ==(PuzzleState left, PuzzleState right) => left.Equals(right);
      public static bool operator !=(PuzzleState left, PuzzleState right) => !(left == right);

      /// <summary>
      /// パリティが偶数かどうかを判定（転倒数が偶数なら偶パリティ）
      /// </summary>
      public bool IsEvenParity()
      {
            int inversions = CalculateInversions();
            return inversions % 2 == 0;
      }

      /// <summary>
      /// 転倒数を計算（0は除外）
      /// </summary>
      private int CalculateInversions()
      {
            // 1次元配列に変換（0は除外）
            var flatArray = new int[TotalCells - 1];
            int index = 0;
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        int value = _blockNumbers[row, col];
                        if (value != 0)
                        {
                              flatArray[index++] = value;
                        }
                  }
            }

            // 転倒数を計算
            int inversions = 0;
            for (int i = 0; i < flatArray.Length; i++)
            {
                  for (int j = i + 1; j < flatArray.Length; j++)
                  {
                        if (flatArray[i] > flatArray[j]) inversions++;
                  }
            }
            return inversions;
      }

      public override bool Equals(object obj)
      {
            return obj is PuzzleState other && Equals(other);
      }

      public override int GetHashCode()
      {
            var hash = new HashCode();
            foreach (var blockNumber in _blockNumbers)
            {
                  hash.Add(blockNumber);
            }
            return hash.ToHashCode();
      }
      
      public bool CanSwap(BlockPosition targetPosition)
      {
            // 範囲チェック
            if (!IsValidPosition(targetPosition))
            {
                  return false;
            }
            
            // 空きブロック位置と隣接しているかチェック（マンハッタン距離が1）
            if (Math.Abs(_emptyPosition.Row - targetPosition.Row) + Math.Abs(_emptyPosition.Column - targetPosition.Column) != 1)
            {
                  return false;
            }
            
            return true;
      }
      
      public PuzzleState? Swap(BlockPosition targetPosition)
      {
            // CanSwap()でチェックし、無効な場合はnullを返す
            if (!CanSwap(targetPosition))
            {
                  return null;
            }

            var newBlockNumbers = new BlockNumber[RowCount, ColumnCount];

            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        newBlockNumbers[row, col] = _blockNumbers[row, col];
                  }
            }

            // 空きブロック位置と指定された位置を交換
            (newBlockNumbers[_emptyPosition.Row, _emptyPosition.Column], newBlockNumbers[targetPosition.Row, targetPosition.Column]) = (newBlockNumbers[targetPosition.Row, targetPosition.Column], newBlockNumbers[_emptyPosition.Row, _emptyPosition.Column]);

            return new PuzzleState(newBlockNumbers);
      }

      public Puzzle.MoveDirection GetMoveDirectionTo(PuzzleState targetState)
      {
            var diff = targetState.EmptyBlockPosition - this.EmptyBlockPosition;
            return diff switch
            {
                  (0, 1) => Puzzle.MoveDirection.Right,
                  (0, -1) => Puzzle.MoveDirection.Left,
                  (-1, 0) => Puzzle.MoveDirection.Up,
                  (1, 0) => Puzzle.MoveDirection.Down,
                  _ => throw new InvalidOperationException("無効な状態変化です")
            };
      }

      public override string ToString()
      {
            var sb = new System.Text.StringBuilder();
            for (int row = 0; row < RowCount; row++)
            {
                  for (int col = 0; col < ColumnCount; col++)
                  {
                        sb.Append(this[new BlockPosition(row, col)]);
                        if (col < ColumnCount - 1)
                        {
                              sb.Append(" ");
                        }
                  }

                  if (row < RowCount - 1)
                  {
                        sb.AppendLine();
                  }
            }
            return sb.ToString();
      }
}