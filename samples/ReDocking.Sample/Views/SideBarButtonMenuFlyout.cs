using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using ReDocking.ViewModels;

namespace ReDocking.Views;

public class CustomSideBarButtonMenuFlyout : MenuFlyout
{
    private readonly ReDockHost _dockHost;

    public CustomSideBarButtonMenuFlyout(ReDockHost dockHost)
    {
        _dockHost = dockHost;
        var list = new List<Control>();

        {
            var moveMenu = new MenuItem();
            moveMenu.Header = "Move to";
            moveMenu.ItemsSource = dockHost.DockAreas;
            moveMenu.DataTemplates.Add(new FuncDataTemplate<DockArea>(_ => true,
                o => new TextBlock
                {
                    [!TextBlock.TextProperty] = o.GetObservable(DockArea.LocalizedNameProperty).ToBinding(),
                }));

            moveMenu.AddHandler(MenuItem.ClickEvent, OnMoveToSubItemClick);
            list.Add(moveMenu);
        }

        {
            var closeMenu = new MenuItem();
            closeMenu.Header = "Close";
            closeMenu.AddHandler(MenuItem.ClickEvent, OnCloseClick);
            list.Add(closeMenu);
        }

        if (dockHost.IsFloatingEnabled)
        {
            var displayMenu = new MenuItem();
            displayMenu.Header = "Display mode";
            displayMenu.ItemsSource = new List<Control>
            {
                new MenuItem { Header = "Docked", Tag = DockableDisplayMode.Docked },
                new MenuItem { Header = "Floating", Tag = DockableDisplayMode.Floating },
            };
            displayMenu.AddHandler(MenuItem.ClickEvent, OnDisplayModeClick);
            list.Add(displayMenu);
        }

        ItemsSource = list;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (Target is not SideBarButton button) return;
        if (button is not { DockLocation: { } location }) return;
        if (button is not { DataContext: ToolWindowViewModel buttonViewModel }) return;
        if (button.FindAncestorOfType<MainWindow>() is not { DataContext: MainWindowViewModel viewModel }) return;

        buttonViewModel.IsSelected.Value = false;
        var itemsSource = MainWindow.GetItemsSource(viewModel, location);
        itemsSource.Remove(buttonViewModel);
    }

    private void OnDisplayModeClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MenuItem { Tag: DockableDisplayMode mode } &&
            Target is SideBarButton button)
        {
            var args = new SideBarButtonDisplayModeChangedEventArgs(ReDockHost.ButtonDisplayModeChangedEvent, this)
            {
                DisplayMode = mode,
                Item = button.DataContext,
                Button = button
            };
            _dockHost.RaiseEvent(args);
        }
    }

    private void OnMoveToSubItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MenuItem { DataContext: DockArea area } &&
            Target is SideBarButton button)
        {
            // Target
            var oldSideBar = button.FindAncestorOfType<SideBar>();
            var newSideBar = area.SideBar;
            if (oldSideBar is null || newSideBar is null) return;
            var oldLocation = button.DockLocation;
            var newLocation = area.Location;
            if (oldLocation is null || oldLocation == newLocation) return;

            var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceSideBar = oldSideBar,
                SourceLocation = oldLocation,
                DestinationSideBar = newSideBar,
                DestinationLocation = newLocation,
                DestinationIndex = 0
            };
            _dockHost.RaiseEvent(args);
        }
    }
}