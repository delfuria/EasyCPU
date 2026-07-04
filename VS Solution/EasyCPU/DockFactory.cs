#nullable enable
using System.Collections.Generic;
using System.Linq;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using EasyCPU.ViewModels;

namespace EasyCPU;

public class DockFactory : Factory
{
    private readonly MainViewModel _mainVm;

    public CodeEditorViewModel? CodeEditor { get; private set; }
    public DataEditorViewModel? DataEditor  { get; private set; }
    public RegistersViewModel?  Registers   { get; private set; }
    public StackViewModel?      Stack       { get; private set; }
    public MemoryViewModel?     Memory      { get; private set; }
    public ErrorsViewModel?     Errors      { get; private set; }

    // Container refs — used by panel visibility management
    internal IDock? DocumentContainer { get; private set; }
    internal IDock? ToolContainer     { get; private set; }
    internal IDock? ErrorsContainer   { get; private set; }

    public DockFactory(MainViewModel mainVm) => _mainVm = mainVm;

    internal IRootDock? CurrentLayout { get; set; }

    // Returns the container that CURRENTLY holds the panel by walking the full layout tree.
    // Falls back to the home container when the panel is not visible anywhere.
    internal IDock? ContainerFor(IDockable? d)
    {
        if (d is null) return null;
        if (CurrentLayout is not null)
        {
            var found = FindContainerInTree(CurrentLayout, d);
            if (found is not null) return found;
        }
        if (d == CodeEditor || d == DataEditor) return DocumentContainer;
        if (d == Errors) return ErrorsContainer;
        return ToolContainer;
    }

    private static IDock? FindContainerInTree(IDock parent, IDockable target)
    {
        if (parent.VisibleDockables is null) return null;
        foreach (var child in parent.VisibleDockables)
        {
            if (child == target) return parent;
            if (child is IDock childDock)
            {
                var found = FindContainerInTree(childDock, target);
                if (found is not null) return found;
            }
        }
        return null;
    }

    // Visibility check: is the panel currently in its container's VisibleDockables?
    internal bool IsPanelVisible(IDockable? d) =>
        d is not null && ContainerFor(d)?.VisibleDockables?.Contains(d) == true;

    // Override CloseDockable (X button) to use RemoveDockable with collapse=false.
    // collapse=false prevents the container from being removed from the tree when emptied,
    // which would break AddDockable when trying to show the panel again.
    public override void CloseDockable(IDockable dockable)
    {
        RemoveDockable(dockable, collapse: false);
        _mainVm.OnPanelVisibilityChanged();
    }

    // ── Layout serialization ──────────────────────────────────────────────────

    internal DockNode ToDockNode(IDockable d)
    {
        switch (d)
        {
            case RootDock root:
                return new RootNode(
                    root.ActiveDockable?.Id,
                    root.VisibleDockables?.Select(ToDockNode).ToArray() ?? []);

            case DocumentDock doc:
                return new DocNode(doc.Proportion, doc.ActiveDockable?.Id, LeafIds(doc));

            case ToolDock tool:
                return new ToolNode(tool.Proportion, tool.ActiveDockable?.Id, LeafIds(tool));

            case ProportionalDock prop:
                return new PropNode(
                    prop.Orientation == Orientation.Vertical ? "V" : "H",
                    prop.Proportion,
                    prop.ActiveDockable?.Id,
                    prop.VisibleDockables?.Select(ToDockNode).ToArray() ?? []);

            case ProportionalDockSplitter:
                return new SplitterNode();

            default:
                return new SplitterNode();
        }
    }

    private static string[] LeafIds(IDock dock) =>
        dock.VisibleDockables?.Where(d => d.Id is not null).Select(d => d.Id!).ToArray() ?? [];

    internal IRootDock? RebuildLayout(DockNode root, Dictionary<string, IDockable?> all)
    {
        DocumentContainer = null;
        ToolContainer     = null;
        ErrorsContainer   = null;
        var result = RebuildNode(root, all);
        return result as IRootDock;
    }

