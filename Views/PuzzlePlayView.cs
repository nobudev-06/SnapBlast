using SnapBlast.Game;
using SnapBlast.Models;

namespace SnapBlast.Views;

public sealed class PuzzlePlayView : GraphicsView
{
    private PuzzleTile? draggingTile;
    private int ghostRow = -1;
    private int ghostColumn = -1;
    private bool ghostIsValid;

    public static readonly BindableProperty EngineProperty = BindableProperty.Create(
        nameof(Engine), typeof(PuzzleEngine), typeof(PuzzlePlayView), propertyChanged: OnViewPropertyChanged);

    public static readonly BindableProperty SelectedTileIndexProperty = BindableProperty.Create(
        nameof(SelectedTileIndex), typeof(int), typeof(PuzzlePlayView), -1, propertyChanged: OnViewPropertyChanged);

    public PuzzlePlayView()
    {
        Drawable = new ExactFitDrawable(this);
        BackgroundColor = Colors.Transparent;
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
    }

    public event EventHandler<TileDropEventArgs>? TileDropped;
    public event EventHandler? TileDropRejected;

    public PuzzleEngine? Engine
    {
        get => (PuzzleEngine?)GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    public int SelectedTileIndex
    {
        get => (int)GetValue(SelectedTileIndexProperty);
        set => SetValue(SelectedTileIndexProperty, value);
    }

    public void Refresh() => Invalidate();

    private static void OnViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((PuzzlePlayView)bindable).Invalidate();
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (Engine is null || point == default)
        {
            return;
        }

        if (!TryGetTrayTile(point, out var trayTile))
        {
            trayTile = SelectedTileIndex >= 0 && SelectedTileIndex < Engine.RemainingTiles.Count && IsInBoard(point)
                ? Engine.RemainingTiles[SelectedTileIndex]
                : null;
        }

        if (trayTile is null)
        {
            return;
        }

        draggingTile = trayTile;
        UpdateGhost(point);
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (draggingTile is null)
        {
            return;
        }

        UpdateGhost(e.Touches.FirstOrDefault());
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (draggingTile is null)
        {
            return;
        }

        if (ghostIsValid && Engine is not null)
        {
            TileDropped?.Invoke(this, new TileDropEventArgs(draggingTile, ghostRow, ghostColumn));
        }
        else
        {
            TileDropRejected?.Invoke(this, EventArgs.Empty);
        }

        draggingTile = null;
        ghostRow = -1;
        ghostColumn = -1;
        ghostIsValid = false;
        Invalidate();
    }

    private void UpdateGhost(PointF point)
    {
        ghostRow = -1;
        ghostColumn = -1;
        ghostIsValid = false;

        if (draggingTile is not null && Engine is not null && TryGetBoardCell(point, out var row, out var column))
        {
            var bounds = GetBounds(draggingTile.TargetCells);
            ghostRow = row - bounds.Height / 2;
            ghostColumn = column - bounds.Width / 2;
            ghostIsValid = Engine.CanPlace(draggingTile, ghostRow, ghostColumn);
        }

        Invalidate();
    }

    private bool TryGetTrayTile(PointF point, out PuzzleTile? tile)
    {
        tile = null;
        if (Engine is null)
        {
            return false;
        }

        var tray = GetTrayLayout();
        if (!tray.Bounds.Contains(point))
        {
            return false;
        }

        var column = Math.Clamp((int)((point.X - tray.Bounds.Left) / tray.CardWidth), 0, tray.Columns - 1);
        var row = Math.Max(0, (int)((point.Y - tray.Bounds.Top) / tray.CardHeight));
        var index = row * tray.Columns + column;
        if (index >= Engine.RemainingTiles.Count)
        {
            return false;
        }

        tile = Engine.RemainingTiles[index];
        return true;
    }

    private bool IsInBoard(PointF point) => GetBoardBounds().Contains(point);

    private bool TryGetBoardCell(PointF point, out int row, out int column)
    {
        var board = GetBoardBounds();
        var cellSize = board.Width / PuzzleEngine.BoardSize;
        column = (int)((point.X - board.Left) / cellSize);
        row = (int)((point.Y - board.Top) / cellSize);
        return board.Contains(point) && row is >= 0 and < PuzzleEngine.BoardSize && column is >= 0 and < PuzzleEngine.BoardSize;
    }

    private RectF GetBoardBounds()
    {
        var viewWidth = (float)Width;
        var size = MathF.Min(viewWidth - 12f, 350f);
        return new RectF((viewWidth - size) / 2f, 8f, size, size);
    }

    private (RectF Bounds, float CardWidth, float CardHeight, int Columns) GetTrayLayout()
    {
        var board = GetBoardBounds();
        const int columns = 3;
        const float gap = 6f;
        var cardWidth = ((float)Width - 16f - gap * (columns - 1)) / columns;
        var cardHeight = 68f;
        return (new RectF(8f, board.Bottom + 28f, (float)Width - 16f, (float)Height - board.Bottom - 28f), cardWidth + gap, cardHeight + gap, columns);
    }

