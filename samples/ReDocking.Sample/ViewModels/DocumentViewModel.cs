using Reactive.Bindings;

namespace ReDocking.ViewModels;

/// <summary>
/// ViewModel for a document tab in the dynamic docking area.
/// </summary>
public class DocumentViewModel
{
    public DocumentViewModel(string title, string content)
    {
        Title = new ReactiveProperty<string>(title);
        Content = new ReactiveProperty<string>(content);
    }

    public ReactiveProperty<string> Title { get; }

    public ReactiveProperty<string> Content { get; }

    public override string ToString() => Title.Value;
}