    private IDockable RebuildNode(DockNode node, Dictionary<string, IDockable?> all)
    {
        switch (node)
        {
            case RootNode rn:
            {
                var rd = CreateRootDock();
                rd.Id = "Root";
                rd.VisibleDockables = CreateList<IDockable>(rn.Children.Select(c => RebuildNode(c, all)).ToArray());
                rd.ActiveDockable   = FindById(rd.VisibleDockables, rn.ActiveId);
                rd.DefaultDockable  = rd.ActiveDockable ?? rd.VisibleDockables?.FirstOrDefault();
                return rd;
            }

            case PropNode pn:
            {
                var pd = new ProportionalDock
                {
                    Proportion  = pn.Prop,
                    Orientation = pn.Orient == "V" ? Orientation.Vertical : Orientation.Horizontal,
                };
                pd.VisibleDockables = CreateList<IDockable>(pn.Children.Select(c => RebuildNode(c, all)).ToArray());
                pd.ActiveDockable   = FindById(pd.VisibleDockables, pn.ActiveId);
                return pd;
            }

            case DocNode dn:
            {
                var dd = new DocumentDock { Proportion = dn.Prop, CanCreateDocument = false };
                dd.VisibleDockables = CreateList<IDockable>(ResolveIds(dn.Ids, all));
                dd.ActiveDockable   = FindById(dd.VisibleDockables, dn.ActiveId) ?? dd.VisibleDockables?.FirstOrDefault();
                DocumentContainer ??= dd;
                return dd;
            }

            case ToolNode tn:
            {
                var td = new ToolDock { Proportion = tn.Prop };
                td.VisibleDockables = CreateList<IDockable>(ResolveIds(tn.Ids, all));
                td.ActiveDockable   = FindById(td.VisibleDockables, tn.ActiveId) ?? td.VisibleDockables?.FirstOrDefault();
                if (tn.Ids.Contains("Errors"))
                    ErrorsContainer ??= td;
                else
                    ToolContainer ??= td;
                return td;
            }

            case SplitterNode:
                return new ProportionalDockSplitter();

            default:
                return new ProportionalDockSplitter();
        }
    }

    private static IDockable[] ResolveIds(string[] ids, Dictionary<string, IDockable?> all) =>
        ids.Select(id => all.TryGetValue(id, out var p) ? p : null)
           .Where(p => p is not null).Cast<IDockable>().ToArray();

    private static IDockable? FindById(IList<IDockable>? list, string? id) =>
        id is null ? null : list?.FirstOrDefault(d => d.Id == id);

    // ── Layout creation ───────────────────────────────────────────────────────

    public override IRootDock CreateLayout()
    {
        CodeEditor = new CodeEditorViewModel(_mainVm) { Id = "CodeEditor", Title = "Codice" };
        DataEditor = new DataEditorViewModel(_mainVm) { Id = "DataEditor", Title = "Dati" };
        Registers  = new RegistersViewModel  { Id = "Registers",  Title = "Registri" };
        Stack      = new StackViewModel      { Id = "Stack",      Title = "Stack" };
        Memory     = new MemoryViewModel     { Id = "Memory",     Title = "Memoria" };
        Errors     = new ErrorsViewModel(_mainVm) { Id = "Errors", Title = "Errori" };

        var documentDock = new DocumentDock
        {
            Id = "Documents",
            Title = "Documenti",
            CanCreateDocument = false,
            Proportion = 0.7,
            ActiveDockable = CodeEditor,
            VisibleDockables = CreateList<IDockable>(CodeEditor, DataEditor),
        };

        var errorsDock = new ToolDock
        {
            Id = "ErrorsDock",
            Title = "Errori",
            Proportion = 0.3,
            ActiveDockable = Errors,
            VisibleDockables = CreateList<IDockable>(Errors),
        };

        var documentsColumn = new ProportionalDock
        {
            Id = "DocumentsColumn",
            Orientation = Orientation.Vertical,
            Proportion = 0.65,
            ActiveDockable = documentDock,
            VisibleDockables = CreateList<IDockable>(
                documentDock,
                new ProportionalDockSplitter(),
                errorsDock),
        };

        var toolDock = new ToolDock
        {
            Id = "Tools",
            Title = "Strumenti",
            Proportion = 0.35,
            ActiveDockable = Registers,
            VisibleDockables = CreateList<IDockable>(Registers, Stack, Memory),
        };

        DocumentContainer = documentDock;
        ToolContainer     = toolDock;
        ErrorsContainer   = errorsDock;

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            ActiveDockable = documentsColumn,
            VisibleDockables = CreateList<IDockable>(
                documentsColumn,
                new ProportionalDockSplitter(),
                toolDock),
        };

        var rootDock = CreateRootDock();
        rootDock.Id = "Root";
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return rootDock;
    }
}
