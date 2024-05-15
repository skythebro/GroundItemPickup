using System.Collections.Generic;
using Bloodstone.API;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using Il2CppInterop.Runtime;
using Stunlock.Core;


namespace GroundItemPickup;

public static class ItemUtil
{
    private static NativeArray<Entity> GetItems()
    {
        var itemQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<ItemPickup>()
                
            },
            None = new[] { ComponentType.ReadOnly<DestroyTag>() }
        });
        return itemQuery.ToEntityArray(Allocator.Temp);
    }

    internal static List<Entity> ClosestItems(ChatCommandContext ctx, float radius)
    {
        try
        {
            var e = ctx.Event.SenderCharacterEntity;
            var items = GetItems();
            var results = new List<Entity>();
            var origin = VWorld.Server.EntityManager.GetComponentData<LocalToWorld>(e).Position;

            foreach (var mob in items)
            {
                var position = VWorld.Server.EntityManager.GetComponentData<LocalToWorld>(mob).Position;
                var distance = UnityEngine.Vector3.Distance(origin, position); // wait really?
                if (distance < radius)
                {
                    results.Add(mob);
                }
            }

            return results;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
    
    public delegate void ActionRefTest<T>(ref T item);
    
    public static void WithComponentDataTestAOT<T>(this Entity entity, ActionRefTest<T> action) where T : unmanaged
    {
        var componentData = VWorld.Game.EntityManager.GetComponentDataAOT<T>(entity);
        action(ref componentData);
        VWorld.Game.EntityManager.SetComponentData<T>(entity, componentData);
    }
    
    private static Il2CppSystem.Type GetType<T>() => Il2CppType.Of<T>();
    
    public static unsafe T GetComponentDataAOT<T>(this EntityManager entityManager, Entity entity) where T : unmanaged
    {
        var type = TypeManager.GetTypeIndex(GetType<T>());
        var result = (T*)entityManager.GetComponentDataRawRW(entity, type);
        return *result;
    }
    
    public static bool TryGiveItem(EntityManager entityManager, NativeParallelHashMap<PrefabGUID, ItemData>? itemDataMap,
        Entity recipient, PrefabGUID itemType, int amount, out int remainingitems, bool dropRemainder = false)
    {
            
        itemDataMap ??=  VWorld.Game.GetExistingSystemManaged<GameDataSystem>().ItemHashLookupMap;
        var itemSettings = AddItemSettings.Create(entityManager, itemDataMap.Value, false, default, default, false, false, dropRemainder);
        AddItemResponse response = InventoryUtilitiesServer.TryAddItem(itemSettings, recipient, itemType, amount);
        remainingitems = response.RemainingAmount;
        return response.Success;
    }
}