    private static TileBounds GetBounds(IReadOnlyList<GridCell> cells)
    {
        var minRow = cells.Min(cell => cell.Row);
        var minColumn = cells.Min(cell => cell.Column);
        var maxRow = cells.Max(cell => cell.Row);
        var maxColumn = cells.Max(cell => cell.Column);
        return new TileBounds(minRow, minColumn, maxRow - minRow + 1, maxColumn - minColumn + 1);
    }

    private sealed class ExactFitDrawable(PuzzlePlayView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var board = owner.GetBoardBounds();
            var cellSize = board.Width / PuzzleEngine.BoardSize;
            DrawBoard(canvas, board, cellSize);
            DrawTray(canvas);
        }

        private void DrawBoard(ICanvas canvas, RectF board, float cellSize)
        {
            canvas.FillColor = Color.FromArgb("#E8DFF5");
            canvas.FillRoundedRectangle(board.Left, board.Top, board.Width, board.Height, 18);

            for (var row = 0; row < PuzzleEngine.BoardSize; row++)
            {
                for (var column = 0; column < PuzzleEngine.BoardSize; column++)
                {
                    var x = board.Left + column * cellSize + 2;
                    var y = board.Top + row * cellSize + 2;
                    canvas.FillColor = owner.Engine?.IsOccupied(row, column) == true
                        ? Color.FromArgb("#FFFDFB")
                        : Color.FromArgb("#D8CDE8");
                    canvas.FillRoundedRectangle(x, y, cellSize - 4, cellSize - 4, 5);
                }
            }

            if (owner.Engine is not null)
            {
                var drawn = new HashSet<string>();
                foreach (var tile in owner.Engine.Layout.Tiles)
                {
                    var cells = owner.Engine.GetPlacedCells(tile);
                    if (cells.Count == 0 || !drawn.Add(tile.Id))
                    {
                        continue;
                    }

                    DrawPlacedTile(canvas, tile, cells, board, cellSize);
                }

                DrawPairConnections(canvas, board, cellSize);
            }

            if (owner.draggingTile is not null && owner.ghostRow >= 0 && owner.ghostColumn >= 0)
            {
                DrawGhost(canvas, owner.draggingTile, board, cellSize);
            }
        }

        private void DrawPairConnections(ICanvas canvas, RectF board, float cellSize)
        {
            if (owner.Engine is null)
            {
                return;
            }

            foreach (var pairId in owner.Engine.ConnectedPairIds)
            {
                var question = owner.Engine.Layout.Tiles.FirstOrDefault(tile => tile.PairId == pairId && tile.IsQuestion);
                var answer = owner.Engine.Layout.Tiles.FirstOrDefault(tile => tile.PairId == pairId && !tile.IsQuestion);
                if (question is null || answer is null)
                {
                    continue;
                }

                var questionCells = owner.Engine.GetPlacedCells(question);
                var answerCells = owner.Engine.GetPlacedCells(answer);
                if (questionCells.Count == 0 || answerCells.Count == 0)
                {
                    continue;
                }

                GridCell? adjacentQuestion = questionCells.FirstOrDefault(left => answerCells.Any(right => Math.Abs(left.Row - right.Row) + Math.Abs(left.Column - right.Column) == 1));
                GridCell? adjacentAnswer = adjacentQuestion is { } questionCell
                    ? answerCells.FirstOrDefault(right => Math.Abs(right.Row - questionCell.Row) + Math.Abs(right.Column - questionCell.Column) == 1)
                    : null;
                if (adjacentQuestion is null || adjacentAnswer is null)
                {
                    continue;
                }

                var start = CellCenter(board, cellSize, adjacentQuestion.Value);
                var end = CellCenter(board, cellSize, adjacentAnswer.Value);
                canvas.StrokeColor = Color.FromArgb("#E6B85C");
                canvas.StrokeSize = Math.Max(3, cellSize / 9);
                canvas.DrawLine(start.X, start.Y, end.X, end.Y);
                canvas.FillColor = Color.FromArgb("#E6B85C");
                canvas.FillCircle((start.X + end.X) / 2, (start.Y + end.Y) / 2, Math.Max(4, cellSize / 7));
            }
        }

        private static PointF CellCenter(RectF board, float cellSize, GridCell cell) =>
            new(board.Left + cell.Column * cellSize + cellSize / 2, board.Top + cell.Row * cellSize + cellSize / 2);

