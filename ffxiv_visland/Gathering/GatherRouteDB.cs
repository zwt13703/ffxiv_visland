﻿using Dalamud.Game.ClientState.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using visland.Helpers;

namespace visland.Gathering;

public class GatherRouteDB : Configuration.Node
{
    public enum Movement
    {
        Normal = 0,
        MountFly = 1,
        MountNoFly = 2,
    }

    public enum InteractionType
    {
        None = 0,
        Standard = 1,
        Emote = 2,
        UseItem = 3,
        UseAction = 4,
        QuestTalk = 5,
        Grind = 6,
    }

    public class Waypoint
    {
        public Vector3 Position;
        public int ZoneID;
        public float Radius;
        public Movement Movement;
        public bool Pathfind = true;
        public uint InteractWithOID = 0;
        public string InteractWithName = "";

        public bool showInteractions;
        public InteractionType Interaction = InteractionType.Standard;
        public int EmoteID;
        public int ItemID;
        public int ActionID;
        public int QuestID;
        public int MobID;

        public bool showWaits;
        public ConditionFlag WaitForCondition;
        public int WaitTimeMs;
    }

    public class Route
    {
        public string Name = "";
        public List<Waypoint> Waypoints = [];
    }

    public List<Route> Routes = [];
    public float DefaultWaypointRadius = 3;
    public float DefaultInteractionRadius = 2;
    public bool GatherModeOnStart = true;
    public bool DisableOnErrors = false;
    public bool WasFlyingInManual = false;
    public bool WasStandard = false;

    public override void Deserialize(JObject j, JsonSerializer ser)
    {
        Routes.Clear();
        if (j["Routes"] is JArray ja)
        {
            foreach (var jr in ja)
            {
                var jn = jr["Name"]?.Value<string>();
                if (jn != null && jr["Waypoints"] is JArray jw)
                    Routes.Add(new Route() { Name = jn, Waypoints = LoadFromJSONWaypoints(jw) });
            }
        }
        DisableOnErrors = (bool?)j["DisableOnErrors"] ?? false;
        GatherModeOnStart = (bool?)j["GatherModeOnStart"] ?? true;
        DefaultWaypointRadius = (float?)j["DefaultWaypointRadius"] ?? 3;
        DefaultInteractionRadius = (float?)j["DefaultInteractionRadius"] ?? 2;
    }

    public override JObject Serialize(JsonSerializer ser)
    {
        JArray res = [];
        foreach (var r in Routes)
        {
            res.Add(new JObject()
            {
                { "Name", r.Name },
                { "Waypoints", SaveToJSONWaypoints(r.Waypoints) }
            });
        }
        return new JObject() {
            { "Routes", res },
            { "DisableOnErrors", DisableOnErrors },
            { "GatherModeOnStart", GatherModeOnStart },
            { "DefaultWaypointRadius", DefaultWaypointRadius },
            { "DefaultInteractionRadius", DefaultInteractionRadius }
        };
    }

    public static JArray SaveToJSONWaypoints(List<Waypoint> waypoints)
    {
        JArray jw = [];
        foreach (var wp in waypoints)
            jw.Add(new JArray()
            {
                wp.Position.X,
                wp.Position.Y,
                wp.Position.Z,
                wp.Radius,
                wp.InteractWithName,
                wp.Movement,
                wp.InteractWithOID,
                wp.showInteractions,
                wp.Interaction,
                wp.EmoteID,
                wp.ActionID,
                wp.ItemID,
                wp.showWaits,
                wp.WaitTimeMs,
                wp.WaitForCondition,
                wp.Pathfind,
                wp.MobID,
            });
        return jw;
    }

    public static List<Waypoint> LoadFromJSONWaypoints(JArray j)
    {
        List<Waypoint> res = [];
        foreach (var jwe in j)
        {
            if (jwe is not JArray jwea || jwea.Count < 5)
                continue;
            var movement = jwea.Count <= 5 ? Movement.Normal
                : jwea[5].Type == JTokenType.Boolean ? jwea[5].Value<bool>() ? Movement.MountFly : Movement.Normal
                : (Movement)jwea[5].Value<int>();
            res.Add(new()
            {
                Position = new(jwea[0].Value<float>(), jwea[1].Value<float>(), jwea[2].Value<float>()),
                Radius = jwea[3].Value<float>(),
                Movement = movement,
                InteractWithOID = jwea.Count > 6 ? jwea[6].Value<uint>() : 2012985,
                InteractWithName = jwea[4].Value<string>() ?? "",
                showInteractions = jwea[7].Value<bool>(),
                Interaction = jwea.Count > 8 ? (InteractionType)jwea[8].Value<int>() : InteractionType.Standard,
                EmoteID = jwea[9].Value<int>(),
                ActionID = jwea[10].Value<int>(),
                ItemID = jwea[11].Value<int>(),
                showWaits = jwea[12].Value<bool>(),
                WaitTimeMs = jwea[13].Value<int>(),
                WaitForCondition = jwea.Count > 14 ? (ConditionFlag)jwea[14].Value<int>() : ConditionFlag.None,
                Pathfind = jwea.ElementAtOrDefault(15)?.Value<bool>() ?? false,
                MobID = jwea.ElementAtOrDefault(16)?.Value<int>() ?? default,
            });
        }
        return res;
    }
}
