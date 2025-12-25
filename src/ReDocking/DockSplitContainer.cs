using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace ReDocking;

/// <summary>
/// Orientation for the split container.
/// </summary>
public enum DockSplitOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// A container that can dynamically split its content horizontally or vertically.
/// </summary>
public class DockSplitContainer : TemplatedControl
{
    public static readonly StyledProperty<object?> FirstProperty =
        AvaloniaProperty.Register<DockSplitContainer, object?>(nameof(First));

    public static readonly StyledProperty<object?> SecondProperty =
        AvaloniaProperty.Register<DockSplitContainer, object?>(nameof(Second));

    public static readonly StyledProperty<DockSplitOrientation> OrientationProperty =
        AvaloniaProperty.Register<DockSplitContainer, DockSplitOrientation>(nameof(Orientation));

    public static readonly StyledProperty<double> FirstProportionProperty =
        AvaloniaProperty.Register<DockSplitContainer, double>(nameof(FirstProportion), defaultValue: 0.5);

    public static readonly StyledProperty<double> SecondProportionProperty =
        AvaloniaProperty.Register<DockSplitContainer, double>(nameof(SecondProportion), defaultValue: 0.5);

    public static readonly StyledProperty<double> MinimumSizeProperty =
        AvaloniaProperty.Register<DockSplitContainer, double>(nameof(MinimumSize), defaultValue: 50);

    private ContentPresenter? _firstPresenter;
    private ContentPresenter? _secondPresenter;
    private Thumb? _thumb;

    private const double ThumbPadding = 2;

    public object? First
    {
        get => GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    public object? Second
    {
        get => GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    public DockSplitOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double FirstProportion
    {
        get => GetValue(FirstProportionProperty);
        set => SetValue(FirstProportionProperty, value);
    }

    public double SecondProportion
    {
        get => GetValue(SecondProportionProperty);
        set => SetValue(SecondProportionProperty, value);
    }

    public double MinimumSize
    {
        get => GetValue(MinimumSizeProperty);
        set => SetValue(MinimumSizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FirstProperty ||
            change.Property == SecondProperty)
        {
            ContentChanged(change);
        }
        else if (change.Property == FirstProportionProperty ||
                 change.Property == SecondProportionProperty ||
                 change.Property == OrientationProperty)
        {
            UpdateLayout(Bounds.Size);
        }
    }

    private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is ILogical oldChild)
        {
            LogicalChildren.Remove(oldChild);
        }

        if (e.NewValue is ILogical newChild)
        {
            LogicalChildren.Add(newChild);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _firstPresenter = e.NameScope.Get<ContentPresenter>("PART_FirstPresenter");
        _secondPresenter = e.NameScope.Get<ContentPresenter>("PART_SecondPresenter");
        _thumb = e.NameScope.Get<Thumb>("PART_Thumb");

        _thumb.DragDelta += OnThumbDragDelta;

        UpdateLayout(Bounds.Size);
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_firstPresenter == null || _secondPresenter == null)
            return;

        var size = Bounds.Size;
        var totalSize = Orientation == DockSplitOrientation.Horizontal ? size.Width : size.Height;
        var delta = Orientation == DockSplitOrientation.Horizontal ? e.Vector.X : e.Vector.Y;

        var (firstSize, secondSize) = GetAbsoluteSizes(size);
        var newFirstSize = firstSize + delta;
        var newSecondSize = secondSize - delta;

        // Ensure minimum sizes
        if (newFirstSize < MinimumSize || newSecondSize < MinimumSize)
            return;

        var newFirstProportion = newFirstSize / totalSize;
        var newSecondProportion = newSecondSize / totalSize;

        FirstProportion = Math.Clamp(newFirstProportion, 0.1, 0.9);
        SecondProportion = Math.Clamp(newSecondProportion, 0.1, 0.9);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateLayout(e.NewSize);
    }

