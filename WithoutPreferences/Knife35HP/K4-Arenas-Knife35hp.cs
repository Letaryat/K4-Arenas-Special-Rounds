
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;

namespace K4ArenaKnife35HP;

[MinimumApiVersion(205)]
public class PluginK4ArenaKnife35HP : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Knife 35hp";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.1";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private List<CCSPlayerController>? t1 = new List<CCSPlayerController>();
    private List<CCSPlayerController>? t2 = new List<CCSPlayerController>();
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("35HP Knife", 1, true, RoundStart, RoundEnd);
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

    public void RoundStart(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }

        foreach(var p in team1){
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            Server.NextFrame(() =>
            {
                p.PlayerPawn.Value!.Health = 35;
                Utilities.SetStateChanged(p.PlayerPawn!.Value!, "CBaseEntity", "m_iHealth");
            });

            t1!.Add(p);
        }
        foreach(var p in team2){
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.Health = 35;
            Server.NextFrame(() =>
            {
                p.PlayerPawn.Value!.Health = 35;
                Utilities.SetStateChanged(p.PlayerPawn!.Value!, "CBaseEntity", "m_iHealth");
            });
            t2!.Add(p);
        }
    }

    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        if(team1 != null){
            t1!.Clear();
        }
        if(team2 != null){
            t2!.Clear();
        }
        return;
    }


}

