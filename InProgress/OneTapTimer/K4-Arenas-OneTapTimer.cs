using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

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
        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventWeaponFire>(OnPlayerShoot);
        RegisterEventHandler<EventWeaponFire>(OnPlayerShootPre, HookMode.Pre);
        RegisterEventHandler<EventRoundFreezeEnd>(OnEndFreeze);
        RegisterListener<Listeners.OnTick>(OnTick);
        Logger.LogInformation("Started plugin!");

        AddCommand("css_tap", "Responds to the caller with \"pong\"", (player, commandInfo) =>
        {
            Logger.LogInformation("T1:");
            foreach (var p in t1)
            {
                Logger.LogInformation($"Gracz: {p.Value.controller.PlayerName}");
            }
            Logger.LogInformation("T2:");
            foreach (var p in t2)
            {
                Logger.LogInformation($"Gracz: {p.Value.controller.PlayerName}");
            }
        });
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
        foreach(var p in team1)
        {
            if (t1.ContainsKey(p)) { continue; }
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_ak47");
            t1.Add(p, new PlayerInfo
            {
                controller = p,
                ifShot = false,
                timer = null,
                placement = checkApi.GetArenaPlacement(p),
                timerSec = 5,
            });
            var playerWeapon = p.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
            playerWeapon!.Clip1 = 1; playerWeapon.ReserveAmmo.Fill(0);
        }
        foreach (var p in team2)
        {
            if (t2.ContainsKey(p)) { continue; }
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_ak47");
            t2.Add(p, new PlayerInfo
            {
                controller = p,
                ifShot = false,
                timer = null,
                placement = checkApi.GetArenaPlacement(p),
                timerSec = 5,
            });
            var playerWeapon = p.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
            playerWeapon!.Clip1 = 1; playerWeapon.ReserveAmmo.Fill(0);
        }
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {

        return HookResult.Continue;
    }

    public HookResult OnPlayerShoot(EventWeaponFire @event, GameEventInfo info)
    {
        // Pobierz strzelającego gracza
        CCSPlayerController? shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;

        // Sprawdź, w której drużynie jest gracz
        if (t1.TryGetValue(shooter, out PlayerInfo? shooterInfo))
        {
            if (shooterInfo.ifShot == true && shooterInfo.timer == null) 
            {
                Server.PrintToChatAll("Juz strzeliles!");
                return HookResult.Handled;
            }
            // Strzelający jest w drużynie 1, sprawdzamy przeciwników z t2
            foreach (var enemy in t2.Values)
            {
                if (enemy.placement == shooterInfo.placement)
                {
                    if(shooterInfo.ifShot == false && shooterInfo.timer != null)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                        shooterInfo.ifShot = true;
                        shooterInfo.timer.Kill();
                        enemy.controller.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.Clip1 = 1;
                        Utilities.SetStateChanged(enemy.controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!, "CBasePlayerWeapon", "m_iClip1");
                        enemy.timerSec = 5;
                        enemy.ifShot = false;
                        if(enemy.timer != null)
                        {
                            enemy.timer.Kill();
                            enemy.timer = AddTimer(1.0f, () =>
                            {
                                enemy.timerSec = enemy.timerSec - 1;
                            }, TimerFlags.REPEAT);
                        }
                    }
                    
                    // Możesz tu dodać dodatkową logikę np. anulowanie strzału
                }
            }
        }
        else if (t2.TryGetValue(shooter, out shooterInfo))
        {
            if (shooterInfo.ifShot == true && shooterInfo.timer == null)
            {
                Server.PrintToChatAll("Juz strzeliles!");
                return HookResult.Handled;
            }
            // Strzelający jest w drużynie 2, sprawdzamy przeciwników z t1
            foreach (var enemy in t1.Values)
            {
                if (enemy.placement == shooterInfo.placement)
                {
                    if (shooterInfo.ifShot == false && shooterInfo.timer != null)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                        enemy.controller.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.Clip1 = 1;
                        Utilities.SetStateChanged(enemy.controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!, "CBasePlayerWeapon", "m_iClip1");

                    }
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerShootPre(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;
        if (t1.TryGetValue(shooter, out PlayerInfo? shooterInfo))
        {
            if (shooterInfo.ifShot == true && shooterInfo.timer == null)
            {
                Server.PrintToChatAll("Juz strzeliles!");
                return HookResult.Handled;
            }
        }
        else if (t2.TryGetValue(shooter, out shooterInfo))
        {
            if (shooterInfo.ifShot == true && shooterInfo.timer == null)
            {
                Server.PrintToChatAll("Juz strzeliles!");
                return HookResult.Handled;
            }
        }
        return HookResult.Continue;
    }
    public HookResult OnEndFreeze(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        foreach (var p in t1)
        {
            if (p.Value.timer == null)
            {
                p.Value.timer = AddTimer(1.0f, () =>
                {
                    p.Value.timerSec = p.Value.timerSec - 1;
                    Server.PrintToChatAll($"Koniec timer'a 1 - {p.Value.controller.PlayerName}");
                }, TimerFlags.REPEAT);
            }
        }
        foreach (var p in t2)
        {
            if (p.Value.timer == null)
            {
                p.Value.timer = AddTimer(1.0f, () =>
                {
                    p.Value.timerSec = p.Value.timerSec - 1;
                    Server.PrintToChatAll($"Koniec timer'a 2 - {p.Value.controller.PlayerName}");
                });
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
    private void OnTick()
    {
        int barWidth = 20; // Szerokość paska
        if (t1 == null || t2 == null) return;

        foreach (var p in t1.Concat(t2))
        {
            if (p.Value == null || p.Value.controller == null) continue;
            if (p.Value.controller.IsBot || p.Value.controller.IsHLTV) continue;

            if (p.Value.timerSec <= 0)
            {
                if (p.Value.timer != null)
                {
                    p.Value.timer.Kill();
                    p.Value.timer = null;
                }
                p.Value.timerSec = 0; // Zabezpieczenie przed ujemnymi wartościami
            }
            else
            {
                p.Value.timerSec = Math.Max(0, p.Value.timerSec - 1); // Chroni przed ujemnymi wartościami

                float timeLeft = p.Value.timerSec;
                int numBars = Math.Max(0, (int)((timeLeft / 5.0f) * barWidth)); // Chroni przed ujemnymi wartościami

                string progressBar = new string('|', numBars) + new string(' ', barWidth - numBars);
                p.Value.controller.PrintToCenter($"> {progressBar} <");
            }
        }
    }




    public class PlayerInfo
    {
        public CCSPlayerController? controller { get; set; }
        public bool ifShot { get; set; }
        public CounterStrikeSharp.API.Modules.Timers.Timer? timer { get; set; }
        public float timerSec { get; set; }
        public int? placement { get; set; }
    }

}

