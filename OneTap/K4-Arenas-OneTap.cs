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
            p.GiveNamedItem("weapon_ak47");
            t1.Add(p, new PlayerInfo
            {
                controller = p,
                ifShot = false,
                timer = null,
                placement = checkApi.GetArenaPlacement(p),
                timerSec = 5,
            });
        }
        foreach (var p in team2)
        {
            if (t1.ContainsKey(p)) { continue; }
            p.GiveNamedItem("weapon_ak47");
            t2.Add(p, new PlayerInfo
            {
                controller = p,
                ifShot = false,
                timer = null,
                placement = checkApi.GetArenaPlacement(p),
                timerSec = 5,
            });
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
            // Strzelający jest w drużynie 1, sprawdzamy przeciwników z t2
            foreach (var enemy in t2.Values)
            {
                if (enemy.placement == shooterInfo.placement)
                {
                    if(shooterInfo.ifShot == false && shooterInfo.timer != null)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                        shooterInfo.ifShot = true;
                        shooterInfo.timer = null;
                        enemy.timerSec = 5;
                        enemy.ifShot = false;
                    }
                    
                    // Możesz tu dodać dodatkową logikę np. anulowanie strzału
                }
            }
        }
        else if (t2.TryGetValue(shooter, out shooterInfo))
        {
            // Strzelający jest w drużynie 2, sprawdzamy przeciwników z t1
            foreach (var enemy in t1.Values)
            {
                if (enemy.placement == shooterInfo.placement)
                {
                    if (shooterInfo.ifShot == false && shooterInfo.timer != null)
                    {
                        Server.PrintToChatAll($"Gracz {shooter.PlayerName} strzelił do przeciwnika na tej samej arenie! {enemy.controller.PlayerName}");
                    }
                }
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
                p.Value.timer = AddTimer(5.0f, () =>
                {
                    p.Value.timerSec = p.Value.timerSec - 1;
                    Server.PrintToChatAll($"Koniec timer'a 1 - {p.Value.controller.PlayerName}");
                    if(p.Value.timerSec == 0)
                    {
                        p.Value.timer.Kill();
                    }
                }, TimerFlags.REPEAT);
            }
        }
        foreach (var p in t2)
        {
            if (p.Value.timer == null)
            {
                p.Value.timer = AddTimer(5.0f, () =>
                {
                    p.Value.timerSec = p.Value.timerSec - 1;
                    Server.PrintToChatAll($"Koniec timer'a 2 - {p.Value.controller.PlayerName}");
                    if (p.Value.timerSec == 0)
                    {
                        p.Value.timer.Kill();
                    }
                });
            }
        }
        return HookResult.Continue;
    }
    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        //tapPlayers.Clear();
        return;
    }
    private void OnTick()
    {
        int barWidth = 20; // Ustalona szerokość paska

        foreach (var p in t1.Concat(t2))
        {
            float timeLeft = p.Value.timerSec;  // Pozostały czas w sekundach

            // Obliczamy liczbę kresek proporcjonalnie do czasu (jeśli 5 sek = cały pasek)
            int numBars = (int)((timeLeft / 5.0f) * barWidth);

            // Tworzymy pasek postępu (| dla wypełnionych, spacja dla pustych)
            string progressBar = new string('|', numBars) + new string(' ', barWidth - numBars);

            // Wyświetlamy HUD
            p.Value.controller.PrintToCenter($"> {progressBar} <");

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

