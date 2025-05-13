
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;

namespace K4ArenaOnlyHS;

[MinimumApiVersion(205)]
public class PluginK4ArenaOnlyHS : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Deagle-Discipline";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.1";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private Dictionary<ulong, bool> playerHitMap = new();
    private List<CCSPlayerController>? t1 = new List<CCSPlayerController>();
    private List<CCSPlayerController>? t2 = new List<CCSPlayerController>();
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("Deagle-Discipline", 1, true, RoundStart, RoundEnd);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
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
            p.GiveNamedItem(CsItem.Deagle);
            t1!.Add(p);
        }
        foreach(var p in team2){
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(CsItem.Deagle);
            t2!.Add(p);
        }
    }

    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null || shooter.SteamID == 0) return HookResult.Continue;

        if (!t1!.Contains(shooter) && !t2!.Contains(shooter)) return HookResult.Continue;


        ulong steamId = shooter.SteamID;
        var pawn = shooter.PlayerPawn.Value;
        if (pawn == null) return HookResult.Continue;

        var minusHP = 15;

        playerHitMap[steamId] = false;

        AddTimer(0.1f, () =>
        {
            if (playerHitMap.TryGetValue(steamId, out bool didHit) && !didHit)
            {
                if(pawn.Health <= minusHP)
                {
                    pawn.CommitSuicide(true, false);
                }
                pawn.Health -= minusHP;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
            playerHitMap.Remove(steamId);
        });

        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        if (attacker == null || attacker.SteamID == 0) return HookResult.Continue;
        if (!t1!.Contains(attacker) && !t2!.Contains(attacker)) return HookResult.Continue;



        ulong steamId = attacker.SteamID;
        playerHitMap[steamId] = true;

        return HookResult.Continue;
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
        if (playerHitMap != null) 
        {
            playerHitMap.Clear();
        }
        return;
    }


}

