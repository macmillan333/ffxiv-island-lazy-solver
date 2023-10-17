using NReco;
using NReco.Csv;
using System.Text;

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

    public override string ToString()
    {
        return name;
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
        totalLeavingNeeded = 0;
        totalProduceNeeded = 0;
    }

    public int totalLeavingNeeded;
    public int totalProduceNeeded;

    public bool HasEfficiencyBonusAfter(Handicraft? prev)
    {
        if (prev == null) return false;
        if (name == prev.name) return false;

        // Is there a common category with the previous item?
        // Note that category2 may be None
        if (prev.category1 == this.category1 ||
            prev.category1 == this.category2 ||
            prev.category2 == this.category1)
        {
            return true;
        }
        if (prev.category2 == this.category2 &&
            prev.category2 != Category.None)
        {
            return true;
        }

        return false;
    }
}

class ExpeditionArea
{
    public string terrain;
    public List<Item> resources;
    public Item rareResources;
    public int quantityPerRareMaterialPerWeek;
    public int expectedQuantityPerMaterialPerWeek;

    public ExpeditionArea(string terrain, Item rareResources)
    {
        this.terrain = terrain;
        this.resources = new List<Item>();
        this.rareResources = rareResources;
    }
}

class Inventory
{
    private Dictionary<Item, int> content = new Dictionary<Item, int>();
    private int leavingQuantity;
    private int produceQuantity;

    public Inventory(ExpeditionArea area1, ExpeditionArea area2)
    {
        content.TryAdd(area1.rareResources, 0);
        content[area1.rareResources] += area1.quantityPerRareMaterialPerWeek;
        content.TryAdd(area2.rareResources, 0);
        content[area2.rareResources] += area2.quantityPerRareMaterialPerWeek;
        foreach (Item i in area1.resources)
        {
            content.TryAdd(i, 0);
            content[i] += area1.expectedQuantityPerMaterialPerWeek;
        }
        foreach (Item i in area2.resources)
        {
            content.TryAdd(i, 0);
            content[i] += area2.expectedQuantityPerMaterialPerWeek;
        }

        leavingQuantity = Program.kTotalLeavingPerWeek;
        produceQuantity = Program.kProduceAvailableForCraftPerWeek;
    }

    private Inventory() { }

    public Inventory Clone()
    {
        Inventory clone = new Inventory()
        {
            leavingQuantity = leavingQuantity,
            produceQuantity = produceQuantity
        };
        foreach (KeyValuePair<Item, int> pair in content)
        {
            clone.content.Add(pair.Key, pair.Value);
        }
        return clone;
    }

    public bool CanCraft(Handicraft craft)
    {
        if (leavingQuantity < craft.totalLeavingNeeded) return false;
        if (produceQuantity < craft.totalProduceNeeded) return false;
        foreach (Handicraft.Ingredient i in craft.ingredients)
        {
            switch (i.item.type)
            {
                case Item.Type.GardeningStarter:
                    // Don't care
                case Item.Type.Leavings:
                case Item.Type.Produce:
                    // Already checked
                    break;
                case Item.Type.RareMaterial:
                case Item.Type.Material:
                    if (!content.ContainsKey(i.item) ||
                        content[i.item] < i.quantity)
                    {
                        return false;
                    }
                    break;
            }
        }
        return true;
    }

    public void Craft(Handicraft craft)
    {
        foreach (Handicraft.Ingredient i in craft.ingredients)
        {
            switch (i.item.type)
            {
                case Item.Type.GardeningStarter:
                    // Don't care
                    break;
                case Item.Type.Leavings:
                    leavingQuantity -= i.quantity;
                    break;
                case Item.Type.Produce:
                    produceQuantity -= i.quantity;
                    break;
                case Item.Type.RareMaterial:
                case Item.Type.Material:
                    content[i.item] -= i.quantity;
                    break;
            }
        }
    }

    public void Uncraft(Handicraft craft)
    {
        foreach (Handicraft.Ingredient i in craft.ingredients)
        {
            switch (i.item.type)
            {
                case Item.Type.GardeningStarter:
                    // Don't care
                    break;
                case Item.Type.Leavings:
                    leavingQuantity += i.quantity;
                    break;
                case Item.Type.Produce:
                    produceQuantity += i.quantity;
                    break;
                case Item.Type.RareMaterial:
                case Item.Type.Material:
                    content[i.item] += i.quantity;
                    break;
            }
        }
    }
}

class DayPlan
{
    public List<Handicraft> content;
    public int totalTime;

    public DayPlan()
    {
        content = new List<Handicraft>();
        totalTime = 0;
    }

    public bool CanAdd(Handicraft craft)
    {
        return totalTime + craft.time <= 24;
    }
    public void Add(Handicraft craft)
    {
        content.Add(craft);
        totalTime += craft.time;
    }
    public Handicraft? Last()
    {
        if (content.Count == 0) return null;
        return content[^1];
    }
    public void RemoveLast()
    {
        Handicraft last = content[content.Count - 1];
        totalTime -= last.time;
        content.RemoveAt(content.Count - 1);
    }

    public int TotalValue()
    {
        if (content.Count == 0) return 0;
        int value = content[0].value;
        for (int i = 1; i < content.Count; i++)
        {
            int multiplier = 1;
            if (content[i].HasEfficiencyBonusAfter(content[i - 1]))
            {
                multiplier = 2;
            }

            value += multiplier * content[i].value;
        }
        return value;
    }
}

