
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;

namespace K4ArenaOnlyHS;

[MinimumApiVersion(205)]
public class PluginK4ArenaOnlyHSUSP : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - OnlyHS-USP";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private bool isRoundActive;
    private List<CCSPlayerController>? t1;
    private List<CCSPlayerController>? t2;
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("OnlyHS-USP", 1, true, RoundStart, RoundEnd);
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

        team1[0].RemoveWeapons();
        team2[0].RemoveWeapons();

        /*
        * I have no fucking idea if this is a proper way to do it or if is it optimized
        * But I had no idea also how to pass Lists to OnHurt Method so if it works I am happy with that.
        */

        t1 = team1;
        t2 = team2;

        team1[0].GiveNamedItem(CsItem.Knife);
        team1[0].GiveNamedItem("weapon_usp_silencer");

        team2[0].GiveNamedItem(CsItem.Knife);
        team2[0].GiveNamedItem("weapon_usp_silencer");
        RegisterEventHandler<EventPlayerHurt>(OnHurt);

    }

    public HookResult OnHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var attacker = @event.Attacker;
        var DmgHealth = @event.DmgHealth;
        var DmgArmor = @event.DmgArmor;
        if (t1!.Contains(player!) && t2!.Contains(attacker!) || t1!.Contains(attacker!) && t2!.Contains(player!))
        {
            if (@event.Hitgroup != 1)
            {
                if (player!.PlayerPawn!.Value!.Health < 100)
                {
                    player.PlayerPawn.Value.Health += DmgHealth;
                }
                if (player!.PlayerPawn!.Value!.ArmorValue < 100)
                {
                    player.PlayerPawn.Value.ArmorValue += DmgArmor;
                }
                return HookResult.Continue;
            }
        }
        return HookResult.Continue;
    }
    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        DeregisterEventHandler<EventPlayerHurt>(OnHurt);
        if (team1 == null || team2 == null) { return; }
        t1!.Clear();
        t2!.Clear();
    }


}

