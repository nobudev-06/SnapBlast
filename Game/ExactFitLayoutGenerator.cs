using SnapBlast.Models;

namespace SnapBlast.Game;

public sealed class ExactFitLayoutGenerator
{
    public const int BoardSize = 8;

    private readonly Random random;

    public ExactFitLayoutGenerator(Random? random = null)
    {
        this.random = random ?? new Random();
    }

    public PuzzleLayout Generate(IReadOnlyList<GameBlock> blocks)
    {
        if (blocks.Count is < 2 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(blocks), "The exact-fit board requires between 2 and 32 quiz pairs.");
        }

        var regions = new List<RectangleRegion> { new(0, 0, BoardSize, BoardSize) };
        while (regions.Count < blocks.Count)
        {
            var candidates = regions
                .SelectMany((region, index) => GetSplits(region).Select(split => (index, split)))
                .Where(candidate => candidate.split.First.Area >= 2 && candidate.split.Second.Area >= 2)
                .ToList();

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("Unable to partition the board for this number of quiz pairs.");
            }

            var candidate = candidates[random.Next(candidates.Count)];
            regions.RemoveAt(candidate.index);
            regions.Add(candidate.split.First);
            regions.Add(candidate.split.Second);
        }

        var shuffledBlocks = blocks.OrderBy(_ => random.Next()).ToArray();
        var tiles = new List<PuzzleTile>(blocks.Count * 2);

        for (var pairIndex = 0; pairIndex < regions.Count; pairIndex++)
        {
            var block = shuffledBlocks[pairIndex];
            var pairId = $"pair-{pairIndex}";
            var split = SplitPairRegion(regions[pairIndex]);
            var questionFirst = random.Next(2) == 0;

            tiles.Add(CreateTile(pairId, block, PuzzleTileKind.Question, questionFirst ? split.First : split.Second));
            tiles.Add(CreateTile(pairId, block, PuzzleTileKind.Answer, questionFirst ? split.Second : split.First));
        }

        var layout = new PuzzleLayout { Tiles = tiles };
        if (layout.CellCount != BoardSize * BoardSize)
        {
            throw new InvalidOperationException("The generated layout does not fill the complete board.");
        }

        return layout;
    }

    private static PuzzleTile CreateTile(string pairId, GameBlock block, PuzzleTileKind kind, RectangleRegion region)
    {
        var cells = new List<GridCell>(region.Area);
        for (var row = region.Row; row < region.Row + region.Height; row++)
        {
            for (var column = region.Column; column < region.Column + region.Width; column++)
            {
                cells.Add(new GridCell(row, column));
            }
        }

        return new PuzzleTile
        {
            Id = $"{pairId}-{kind.ToString().ToLowerInvariant()}",
            PairId = pairId,
            Block = block,
            Kind = kind,
            TargetCells = cells
        };
    }

    private static (RectangleRegion First, RectangleRegion Second) SplitPairRegion(RectangleRegion region)
    {
        if (region.Width >= 2)
        {
            var splitColumn = Math.Max(1, region.Width / 2);
            return
            (
                new RectangleRegion(region.Row, region.Column, region.Height, splitColumn),
                new RectangleRegion(region.Row, region.Column + splitColumn, region.Height, region.Width - splitColumn)
            );
        }

        var splitRow = Math.Max(1, region.Height / 2);
        return
        (
            new RectangleRegion(region.Row, region.Column, splitRow, region.Width),
            new RectangleRegion(region.Row + splitRow, region.Column, region.Height - splitRow, region.Width)
        );
    }

    private static IEnumerable<(RectangleRegion First, RectangleRegion Second)> GetSplits(RectangleRegion region)
    {
        for (var width = 1; width < region.Width; width++)
        {
            yield return
            (
                new RectangleRegion(region.Row, region.Column, region.Height, width),
                new RectangleRegion(region.Row, region.Column + width, region.Height, region.Width - width)
            );
        }

        for (var height = 1; height < region.Height; height++)
        {
            yield return
            (
                new RectangleRegion(region.Row, region.Column, height, region.Width),
                new RectangleRegion(region.Row + height, region.Column, region.Height - height, region.Width)
            );
        }
    }

    private readonly record struct RectangleRegion(int Row, int Column, int Height, int Width)
    {
        public int Area => Height * Width;
    }
}