    private void UpdateLayout(Size size)
    {
        if (_firstPresenter == null || _secondPresenter == null || _thumb == null)
            return;

        var (firstSize, secondSize) = GetAbsoluteSizes(size);
        var isHorizontal = Orientation == DockSplitOrientation.Horizontal;

        if (isHorizontal)
        {
            _firstPresenter.Margin = new Thickness(0);
            _firstPresenter.Width = firstSize;
            _firstPresenter.Height = size.Height;

            _thumb.Margin = new Thickness(firstSize - ThumbPadding, 0, 0, 0);
            _thumb.Width = ThumbPadding * 2;
            _thumb.Height = size.Height;
            _thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);

            _secondPresenter.Margin = new Thickness(firstSize + ThumbPadding, 0, 0, 0);
            _secondPresenter.Width = Math.Max(secondSize - ThumbPadding, 0);
            _secondPresenter.Height = size.Height;
        }
        else
        {
            _firstPresenter.Margin = new Thickness(0);
            _firstPresenter.Width = size.Width;
            _firstPresenter.Height = firstSize;

            _thumb.Margin = new Thickness(0, firstSize - ThumbPadding, 0, 0);
            _thumb.Width = size.Width;
            _thumb.Height = ThumbPadding * 2;
            _thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);

            _secondPresenter.Margin = new Thickness(0, firstSize + ThumbPadding, 0, 0);
            _secondPresenter.Width = size.Width;
            _secondPresenter.Height = Math.Max(secondSize - ThumbPadding, 0);
        }
    }

    private (double First, double Second) GetAbsoluteSizes(Size availableSize)
    {
        var totalSize = Orientation == DockSplitOrientation.Horizontal
            ? availableSize.Width
            : availableSize.Height;
        var denominator = FirstProportion + SecondProportion;
        return (
            totalSize * FirstProportion / denominator,
            totalSize * SecondProportion / denominator
        );
    }

    /// <summary>
    /// Splits the container by inserting a new item at the specified position.
    /// </summary>
    /// <param name="newContent">The content to insert.</param>
    /// <param name="position">The position where to insert (Left, Right, Top, Bottom).</param>
    /// <param name="targetContent">The content next to which the new content will be placed.</param>
    public void Split(object newContent, DockSplitPosition position, object? targetContent = null)
    {
        var isHorizontal = position == DockSplitPosition.Left || position == DockSplitPosition.Right;
        var insertFirst = position == DockSplitPosition.Left || position == DockSplitPosition.Top;

        // If this container is empty, just set the first content
        if (First == null && Second == null)
        {
            First = newContent;
            return;
        }

        // If only First is set, set Second
        if (First != null && Second == null)
        {
            Orientation = isHorizontal ? DockSplitOrientation.Horizontal : DockSplitOrientation.Vertical;
            if (insertFirst)
            {
                Second = First;
                First = newContent;
            }
            else
            {
                Second = newContent;
            }
            return;
        }

        // If both are set, we need to create a nested container
        var existingContent = targetContent == First ? First : Second;
        var newContainer = new DockSplitContainer
        {
            Orientation = isHorizontal ? DockSplitOrientation.Horizontal : DockSplitOrientation.Vertical,
            FirstProportion = 0.5,
            SecondProportion = 0.5
        };

        if (insertFirst)
        {
            newContainer.First = newContent;
            newContainer.Second = existingContent;
        }
        else
        {
            newContainer.First = existingContent;
            newContainer.Second = newContent;
        }

        if (targetContent == First)
        {
            First = newContainer;
        }
        else
        {
            Second = newContainer;
        }
    }

    /// <summary>
    /// Removes content and collapses the container if only one child remains.
    /// </summary>
    /// <param name="content">The content to remove.</param>
    /// <returns>The remaining content if the container should be collapsed, null otherwise.</returns>
    public object? RemoveContent(object content)
    {
        if (First == content)
        {
            First = null;
            return Second;
        }

        if (Second == content)
        {
            Second = null;
            return First;
        }

        return null;
    }
}

/// <summary>
/// Position for splitting a dock container.
/// </summary>
public enum DockSplitPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Center
}
