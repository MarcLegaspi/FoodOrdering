namespace FoodOrdering.Domain.Catalog;

public sealed class MenuItemOption
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long MenuItemOptionGroupId { get; set; }

    public MenuItemOptionGroup OptionGroup { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal PriceDelta { get; set; }

    public int SortOrder { get; set; }
}
