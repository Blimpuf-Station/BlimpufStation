using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using System.Linq;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed partial class CrewManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    // Blimpuf start - ensure columns remain aligned in manifest regardless of name length
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var sections = Children.OfType<CrewManifestSection>();
        var nameWidth = 0f;
        foreach (var section in sections)
        {
            foreach (var name in section.NamesColumn.Children)
            {
                name.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
                nameWidth = Math.Max(nameWidth, name.DesiredSize.X);
            }
        }

        foreach (var section in sections)
        {
            if (section.NamesColumn.MinWidth != nameWidth)
                section.NamesColumn.MinWidth = nameWidth;
        }

        return base.MeasureOverride(availableSize);
    }
    // Blimpuf end

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        foreach (var entry in entries.Entries)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        foreach (var item in entryList)
        {
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries));
        }
    }
}
