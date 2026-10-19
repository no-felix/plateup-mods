using System;
using System.Linq;
using Kitchen;
using KitchenData;
using Unity.Entities;

namespace KitchenRefillContainers.GameTests
{
    internal sealed class TestWorld : IDisposable
    {
        internal readonly World World;
        internal EntityManager Em => World.EntityManager;
        internal EntityContext Ctx => new EntityContext(Em);
        internal readonly Entity Source, Target;
        internal readonly AcceptRefillEmptyContainer Refill;
        private readonly TakeFromHolder sender;
        private readonly TakeFromProvider providerSender;
        private readonly AcceptIntoHolder receiver;
        private readonly PruneByInteraction interactions;
        private readonly PruneByAutomations automations;
        private readonly PruneByLocks locks;
        private readonly MarkAcceptedTransfers mark;
        private readonly ResolveTransfers resolver;
        private readonly ItemTransferAccept accepts;
        private readonly EntityQuery proposals, acceptances, items;

        internal TestWorld(World bootstrapped = null)
        {
            World = bootstrapped ?? new World("Refill Containers tests");
            Source = Em.CreateEntity(typeof(CItemHolder));
            Target = Em.CreateEntity(typeof(CItemHolder));
            sender = World.GetOrCreateSystem<TakeFromHolder>();
            providerSender = World.GetOrCreateSystem<TakeFromProvider>();
            receiver = World.GetOrCreateSystem<AcceptIntoHolder>();
            interactions = World.GetOrCreateSystem<PruneByInteraction>();
            automations = World.GetOrCreateSystem<PruneByAutomations>();
            locks = World.GetOrCreateSystem<PruneByLocks>();
            mark = World.GetOrCreateSystem<MarkAcceptedTransfers>();
            resolver = World.GetOrCreateSystem<ResolveTransfers>();
            Refill = World.GetOrCreateSystem<AcceptRefillEmptyContainer>();
            World.GetOrCreateSystem<ReplacedDepletedSplits>();
            World.GetOrCreateSystem<ApplyItemTypeChanges>();
            accepts = World.GetOrCreateSystem<ItemTransferAccept>();
            foreach (var system in new ComponentSystemBase[] { receiver, World.GetOrCreateSystem<AcceptMergeIntoHolder>(),
                World.GetOrCreateSystem<AcceptRefillMergeIntoHolder>(), Refill, mark })
                accepts.AddSystemToUpdateList(system);
            accepts.SortSystems();
            if (bootstrapped == null)
                foreach (var system in World.Systems)
                    if (system is GenericSystemBase generic) generic.PostInitialisation();
            Em.CreateEntity(typeof(SGameplayMarker));
            proposals = Em.CreateEntityQuery(typeof(CItemTransferProposal));
            acceptances = Em.CreateEntityQuery(typeof(CItemTransferAccept));
            items = Em.CreateEntityQuery(typeof(CItem));
        }

        internal Entity Held(Entity holder) => Em.GetComponentData<CItemHolder>(holder).HeldItem;
        internal int AcceptanceCount => acceptances.CalculateEntityCount();

        internal Entity TakeCarrier(int id)
        {
            var provider = GameData.Main.Get<Item>(id).DedicatedProvider.Properties.OfType<CItemProvider>().Single();
            provider.SetAsItem(provider.DefaultProvidedItem);
            provider.Available = provider.Maximum;
            var source = Em.CreateEntity();
            Ctx.Set(source, provider);
            TakeFromProvider.CreateProposal(providerSender, Ctx, source, Target);
            receiver.Update();
            Tests.Check(AcceptanceCount == 1, "Carrier provider did not offer exactly one transfer.");
            Resolve();
            var carrier = Held(Target);
            Tests.Check(Em.GetComponentData<CItem>(carrier).ID == id, "Provider returned the wrong carrier.");
            return carrier;
        }

        internal Entity Deplete(Recipe recipe)
        {
            var food = recipe.Create(Ctx);
            Ctx.UpdateHolder(food, Target);
            var split = Em.GetComponentData<CSplittableItem>(food);
            split.RemainingCount = 0;
            Ctx.Set(food, split);
            World.GetExistingSystem<ReplacedDepletedSplits>().Update();
            World.GetExistingSystem<EndSimBarrier>().Update();
            World.GetExistingSystem<ApplyItemTypeChanges>().Update();
            var carrier = Held(Target);
            Tests.Check(!Em.HasComponent<CSplittableItem>(carrier), "Native depletion left a portionable target.");
            return carrier;
        }

        internal void Transfer(Entity from, Entity to, bool automated)
        {
            if (automated)
                Ctx.Set(from, GameData.Main.Get<Appliance>(-1906799936).Properties.OfType<CAutomatedInteractor>().Single());
            else if (Em.HasComponent<CAutomatedInteractor>(from)) Em.RemoveComponent<CAutomatedInteractor>(from);
            Ctx.Set(from, new CAttemptingInteraction { Target = to, Type = InteractionType.Grab, Mode = InteractionMode.Items });
            sender.Update();
            automations.Update();
            locks.Update();
            interactions.Update();
            accepts.Update();
            interactions.Update();
            Resolve();
            Em.RemoveComponent<CAttemptingInteraction>(from);
        }

        internal void Propose(Entity from, Entity to, TransferFlags flags = TransferFlags.Null)
            => TransferProposalSystem.CreateProposal(Ctx, sender, Held(from), from, to, flags | TransferFlags.Holder);

        internal void Resolve()
        {
            mark.Update();
            resolver.Update();
            Em.DestroyEntity(acceptances);
            Em.DestroyEntity(proposals);
        }

        internal void AssertExchange(Entity from, Entity to, Entity food, Entity carrier, CItem original, int count)
        {
            Tests.Check(Held(from) == carrier && Held(to) == food, "Food or returned carrier is in the wrong holder.");
            Tests.Check(Em.GetComponentData<CHeldBy>(food).Holder == to && Em.GetComponentData<CHeldBy>(carrier).Holder == from,
                "Holder and item links disagree.");
            var current = Em.GetComponentData<CItem>(food);
            Tests.Check(current.ID == original.ID && current.Items.IsEquivalent(original.Items), "Food type or recipe components changed.");
            Tests.Check(Em.GetComponentData<CSplittableItem>(food).RemainingCount == count, "Portions were lost or duplicated.");
            Tests.Check(items.CalculateEntityCount() == 2, "Food or carrier entities were created or lost.");
        }

        public void Dispose()
        {
            ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(World);
            World.Dispose();
        }
    }
}
