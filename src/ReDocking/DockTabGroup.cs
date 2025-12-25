using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

/// <summary>
/// A tab group that can contain multiple dockable items.
/// </summary>
public class DockTabGroup : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DockTabGroup, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DockTabGroup, object?>(nameof(SelectedItem));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<DockTabGroup, int>(nameof(SelectedIndex), defaultValue: -1);

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<DockTabGroup, IDataTemplate?>(nameof(HeaderTemplate));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<DockTabGroup, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly RoutedEvent<DockTabDragEventArgs> TabDragStartedEvent =
        RoutedEvent.Register<DockTabGroup, DockTabDragEventArgs>(
            nameof(TabDragStarted), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<DockTabDropEventArgs> TabDropEvent =
        RoutedEvent.Register<DockTabGroup, DockTabDropEventArgs>(
            nameof(TabDrop), RoutingStrategies.Bubble);

    private ItemsControl? _tabsPresenter;
    private ContentPresenter? _contentPresenter;
    private readonly ObservableCollection<object> _items = new();
    private bool _isUpdatingSelection;

    public DockTabGroup()
    {
        _items.CollectionChanged += OnItemsChanged;

        // Attach drag drop behavior
        var behaviors = Interaction.GetBehaviors(this);
        behaviors.Add(new DockTabDragDropBehavior());
    }

    [Content]
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public event EventHandler<DockTabDragEventArgs>? TabDragStarted
    {
        add => AddHandler(TabDragStartedEvent, value);
        remove => RemoveHandler(TabDragStartedEvent, value);
    }

    public event EventHandler<DockTabDropEventArgs>? TabDrop
    {
        add => AddHandler(TabDropEvent, value);
        remove => RemoveHandler(TabDropEvent, value);
    }

    /// <summary>
    /// Gets the internal items collection.
    /// </summary>
    public ObservableCollection<object> Items => _items;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            OnItemsSourceChanged(change);
        }
        else if (change.Property == SelectedItemProperty)
        {
            OnSelectedItemChanged(change);
        }
        else if (change.Property == SelectedIndexProperty)
        {
            OnSelectedIndexChanged(change);
        }
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnSourceCollectionChanged;
        }

        _items.Clear();

        if (e.NewValue is IEnumerable newItems)
        {
            foreach (var item in newItems)
            {
                _items.Add(item);
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += OnSourceCollectionChanged;
            }
        }

        // Select first item if nothing is selected
        if (SelectedIndex < 0 && _items.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    var index = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        _items.Insert(index++, item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        _items.Remove(item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null && e.NewItems != null)
                {
                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        var index = _items.IndexOf(e.OldItems[i]!);
                        if (index >= 0)
                        {
                            _items[index] = e.NewItems[i]!;
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _items.Clear();
                if (ItemsSource != null)
                {
                    foreach (var item in ItemsSource)
                    {
                        _items.Add(item);
                    }
                }
                break;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update selection if items changed
        if (_items.Count == 0)
        {
            SelectedIndex = -1;
            SelectedItem = null;

            // Notify parent that this tab group is now empty
            OnBecameEmpty();
        }
        else if (SelectedIndex < 0 || SelectedIndex >= _items.Count)
        {
            SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Called when the tab group becomes empty.
    /// Can be overridden to handle cleanup.
    /// </summary>
    protected virtual void OnBecameEmpty()
    {
        // Request removal from parent split container
        if (Parent is DockSplitContainer splitContainer)
        {
            var remaining = splitContainer.RemoveContent(this);
            if (remaining != null)
            {
                // Collapse the split container
                if (splitContainer.Parent is DockSplitContainer parentSplit)
                {
                    if (parentSplit.First == splitContainer)
                    {
                        parentSplit.First = remaining;
                    }
                    else if (parentSplit.Second == splitContainer)
                    {
                        parentSplit.Second = remaining;
                    }
                }
                else if (splitContainer.Parent is ContentPresenter contentPresenter)
                {
                    contentPresenter.Content = remaining;
                }
            }
        }
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdatingSelection)
            return;

        _isUpdatingSelection = true;
        try
        {
            if (e.NewValue != null)
            {
                var index = _items.IndexOf(e.NewValue);
                if (index >= 0)
                {
                    SelectedIndex = index;
                }
            }
            else
            {
                SelectedIndex = -1;
            }

            UpdateContent();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void OnSelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdatingSelection)
            return;

        _isUpdatingSelection = true;
        try
        {
            var index = (int)(e.NewValue ?? -1);
            if (index >= 0 && index < _items.Count)
            {
                SelectedItem = _items[index];
            }
            else
            {
                SelectedItem = null;
            }

            UpdateContent();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void UpdateContent()
    {
        if (_contentPresenter != null)
        {
            _contentPresenter.Content = SelectedItem;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _tabsPresenter = e.NameScope.Find<ItemsControl>("PART_TabsPresenter");
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");

        if (_tabsPresenter != null)
        {
            _tabsPresenter.ItemsSource = _items;
        }

        UpdateContent();
    }

    /// <summary>
    /// Adds an item to the tab group.
    /// </summary>
    public void AddItem(object item, int index = -1)
    {
        if (index < 0 || index > _items.Count)
        {
            _items.Add(item);
            index = _items.Count - 1;
        }
        else
        {
            _items.Insert(index, item);
        }

        SelectedIndex = index;
    }

    /// <summary>
    /// Removes an item from the tab group.
    /// </summary>
    public bool RemoveItem(object item)
    {
        var index = _items.IndexOf(item);
        if (index < 0)
            return false;

        _items.RemoveAt(index);

        if (SelectedIndex >= _items.Count)
        {
            SelectedIndex = _items.Count - 1;
        }

        return true;
    }

    /// <summary>
    /// Gets whether this tab group is empty.
    /// </summary>
    public bool IsEmpty => _items.Count == 0;

    /// <summary>
    /// Gets the number of items in this tab group.
    /// </summary>
    public int Count => _items.Count;

    internal void SelectTab(object item)
    {
        var index = _items.IndexOf(item);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
    }
}

/// <summary>
/// Event args for tab drag events.
/// </summary>
public class DockTabDragEventArgs : RoutedEventArgs
{
    public DockTabDragEventArgs(RoutedEvent routedEvent, object? source, object item, DockTabGroup sourceGroup)
        : base(routedEvent, source)
    {
        Item = item;
        SourceGroup = sourceGroup;
    }

    public object Item { get; }
    public DockTabGroup SourceGroup { get; }
}

/// <summary>
/// Event args for tab drop events.
/// </summary>
public class DockTabDropEventArgs : RoutedEventArgs
{
    public DockTabDropEventArgs(
        RoutedEvent routedEvent,
        object? source,
        object item,
        DockTabGroup sourceGroup,
        DockTabGroup targetGroup,
        DockSplitPosition position)
        : base(routedEvent, source)
    {
        Item = item;
        SourceGroup = sourceGroup;
        TargetGroup = targetGroup;
        Position = position;
    }

    public object Item { get; }
    public DockTabGroup SourceGroup { get; }
    public DockTabGroup TargetGroup { get; }
    public DockSplitPosition Position { get; }
}
