using Avalonia.Interactivity;

namespace ReDocking;

public class SideBarButtonFlyoutRequestedEventArgs(
    SideBarButton button,
    ReDockHost dockHost,
    RoutedEvent routedEvent,
    object source)
    : RoutedEventArgs(routedEvent, source)
{
    public SideBarButton Button { get; } = button;
    public ReDockHost DockHost { get; } = dockHost;
}