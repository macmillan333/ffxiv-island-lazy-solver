using NReco;
using NReco.Csv;
using System;
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

    public static int TotalValue(List<Handicraft> content)
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

    public int TotalValue()
    {
        return TotalValue(content);
    }

    public void FindBestPermutation()
    {
        List<Handicraft> bestPermutation = new List<Handicraft>();
        foreach (Handicraft h in content)
        {
            bestPermutation.Add(h);
        }
        int bestValue = TotalValue();
        bool foundBetterPermutation = false;

        List<Handicraft> permutation = new List<Handicraft>();
        HashSet<int> addedIndices = new HashSet<int>();
        Action<int>? addFromContent = null;
        addFromContent = (int index) =>
        {
            permutation.Add(content[index]);
            addedIndices.Add(index);
            if (permutation.Count == content.Count)
            {
                int value = TotalValue(permutation);
                if (value > bestValue)
                {
                    bestPermutation.Clear();
                    foreach (Handicraft h in permutation)
                    {
                        bestPermutation.Add(h);
                    }
                    bestValue = value;
                    foundBetterPermutation = true;
                }
            }
            else
            {
                for (int i = 0; i < content.Count; i++)
                {
                    if (addedIndices.Contains(i)) continue;
                    addFromContent!(i);
                }
            }
            permutation.RemoveAt(permutation.Count - 1);
            addedIndices.Remove(index);
        };
        for (int i = 0; i < content.Count; i++)
        {
            addFromContent(i);
        }

        if (foundBetterPermutation)
        {
            content.Clear();
            foreach (Handicraft h in bestPermutation)
            {
                content.Add(h);
            }
        }
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

    // Day plans with the same content but different order
    // should have the same hash.
    public override int GetHashCode()
    {
        List<int> nameHashes = new List<int>();
        foreach (Handicraft h in content)
        {
            nameHashes.Add(h.name.GetHashCode());
        }
        nameHashes.Sort();
        StringBuilder builder = new StringBuilder();
        foreach (int hash in nameHashes)
        {
            builder.Append(hash);
        }
        return builder.ToString().GetHashCode();
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

    public int RankingScore()
    {
        return TotalValue() - 200 * NumUniqueDays();
    }

    public Dictionary<Item, int> TotalMaterials()
    {
        Dictionary<Item, int> materials = new Dictionary<Item, int>();
        foreach (DayPlan day in content)
        {
            foreach (Handicraft craft in day.content)
            {
                foreach (Handicraft.Ingredient i in craft.ingredients)
                {
                    materials.TryAdd(i.item, 0);
                    materials[i.item] += i.quantity;
                }
            }
        }
        return materials;
    }

    public bool CanAdd(Handicraft craft, int maxDays)
    {
        if (content.Count > maxDays) return false;
        if (content[^1].CanAdd(craft)) return true;
        // Adding this craft will cause a new day
        return content.Count < maxDays;
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

    public int NumUniqueDays()
    {
        HashSet<int> dayHashes = new HashSet<int>();
        foreach (DayPlan day in content)
        {
            dayHashes.Add(day.GetHashCode());
        }
        return dayHashes.Count;
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
    const int kNumWorkshops = 4;
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
    public const int kMaxDaysInWeekPlan = kNumWorkshops * 5;

    // Algorithm parameters
    const int kNoBetterPlanThreshold = 100000;
    const int kAttemptsToAddItemToPlan = 100;

    // Final result
    WeekPlan? globalBestPlan = null;
    int globalBestScore = 0;
    ExpeditionArea? bestPlanArea1 = null;
    ExpeditionArea? bestPlanArea2 = null;

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
        Console.WriteLine("Inventory:");
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

        WeekPlan? bestPlan = null;
        int bestScore = 0;
        int noBetterPlanCounter = 0;
        while (noBetterPlanCounter < kNoBetterPlanThreshold)
        {
            Tuple<WeekPlan, int> candidate = GenerateRandomSolution(
                inventory.Clone(), craftables);
            if (candidate.Item2 > bestScore)
            {
                bestPlan = candidate.Item1;
                bestScore = candidate.Item2;
                Console.WriteLine($"Found plan with score {bestScore} after {noBetterPlanCounter} attempts.");
                noBetterPlanCounter = 0;
            }
            else
            {
                noBetterPlanCounter++;
            }
        }
        if (bestScore > globalBestScore)
        {
            globalBestPlan = bestPlan;
            globalBestScore = bestScore;
            bestPlanArea1 = area1;
            bestPlanArea2 = area2;
        }
    }

    private Tuple<WeekPlan, int> GenerateRandomSolution(
        Inventory inventory, List<Handicraft> craftables)
    {
        WeekPlan plan = new WeekPlan();
        while (true)
        {
            bool addedAnything = false;
            for (int i = 0;
                i < kAttemptsToAddItemToPlan;
                i++)
            {
                Handicraft handicraft = craftables[
                    random.Next(0, craftables.Count)];
                if (inventory.CanCraft(handicraft) &&
                    plan.CanAdd(handicraft, kMaxDaysInWeekPlan))
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
        foreach (DayPlan day in plan.content)
        {
            day.FindBestPermutation();
        }
        // Console.WriteLine("Generated plan:");
        // Console.WriteLine(plan);
        return new Tuple<WeekPlan, int>(plan, plan.RankingScore());
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

        Console.WriteLine($"Areas for global best plan: {bestPlanArea1!.terrain}, {bestPlanArea2!.terrain}");
        Console.WriteLine("Plan content:");
        Console.WriteLine(globalBestPlan);
        Console.WriteLine("Value: " + globalBestPlan!.TotalValue());
        Console.WriteLine("Score: " + globalBestScore);
        Inventory inventory = new Inventory(bestPlanArea1, bestPlanArea2);
        Console.WriteLine("Inventory:");
        Console.WriteLine(inventory);
        Console.WriteLine("Total materials needed:");
        Dictionary<Item, int> materials = globalBestPlan!.TotalMaterials();
        foreach (KeyValuePair<Item, int> pair in materials)
        {
            Console.WriteLine(pair.Key.name + " " + pair.Value);
        }
    }

    static void Main(string[] args)
    {
        new Program().InstanceMain();
    }
}