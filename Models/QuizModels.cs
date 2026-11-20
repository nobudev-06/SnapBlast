namespace SnapBlast.Models;

public sealed class Quiz
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<GameBlock> Blocks { get; init; } = [];
}

public sealed class GameBlock
{
    public string Term { get; init; } = string.Empty;
    public string Definition { get; init; } = string.Empty;
    public IReadOnlyList<string> Distractors { get; init; } = [];
}

public sealed class PieceDefinition
{
    public PieceDefinition(string name, bool[,] cells, GameBlock block)
    {
        Name = name;
        Cells = cells;
        Block = block;
        Height = cells.GetLength(0);
        Width = cells.GetLength(1);
    }

    public string Name { get; }
    public bool[,] Cells { get; }
    public GameBlock Block { get; }
    public int Width { get; }
    public int Height { get; }
}

public enum PuzzleTileKind
{
    Question,
    Answer
}

public readonly record struct GridCell(int Row, int Column);

public sealed class PuzzleTile
{
    public required string Id { get; init; }
    public required string PairId { get; init; }
    public required GameBlock Block { get; init; }
    public required PuzzleTileKind Kind { get; init; }
    public required IReadOnlyList<GridCell> TargetCells { get; init; }

    public bool IsQuestion => Kind == PuzzleTileKind.Question;
}

public sealed class PuzzleLayout
{
    public required IReadOnlyList<PuzzleTile> Tiles { get; init; }
    public int CellCount => Tiles.Sum(tile => tile.TargetCells.Count);
}
