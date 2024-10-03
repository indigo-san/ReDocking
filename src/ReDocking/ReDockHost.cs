using System;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ReDocking;

public class ReDockHost : ContentControl
{
    public static readonly RoutedEvent<SideBarButtonMoveEventArgs> ButtonMoveEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonMoveEventArgs>(nameof(ButtonMove), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<SideBarButtonDisplayModeChangedEventArgs> ButtonDisplayModeChangedEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonDisplayModeChangedEventArgs>(nameof(ButtonDisplayModeChanged),
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent<SideBarButtonFlyoutRequestedEventArgs> ButtonFlyoutRequestedEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonFlyoutRequestedEventArgs>(nameof(ButtonFlyoutRequested),
            RoutingStrategies.Bubble);

    public static readonly StyledProperty<bool> IsFloatingEnabledProperty =
        AvaloniaProperty.Register<ReDockHost, bool>(nameof(IsFloatingEnabled));

    public bool IsFloatingEnabled
    {
        get => GetValue(IsFloatingEnabledProperty);
        set => SetValue(IsFloatingEnabledProperty, value);
    }

    public AvaloniaList<DockArea> DockAreas { get; } = [];

    public event EventHandler<SideBarButtonMoveEventArgs> ButtonMove
    {
        add => AddHandler(ButtonMoveEvent, value);
        remove => RemoveHandler(ButtonMoveEvent, value);
    }

    public event EventHandler<SideBarButtonDisplayModeChangedEventArgs> ButtonDisplayModeChanged
    {
        add => AddHandler(ButtonDisplayModeChangedEvent, value);
        remove => RemoveHandler(ButtonDisplayModeChangedEvent, value);
    }

    public event EventHandler<SideBarButtonFlyoutRequestedEventArgs> ButtonFlyoutRequested
    {
        add => AddHandler(ButtonFlyoutRequestedEvent, value);
        remove => RemoveHandler(ButtonFlyoutRequestedEvent, value);
    }

    internal void ShowFlyout(SideBarButton button)
    {
        var args = new SideBarButtonFlyoutRequestedEventArgs(button, this, ButtonFlyoutRequestedEvent, this);
        RaiseEvent(args);
        if (args.Handled) return;

        var flyout = new SideBarButtonMenuFlyout(this);
        if (button.DockLocation?.LeftRight == SideBarLocation.Left)
        {
            flyout.Placement = PlacementMode.Right;
        }
        else
        {
            flyout.Placement = PlacementMode.Left;
        }

        flyout.ShowAt(button);
    }
}