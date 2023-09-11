using NReco;
using NReco.Csv;

class Item
{
    public enum Type
    {
        Material,
        RareMaterial,
        GardeningStarter,
        Produce,
        Leavings
    }
    public string name;
    public Type type;
    public Item(string name, Type type)
    {
        this.name = name;
        this.type = type;
    }
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
    public string name;
    public int time;
    public int value;
    public Category category1;
    public Category category2;
    public class Ingredient
    {
        public Item item;
        public int quantity;

        public Ingredient(Item item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }
    public List<Ingredient> ingredients;

    public Handicraft(string name)
    {
        this.name = name;
        this.ingredients = new List<Ingredient>();
    }
}

class ExpeditionArea
{
    public string terrain;
    public List<Item> resources;
    public Item rareResources;
    public int expectedQuantityPerMaterialPerWeek;

    public ExpeditionArea(string terrain, Item rareResources)
    {
        this.terrain = terrain;
        this.resources = new List<Item>();
        this.rareResources = rareResources;
    }
}

class Program
{
    // File locations
    const string kItemsFilename = "../../../Items.csv";
    const string kGranaryFilename = "../../../Granary.csv";
    const string kHandicraftFilename = "../../../Handicraft.csv";

    // Parameters
    const int kGranaryLevel = 4;
    const int kNumLandmarks = 4;
    const int kMaterialsBaseValue = 9;  // 64.34% to be 9+, 46.84% to be 10+

    Dictionary<string, Item> items = new Dictionary<string, Item>();
    List<ExpeditionArea> areas = new List<ExpeditionArea>();
    List<Handicraft> handicrafts = new List<Handicraft>();

    private void LoadData()
    {
        using (var streamReader = new StreamReader(kItemsFilename))
        {
            CsvReader reader = new CsvReader(streamReader, ",");
            reader.Read();  // header
            while (reader.Read())
            {
                string name = reader[0];
                Item.Type type = Enum.Parse<Item.Type>(reader[1]);
                items.Add(name, new Item(name, type));
            }
        }

        using (var streamReader = new
            StreamReader(kGranaryFilename))
        {
            CsvReader reader = new CsvReader(streamReader, ",");
            reader.Read();  // header
            while (reader.Read())
            {
                ExpeditionArea area = new ExpeditionArea(
                    terrain: reader[0],
                    rareResources: items[reader[1]]);
                for (int i = 2; i < reader.FieldsCount; i++)
                {
                    if (reader[i] == "") break;
                    area.resources.Add(items[reader[i]]);
                }
                areas.Add(area);
            }
        }

        using (var streamReader = new
            StreamReader(kHandicraftFilename))
        {
            CsvReader reader = new CsvReader(streamReader, ",");
            reader.Read();  // header
            while (reader.Read())
            {
                Handicraft craft = new Handicraft(reader[0]);
                craft.time = int.Parse(reader[1]);
                craft.value = int.Parse(reader[2]);
                craft.category1 = Enum.Parse<Handicraft.Category>(
                    reader[3]);
                craft.category2 = Handicraft.Category.None;
                if (reader[4] != "")
                {
                    craft.category2 = Enum.Parse<Handicraft.Category>(
                        reader[4]);
                }
                for (int i = 5; i < reader.FieldsCount; i += 2)
                {
                    if (reader[i] == "") break;
                    craft.ingredients.Add(new Handicraft.Ingredient(
                        item: items[reader[i]],
                        quantity: int.Parse(reader[i + 1])));
                }
                handicrafts.Add(craft);
            }
        }

        const int kMaterialsPerGranaryPerWeek = (kMaterialsBaseValue + kNumLandmarks) * 2 * 7;
        foreach (ExpeditionArea a in areas)
        {
            a.expectedQuantityPerMaterialPerWeek = kMaterialsPerGranaryPerWeek / a.resources.Count;
        }
    }

    private void SolveForInventory(Dictionary<Item, int> inventory)
    {
        // Find the craftable things
        List<Handicraft> craftables = new List<Handicraft>();
        foreach (Handicraft c in handicrafts)
        {
            bool craftable = true;
            foreach (Handicraft.Ingredient i in c.ingredients)
            {
                switch (i.item.type)
                {
                    case Item.Type.GardeningStarter:
                    case Item.Type.Leavings:
                    case Item.Type.Produce:
                        // For simplicity we assume we have infinite of those
                        break;
                    case Item.Type.RareMaterial:
                    case Item.Type.Material:
                        if (!inventory.ContainsKey(i.item) ||
                            inventory[i.item] < i.quantity)
                        {
                            craftable = false;
                            break;
                        }
                        break;
                }
            }
            if (craftable)
            {
                craftables.Add(c);
            }
        }

        Console.WriteLine($"{craftables.Count} out of {handicrafts.Count} are craftable.");
    }

    private void InstanceMain()
    {
        LoadData();

        // Start picking granary expeditions
        const int kRareMaterialsPerGranaryPerWeek = (kGranaryLevel + 1) * 7;
        for (int choice1 = 0; choice1 < areas.Count; choice1++)
        {
            for (int choice2 = choice1; choice2 < areas.Count; choice2++)
            {
                ExpeditionArea area1 = areas[choice1];
                ExpeditionArea area2 = areas[choice2];

                // Build inventory
                Dictionary<Item, int> inventory = new Dictionary<Item, int>();
                inventory.TryAdd(area1.rareResources, 0);
                inventory[area1.rareResources] += kRareMaterialsPerGranaryPerWeek;
                inventory.TryAdd(area2.rareResources, 0);
                inventory[area2.rareResources] += kRareMaterialsPerGranaryPerWeek;
                foreach (Item i in area1.resources)
                {
                    inventory.TryAdd(i, 0);
                    inventory[i] += area1.expectedQuantityPerMaterialPerWeek;
                }
                foreach (Item i in area2.resources)
                {
                    inventory.TryAdd(i, 0);
                    inventory[i] += area2.expectedQuantityPerMaterialPerWeek;
                }

                SolveForInventory(inventory);
            }
        }
    }

    static void Main(string[] args)
    {
        new Program().InstanceMain();
    }
}