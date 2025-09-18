﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using FrooxEngine;
using HarmonyLib;
using ResoniteMario64.Mario64.Components.Interfaces;
using ResoniteMario64.Mario64.Components.Objects;
using ResoniteMario64.Mario64.libsm64;

namespace ResoniteMario64.Mario64.Components.Context;

public sealed partial class SM64Context
{
    internal readonly Dictionary<Collider, SM64StaticCollider> StaticColliders = new Dictionary<Collider, SM64StaticCollider>();
    internal readonly Dictionary<Collider, SM64DynamicCollider> DynamicColliders = new Dictionary<Collider, SM64DynamicCollider>();
    internal readonly Dictionary<Collider, SM64Interactable> Interactables = new Dictionary<Collider, SM64Interactable>();
    internal readonly Dictionary<Collider, SM64WaterBox> WaterBoxes = new Dictionary<Collider, SM64WaterBox>();

    public void HandleCollider(Collider collider, bool log = true)
    {
        if (collider == null) return;
        
        if (collider.World != World) return;
        if (collider.IsDestroyed)
        {
            HandleColliderDestroyed(collider);
            return;
        }

        int? added = TryAddCollider(collider);
        if (added != null)
        {
            collider.Destroyed -= HandleColliderDestroyed;
            collider.Destroyed += HandleColliderDestroyed;
        }

        if (log) LogCollider(collider, added, collider.IsDestroyed);
    }

    private void HandleColliderDestroyed(IDestroyable instance)
    {
        if (instance is not Collider collider) return;

        int? removed = TryRemoveCollider(collider);
        if (removed != null)
        {
            collider.Destroyed -= HandleColliderDestroyed;
        }

        LogCollider(collider, removed, true);
    }

    private int? TryAddCollider(Collider collider)
    {
        if (Utils.IsStaticCollider(collider))
        {
            return RegisterStaticCollider(collider);
        }

        if (Utils.IsDynamicCollider(collider))
        {
            return RegisterDynamicCollider(collider);
        }

        if (Utils.IsInteractable(collider))
        {
            return RegisterInteractable(collider);
        }

        if (Utils.IsWaterBox(collider))
        {
            return RegisterWaterBox(collider);
        }

        return null;
    }

    private int? TryRemoveCollider(Collider collider)
    {
        if (StaticColliders.TryGetValue(collider, out SM64StaticCollider staticCollider))
        {
            staticCollider.Dispose();
            return 1;
        }

        if (DynamicColliders.TryGetValue(collider, out SM64DynamicCollider dynamicCollider))
        {
            dynamicCollider.Dispose();
            return 2;
        }

        if (Interactables.TryGetValue(collider, out SM64Interactable interactable))
        {
            interactable.Dispose();
            return 3;
        }

        if (WaterBoxes.TryGetValue(collider, out SM64WaterBox waterBox))
        {
            waterBox.Dispose();
            return 4;
        }

        return null;
    }

    private static System.Timers.Timer _staticUpdateTimer;

    private void QueueStaticCollidersUpdate()
    {
        if (_staticUpdateTimer != null) return;

        _staticUpdateTimer = new System.Timers.Timer(1500);
        _staticUpdateTimer.Elapsed += delegate
        {
            _staticUpdateTimer.Stop();
            _staticUpdateTimer.Dispose();
            _staticUpdateTimer = null;

            _staticColliderUpdate = true;
        };
        _staticUpdateTimer.AutoReset = false;
        _staticUpdateTimer.Start();
    }

    // Static Colliders
    private int RegisterStaticCollider(Collider collider)
    {
        QueueStaticCollidersUpdate();
        
        if (StaticColliders.ContainsKey(collider))
        {
            return 10;
        }

        SM64StaticCollider col = new SM64StaticCollider(collider, this);
        StaticColliders.Add(collider, col);
        return 1;
    }

    internal void UnregisterStaticCollider(Collider collider)
    {
        QueueStaticCollidersUpdate();

        StaticColliders.Remove(collider);
    }
    
    // Dynamic Colliders
    private int RegisterDynamicCollider(Collider collider)
    {
        if (DynamicColliders.TryGetValue(collider, out SM64DynamicCollider dynamicCollider))
        {
            if (dynamicCollider.InitScale.Approximately(collider.Slot.GlobalScale, 0.001f))
            {
                return 20;
            }

            dynamicCollider.Dispose();
        }

        SM64DynamicCollider col = new SM64DynamicCollider(collider, this);
        DynamicColliders.Add(collider, col);
        return 2;
    }