class WeekPlan
{
    List<DayPlan> content;
    
    public WeekPlan()
    {
        content = new List<DayPlan>();
        NewDay();
    }

    public DayPlan currentDay => content[^1];
    public void NewDay()
    {
        content.Add(new DayPlan());
    }
    public void RemoveDay()
    {
        content.RemoveAt(content.Count - 1);
    }
    public int TotalValue()
    {
        int value = 0;
        foreach (DayPlan day in content) { value += day.TotalValue(); }
        return value;
    }

    public void Add(Handicraft craft)
    {
        if (!content[^1].CanAdd(craft))
        {
            content.Add(new DayPlan());
        }
        content[^1].Add(craft);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < content.Count; i++)
        {
            sb.Append($"Day {i} ({content[i].TotalValue()}): ");
            foreach (Handicraft c in content[i].content)
            {
                sb.Append(c.name + ", ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
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
    const int kNumLandmarks = 5;
    const int kMaterialsBaseValue = 9;  // 64.34% to be 9+, 46.84% to be 10+
    const int kCroplandPlots = 20;
    const int kPastureSlots = 20;

    // Derived parameters on cropland and pasture
    const int kCroplandTotalYieldPerWeek = kCroplandPlots
        * 7 / 2  // 1 harvest every 2 days
        * 5;  // 5 yield per harvest
    const int kProduceUsedForFeedPerWeek = kPastureSlots
        * 7  // 1 feed per day
        * 2 / 3  // 3 crops craft into 2 feeds
        + 2;  // compensation for integer division
    public const int kProduceAvailableForCraftPerWeek = 
        kCroplandTotalYieldPerWeek - kProduceUsedForFeedPerWeek;
    public const int kTotalLeavingPerWeek = kPastureSlots
        * 7  // 1 drop chance per day
        * 3 / 2;  // 1 normal + 50% chance bonus drop

    // Algorithm parameters
    const int kGenerationSize = 10;
    const int kAttemptsToAddHandicraftToInitialGen = 100;

    Dictionary<string, Item> items = new Dictionary<string, Item>();
    List<ExpeditionArea> areas = new List<ExpeditionArea>();
    List<Handicraft> handicrafts = new List<Handicraft>();
    Random random = new Random();

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
                    Item item = items[reader[i]];
                    int quantity = int.Parse(reader[i + 1]);
                    craft.ingredients.Add(new Handicraft.Ingredient(
                        item: item,
                        quantity: quantity));
                    if (item.type == Item.Type.Leavings)
                    {
                        craft.totalLeavingNeeded += quantity;
                    }
                    if (item.type == Item.Type.Produce)
                    {
                        craft.totalProduceNeeded += quantity;
                    }
                }
                handicrafts.Add(craft);
            }
        }

        const int kRareMaterialsPerGranaryPerWeek = (kGranaryLevel + 1) * 7;
        const int kMaterialsPerGranaryPerWeek = (kMaterialsBaseValue + kNumLandmarks) * 2 * 7;
        foreach (ExpeditionArea a in areas)
        {
            a.quantityPerRareMaterialPerWeek = kRareMaterialsPerGranaryPerWeek;
            a.expectedQuantityPerMaterialPerWeek = kMaterialsPerGranaryPerWeek / a.resources.Count;
        }
    }

    private void SolveFor(ExpeditionArea area1, ExpeditionArea area2)
    {
        Console.WriteLine($"Solving for: {area1.terrain} and {area2.terrain}");

        // Build inventory
        Inventory inventory = new Inventory(area1, area2);

        // Find the craftable things
        List<Handicraft> craftables = new List<Handicraft>();
        foreach (Handicraft c in handicrafts)
        {
            if (inventory.CanCraft(c))
            {
                craftables.Add(c);
            }
        }

        // Generate generation 0
        List<WeekPlan> generation = new List<WeekPlan>();
        for (int i = 0; i < kGenerationSize; i++)
        {
            generation.Add(GenerateRandomSolution(
                inventory.Clone(), craftables));
        }
    }

    private WeekPlan GenerateRandomSolution(Inventory inventory,
        List<Handicraft> craftables)
    {
        WeekPlan plan = new WeekPlan();
        while (true)
        {
            bool addedAnything = false;
            for (int i = 0;
                i < kAttemptsToAddHandicraftToInitialGen;
                i++)
            {
                Handicraft handicraft = craftables[
                    random.Next(0, craftables.Count)];
                if (inventory.CanCraft(handicraft))
                {
                    plan.Add(handicraft);
                    inventory.Craft(handicraft);
                    addedAnything = true;
                    break;
                }
            }
            if (!addedAnything)
            {
                break;
            }
        }
        Console.WriteLine($"Generated plan: {plan}");
        return plan;
    }

    private void InstanceMain()
    {
        LoadData();

        for (int choice1 = 0; choice1 < areas.Count; choice1++)
        {
            for (int choice2 = choice1; choice2 < areas.Count; choice2++)
            {
                SolveFor(areas[choice1], areas[choice2]);
            }
        }
    }

    static void Main(string[] args)
    {
        new Program().InstanceMain();
    }
}