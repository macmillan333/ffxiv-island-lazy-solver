class Item
{
    public enum Type
    {
        Material,
        RareMaterial,
        Produce,
        Leavings
    }
    public string? name;
    public Type type;
}

class Handicraft
{
    public enum Category
    {
        None,
        PreservedFood,
        Attire,
        Foodstuffs,
        Confections,
        Sundries,
        Furnishings,
        Arms,
        Concoctions,
        Ingredients,
        Accessories,
        Metalworks,
        Woodworks,
        Textiles,
        CreatureCreations,
        MarineMerchandise,
        UnburiedTreasures
    }
    public string? name;
    public int time;
    public int value;
    public Category category1;
    public Category category2;
    public class Ingredient
    {
        public Item? item;
        public int quantity;
    }
    public List<Ingredient>? ingredients;
}

class ExpeditionArea
{
    public string? terrain;
    public List<string>? resources;
    public string? rareResources;
}

class Program
{
    static void Main(string[] args)
    {
    }
}