using System;
using System.Collections.Generic;
using System.Linq;
using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Entities;
using UnityEngine;

namespace KitchenRefillContainers.GameTests
{
    public sealed class Initializer : IModInitializer
    {
        public void PreInject() { }
        public void PostInject() { }
        public void PostActivate(Mod mod)
        {
            if (!Environment.GetCommandLineArgs().Contains("-plateup-mod-tests")) return;
            Application.runInBackground = true;
            Application.onBeforeRender += Tests.Run;
        }
    }

    internal static partial class Tests
    {
        private static bool finished;
        private static string currentCase;
        private static readonly Dictionary<int, int> Supported = new Dictionary<int, int>
        {
            { -502245988, -622622812 }, { -319704939, -1190642520 }, { -425645565, 122760125 },
            { -1790201643, 723989167 }, { -1354941517, 933861469 }, { 1390528274, -329134625 },
            { 2019756794, -486398094 }, { 1286433124, -486398094 }, { -1341614392, -486398094 },
            { -1089548751, -486398094 }, { 98665743, -486398094 }, { -1681933989, -486398094 },
            { 65943925, -486398094 }, { 1690253467, -486398094 }, { -748726515, -486398094 },
            { -34663320, -2080052245 }, { 1815024071, 1626110274 }, { 1186100927, 1626110274 },
            { -1033480722, 1626110274 }
        };

        internal static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(currentCase + ": " + message);
        }

        internal static void Run()
        {
            if (finished || GameData.Main == null) return;
            finished = true;
            Application.onBeforeRender -= Run;
            try
            {
                Candidates();
                Restrictions();
                Conflict();
                Bootstrap();
                Debug.Log("PLATEUP_MOD_TESTS: PASS");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("PLATEUP_MOD_TESTS: FAIL " + error);
                Application.Quit(1);
            }
        }

        private static void Candidates()
        {
            var candidates = GameData.Main.Get<Item>().Where(i => i.IsSplittable || i.SplitCount > 0).OrderBy(i => i.name).ToArray();
            Check(Supported.Keys.All(id => candidates.Any(i => i.ID == id)), "An expected supported item is missing from this game build.");
            Debug.Log("PLATEUP_MOD_REPORT: | Item | ID | Portions | Recipe variants | Native cases | Result |");
            Debug.Log("PLATEUP_MOD_REPORT: |---|---:|---:|---:|---:|---|");
            foreach (var item in candidates)
            {
                currentCase = item.name + " (" + item.ID + ")";
                if (!Supported.TryGetValue(item.ID, out var carrier))
                {
                    int probes = RejectCandidate(item);
                    Debug.Log($"PLATEUP_MOD_REPORT: | {item.name} | {item.ID} | {item.SplitCount} | 0 | {probes} | Rejected: {Exclusion(item)} |");
                    continue;
                }
                var recipes = Recipe.Variants(item).ToArray();
                Check(recipes.Length > 0, "No complete native recipe variants were constructed.");
                int cases = 0;
                foreach (var recipe in recipes)
                    foreach (int count in new[] { item.SplitCount, 1 })
                        foreach (bool automated in new[] { false, true })
                            foreach (bool provider in new[] { false, true })
                            {
                                currentCase = $"{item.name}: variant {Array.IndexOf(recipes, recipe)}, {count} portions, combiner={automated}, provider={provider}";
                                Refill(recipe, carrier, count, automated, provider);
                                cases++;
                            }
                Debug.Log($"PLATEUP_MOD_REPORT: | {item.name} | {item.ID} | {item.SplitCount} | {recipes.Length} | {cases} | Passed |");
            }
            Debug.Log($"PLATEUP_MOD_SUMMARY: {candidates.Length} candidates, {Supported.Count} supported item types");
        }

        private static void Refill(Recipe recipe, int carrierId, int count, bool automated, bool provider)
        {
            using (var test = new TestWorld())
            {
                var carrier = provider ? test.TakeCarrier(carrierId) : test.Deplete(recipe);
                Check(test.Em.GetComponentData<CItem>(carrier).ID == carrierId, "Wrong carrier after provider/depletion.");
                var food = recipe.Create(test.Ctx);
                var original = test.Em.GetComponentData<CItem>(food);
                Check(original.ID == recipe.Item.ID && !original.IsPartial, "Recipe factory did not create the expected complete item.");
                var split = test.Em.GetComponentData<CSplittableItem>(food);
                Check(split.TotalCount == recipe.Item.SplitCount, "Recipe capacity differs from game data.");
                split.RemainingCount = count;
                test.Ctx.Set(food, split);
                test.Ctx.UpdateHolder(food, test.Source);
                test.Transfer(test.Source, test.Target, automated);
                test.AssertExchange(test.Source, test.Target, food, carrier, original, count);
                test.Transfer(test.Target, test.Source, automated);
                test.AssertExchange(test.Target, test.Source, food, carrier, original, count);
            }
        }

