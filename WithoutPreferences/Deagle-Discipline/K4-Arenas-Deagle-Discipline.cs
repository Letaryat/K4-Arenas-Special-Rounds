
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace K4ArenaDeagleDiscipline;

[MinimumApiVersion(205)]
public class PluginK4ArenaDeagleDiscipline : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Deagle-Discipline";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.1";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private Dictionary<int, bool> playerHitMap = new();
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

        playerHitMap.Clear();

        foreach (var p in team1)
        {
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(CsItem.Deagle);
            playerHitMap.TryAdd(p.Slot, true);
            t1!.Add(p);
        }
        foreach(var p in team2){
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(CsItem.Deagle);
            playerHitMap.TryAdd(p.Slot, true);
            t2!.Add(p);
        }
    }


    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;

        if (!t1!.Contains(shooter) && !t2!.Contains(shooter)) return HookResult.Continue;

        var slot = shooter.Slot;

        var pawn = shooter.PlayerPawn.Value;
        if (pawn == null ) return HookResult.Continue;

        if (pawn.WeaponServices == null) return HookResult.Continue;
        var weapon = pawn.WeaponServices.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid) return HookResult.Continue;
        if (weapon.DesignerName != null && weapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase)) return HookResult.Continue;

        var minusHP = 15;

        playerHitMap[slot] = false;

        if (pawn.Health <= minusHP)
        {
            pawn.CommitSuicide(true, false);
            return HookResult.Continue;
        }

        AddTimer(0.1f, () =>
        {
            if (playerHitMap.TryGetValue(slot, out bool didHit) && !didHit)
            {
                    pawn.Health -= minusHP;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
            playerHitMap.Remove(slot);
        });

        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        if (attacker == null || attacker.SteamID == 0) return HookResult.Continue;
        if (!t1!.Contains(attacker) && !t2!.Contains(attacker)) return HookResult.Continue;

        var slot = attacker.Slot;

        playerHitMap[slot] = true;

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

