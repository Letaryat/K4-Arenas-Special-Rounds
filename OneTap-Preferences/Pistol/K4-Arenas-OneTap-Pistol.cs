using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System;


namespace K4_Arenas_OneTap;

[MinimumApiVersion(205)]
public class PluginK4_Arenas_OneTap: BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - OneTap Pistol";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    public IK4ArenaSharedApi? checkApi;
    private Dictionary <CCSPlayerController, PlayerInfo> t1 = new Dictionary<CCSPlayerController, PlayerInfo>();
    private Dictionary<CCSPlayerController, PlayerInfo> t2 = new Dictionary<CCSPlayerController, PlayerInfo>();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();
        checkApi = checkAPI;
        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("OneTap - Pistolety", 1, true, RoundStart, RoundEnd);
        }
        else
        {
            Logger.LogError("Failed to get shared API capability for K4-Arenas.");
        }
        
    }
    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWeaponFire>(OnPlayerShoot);
        AddCommandListener("changelevel", ListenerChangeLevel, HookMode.Pre);
        AddCommandListener("map", ListenerChangeLevel, HookMode.Pre);
        AddCommandListener("host_workshop_map", ListenerChangeLevel, HookMode.Pre);
        AddCommandListener("ds_workshop_changelevel", ListenerChangeLevel, HookMode.Pre);
        Logger.LogInformation("Started plugin!");
    }

    public static CsItem GetRandomItem()
    {
        List<CsItem> Items =
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
        return Items[Random.Shared.Next(0, Items.Count)];
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
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (team1 == null || team2 == null) return;
            foreach (var p in team1)
            {
                if (t1.ContainsKey(p)) { continue; }
                t1!.Add(p, new PlayerInfo
                {
                    controller = p,
                    placement = checkApi!.GetArenaPlacement(p),
                });
                var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
                var pistol = playerPrefernce["Pistol"] ?? GetRandomItem();
                p.RemoveWeapons();
                p.GiveNamedItem("weapon_knife");
                p.GiveNamedItem(pistol);
                OneClip(p);
            }
            foreach (var p in team2)
            {
                if (t2.ContainsKey(p)) { continue; }
                t2!.Add(p, new PlayerInfo
                {
                    controller = p,
                    placement = checkApi!.GetArenaPlacement(p),
                });
                var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
                var pistol = playerPrefernce["Pistol"] ?? GetRandomItem();
                p.RemoveWeapons();
                p.GiveNamedItem("weapon_knife");
                p.GiveNamedItem(pistol);
                OneClip(p);
            }
    }

    public HookResult OnPlayerShoot(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;
        if (shooter.PlayerPawn.Value == null) return HookResult.Continue;
        if(t1 == null || t2 == null) return HookResult.Continue;
        var weapon = @event.Weapon;
        if (!shooter.PlayerPawn.Value.WeaponServices!.ActiveWeapon.IsValid) return HookResult.Continue;
        if (t1.TryGetValue(shooter, out PlayerInfo? shooterInfo))
            {
                foreach (var enemy in t2.Values)
                {
                    if (enemy.placement == shooterInfo.placement)
                    {
                        var activeWeapon = enemy.controller!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
                        if (activeWeapon != null && activeWeapon.IsValid && !string.IsNullOrEmpty(activeWeapon.DesignerName))
                        {
                            OneClip(enemy.controller);
                        } 
                    }
                }
            }
            else if (t2.TryGetValue(shooter, out shooterInfo))
            {
                foreach (var enemy in t1.Values)
                {
                    if (enemy.placement == shooterInfo.placement)
                    {
                    var activeWeapon = enemy.controller!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
                    if (activeWeapon != null && activeWeapon.IsValid && !string.IsNullOrEmpty(activeWeapon.DesignerName))
                    {
                        OneClip(enemy.controller);
                    }
                }
            }
            }


        return HookResult.Continue;
    }
    /*
    public void OneClip(CCSPlayerController player, CsItem weaponName1)
    {
        try
        {
            var weaponName = $"weapon_{weaponName1.ToString().ToLower()}";
            if (player.PlayerPawn == null || player.PlayerPawn.Value!.WeaponServices == null || player.PlayerPawn.Value.WeaponServices.MyWeapons == null) { return; }
            var weapon = player.PlayerPawn.Value!.WeaponServices!.MyWeapons.Where(x => x.Value?.DesignerName == weaponName).FirstOrDefault();
            CCSWeaponBase _weapon = weapon!.Value!.As<CCSWeaponBase>();

            if(weapon.Value.DesignerName)

            if (weapon != null && weapon.IsValid)
            {

                if (weapon.Value!.Clip1 != 1)
                {
                    weapon.Value!.Clip1 = 1;
                    weapon.Value.Clip2 = 0;
                }
                if (weapon.Value.ReserveAmmo[0] != 0)
                {
                    weapon.Value.ReserveAmmo.Fill(0);
                }
                Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_iClip1");
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Onetap-Error: {ex}");
        }

    }
    */
    public void OneClip(CCSPlayerController player)
    {
        try
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || pawn.WeaponServices?.MyWeapons == null)
                return;

            foreach (var wepHandle in pawn.WeaponServices.MyWeapons)
            {
                var weapon = wepHandle.Value;
                if (weapon == null || !weapon.IsValid)
                    continue;

                // Pomijamy noże
                if (weapon.DesignerName != null && weapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase))
                    continue;

                var baseWeapon = weapon.As<CCSWeaponBase>();
                if (baseWeapon != null)
                {
                    baseWeapon.Clip1 = 1;
                    baseWeapon.Clip2 = 0;
                    baseWeapon.ReserveAmmo.Fill(0);

                    Utilities.SetStateChanged(baseWeapon, "CBasePlayerWeapon", "m_iClip1");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"OneClipAllWeapons Error: {ex}");
        }
    }

    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        t1.Clear();
        t2.Clear();
        return;
    }

    public HookResult ListenerChangeLevel(CCSPlayerController? player, CommandInfo info)
    {
        if(t1 == null || t2 == null) { return HookResult.Continue; }
        t1.Clear();
        t2.Clear();
        return HookResult.Continue;
    }

    public class PlayerInfo
    {
        public CCSPlayerController? controller { get; set; }
        public int? placement { get; set; }
    }

}

