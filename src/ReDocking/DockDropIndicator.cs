using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace ReDocking;

/// <summary>
/// Visual indicator for showing drop zones during drag operations.
/// </summary>
public class DockDropIndicator : TemplatedControl
{
    public static readonly StyledProperty<DockSplitPosition?> ActivePositionProperty =
        AvaloniaProperty.Register<DockDropIndicator, DockSplitPosition?>(nameof(ActivePosition));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<DockDropIndicator, bool>(nameof(IsActive));

    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<DockDropIndicator, IBrush?>(nameof(IndicatorBrush));

    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<DockDropIndicator, IBrush?>(nameof(HighlightBrush));

    private Border? _leftIndicator;
    private Border? _rightIndicator;
    private Border? _topIndicator;
    private Border? _bottomIndicator;
    private Border? _centerIndicator;
    private Border? _previewOverlay;

    private const double IndicatorSize = 40;
    private const double IndicatorMargin = 10;

    public DockSplitPosition? ActivePosition
    {
        get => GetValue(ActivePositionProperty);
        set => SetValue(ActivePositionProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public IBrush? IndicatorBrush
    {
        get => GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _leftIndicator = e.NameScope.Find<Border>("PART_LeftIndicator");
        _rightIndicator = e.NameScope.Find<Border>("PART_RightIndicator");
        _topIndicator = e.NameScope.Find<Border>("PART_TopIndicator");
        _bottomIndicator = e.NameScope.Find<Border>("PART_BottomIndicator");
        _centerIndicator = e.NameScope.Find<Border>("PART_CenterIndicator");
        _previewOverlay = e.NameScope.Find<Border>("PART_PreviewOverlay");

        UpdateIndicators();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivePositionProperty ||
            change.Property == IsActiveProperty)
        {
            UpdateIndicators();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateIndicatorPositions(e.NewSize);
    }

    private void UpdateIndicatorPositions(Size size)
    {
        var centerX = (size.Width - IndicatorSize) / 2;
        var centerY = (size.Height - IndicatorSize) / 2;

        if (_leftIndicator != null)
        {
            _leftIndicator.Width = IndicatorSize;
            _leftIndicator.Height = IndicatorSize;
            _leftIndicator.Margin = new Thickness(IndicatorMargin, centerY, 0, 0);
        }

        if (_rightIndicator != null)
        {
            _rightIndicator.Width = IndicatorSize;
            _rightIndicator.Height = IndicatorSize;
            _rightIndicator.Margin = new Thickness(size.Width - IndicatorSize - IndicatorMargin, centerY, 0, 0);
        }

        if (_topIndicator != null)
        {
            _topIndicator.Width = IndicatorSize;
            _topIndicator.Height = IndicatorSize;
            _topIndicator.Margin = new Thickness(centerX, IndicatorMargin, 0, 0);
        }

        if (_bottomIndicator != null)
        {
            _bottomIndicator.Width = IndicatorSize;
            _bottomIndicator.Height = IndicatorSize;
            _bottomIndicator.Margin = new Thickness(centerX, size.Height - IndicatorSize - IndicatorMargin, 0, 0);
        }

        if (_centerIndicator != null)
        {
            _centerIndicator.Width = IndicatorSize;
            _centerIndicator.Height = IndicatorSize;
            _centerIndicator.Margin = new Thickness(centerX, centerY, 0, 0);
        }

        UpdatePreviewOverlay(size);
    }

    private void UpdateIndicators()
    {
        SetIndicatorHighlight(_leftIndicator, ActivePosition == DockSplitPosition.Left);
        SetIndicatorHighlight(_rightIndicator, ActivePosition == DockSplitPosition.Right);
        SetIndicatorHighlight(_topIndicator, ActivePosition == DockSplitPosition.Top);
        SetIndicatorHighlight(_bottomIndicator, ActivePosition == DockSplitPosition.Bottom);
        SetIndicatorHighlight(_centerIndicator, ActivePosition == DockSplitPosition.Center);

        UpdatePreviewOverlay(Bounds.Size);
    }

    private void SetIndicatorHighlight(Border? indicator, bool isHighlighted)
    {
        if (indicator == null)
            return;

        indicator.Opacity = isHighlighted ? 1.0 : 0.5;
    }

    private void UpdatePreviewOverlay(Size size)
    {
        if (_previewOverlay == null)
            return;

        if (!IsActive || ActivePosition == null)
        {
            _previewOverlay.IsVisible = false;
            return;
        }

        _previewOverlay.IsVisible = true;

        switch (ActivePosition)
        {
            case DockSplitPosition.Left:
                _previewOverlay.Margin = new Thickness(0);
                _previewOverlay.Width = size.Width / 2;
                _previewOverlay.Height = size.Height;
                break;
            case DockSplitPosition.Right:
                _previewOverlay.Margin = new Thickness(size.Width / 2, 0, 0, 0);
                _previewOverlay.Width = size.Width / 2;
                _previewOverlay.Height = size.Height;
                break;
            case DockSplitPosition.Top:
                _previewOverlay.Margin = new Thickness(0);
                _previewOverlay.Width = size.Width;
                _previewOverlay.Height = size.Height / 2;
                break;
            case DockSplitPosition.Bottom:
                _previewOverlay.Margin = new Thickness(0, size.Height / 2, 0, 0);
                _previewOverlay.Width = size.Width;
                _previewOverlay.Height = size.Height / 2;
                break;
            case DockSplitPosition.Center:
                _previewOverlay.Margin = new Thickness(0);
                _previewOverlay.Width = size.Width;
                _previewOverlay.Height = size.Height;
                break;
        }
    }

    /// <summary>
    /// Determines the drop position based on the pointer location.
    /// </summary>
    public DockSplitPosition? GetDropPosition(Point position)
    {
        if (!IsActive)
            return null;

        var size = Bounds.Size;
        var centerX = size.Width / 2;
        var centerY = size.Height / 2;

        // Check if pointer is over any indicator
        if (IsOverIndicator(_leftIndicator, position))
            return DockSplitPosition.Left;
        if (IsOverIndicator(_rightIndicator, position))
            return DockSplitPosition.Right;
        if (IsOverIndicator(_topIndicator, position))
            return DockSplitPosition.Top;
        if (IsOverIndicator(_bottomIndicator, position))
            return DockSplitPosition.Bottom;
        if (IsOverIndicator(_centerIndicator, position))
            return DockSplitPosition.Center;

        // Check regions if not over specific indicators
        var relX = position.X / size.Width;
        var relY = position.Y / size.Height;

        if (relX < 0.25)
            return DockSplitPosition.Left;
        if (relX > 0.75)
            return DockSplitPosition.Right;
        if (relY < 0.25)
            return DockSplitPosition.Top;
        if (relY > 0.75)
            return DockSplitPosition.Bottom;

        return DockSplitPosition.Center;
    }

    private bool IsOverIndicator(Border? indicator, Point position)
    {
        if (indicator == null)
            return false;

        var indicatorBounds = new Rect(
            indicator.Margin.Left,
            indicator.Margin.Top,
            indicator.Width,
            indicator.Height);

        return indicatorBounds.Contains(position);
    }
}
