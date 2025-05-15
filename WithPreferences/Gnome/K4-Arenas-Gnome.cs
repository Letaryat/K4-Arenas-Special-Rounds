
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Numerics;
using static System.Formats.Asn1.AsnWriter;

namespace K4ArenaOnlyHS;

[MinimumApiVersion(205)]
public class PluginK4ArenaOnlyHS : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Gnome";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private List<CCSPlayerController>? t1 = new List<CCSPlayerController>();
    private List<CCSPlayerController>? t2 = new List<CCSPlayerController>();
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("Gnome", 1, true, RoundStart, RoundEnd);
        }
        else
            Logger.LogError("Failed to get shared API capability for K4-Arenas.");
    }

    public override void Unload(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            checkAPI.RemoveSpecialRound(RoundTypeID);
        }
        else
            Logger.LogError("Failed to get shared API capability for K4-Arenas.");
    }

    public static CsItem GetRandomItem()
    {
        List<CsItem> Items =
        [
            CsItem.AK47,
            CsItem.M4A1S,
            CsItem.M4A1,
            CsItem.GalilAR,
            CsItem.Famas,
            CsItem.SG556,
            CsItem.AUG,
        ];
        return Items[Random.Shared.Next(0, Items.Count)];
    }

    public void RoundStart(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }

        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        foreach (var p in team1){
            p.RemoveWeapons();
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Rifle"] ?? GetRandomItem();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(rifle);
            SetScale(p);
            t1!.Add(p);
        }
        foreach(var p in team2){
            p.RemoveWeapons();
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Rifle"] ?? GetRandomItem();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(rifle);
            SetScale(p);
            t2!.Add(p);
        }
    }

    public void SetScale(CCSPlayerController player)
    {
        if (player == null || player.PlayerPawn.Value == null) return;
        var sceneNode = player.PlayerPawn.Value!.CBodyComponent?.SceneNode;
        if (sceneNode != null)
        {
            sceneNode.GetSkeletonInstance().Scale = 0.3f;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, "0.3");

            Server.NextFrame(() =>
            {
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
        }
    }

    public void Resize(CCSPlayerController player)
    {
        if(player == null || player.PlayerPawn.Value == null) return;
        var sceneNode = player.PlayerPawn.Value!.CBodyComponent?.SceneNode;
        if (sceneNode != null)
        {
            sceneNode.GetSkeletonInstance().Scale = 1;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, "1");

            Server.NextFrame(() =>
            {
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
        }
    }

    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        if(team1 != null){
            foreach(var p in team1)
            {
                Resize(p);
            }
            t1!.Clear();
        }
        if(team2 != null){
            foreach (var p in team2)
            {
                Resize(p);
            }
            t2!.Clear();
        }
        return;
    }


}

