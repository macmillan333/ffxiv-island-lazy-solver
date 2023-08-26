using NReco;
using NReco.Csv;

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
        const string kItemsFilename = "../../../Items.csv";

        Dictionary<string, Item> items = new Dictionary<string, Item>();
        using (var streamReader = new StreamReader(kItemsFilename))
        {
            CsvReader reader = new CsvReader(streamReader, ",");
            reader.Read(); // header
            while (reader.Read())
            {
                string name = reader[0];
                Item.Type type = Enum.Parse<Item.Type>(reader[1]);
                items.Add(name, new Item()
                {
                    name = name, type = type
                });
            }
        }
        foreach (Item i in items.Values)
        {
            Console.WriteLine($"{i.name} is a {i.type}");
        }
    }
}