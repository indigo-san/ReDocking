using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

/// <summary>
/// Behavior that handles drag and drop operations for DockTabGroup.
/// </summary>
public class DockTabDragDropBehavior : Behavior<DockTabGroup>
{
    private DockDropIndicator? _dropIndicator;
    private AdornerLayer? _adornerLayer;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, true);
            AssociatedObject.AddHandler(DragDrop.DropEvent, OnDrop);
            AssociatedObject.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AssociatedObject.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AssociatedObject.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, false);
            AssociatedObject.RemoveHandler(DragDrop.DropEvent, OnDrop);
            AssociatedObject.RemoveHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AssociatedObject.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            AssociatedObject.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        if (e.Data.Contains("DockTabContent") || e.Data.Contains("SideBarButton"))
        {
            ShowDropIndicator();
            UpdateDropPosition(e);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (AssociatedObject == null || _dropIndicator == null)
            return;

        if (e.Data.Contains("DockTabContent") || e.Data.Contains("SideBarButton"))
        {
            UpdateDropPosition(e);
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        HideDropIndicator();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (AssociatedObject == null || _dropIndicator == null)
            return;

        var position = _dropIndicator.ActivePosition;
        HideDropIndicator();

        if (position == null)
            return;

        // Handle drop from another DockTabGroup
        if (e.Data.Contains("DockTabContent"))
        {
            var content = e.Data.Get("DockTabContent");
            var sourceGroup = e.Data.Get("DockTabGroup") as DockTabGroup;

            if (content == null || sourceGroup == null)
                return;

            HandleTabDrop(content, sourceGroup, position.Value);
        }
        // Handle drop from SideBar
        else if (e.Data.Contains("SideBarButton"))
        {
            if (e.Data.Get("SideBarButton") is not SideBarButton { DataContext: not null } button)
                return;

            HandleSideBarDrop(button, position.Value);
        }
    }

    private void HandleTabDrop(object content, DockTabGroup sourceGroup, DockSplitPosition position)
    {
        if (AssociatedObject == null)
            return;

        // If dropping on center, add to this tab group
        if (position == DockSplitPosition.Center)
        {
            if (sourceGroup != AssociatedObject)
            {
                sourceGroup.RemoveItem(content);
                AssociatedObject.AddItem(content);
            }
            return;
        }

        // For other positions, we need to create a split
        // Remove from source first
        sourceGroup.RemoveItem(content);

        // Create new tab group for the dropped content
        var newTabGroup = new DockTabGroup();
        newTabGroup.AddItem(content);

        // Find parent split container or create one
        var parent = AssociatedObject.Parent;
        if (parent is DockSplitContainer splitContainer)
        {
            // Determine which child we are
            var isFirst = splitContainer.First == AssociatedObject;

            // Create new nested split container
            var newSplit = new DockSplitContainer
            {
                Orientation = position == DockSplitPosition.Left || position == DockSplitPosition.Right
                    ? DockSplitOrientation.Horizontal
                    : DockSplitOrientation.Vertical,
                FirstProportion = 0.5,
                SecondProportion = 0.5
            };

            if (position == DockSplitPosition.Left || position == DockSplitPosition.Top)
            {
                newSplit.First = newTabGroup;
                newSplit.Second = AssociatedObject;
            }
            else
            {
                newSplit.First = AssociatedObject;
                newSplit.Second = newTabGroup;
            }

            if (isFirst)
            {
                splitContainer.First = newSplit;
            }
            else
            {
                splitContainer.Second = newSplit;
            }
        }
        else if (parent is ContentPresenter contentPresenter)
        {
            // We're directly in a content presenter, need to wrap in a split container
            var newSplit = new DockSplitContainer
            {
                Orientation = position == DockSplitPosition.Left || position == DockSplitPosition.Right
                    ? DockSplitOrientation.Horizontal
                    : DockSplitOrientation.Vertical,
                FirstProportion = 0.5,
                SecondProportion = 0.5
            };

            if (position == DockSplitPosition.Left || position == DockSplitPosition.Top)
            {
                newSplit.First = newTabGroup;
                newSplit.Second = AssociatedObject;
            }
            else
            {
                newSplit.First = AssociatedObject;
                newSplit.Second = newTabGroup;
            }

            contentPresenter.Content = newSplit;
        }

        // Raise event for external handling
        var args = new DockTabDropEventArgs(
            DockTabGroup.TabDropEvent,
            AssociatedObject,
            content,
            sourceGroup,
            AssociatedObject,
            position);
        AssociatedObject.RaiseEvent(args);
    }

    private void HandleSideBarDrop(SideBarButton button, DockSplitPosition position)
    {
        if (AssociatedObject == null)
            return;

        // This will be handled by the existing SideBar event system
        // Just raise a notification event
        var oldSideBar = button.FindAncestorOfType<SideBar>();
        if (oldSideBar == null) return;

        // Raise the ButtonMove event for the ReDockHost to handle
        var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, AssociatedObject)
        {
            Item = button.DataContext,
            Button = button,
            SourceSideBar = oldSideBar,
            SourceLocation = button.DockLocation,
            DestinationSideBar = oldSideBar, // Will be updated by handler
            DestinationLocation = button.DockLocation, // Will be updated by handler
            DestinationIndex = 0
        };
        AssociatedObject.RaiseEvent(args);
    }

    private void ShowDropIndicator()
    {
        if (AssociatedObject == null)
            return;

        _adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
        if (_adornerLayer == null)
            return;

        _dropIndicator = new DockDropIndicator
        {
            IsActive = true,
            Width = AssociatedObject.Bounds.Width,
            Height = AssociatedObject.Bounds.Height
        };

        var pos = AssociatedObject.TranslatePoint(default, _adornerLayer) ?? default;
        _dropIndicator.Margin = new Thickness(pos.X, pos.Y, 0, 0);

        _adornerLayer.Children.Add(_dropIndicator);
    }

    private void HideDropIndicator()
    {
        if (_adornerLayer != null && _dropIndicator != null)
        {
            _adornerLayer.Children.Remove(_dropIndicator);
        }

        _dropIndicator = null;
        _adornerLayer = null;
    }

    private void UpdateDropPosition(DragEventArgs e)
    {
        if (AssociatedObject == null || _dropIndicator == null)
            return;

        var position = e.GetPosition(AssociatedObject);
        _dropIndicator.ActivePosition = _dropIndicator.GetDropPosition(position);
    }
}

/// <summary>
/// Behavior that handles drag and drop for the entire docking area.
/// </summary>
public class DockingAreaDragDropBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, true);
            AssociatedObject.AddHandler(DragDrop.DropEvent, OnDrop, handledEventsToo: true);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, false);
            AssociatedObject.RemoveHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Handle drops that weren't handled by specific tab groups
        // This can be used for creating new floating windows or other behaviors
    }
}
