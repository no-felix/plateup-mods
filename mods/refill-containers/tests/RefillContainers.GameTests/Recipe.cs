using System;
using System.Collections.Generic;
using System.Linq;
using Kitchen;
using KitchenData;
using Unity.Entities;

namespace KitchenRefillContainers.GameTests
{
    internal sealed class Recipe
    {
        internal readonly Item Item;
        internal readonly Func<EntityContext, Entity> Create;
        internal Recipe(Item item, Func<EntityContext, Entity> create) { Item = item; Create = create; }

        internal static IEnumerable<Recipe> Variants(Item item)
        {
            // Non-composite groups, such as drained spaghetti, are also created directly by native interactions.
            if (!(item is ItemGroup group) || group.AutoCollapsing ||
                (group.DerivedSets.Count == 0 && !group.HasImplicitlyModifiedComponents))
            {
                yield return new Recipe(item, ctx => ctx.CreateItem(item.ID));
                yield break;
            }
            if (group.DerivedSets.Count > 0)
            {
                IEnumerable<List<int>> combinations = new[] { new List<int>() };
                foreach (var set in group.DerivedSets)
                {
                    var options = Enumerable.Range(0, 1 << set.Items.Count)
                        .Select(mask => set.Items.Where((_, i) => (mask & (1 << i)) != 0).Select(i => i.ID).ToList())
                        .Where(ids => ids.Count >= set.Min && ids.Count <= set.Max).ToArray();
                    combinations = combinations.SelectMany(prefix => options.Select(option => prefix.Concat(option).ToList())).ToArray();
                }
                foreach (var ids in combinations)
                {
                    var components = new ItemList(ids);
                    if (GameData.Main.ItemSetView.IsValidGroup(components, out var result) != Satisfaction.Correct || result != item.ID)
                        continue;
                    yield return new Recipe(item, ctx =>
                    {
                        Tests.Check(ctx.AttemptComponentMerge(out var entity, components), "Valid recipe components could not be merged.");
                        return entity;
                    });
                }
                yield break;
            }
            // Baked pastry groups retain the components of their raw recipe. Use the declared process transition.
            foreach (var parent in GameData.Main.Get<Item>())
                foreach (var process in parent.DerivedProcesses.Where(p => !p.IsBad && p.Result != null && p.Result.ID == item.ID))
                    foreach (var recipe in Variants(parent))
                        yield return new Recipe(item, ctx =>
                        {
                            var entity = recipe.Create(ctx);
                            ctx.ChangeItemType(entity, item.ID, false, process.Process.ID, false);
                            return entity;
                        });
        }
    }
}

