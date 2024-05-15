using System;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using ProjectM;
using ProjectM.Shared;
using Unity.Entities;
using VampireCommandFramework;

namespace GroundItemPickup.VCFCompat;

public static partial class Commands
{
    private static ManualLogSource _log => Plugin.LogInstance;

    static Commands()
    {
        Enabled = IL2CPPChainloader.Instance.Plugins.TryGetValue("gg.deca.VampireCommandFramework", out var info);
        if (Enabled) _log.LogWarning($"VCF Version: {info.Metadata.Version}");
    }

    public static bool Enabled { get; private set; }

    public static void Register() => CommandRegistry.RegisterAll();

    private const float Radius = 25f;

    public record Item(Entity Entity);

    public class Tester : CommandArgumentConverter<Item, ChatCommandContext>
    {
        public override Item Parse(ChatCommandContext ctx, string input)
        {
            var items = ItemUtil.ClosestItems(ctx, Radius);
            
            foreach (var item in items)
            {
                return new Item(item);
            }

            throw ctx.Error($"Could not find an item within {Radius:F1}");
        }

        public class ItemCommands
        {
            /*
            [Command("getItems", shortHand: "gitem", adminOnly: true, description: "Get items in range.",
                usage: "Usage: .getItems [radius]")]
            public void getitems(ChatCommandContext ctx, float radius = 25f)
            {
                var items = ItemUtil.ClosestItems(ctx, radius);

                foreach (var item in items)
                {
                    _log.LogWarning($"[Item][{ctx.Event.SenderUserEntity.ToString()}]");
                    var i = 1;
                    foreach (var type in VWorld.Server.EntityManager.GetComponentTypes(item))
                    {
                        _log.LogWarning($"[{i++}][ComponentType][{type}]");
                    }

                    _log.LogWarning($"----------------------------------------");
                    var prefabSystem = VWorld.Server.EntityManager.World.GetExistingSystem<PrefabCollectionSystem>();
                    VWorld.Server.EntityManager.TryGetComponentData<PrefabGUID>(item, out var prefabGuid);
                    _log.LogWarning($"Getting PrefabGUID: {prefabGuid.GuidHash}");
                    try
                    {
                        _log.LogWarning(
                            $"Trying to get name from PrefabGUID: {prefabSystem.PrefabGuidToNameDictionary[prefabGuid]}");
                    }
                    catch (Exception)
                    {
                        _log.LogWarning($"Tried to get name from PrefabGUID, but failed.");
                    }

                    _log.LogWarning($"----------------------------------------");
                    var playSequenceOnPickup =
                        VWorld.Server.EntityManager.GetComponentDataAOT<PlaySequenceOnPickup>(item);

                    _log.LogWarning($"Getting PickupSequenceGuid: {playSequenceOnPickup.PickupSequenceGuid.GuidHash}");
                    _log.LogWarning(
                        $"Getting InventoryFullSequenceGuid: {playSequenceOnPickup.InventoryFullSequenceGuid.GuidHash}");
                }

                if (items.Count < 1)
                {
                    ctx.Error("Failed to find any items, are there any in range?");
                }
                else
                {
                    ctx.Reply("Items got got!");
                }
            }
            
            */
            [Command("addItems", shortHand: "aitem", adminOnly: true, description: "Get items in range.",
                usage: "Usage: .addItems [radius]")]
            public void addToInv(ChatCommandContext ctx, float radius = 25f)
            {
                var items = ItemUtil.ClosestItems(ctx, radius);
                var server = VWorld.Server;
                var gameDataSystem = server.GetExistingSystemManaged<GameDataSystem>();
                var itemHashLookupMap = gameDataSystem.ItemHashLookupMap;
                var entityManager = server.EntityManager;


                if (!InventoryUtilities.TryGetInventoryEntity(entityManager, ctx.Event.SenderCharacterEntity,
                        out Entity playerInventory) || playerInventory == Entity.Null)
                {
                    // Player inventory couldn't be found -> stop trying to move items
                    ctx.Error("Player inventory couldn't be found");
                    return;
                }

                foreach (var item in items)
                {
                    VWorld.Server.EntityManager.TryGetBuffer<InventoryBuffer>(item, out var itemInventory);
                    for (int i = 0; i < itemInventory.Length; i++)
                    {
                        var droppedItem = itemInventory[i];

                        var transferAmount = droppedItem.Amount;
                        if (!ItemUtil.TryGiveItem(VWorld.Server.EntityManager, itemHashLookupMap, playerInventory,
                                droppedItem.ItemType,
                                transferAmount, out var remainingStacks))
                        {
                            // Failed to add the item(s) to the player's inventory -> stop trying to move any items at all
                            ctx.Error("Failed to add the item(s) to the player's inventory");
                            return;
                        }

                        transferAmount -= remainingStacks;
                        try
                        {
                            DestroyUtility.DestroyWithReason(VWorld.Server.EntityManager, item, DestroyReason.Default);
                        }
                        catch (Exception)
                        {
                            ctx.Error("Failed to destroy dropped item.");
                            // Failed to destroy the items on the ground -> Remove the items from the player's inventory & stop trying to move any items at all
                            InventoryUtilitiesServer.TryRemoveItem(entityManager, playerInventory, droppedItem.ItemType,
                                transferAmount);
                            return;
                        }
/*
                        if (!InventoryUtilitiesServer.TryRemoveItem(entityManager, item, droppedItem.ItemType,
                                transferAmount))
                        {
                            // Failed to remove the item from the ground -> Remove the items from the player's inventory & stop trying to move any items at all
                            InventoryUtilitiesServer.TryRemoveItem(entityManager, playerInventory, droppedItem.ItemType,
                                transferAmount);
                            ctx.Error("Failed to remove the item from the ground");
                            return;
                        }
*/
                        InventoryUtilitiesServer.CreateInventoryChangedEvent(entityManager,
                            ctx.Event.SenderCharacterEntity,
                            droppedItem.ItemType, droppedItem.Amount, droppedItem.ItemEntity._Entity ,InventoryChangedEventType.Obtained);
                    }
                }

                ctx.Reply("Items added to Inventory!");
            }
/*
            [Command("tpItems", shortHand: "tpitem", adminOnly: true, description: "Get items in range.",
                usage: "Usage: .tpItems [radius]")]
            public void tpitems(ChatCommandContext ctx, float radius = 25f)
            {
                var items = ItemUtil.ClosestItems(ctx, radius);
                VWorld.Server.EntityManager
                    .TryGetComponentData<Translation>(ctx.Event.SenderUserEntity, out var userPos);
                foreach (var item in items)
                {
                    item.WithComponentData((ref Translation pos) => { pos.Value = userPos.Value; });
                }

                if (items.Count < 1)
                {
                    ctx.Error("Failed to find any items, are there any in range?");
                }
                else
                {
                    ctx.Reply("Items got tpd!");
                }
            }
            */
        }
    }
}