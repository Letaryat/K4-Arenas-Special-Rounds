using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;

namespace K4ArenaOnlyHSPistolPreference;

[MinimumApiVersion(205)]
public class PluginK4ArenaOnlyHSPistolPreference : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - OnlyHS Pistol";
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
            RoundTypeID = checkAPI.AddSpecialRound("Headshoty - Pistolety", 1, true, RoundStart, RoundEnd);
            RegisterEventHandler<EventPlayerHurt>(OnHurt, HookMode.Pre);
        }
    }

    public override void Unload(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            checkAPI.RemoveSpecialRound(RoundTypeID);
        }

    }

    public static CsItem GetRandomPistol()
    {
        List<CsItem> pistolItems =
        [
            CsItem.Deagle,
            CsItem.Glock,
            CsItem.USPS,
            CsItem.HKP2000,
            CsItem.Elite,
            CsItem.Tec9,
            CsItem.P250,
            CsItem.CZ,
            CsItem.FiveSeven,
            CsItem.Revolver
        ];
        return pistolItems[Random.Shared.Next(0, pistolItems.Count)];
}


    public void RoundStart(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }

        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        foreach (var p in team1){
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Pistol"] ?? GetRandomPistol();
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(rifle);
            t1!.Add(p);
        }
        foreach(var p in team2){
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Pistol"] ?? GetRandomPistol();
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(rifle);
            t2!.Add(p);
        }
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