    internal void UnregisterDynamicCollider(Collider collider)
    {
        DynamicColliders.Remove(collider);
    }

    // Interactables
    private int RegisterInteractable(Collider collider)
    {
        if (Interactables.ContainsKey(collider))
        {
            return 30;
        }

        SM64Interactable col = new SM64Interactable(collider, this);
        Interactables.Add(collider, col);
        return 3;
    }

    internal void UnregisterInteractable(Collider collider)
    {
        Interactables.Remove(collider);
    }

    // WaterBoxes
    private int RegisterWaterBox(Collider collider)
    {
        if (WaterBoxes.ContainsKey(collider))
        {
            return 40;
        }

        SM64WaterBox col = new SM64WaterBox(collider, this);
        WaterBoxes.Add(collider, col);
        return 4;
    }

    internal void UnregisterWaterBox(Collider collider)
    {
        WaterBoxes.Remove(collider);
    }

    // Patches
    [HarmonyPatch(typeof(Collider))]
    public class ColliderPatch
    {
        [HarmonyPatch("OnAwake"), HarmonyPostfix]
        public static void OnAwakePatch(Collider __instance)
        {
            if (SM64Context.Instance == null) return;

            __instance.RunInUpdates(1, () => SM64Context.Instance?.HandleCollider(__instance));
        }

        [HarmonyPatch("OnChanges"), HarmonyPostfix]
        public static void OnChangesPatch(Collider __instance)
        {
            if (SM64Context.Instance == null) return;

            SM64Context.Instance?.HandleCollider(__instance);
        }
    }

    private static void LogCollider(object obj, int? added, bool destroyed, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
    {
        if (added == null) return;
        if (obj is not Collider collider) return;
        if (!Config.LogColliderChanges.Value || !Utils.CheckDebug()) return;
        
        bool isNewlyAdded = added is 1 or 2 or 3 or 4;
        string name = added switch
        {
            1 or 10 => "Static Collider",
            2 or 20 => "Dynamic Collider",
            3 or 30 => "Interactable",
            4 or 40 => "WaterBox",
            _       => "Collider"
        };

        string tag = collider.Slot?.Tag;
        string[] tagParts = tag?.Split(',');

        Utils.TryParseTagParts(
            tagParts,
            out SM64Constants.SM64SurfaceType surfaceType,
            out SM64Constants.SM64TerrainType terrainType,
            out SM64Constants.SM64InteractableType interactableType,
            out int interactableId
        );

        string state = "Already Added";
        if (isNewlyAdded) state = "Added";
        if (destroyed) state = "Destroyed";

        string message = $"{name} {state}: Name: {collider.Slot?.Name}, ID: {collider.ReferenceID}, Surface: {surfaceType}, Terrain: {terrainType}, Interactable: {interactableType}, ID/Force: {interactableId}";

        if (destroyed)
            Logger.Error(message, caller, line);
        else if (isNewlyAdded)
            Logger.Msg(message, caller, line);
        else
            Logger.Warn(message, caller, line);
    }

    public void GetAllColliders(bool log, out Dictionary<string, List<ISM64Object>> colliders)
    {
        Dictionary<string, (int Key, IEnumerable<ISM64Object> Source)> sources = new Dictionary<string, (int, IEnumerable<ISM64Object>)>
        {
            ["Static Collider"] = (10, StaticColliders.Values),
            ["Dynamic Collider"] = (20, DynamicColliders.Values),
            ["Interactable"] = (30, Interactables.Values),
            ["Waterbox"] = (40, WaterBoxes.Values)
        };

        colliders = sources.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Source.GetTempList()
        );

        if (!log) return;

        foreach (KeyValuePair<string, (int Key, IEnumerable<ISM64Object> Source)> kvp in sources)
        {
            foreach (ISM64Object obj in kvp.Value.Source)
            {
                LogCollider(obj.Collider, kvp.Value.Key, obj.Collider.IsDestroyed);
            }
        }
    }

    public void ReloadAllColliders(bool log = true)
    {
        World.RootSlot.ForeachComponentInChildren<Collider>(c =>
        {
            TryRemoveCollider(c);
            HandleCollider(c, log);
        });
    }
}