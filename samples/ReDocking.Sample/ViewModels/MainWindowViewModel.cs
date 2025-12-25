using System;
using System.Linq;
using System.Reactive.Linq;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ReDocking.ViewModels;

public class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        void ConfigureToolsList(ReactiveCollection<ToolWindowViewModel> list,
            ReactiveProperty<ToolWindowViewModel?> selected)
        {
            selected.Subscribe(x =>
                list.ToObservable()
                    .Where(y => y != x && y.DisplayMode.Value == DockableDisplayMode.Docked)
                    .Subscribe(y => y.IsSelected.Value = false));

            list.ObserveAddChanged()
                .Select(x => x.IsSelected.Select(y => (x, y)))
                .Subscribe(z =>
                {
                    z.Subscribe(w =>
                    {
                        if (w is { y: true, x.DisplayMode.Value: DockableDisplayMode.Docked })
                        {
                            selected.Value = w.x;
                        }
                        else
                        {
                            selected.Value = list.FirstOrDefault(xx =>
                                xx.IsSelected.Value && xx.DisplayMode.Value == DockableDisplayMode.Docked);
                        }
                    });
                });

            list.ObserveRemoveChanged()
                .Subscribe(x => x.Dispose());
        }

        ConfigureToolsList(LeftUpperTopTools, SelectedLeftUpperTopTool);
        ConfigureToolsList(LeftUpperBottomTools, SelectedLeftUpperBottomTool);
        ConfigureToolsList(LeftLowerTopTools, SelectedLeftLowerTopTool);
        ConfigureToolsList(LeftLowerBottomTools, SelectedLeftLowerBottomTool);
        ConfigureToolsList(RightUpperTopTools, SelectedRightUpperTopTool);
        ConfigureToolsList(RightUpperBottomTools, SelectedRightUpperBottomTool);
        ConfigureToolsList(RightLowerTopTools, SelectedRightLowerTopTool);
        ConfigureToolsList(RightLowerBottomTools, SelectedRightLowerBottomTool);

        LeftUpperTopTools.Add(new ToolWindowViewModel("Search", "\ue721", new SearchViewModel()));
        LeftUpperBottomTools.Add(new ToolWindowViewModel("Explorer", "\uec50", new ExplorerViewModel()));
        LeftLowerBottomTools.Add(new ToolWindowViewModel("Debug", "\uebe8", new DebugViewModel()));
        RightUpperTopTools.Add(new ToolWindowViewModel("Notifications", "\uea8f", new NotificationsViewModel()));
        RightUpperBottomTools.Add(new ToolWindowViewModel("Properties", "\ue15e", new PropertiesViewModel()));
        RightLowerBottomTools.Add(new ToolWindowViewModel("Problem", "\ue946", new ProblemViewModel()));

        // Add sample documents for the dynamic docking demo
        Documents.Add(new DocumentViewModel("Program.cs", "// Main program file\nusing System;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}"));
        Documents.Add(new DocumentViewModel("MainWindow.xaml", "<Window>\n  <Grid>\n    <!-- Content here -->\n  </Grid>\n</Window>"));
        Documents.Add(new DocumentViewModel("README.md", "# My Project\n\nThis is a sample project demonstrating dynamic dock splitting."));
    }

    public ReactiveCollection<ToolWindowViewModel> LeftUpperTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftUpperTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> LeftUpperBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftUpperBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> LeftLowerTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftLowerTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> LeftLowerBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftLowerBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightUpperTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightUpperTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightUpperBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightUpperBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightLowerTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightLowerTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightLowerBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightLowerBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> FloatingWindows { get; } = [];

    /// <summary>
    /// Documents displayed in the dynamic docking area.
    /// </summary>
    public ReactiveCollection<DocumentViewModel> Documents { get; } = [];

    public void Dispose()
    {
        SelectedLeftUpperTopTool.Dispose();
        SelectedLeftUpperBottomTool.Dispose();
        SelectedLeftLowerTopTool.Dispose();
        SelectedLeftLowerBottomTool.Dispose();
        SelectedRightUpperTopTool.Dispose();
        SelectedRightUpperBottomTool.Dispose();
        SelectedRightLowerTopTool.Dispose();
        SelectedRightLowerBottomTool.Dispose();
    }
}