        private static int RejectCandidate(Item item)
        {
            int probes = 0;
            using (var test = new TestWorld())
            {
                var food = test.Ctx.CreateItem(item.ID);
                test.Ctx.UpdateHolder(food, test.Source);
                var declared = item.SplitDepletedItems.Select(i => i.ID).Concat(
                    item.SplitByComponentsHolder == null ? new int[0] : new[] { item.SplitByComponentsHolder.ID });
                foreach (int id in Supported.Values.Concat(declared).Distinct())
                {
                    var carrier = test.Ctx.CreateItemGroup(id, new ItemList(id));
                    test.Ctx.UpdateHolder(carrier, test.Target);
                    test.Propose(test.Source, test.Target);
                    test.Refill.Update();
                    Check(test.AcceptanceCount == 0, "Unexpected support outside the verified candidate list.");
                    test.Resolve();
                    test.Ctx.UpdateHolder(carrier);
                    test.Ctx.Destroy(carrier);
                    probes++;
                }
            }
            return probes;
        }

        private static string Exclusion(Item item)
        {
            if (!item.IsSplittable) return "Non-portionable recipe template";
            if (item.SplitCount <= 1) return "Single portion";
            if (item.SplitByCopying) return "Copy source";
            if (item.RequiresCleaning) return "Requires cleaning";
            if (item.SplitDepletedItems.Count != 1) return "No single reusable remainder";
            var remainder = item.SplitDepletedItems[0];
            if (remainder.ID == item.ID) return "Source replenishes itself";
            if (remainder.IsSplittable) return "Food remainder";
            return "Remainder is not clean provider-backed equipment";
        }

        private static void Conflict()
        {
            currentCase = "Competing manual and combiner transfers";
            using (var test = new TestWorld())
            {
                var recipe = Recipe.Variants(GameData.Main.Get<Item>(-319704939)).First();
                var carrier = test.TakeCarrier(-1190642520);
                var other = test.Em.CreateEntity(typeof(CItemHolder));
                var first = recipe.Create(test.Ctx);
                var second = recipe.Create(test.Ctx);
                test.Ctx.UpdateHolder(first, test.Source);
                test.Ctx.UpdateHolder(second, other);
                test.Propose(test.Source, test.Target, TransferFlags.Interaction | TransferFlags.Drop);
                test.Propose(other, test.Target, TransferFlags.RequireMerge);
                test.Refill.Update();
                Check(test.AcceptanceCount == 2, "Both proposals should be eligible before resolution.");
                test.Resolve();
                var winner = test.Held(test.Target);
                Check(winner == first || winner == second, "No winning transfer.");
                Check(test.Held(winner == first ? test.Source : other) == carrier, "Winner did not receive the carrier.");
                Check(test.Held(winner == first ? other : test.Source) == (winner == first ? second : first), "Losing source changed.");
                Check(test.Em.GetComponentData<CSplittableItem>(first).RemainingCount +
                    test.Em.GetComponentData<CSplittableItem>(second).RemainingCount == 8, "Concurrent transfers changed food quantity.");
            }
        }

        private static void Bootstrap()
        {
            currentCase = "Normal world bootstrap and mod injection";
            using (var test = new TestWorld(new WorldBootstrapper("Refill Containers bootstrap test", GameSetupMode.Server).CreatedWorld))
            {
                var pack = new AssemblyModPack("Refill Containers", new byte[0], new byte[0]) { Asm = typeof(AcceptRefillEmptyContainer).Assembly };
                pack.PostActivate();
                pack.Inject(new ModInjectionContext { World = test.World });
                var recipe = Recipe.Variants(GameData.Main.Get<Item>(-319704939)).First();
                var carrier = test.TakeCarrier(-1190642520);
                var food = recipe.Create(test.Ctx);
                var original = test.Em.GetComponentData<CItem>(food);
                test.Ctx.UpdateHolder(food, test.Source);
                test.Transfer(test.Source, test.Target, false);
                test.AssertExchange(test.Source, test.Target, food, carrier, original, 4);
            }
        }
    }
}

