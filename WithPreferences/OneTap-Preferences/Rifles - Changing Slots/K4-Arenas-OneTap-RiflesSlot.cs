using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System;


namespace K4_Arenas_OneTap;

[MinimumApiVersion(205)]
public class PluginK4_Arenas_OneTap : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - OneTap Rifles";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    public IK4ArenaSharedApi? checkApi;
    private Dictionary<CCSPlayerController, PlayerInfo> t1 = new Dictionary<CCSPlayerController, PlayerInfo>();
    private Dictionary<CCSPlayerController, PlayerInfo> t2 = new Dictionary<CCSPlayerController, PlayerInfo>();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();
        checkApi = checkAPI;
        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("OneTapSlotChange", 1, true, RoundStart, RoundEnd);
        }
        else
        {
            Logger.LogError("Failed to get shared API capability for K4-Arenas.");
        }

    }
    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWeaponFire>(OnPlayerShoot, HookMode.Post);
        //RegisterListener<Listeners.OnTick>(OnTick);
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
            CsItem.AK47,
            CsItem.M4A1S,
            CsItem.M4A1,
            CsItem.GalilAR,
            CsItem.Famas,
            CsItem.SG556,
            CsItem.AUG,
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
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Rifle"] ?? GetRandomItem();
            t1!.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkApi!.GetArenaPlacement(p),
                item = rifle,
            });

            p.RemoveWeapons();
            p.GiveNamedItem("weapon_knife");
            p.GiveNamedItem(rifle);
            OneClip(p);
            SetPlayerHP(p);
        }
        foreach (var p in team2)
        {
            if (t2.ContainsKey(p)) { continue; }
            var playerPrefernce = checkAPI!.GetPlayerWeaponPreferences(p);
            var rifle = playerPrefernce["Rifle"] ?? GetRandomItem();
            t2!.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkApi!.GetArenaPlacement(p),
                item = rifle,
            });

            p.RemoveWeapons();
            p.GiveNamedItem("weapon_knife");
            p.GiveNamedItem(rifle);
            OneClip(p);
            SetPlayerHP(p);
        }
    }

    public HookResult OnPlayerShoot(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null || !shooter.IsValid || shooter.PlayerPawn.Value == null)
            return HookResult.Continue;

        if (shooter.PlayerPawn.Value.WeaponServices == null) return HookResult.Continue;
        var weapon = shooter.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid) return HookResult.Continue;
        if (weapon.DesignerName != null && weapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase)) return HookResult.Continue;

        var placement = checkApi!.GetArenaPlacement(shooter);

        if (t1.TryGetValue(shooter, out var shooterInfo))
        {
            var enemy = t2.Values.FirstOrDefault(e => e.placement == placement);
            if (enemy != null)
            {
                RemoveAllWeapons(shooter);
                AddTimer(0.15f, () =>
                {
                    GiveWeaponToPlayer(enemy);
                });
                
            }
        }
        else if (t2.TryGetValue(shooter, out shooterInfo))
        {
            var enemy = t1.Values.FirstOrDefault(e => e.placement == placement);
            if (enemy != null)
            {
                RemoveAllWeapons(shooter);
                AddTimer(0.15f, () =>
                {
                    GiveWeaponToPlayer(enemy);
                });
            }
        }

        return HookResult.Continue;
    }
    private void GiveWeaponToPlayer(PlayerInfo p)
    {
        var controller = p.controller;
        if (controller == null || !controller.IsValid) return;

       foreach (var gun in controller.PlayerPawn.Value!.WeaponServices!.MyWeapons)
        {
            if (gun != null && gun.Value != null && gun.Value.IsValid)
            {
                CCSWeaponBaseVData? weaponData = gun.Value.As<CCSWeaponBase>().VData;
                if (weaponData == null) continue;
                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE)
                {
                    return;
                }
            }
        }

        Server.NextFrame(() =>
        {
            controller.GiveNamedItem(p.item!.Value);
            OneClip(controller);

            AddTimer(0.1f, () =>
            {
                NativeAPI.IssueClientCommand(controller.Slot, "slot1");
            });
        });
    }

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

    public void DropWeapon(CCSPlayerController player, string weaponName, bool removeWeapon = true)
    {
        CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;

        if (weaponServices == null)
            return;

        var matchedWeapon = weaponServices.MyWeapons
        .FirstOrDefault(w => w?.IsValid == true && w.Value != null && w.Value.DesignerName == weaponName);

        try
        {
            if (matchedWeapon?.IsValid == true)
            {
                weaponServices.ActiveWeapon.Raw = matchedWeapon.Raw;

                CBaseEntity? weaponEntity = weaponServices.ActiveWeapon.Value?.As<CBaseEntity>();
                if (weaponEntity == null || !weaponEntity.IsValid)
                    return;

                player.DropActiveWeapon();
                if (removeWeapon) weaponEntity?.AddEntityIOEvent("Kill", weaponEntity, null, "", 0.1f);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while Refreshing Weapon via className: {ex}", ex.Message);
        }
    }
    private void RemovePlayerWeapon(CCSPlayerController player, bool primary = false, bool secondary = false, bool grenades = false)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null || player.PlayerPawn.Value!.WeaponServices!.MyWeapons == null) return;

        foreach (var gun in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
        {
            if (gun != null && gun.Value != null && gun.Value.IsValid)
            {
                CCSWeaponBaseVData? weaponData = gun.Value.As<CCSWeaponBase>().VData;
                if (weaponData == null) continue;
                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE && primary)
                {
                    DropWeapon(player, gun.Value.DesignerName);
                }
                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_PISTOL && secondary)
                {
                    DropWeapon(player, gun.Value.DesignerName);
                }
                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_GRENADES && grenades)
                {
                    DropWeapon(player, gun.Value.DesignerName);
                }
            }
        }
    }
    private void RemoveAllWeapons(CCSPlayerController player)
    {
        NativeAPI.IssueClientCommand(player.Slot, $"slot3");
        AddTimer(0.1f, () =>
        {
            RemovePlayerWeapon(player, true);
        });
        
    }

    public void SetPlayerHP(CCSPlayerController p)
    {
        if (p == null) return;
        if (p.PlayerPawn.Value == null) return;
        Server.NextFrame(() =>
        {
            p.PlayerPawn.Value!.Health = 1;
            Utilities.SetStateChanged(p.PlayerPawn!.Value!, "CBaseEntity", "m_iHealth");
        });
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
        if (t1 == null || t2 == null) { return HookResult.Continue; }

        t1.Clear();
        t2.Clear();
        return HookResult.Continue;
    }

    public class PlayerInfo
    {
        public CCSPlayerController? controller { get; set; }
        public int? placement { get; set; }
        public CsItem? item { get; set; }
    }

}

