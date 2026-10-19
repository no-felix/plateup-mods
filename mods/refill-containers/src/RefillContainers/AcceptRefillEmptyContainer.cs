using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace KitchenRefillContainers
{
    [UpdateInGroup(typeof(ItemTransferAccept))]
    [UpdateAfter(typeof(AcceptRefillMergeIntoHolder))]
    [UpdateBefore(typeof(MarkAcceptedTransfers))]
    public sealed class AcceptRefillEmptyContainer : TransferAcceptSystem, IModSystem
    {
        private const TransferFlags UnsupportedSenders = TransferFlags.Split | TransferFlags.Provider |
            TransferFlags.Storage | TransferFlags.NoReturns | TransferFlags.AllowSourceDuplication | TransferFlags.RequireSplit;
        private EntityQuery proposals;

        protected override void Initialise()
        {
            proposals = GetEntityQuery(ComponentType.ReadOnly<CItemTransferProposal>());
        }

        protected override void OnUpdate()
        {
            if (GameData.Main == null)
                return;
            using (var entities = proposals.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    var proposal = EntityManager.GetComponentData<CItemTransferProposal>(entity);
                    if (proposal.Status == ItemTransferStatus.Proposed && CanRefill(proposal))
                        Accept(entity, TransferFlags.RequireMerge);
                }
        }

        private bool CanRefill(CItemTransferProposal proposal)
        {
            if (proposal.Source == proposal.Destination || (proposal.Flags & TransferFlags.Holder) == 0 ||
                (proposal.Flags & UnsupportedSenders) != 0 ||
                !Require<CItemHolder>(proposal.Source, out var sourceHolder) || sourceHolder.HeldItem != proposal.Item ||
                !Require<CItem>(proposal.Item, out var source) || source.IsPartial ||
                !Require<CSplittableItem>(proposal.Item, out var portions) || portions.TotalCount <= 1 ||
                portions.RemainingCount <= 0 || portions.RemainingCount > portions.TotalCount || portions.CopySplit || portions.SplitAsSelf ||
                !Require<CItemHolder>(proposal.Destination, out var targetHolder) ||
                !Require<CItem>(targetHolder.HeldItem, out var target) || target.ID == 0 ||
                !target.Items.IsEquivalent(target.ID) || Has<CSplittableItem>(targetHolder.HeldItem) ||
                !GameData.Main.TryGet<Item>(source.ID, out var definition) || EmptyCarrier(definition) != target.ID)
                return false;

            // Native recipes can be transient, and providers can produce grouped empty carriers.
            // The component list above establishes emptiness independently of those representation flags.
            return proposal.RefuseMergeWith != target.ID && proposal.MergeCondition.CanComp() &&
                (!Require<CPreventItemMerge>(targetHolder.HeldItem, out var merge) || merge.Condition.CanComp()) &&
                !Has<CItemHolderPreventMergeIntoHeld>(proposal.Destination) &&
                !Has<CPreventItemTransfer>(proposal.Item) && !Has<CPreventItemTransfer>(targetHolder.HeldItem) &&
                !Has<CDemoLock>(proposal.Item) && !Has<CDemoLock>(targetHolder.HeldItem) &&
                AllowsExchange(proposal.Source, target) && AllowsExchange(proposal.Destination, source);
        }

        private static int EmptyCarrier(Item source)
        {
            if (!source.IsSplittable || source.SplitCount <= 1 || source.RequiresCleaning || source.SplitByCopying)
                return 0;
            var empty = source.SplitByComponents ? source.SplitByComponentsHolder :
                source.SplitDepletedItems.Count == 1 ? source.SplitDepletedItems[0] : null;
            // A depleted food, broth residue or dirty dish is not reusable empty equipment.
            return empty != null && empty.ID != source.ID && !empty.IsSplittable && !empty.RequiresCleaning &&
                empty.IsIndisposable && empty.DedicatedProvider != null ? empty.ID : 0;
        }

        private bool AllowsExchange(Entity holder, CItem incoming)
        {
            // Each holder relinquishes its current item and receives the other one.
            if (Has<CPreventItemTransfer>(holder) || Has<CCrateInbox>(holder) ||
                (Require<CItemHolderPreventTransfer>(holder, out var restriction) &&
                    (restriction.PreventInsertingInto || restriction.PreventTakingFrom)))
                return false;
            if (Require<CItemHolderFilter>(holder, out var filter))
            {
                if (filter.NoDirectInsertion || !filter.AllowCategory(incoming.Category))
                    return false;
            }
            else if (incoming.Category != ItemCategory.Generic)
                return false;
            return !Require<CItemHolderOnlySpecificItem>(holder, out var specific) || specific.ItemID == incoming.ID;
        }

        public override void AcceptTransfer(Entity proposalEntity, Entity acceptance, EntityContext ctx, out Entity returnItem)
        {
            var proposal = EntityManager.GetComponentData<CItemTransferProposal>(proposalEntity);
            returnItem = EntityManager.GetComponentData<CItemHolder>(proposal.Destination).HeldItem;
            // Relocate the initialized entities to preserve recipe components, portions and item properties.
            ctx.UpdateHolder(returnItem);
            ctx.UpdateHolder(proposal.Item, proposal.Destination);
        }
    }
}