        private static void DrawPlacedTile(ICanvas canvas, PuzzleTile tile, IReadOnlyList<GridCell> cells, RectF board, float cellSize)
        {
            var bounds = GetBounds(cells);
            var x = board.Left + bounds.Column * cellSize + 2;
            var y = board.Top + bounds.Row * cellSize + 2;
            var width = bounds.Width * cellSize - 4;
            var height = bounds.Height * cellSize - 4;
            canvas.FillColor = tile.IsQuestion ? Color.FromArgb("#D9CDF7") : Color.FromArgb("#C9EBDD");
            canvas.FillRoundedRectangle(x, y, width, height, 7);
            canvas.StrokeColor = tile.IsQuestion ? Color.FromArgb("#9A82D4") : Color.FromArgb("#71B99D");
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(x, y, width, height, 7);
            DrawTileText(canvas, tile, x, y, width, height, Math.Max(7, Math.Min(14, cellSize / 3.5f)));
        }

        private void DrawGhost(ICanvas canvas, PuzzleTile tile, RectF board, float cellSize)
        {
            var bounds = GetBounds(tile.TargetCells);
            var x = board.Left + owner.ghostColumn * cellSize + 2;
            var y = board.Top + owner.ghostRow * cellSize + 2;
            var width = bounds.Width * cellSize - 4;
            var height = bounds.Height * cellSize - 4;
            canvas.FillColor = owner.ghostIsValid ? Color.FromArgb("#80C7E4F5") : Color.FromArgb("#80F4B6B6");
            canvas.FillRoundedRectangle(x, y, width, height, 7);
        }

        private void DrawTray(ICanvas canvas)
        {
            var tray = owner.GetTrayLayout();
            canvas.FontColor = Color.FromArgb("#554A5B");
            canvas.FontSize = 13;
            canvas.DrawString("Match each separate question tile to its answer tile", 8, tray.Bounds.Top - 21, (float)owner.Width - 16f, 17, HorizontalAlignment.Center, VerticalAlignment.Center);

            for (var index = 0; index < (owner.Engine?.RemainingTiles.Count ?? 0); index++)
            {
                var row = index / tray.Columns;
                var column = index % tray.Columns;
                var x = 8f + column * tray.CardWidth;
                var y = tray.Bounds.Top + row * tray.CardHeight;
                var tile = owner.Engine!.RemainingTiles[index];
                canvas.FillColor = tile.IsQuestion ? Color.FromArgb("#D9CDF7") : Color.FromArgb("#C9EBDD");
                canvas.FillRoundedRectangle(x, y, tray.CardWidth - 6, tray.CardHeight - 6, 10);
                canvas.StrokeColor = tile.IsQuestion ? Color.FromArgb("#9A82D4") : Color.FromArgb("#71B99D");
                canvas.StrokeSize = index == owner.SelectedTileIndex ? 4 : 2;
                canvas.DrawRoundedRectangle(x, y, tray.CardWidth - 6, tray.CardHeight - 6, 10);
                canvas.FontColor = Color.FromArgb("#554A5B");
                canvas.FontSize = 9;
                canvas.DrawString(tile.IsQuestion ? "?" : "✓", x + 5, y + 5, 15, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
                DrawShapePreview(canvas, tile, x + 5, y + 30, 34, 30);
                canvas.FontSize = 10;
                var text = tile.IsQuestion ? tile.Block.Term : tile.Block.Definition;
                canvas.DrawString(Shorten(text), x + 44, y + 7, tray.CardWidth - 52, tray.CardHeight - 14, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        private static void DrawShapePreview(ICanvas canvas, PuzzleTile tile, float x, float y, float width, float height)
        {
            var bounds = GetBounds(tile.TargetCells);
            var cellSize = MathF.Min(width / bounds.Width, height / bounds.Height);
            canvas.FillColor = tile.IsQuestion ? Color.FromArgb("#7654D6") : Color.FromArgb("#329B61");
            foreach (var cell in tile.TargetCells)
            {
                var cellX = x + (cell.Column - bounds.Column) * cellSize;
                var cellY = y + (cell.Row - bounds.Row) * cellSize;
                canvas.FillRoundedRectangle(cellX, cellY, cellSize - 1, cellSize - 1, 2);
            }
        }

        private static void DrawTileText(ICanvas canvas, PuzzleTile tile, float x, float y, float width, float height, float fontSize)
        {
            canvas.FontColor = Color.FromArgb("#172033");
            canvas.FontSize = fontSize;
            canvas.DrawString(tile.IsQuestion ? "?" : "✓", x + 4, y + 3, 18, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(Shorten(tile.IsQuestion ? tile.Block.Term : tile.Block.Definition), x + 4, y + 20, width - 8, height - 24, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private static string Shorten(string value) => value.Length > 90 ? $"{value[..87]}..." : value;
    }

    private readonly record struct TileBounds(int Row, int Column, int Height, int Width);
}

public sealed class TileDropEventArgs(PuzzleTile tile, int row, int column) : EventArgs
{
    public PuzzleTile Tile { get; } = tile;
    public int Row { get; } = row;
    public int Column { get; } = column;
}
