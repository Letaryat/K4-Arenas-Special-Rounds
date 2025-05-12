
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;

namespace K4ArenaOnlyHS;

[MinimumApiVersion(205)]
public class PluginK4ArenaOnlyHS : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Noscope";
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
            RoundTypeID = checkAPI.AddSpecialRound("Noscope", 1, true, RoundStart, RoundEnd);
            RegisterListener<Listeners.OnTick>(OnTick);
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
            CsItem.AWP,
            CsItem.SSG08,
            CsItem.SCAR20,
            CsItem.G3SG1,
        ];
        return Items[Random.Shared.Next(0, Items.Count)];
    }

    public void RoundStart(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }

        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        foreach (var p in team1){
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var sniper = playerPrefernce["Sniper"] ?? GetRandomItem();
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(sniper);
            NoScopeMethod(p);
            t1!.Add(p);
        }
        foreach(var p in team2){
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var sniper = playerPrefernce["Sniper"] ?? GetRandomItem();
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(sniper);
            NoScopeMethod(p);
            t2!.Add(p);
        }
    }

    public void NoScopeMethod(CCSPlayerController p)
    {
        var playerWeapon = p.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;
        var baseWeapon = playerWeapon.Value!.As<CCSWeaponBase>();
        baseWeapon.NextSecondaryAttackTick = Server.TickCount + 500;
    }

    private void OnTick()
    {
        foreach(var player in Utilities.GetPlayers())
        {
            if(t1!.Contains(player) || t2!.Contains(player))
            {
                var ActiveWeaponName = player.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!.DesignerName;
                if (ActiveWeaponName.Contains("weapon_ssg08") || ActiveWeaponName.Contains("weapon_awp")
                || ActiveWeaponName.Contains("weapon_scar20") || ActiveWeaponName.Contains("weapon_g3sg1"))
                {
                    player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!.NextSecondaryAttackTick = Server.TickCount + 500;
                    var buttons = player.Buttons;
                }
            }
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

