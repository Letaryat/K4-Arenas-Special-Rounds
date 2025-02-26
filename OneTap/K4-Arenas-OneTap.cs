using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;


namespace K4_Arenas_OneTap;

[MinimumApiVersion(205)]
public class PluginK4_Arenas_OneTap: BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - OneTap";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    public IK4ArenaSharedApi? checkApi;
    private CounterStrikeSharp.API.Modules.Timers.Timer? Timer { get; set; } = null;
    //private Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> PlayerTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer>();
    //private Dictionary<string, List<PlayerInfo>> tapPlayers = new Dictionary<string, List<PlayerInfo>>();
    public Dictionary <CCSPlayerController, PlayerInfo> t1 = new Dictionary<CCSPlayerController, PlayerInfo>();
    public Dictionary<CCSPlayerController, PlayerInfo> t2 = new Dictionary<CCSPlayerController, PlayerInfo>();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();
        checkApi = checkAPI;
        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("OneTap", 1, true, RoundStart, RoundEnd);
        }
        else
        {
            Logger.LogError("Failed to get shared API capability for K4-Arenas.");
        }
        
    }
    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWeaponFire>(OnPlayerShoot, HookMode.Post);
        Logger.LogInformation("Started plugin!");
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
        if (team1 == null || team2 == null) return;
        foreach (var p in team1)
        {
            if (t1.ContainsKey(p)) { continue; }
            t1.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkApi.GetArenaPlacement(p),
            });
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_knife");
            p.GiveNamedItem("weapon_ak47");
            var playerWeapon = p.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
            playerWeapon!.Clip1 = 1; playerWeapon.ReserveAmmo.Fill(0);
        }
        foreach (var p in team2)
        {
            if (t2.ContainsKey(p)) { continue; }
            t2.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkApi.GetArenaPlacement(p),
            });
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_knife");
            p.GiveNamedItem("weapon_ak47");
            var playerWeapon = p.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
            playerWeapon!.Clip1 = 1; playerWeapon.ReserveAmmo.Fill(0);
        }
    }

    public HookResult OnPlayerShoot(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;
        if (shooter.PlayerPawn.Value == null) return HookResult.Continue;
        if(t1 == null || t2 == null) return HookResult.Continue;
        if (!shooter.PlayerPawn.Value.WeaponServices!.ActiveWeapon.IsValid) return HookResult.Continue;

            if (t1.TryGetValue(shooter, out PlayerInfo? shooterInfo))
            {
                foreach (var enemy in t2.Values)
                {
                    if (enemy.placement == shooterInfo.placement)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                        enemy.controller!.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.Clip1 = 1;
                        Utilities.SetStateChanged(enemy.controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!, "CBasePlayerWeapon", "m_iClip1");
                    }
                }
            }
            else if (t2.TryGetValue(shooter, out shooterInfo))
            {
                foreach (var enemy in t1.Values)
                {
                    if (enemy.placement == shooterInfo.placement)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                        enemy.controller!.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.Clip1 = 1;
                        Utilities.SetStateChanged(enemy.controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!, "CBasePlayerWeapon", "m_iClip1");
                    }
                }
            }


        return HookResult.Continue;
    }

    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        t1.Clear();
        t2.Clear();
        return;
    }


    public class PlayerInfo
    {
        public CCSPlayerController? controller { get; set; }
        public int? placement { get; set; }
    }

}

