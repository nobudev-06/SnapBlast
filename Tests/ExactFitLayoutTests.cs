using SnapBlast.Game;
using SnapBlast.Models;
using Xunit;

namespace SnapBlast.Tests;

public sealed class ExactFitLayoutTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void Generate_creates_a_complete_layout_for_supported_pair_counts(int pairCount)
    {
        var layout = new ExactFitLayoutGenerator(new Random(42)).Generate(CreateBlocks(pairCount));

        Assert.Equal(64, layout.CellCount);
        Assert.Equal(pairCount * 2, layout.Tiles.Count);
        Assert.Equal(64, layout.Tiles.SelectMany(tile => tile.TargetCells).Distinct().Count());
        Assert.All(layout.Tiles.SelectMany(tile => tile.TargetCells), cell =>
        {
            Assert.InRange(cell.Row, 0, PuzzleEngine.BoardSize - 1);
            Assert.InRange(cell.Column, 0, PuzzleEngine.BoardSize - 1);
        });

        foreach (var pair in layout.Tiles.GroupBy(tile => tile.PairId))
        {
            Assert.Equal(2, pair.Count());
            Assert.Single(pair, tile => tile.Kind == PuzzleTileKind.Question);
            Assert.Single(pair, tile => tile.Kind == PuzzleTileKind.Answer);

            var question = pair.Single(tile => tile.IsQuestion);
            var answer = pair.Single(tile => !tile.IsQuestion);
            Assert.True(ShareAnEdge(question.TargetCells, answer.TargetCells));
        }
    }

    [Fact]
    public void Engine_allows_free_placement_but_rejects_out_of_bounds_and_completes()
    {
        var blocks = CreateBlocks(5);
        var engine = new PuzzleEngine(new ExactFitLayoutGenerator(new Random(7)));
        engine.StartNewGame(blocks);

        var first = engine.RemainingTiles[0];
        var target = GetTopLeft(first.TargetCells);
        Assert.False(engine.CanPlace(first, -1, target.Column));
        Assert.True(engine.CanPlace(first, target.Row, target.Column));
        Assert.Contains(
            Enumerable.Range(0, PuzzleEngine.BoardSize)
                .SelectMany(row => Enumerable.Range(0, PuzzleEngine.BoardSize).Select(column => (row, column)))
                .Where(position => position != (target.Row, target.Column)),
            position => engine.CanPlace(first, position.row, position.column));

        foreach (var tile in engine.Layout.Tiles)
        {
            var tileTarget = GetTopLeft(tile.TargetCells);
            Assert.True(engine.Place(tile, tileTarget.Row, tileTarget.Column).Success);
        }

        Assert.True(engine.IsComplete);
        Assert.Empty(engine.RemainingTiles);
    }

    [Fact]
    public void Engine_reports_a_match_when_pair_tiles_are_connected()
    {
        var engine = new PuzzleEngine(new ExactFitLayoutGenerator(new Random(12)));
        engine.StartNewGame(CreateBlocks(5));

        var pair = engine.Layout.Tiles.GroupBy(tile => tile.PairId).First();
        var question = pair.Single(tile => tile.IsQuestion);
        var answer = pair.Single(tile => !tile.IsQuestion);
        var questionTarget = GetTopLeft(question.TargetCells);
        var answerTarget = GetTopLeft(answer.TargetCells);

        Assert.True(engine.Place(question, questionTarget.Row, questionTarget.Column).Success);
        var result = engine.Place(answer, answerTarget.Row, answerTarget.Column);

        Assert.True(result.Success);
        Assert.True(result.PairConnected);
        Assert.Contains(pair.Key, engine.ConnectedPairIds);
    }

    private static IReadOnlyList<GameBlock> CreateBlocks(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new GameBlock
            {
                Term = $"Term {index}",
                Definition = $"Definition {index}"
            })
            .ToArray();
    }

    private static GridCell GetTopLeft(IReadOnlyList<GridCell> cells)
    {
        return new GridCell(cells.Min(cell => cell.Row), cells.Min(cell => cell.Column));
    }

    private static bool ShareAnEdge(IReadOnlyList<GridCell> first, IReadOnlyList<GridCell> second)
    {
        return first.Any(left => second.Any(right => Math.Abs(left.Row - right.Row) + Math.Abs(left.Column - right.Column) == 1));
    }
}
