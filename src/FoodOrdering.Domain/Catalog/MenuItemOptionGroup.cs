namespace FoodOrdering.Domain.Catalog;

public sealed class MenuItemOptionGroup
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool AllowMultiple { get; set; }

    public int SortOrder { get; set; }

    public ICollection<MenuItemOption> Options { get; set; } = new List<MenuItemOption>();
}
