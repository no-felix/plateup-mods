using System;
using Kitchen;
using KitchenData;
using Unity.Entities;
using UnityEngine;

namespace KitchenRefillContainers.GameTests
{
    internal static partial class Tests
    {
        private static void Restrictions()
        {
            Reject("Target insertion lock", (t, f, e) => t.Ctx.Set(t.Target, new CItemHolderPreventTransfer { PreventInsertingInto = true }));
            Reject("Target removal lock", (t, f, e) => t.Ctx.Set(t.Target, new CItemHolderPreventTransfer { PreventTakingFrom = true }));
            Reject("Return insertion lock", (t, f, e) => t.Ctx.Set(t.Source, new CItemHolderPreventTransfer { PreventInsertingInto = true }));
            Reject("Source removal lock", (t, f, e) => t.Ctx.Set(t.Source, new CItemHolderPreventTransfer { PreventTakingFrom = true }));
            Reject("No direct insertion", (t, f, e) => t.Ctx.Set(t.Target, new CItemHolderFilter { NoDirectInsertion = true }));
            Reject("Category filter", (t, f, e) => t.Ctx.Set(t.Target, new CItemHolderFilter { Category = ItemCategory.Crates }));
            Reject("Specific target item", (t, f, e) => t.Ctx.Set(t.Target, new CItemHolderOnlySpecificItem { ItemID = -1190642520 }));
            Reject("Specific return item", (t, f, e) => t.Ctx.Set(t.Source, new CItemHolderOnlySpecificItem { ItemID = -319704939 }));
            Reject("Demo lock", (t, f, e) => t.Ctx.Set<CDemoLock>(e, default));
            Reject("Item transfer lock", (t, f, e) => t.Ctx.Set<CPreventItemTransfer>(e, default));
            Reject("Holder transfer lock", (t, f, e) => t.Ctx.Set<CPreventItemTransfer>(t.Target, default));
            Reject("Holder merge lock", (t, f, e) => t.Ctx.Set<CItemHolderPreventMergeIntoHeld>(t.Target, default));
            Reject("Target merge restriction", (t, f, e) => t.Ctx.Set(e, new CPreventItemMerge { Condition = MergeCondition.NoMerge }));
            Reject("Source merge restriction", (t, f, e) => t.Ctx.Set(f, new CPreventItemMerge { Condition = MergeCondition.NoMerge }));
            foreach (var flag in new[] { TransferFlags.NoReturns, TransferFlags.Split, TransferFlags.Provider,
                TransferFlags.Storage, TransferFlags.AllowSourceDuplication, TransferFlags.RequireSplit })
                Reject("Unsupported sender: " + flag, (t, f, e) => { }, flag);
            Reject("Wrong carrier", (t, f, e) => t.Ctx.ChangeItemType(e, -622622812, true, 0, false));
            Reject("Filled target", (t, f, e) => t.Ctx.ChangeItemType(e, -319704939, true, 0, false));
            Reject("Hidden food in carrier group", (t, f, e) =>
            {
                var item = t.Ctx.Get<CItem>(e);
                item.Items = new ItemList(item.ID, 111245472);
                t.Ctx.Set(e, item);
            });
            Reject("Incomplete recipe", (t, f, e) =>
            {
                var item = t.Ctx.Get<CItem>(f);
                item.IsPartial = true;
                t.Ctx.Set(f, item);
            });
            foreach (int remaining in new[] { 0, 5 })
                Reject("Invalid portions: " + remaining, (t, f, e) =>
                {
                    var split = t.Ctx.Get<CSplittableItem>(f);
                    split.RemainingCount = remaining;
                    t.Ctx.Set(f, split);
                });
            Debug.Log("PLATEUP_MOD_CHECK: transfer restrictions passed");
        }

        private static void Reject(string name, Action<TestWorld, Entity, Entity> setup, TransferFlags flags = TransferFlags.Null)
        {
            currentCase = name;
            using (var test = new TestWorld())
            {
                var recipe = System.Linq.Enumerable.First(Recipe.Variants(GameData.Main.Get<Item>(-319704939)));
                var carrier = test.TakeCarrier(-1190642520);
                var food = recipe.Create(test.Ctx);
                test.Ctx.UpdateHolder(food, test.Source);
                setup(test, food, carrier);
                test.Propose(test.Source, test.Target, flags);
                test.Refill.Update();
                Check(test.AcceptanceCount == 0, "Restricted transfer accepted.");
                Check(test.Held(test.Source) == food && test.Held(test.Target) == carrier, "Rejected transfer changed holder contents.");
            }
        }
    }
}

