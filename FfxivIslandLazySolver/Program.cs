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
        ProduceFromStarter,
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

    public static Dictionary<Item, Item> gardeningStarterToProduce
        = new Dictionary<Item, Item>();
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
            Item itemToAdd = i;
            int quantity = area1.expectedQuantityPerMaterialPerWeek;
            if (i.type == Item.Type.GardeningStarter)
            {
                itemToAdd = Item.gardeningStarterToProduce[i];
                quantity *= 5;
            }
            content.TryAdd(itemToAdd, 0);
            content[itemToAdd] += quantity;
        }
        foreach (Item i in area2.resources)
        {
            Item itemToAdd = i;
            int quantity = area2.expectedQuantityPerMaterialPerWeek;
            if (i.type == Item.Type.GardeningStarter)
            {
                itemToAdd = Item.gardeningStarterToProduce[i];
                quantity *= 5;
            }
            content.TryAdd(itemToAdd, 0);
            content[itemToAdd] += quantity;
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
                case Item.Type.ProduceFromStarter:
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

    public bool CanCraft(DayPlan dayPlan)
    {
        Inventory clone = Clone();
        foreach (Handicraft c in dayPlan.content)
        {
            if (!clone.CanCraft(c)) return false;
            clone.Craft(c);
        }
        return true;
    }

    public bool CanCraft(WeekPlan weekPlan)
    {
        Inventory clone = Clone();
        foreach (DayPlan day in weekPlan.content)
        {
            foreach (Handicraft c in day.content)
            {
                if (!clone.CanCraft(c)) return false;
                clone.Craft(c);
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
                case Item.Type.ProduceFromStarter:
                case Item.Type.RareMaterial:
                case Item.Type.Material:
                    content[i.item] -= i.quantity;
                    break;
            }
        }
    }

    public void Craft(DayPlan dayPlan)
    {
        foreach (Handicraft c in dayPlan.content)
        {
            Craft(c);
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
                case Item.Type.ProduceFromStarter:
                case Item.Type.RareMaterial:
                case Item.Type.Material:
                    content[i.item] += i.quantity;
                    break;
            }
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<Item, int> pair in content)
        {
            builder.AppendLine(pair.Key.name + " " + pair.Value);
        }
        return builder.ToString();
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

    public bool CanChange(int index, Handicraft newCraft)
    {
        return totalTime - content[index].time + newCraft.time <= 24;
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

    public DayPlan Clone()
    {
        DayPlan clone = new DayPlan();
        foreach (Handicraft handicraft in content)
        {
            clone.Add(handicraft);
        }
        clone.totalTime = totalTime;
        return clone;
    }
}

class WeekPlan
{
    public List<DayPlan> content;
    
    public WeekPlan()
    {
        content = new List<DayPlan>();
        content.Add(new DayPlan());
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

    public WeekPlan Clone()
    {
        WeekPlan clone = new WeekPlan();
        clone.content.Clear();
        foreach (DayPlan day in content)
        {
            clone.content.Add(day.Clone());
        }
        return clone;
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
    const int kInitialGenerationSize = 10000;
    const int kNumGenerations = 100;
    const int kAttemptsToAddHandicraftToInitialGen = 100;
    const int kNumTopPlansChosenToReproduce = 900;
    const int kNumOtherPlansChosenToReproduce = 100;
    const int kParentPairs = 1000;
    const int kOffspringPerParentPair = 10;
    const int kMaxMutationAttempts = 20;

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
        foreach (Item i in items.Values)
        {
            if (i.type == Item.Type.GardeningStarter)
            {
                string produceName = i.name.Substring(
                    0, i.name.LastIndexOf(' '));
                Item.gardeningStarterToProduce.Add(
                    i, items[produceName]);
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
        Console.WriteLine(inventory);

        // Find the craftable things
        List<Handicraft> craftables = new List<Handicraft>();
        foreach (Handicraft c in handicrafts)
        {
            if (inventory.CanCraft(c))
            {
                craftables.Add(c);
            }
        }

        // Generate initial gen
        List<Tuple<WeekPlan, int>> generation =
            new List<Tuple<WeekPlan, int>>();
        for (int i = 0; i < kInitialGenerationSize; i++)
        {
            generation.Add(GenerateRandomSolution(
                inventory.Clone(), craftables));
        }

        for (int gen = 0; gen < kNumGenerations; gen++)
        {
            Tuple<WeekPlan, int> bestPlanThisGen =
                new Tuple<WeekPlan, int>(new WeekPlan(), 0);

            // Sort the current generation
            generation.Sort((Tuple<WeekPlan, int> t1,
                Tuple<WeekPlan, int> t2) =>
            {
                return t1.Item2 - t2.Item2;
            });

            // Pick the plans to reproduce
            List<Tuple<WeekPlan, int>> parents = new List<Tuple<WeekPlan, int>>();
            for (int i = 0; i < kNumTopPlansChosenToReproduce; i++)
            {
                parents.Add(CloneTuple(generation[i]));
            }
            for (int i = 0; i < kNumOtherPlansChosenToReproduce; i++)
            {
                int index = random.Next(
                    kNumTopPlansChosenToReproduce, generation.Count);
                parents.Add(CloneTuple(generation[index]));
            }

            // Reproduce and mutate
            generation.Clear();
            for (int i = 0; i < kParentPairs; i++)
            {
                WeekPlan parent1 = parents[random.Next(parents.Count)].Item1;
                WeekPlan parent2 = parents[random.Next(parents.Count)].Item1;
                for (int j = 0; j < kOffspringPerParentPair; j++)
                {
                    Tuple<WeekPlan, int> offspring = ReproduceAndMutate(
                        parent1, parent2, inventory.Clone(),
                        craftables);
                    if (offspring.Item2 > bestPlanThisGen.Item2)
                    {
                        bestPlanThisGen = offspring;
                    }
                    generation.Add(offspring);
                }
            }

            Console.WriteLine("Best plan of this generation: " + bestPlanThisGen.Item2);
            Console.WriteLine(bestPlanThisGen.Item1);
        }
    }

    private static Tuple<WeekPlan, int> CloneTuple(
        Tuple<WeekPlan, int> tuple)
    {
        return new Tuple<WeekPlan, int>(
            tuple.Item1.Clone(), tuple.Item2);
    }

    private Tuple<WeekPlan, int> GenerateRandomSolution(
        Inventory inventory, List<Handicraft> craftables)
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
        // Console.WriteLine("Generated plan:");
        // Console.WriteLine(plan);
        return new Tuple<WeekPlan, int>(plan, plan.TotalValue());
    }

    private Tuple<WeekPlan, int> ReproduceAndMutate(WeekPlan p1,
        WeekPlan p2, Inventory inventory, List<Handicraft> craftables)
    {
        WeekPlan offspring = new WeekPlan();
        offspring.content.Clear();

        // 1. Randomly take days from parents, disregarding inventory
        List<DayPlan> daysFromParents = new List<DayPlan>();
        int largerCount = Math.Max(p1.content.Count, p2.content.Count);
        for (int i = 0; i < largerCount; i++)
        {
            WeekPlan parent = random.Next(2) == 0 ? p1 : p2;
            if (parent.content.Count > i)
            {
                daysFromParents.Add(parent.content[i].Clone());
            }
        }
        Shuffle(daysFromParents);

        // 2. Add days to offspring
        foreach (DayPlan day in daysFromParents)
        {
            if (inventory.CanCraft(day))
            {
                offspring.content.Add(day);
                inventory.Craft(day);
            }
        }

        // 3. Mutate
        for (int i = 0; i < kMaxMutationAttempts; i++)
        {
            int dayIndex = random.Next(offspring.content.Count);
            DayPlan day = offspring.content[dayIndex];

            int itemIndex = random.Next(day.content.Count);
            Handicraft oldCraft = day.content[itemIndex];
            Handicraft newCraft = craftables[
                random.Next(craftables.Count)];

            if (!day.CanChange(itemIndex, newCraft)) continue;
            inventory.Uncraft(oldCraft);
            if (!inventory.CanCraft(newCraft))
            {
                inventory.Craft(oldCraft);
                continue;
            }
            else
            {
                inventory.Craft(newCraft);
                day.content[itemIndex] = newCraft;
            }
        }
        
        return new Tuple<WeekPlan, int>(
            offspring, offspring.TotalValue());
    }

    // https://stackoverflow.com/questions/273313/randomize-a-listt
    public void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
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