using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ReDocking;

/// <summary>
/// A tab item within a DockTabGroup.
/// </summary>
public class DockTabItem : TemplatedControl
{
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<DockTabItem, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<DockTabItem, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DockTabItem, bool>(nameof(IsSelected));

    private Point _startPoint;
    private bool _canDrag;
    private bool _isDragging;

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateIsSelected();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            UpdateIsSelected();
        }
    }

    private void UpdateIsSelected()
    {
        var tabGroup = this.FindAncestorOfType<DockTabGroup>();
        if (tabGroup != null)
        {
            IsSelected = tabGroup.SelectedItem == Content;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(this);
            _canDrag = true;

            // Select this tab
            var tabGroup = this.FindAncestorOfType<DockTabGroup>();
            if (tabGroup != null && Content != null)
            {
                tabGroup.SelectTab(Content);
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _canDrag = false;
        _isDragging = false;
    }

    protected override async void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_canDrag || _isDragging)
            return;

        var point = e.GetPosition(this);
        var threshold = Math.Min(Bounds.Width, Bounds.Height) / 4;

        if (Math.Abs(point.X - _startPoint.X) > threshold ||
            Math.Abs(point.Y - _startPoint.Y) > threshold)
        {
            _isDragging = true;
            _canDrag = false;

            var tabGroup = this.FindAncestorOfType<DockTabGroup>();
            if (tabGroup == null || Content == null)
            {
                _isDragging = false;
                return;
            }

            // Raise drag started event
            var args = new DockTabDragEventArgs(DockTabGroup.TabDragStartedEvent, this, Content, tabGroup);
            RaiseEvent(args);

            // Start drag operation
            var data = new DataObject();
            data.Set("DockTabItem", this);
            data.Set("DockTabContent", Content);
            data.Set("DockTabGroup", tabGroup);

            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

            _isDragging = false;
        }
    }
}
