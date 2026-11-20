using SnapBlast.Models;

namespace SnapBlast.Game;

public sealed class PuzzleEngine
{
    public const int BoardSize = 8;

    private readonly ExactFitLayoutGenerator layoutGenerator;
    private readonly PuzzleTile?[,] placedTiles = new PuzzleTile?[BoardSize, BoardSize];
    private readonly Dictionary<PuzzleTile, List<GridCell>> placedCells = [];
    private readonly List<PuzzleTile> remainingTiles = [];
    private readonly HashSet<string> connectedPairIds = [];

    public PuzzleEngine(ExactFitLayoutGenerator? layoutGenerator = null) => this.layoutGenerator = layoutGenerator ?? new ExactFitLayoutGenerator();
    public PuzzleLayout Layout { get; private set; } = new() { Tiles = [] };
    public IReadOnlyList<PuzzleTile> RemainingTiles => remainingTiles;
    public int Score { get; private set; }
    public bool IsComplete => remainingTiles.Count == 0;
    public IReadOnlySet<string> ConnectedPairIds => connectedPairIds;
    public bool IsOccupied(int row, int column) => placedTiles[row, column] is not null;
    public PuzzleTile? GetTile(int row, int column) => placedTiles[row, column];
    public IReadOnlyList<GridCell> GetPlacedCells(PuzzleTile tile) => placedCells.TryGetValue(tile, out var cells) ? cells : [];

    public void StartNewGame(IReadOnlyList<GameBlock> blocks)
    {
        Layout = layoutGenerator.Generate(blocks);
        remainingTiles.Clear();
        remainingTiles.AddRange(Layout.Tiles.OrderBy(_ => Random.Shared.Next()));
        Array.Clear(placedTiles);
        placedCells.Clear();
        connectedPairIds.Clear();
        Score = 0;
    }

    public bool CanPlace(PuzzleTile tile, int row, int column) => remainingTiles.Contains(tile) && TryGetPlacementCells(tile, row, column, placedTiles, out _);

    public PlacementResult Place(PuzzleTile tile, int row, int column)
    {
        if (!CanPlace(tile, row, column) || !TryGetPlacementCells(tile, row, column, placedTiles, out var cells)) return PlacementResult.Invalid;
        Apply(tile, cells);
        remainingTiles.Remove(tile);
        if (!CanFinishRemaining())
        {
            Remove(tile, cells);
            remainingTiles.Add(tile);
            return PlacementResult.Invalid;
        }

        var pairConnected = PairCanConnect(tile, cells);
        var touchesWrongPair = TouchesWrongPair(tile, cells);
        if (pairConnected)
        {
            connectedPairIds.Add(tile.PairId);
        }

        Score += cells.Count * 5 + (pairConnected ? 25 : 0) - (touchesWrongPair ? 5 : 0);
        return new PlacementResult(true, pairConnected, touchesWrongPair, Score);
    }

public readonly record struct PlacementResult(bool Success, bool PairConnected, bool TouchesWrongPair, int Score)
{
    public static PlacementResult Invalid => new(false, false, false, 0);
}

    private bool CanFinishRemaining()
    {
        if (remainingTiles.Count == 0) return true;
        var candidate = remainingTiles.Select(tile => (Tile: tile, Placements: GetPossiblePlacements(tile).ToList())).OrderBy(item => item.Placements.Count).First();
        if (candidate.Placements.Count == 0) return false;
        foreach (var placement in candidate.Placements)
        {
            Apply(candidate.Tile, placement);
            remainingTiles.Remove(candidate.Tile);
            if (CanFinishRemaining())
            {
                remainingTiles.Add(candidate.Tile);
                Remove(candidate.Tile, placement);
                return true;
            }
            remainingTiles.Add(candidate.Tile);
            Remove(candidate.Tile, placement);
        }
        return false;
    }

    private IEnumerable<List<GridCell>> GetPossiblePlacements(PuzzleTile tile)
    {
        for (var row = 0; row < BoardSize; row++)
            for (var column = 0; column < BoardSize; column++)
                if (TryGetPlacementCells(tile, row, column, placedTiles, out var cells) && PairCanConnect(tile, cells)) yield return cells;
    }

    private void Apply(PuzzleTile tile, List<GridCell> cells)
    {
        placedCells[tile] = cells;
        foreach (var cell in cells) placedTiles[cell.Row, cell.Column] = tile;
    }

    private void Remove(PuzzleTile tile, IReadOnlyList<GridCell> cells)
    {
        foreach (var cell in cells) placedTiles[cell.Row, cell.Column] = null;
        placedCells.Remove(tile);
    }

    private bool PairCanConnect(PuzzleTile tile, IReadOnlyList<GridCell> cells)
    {
        var matchingTile = Layout.Tiles.FirstOrDefault(candidate => candidate.PairId == tile.PairId && candidate.Kind != tile.Kind);
        if (matchingTile is null || !placedCells.TryGetValue(matchingTile, out var matchingCells)) return true;
        return cells.Any(cell => matchingCells.Any(other => Math.Abs(cell.Row - other.Row) + Math.Abs(cell.Column - other.Column) == 1));
    }

    private bool TouchesWrongPair(PuzzleTile tile, IReadOnlyList<GridCell> cells)
    {
        return cells.Any(cell => GetAdjacentTiles(cell).Any(neighbor => neighbor.PairId != tile.PairId));
    }

    private IEnumerable<PuzzleTile> GetAdjacentTiles(GridCell cell)
    {
        foreach (var (row, column) in new[]
        {
            (cell.Row - 1, cell.Column),
            (cell.Row + 1, cell.Column),
            (cell.Row, cell.Column - 1),
            (cell.Row, cell.Column + 1)
        })
        {
            if (row is >= 0 and < BoardSize && column is >= 0 and < BoardSize && placedTiles[row, column] is { } tile)
            {
                yield return tile;
            }
        }
    }

    private static bool TryGetPlacementCells(PuzzleTile tile, int row, int column, PuzzleTile?[,] board, out List<GridCell> cells)
    {
        var targetRow = tile.TargetCells.Min(cell => cell.Row);
        var targetColumn = tile.TargetCells.Min(cell => cell.Column);
        cells = tile.TargetCells.Select(cell => new GridCell(row + cell.Row - targetRow, column + cell.Column - targetColumn)).ToList();
        if (cells.Any(cell => cell.Row < 0 || cell.Row >= BoardSize || cell.Column < 0 || cell.Column >= BoardSize || board[cell.Row, cell.Column] is not null))
        {
            cells.Clear();
            return false;
        }
        return true;
    }
}
