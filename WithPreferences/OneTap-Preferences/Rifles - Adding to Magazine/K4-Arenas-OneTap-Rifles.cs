
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace K4ArenaOneTapMagazine;

[MinimumApiVersion(205)]
public class PlayerInfo
{
    public CCSPlayerController? controller { get; set; }
    public int? placement { get; set; }
}
public class PluginK4ArenaOneTapMagazine : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Onetaps rilfes";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleVersion => "1.0.0";

    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    private Dictionary<CCSPlayerController, PlayerInfo> t1 = new Dictionary<CCSPlayerController, PlayerInfo>();
    private Dictionary<CCSPlayerController, PlayerInfo> t2 = new Dictionary<CCSPlayerController, PlayerInfo>();
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        if (checkAPI != null)
        {
            RoundTypeID = checkAPI.AddSpecialRound("Onetaps Magazine", 1, true, RoundStart, RoundEnd);
            RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);
            RegisterEventHandler<EventItemEquip>(OnWeaponEquip, HookMode.Pre);
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

        IK4ArenaSharedApi? checkAPI = Capability_SharedAPI.Get();

        foreach (var p in team1)
        {
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_ak47");
            t1!.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkAPI!.GetArenaPlacement(p),
            });

        }
        foreach (var p in team2)
        {
            p.RemoveWeapons();
            p.GiveNamedItem("weapon_ak47");
            t2!.Add(p, new PlayerInfo
            {
                controller = p,
                placement = checkAPI!.GetArenaPlacement(p),
            });

        }
    }

    private HookResult OnWeaponEquip(EventItemEquip @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || player.IsBot)
            return HookResult.Continue;
        var weapon = player.PlayerPawn?.Value?.WeaponServices!.ActiveWeapon!.Value;

        if(weapon == null) return HookResult.Continue;

        Server.PrintToChatAll($"Zmieniam bron: {weapon.DesignerName}");
        return HookResult.Continue;
    }
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var shooter = @event.Userid;
        if (shooter == null) return HookResult.Continue;

        if(shooter.PlayerPawn.Value == null) return HookResult.Continue;

        if (!t1.ContainsKey(shooter) && !t2.ContainsKey(shooter)) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            var gun = shooter.PlayerPawn.Value.WeaponServices!.ActiveWeapon!.Value;

            if (gun == null) return;

            gun.Clip1 = 0;
            gun.Clip2 = 0;
            gun.ReserveAmmo[0] = 0;

            Utilities.SetStateChanged(gun, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(gun, "CBasePlayerWeapon", "m_iClip2");
            Utilities.SetStateChanged(gun, "CBasePlayerWeapon", "m_pReserveAmmo");


            var time = (shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!.NextPrimaryAttackTick - Server.TickCount) / 0.08;
            gun.NextPrimaryAttackTick = Convert.ToInt32(time + Server.TickCount);

            Utilities.SetStateChanged(gun, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
        });

        if (t1.TryGetValue(shooter, out PlayerInfo? shooterInfo))
        {
            foreach (var enemy in t2.Values)
            {
                if (enemy.placement == shooterInfo.placement)
                {
                    var activeWeapon = enemy.controller!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value;
                    if (activeWeapon != null && activeWeapon.IsValid && !string.IsNullOrEmpty(activeWeapon.DesignerName))
                    {
                        Server.PrintToChatAll($"Gracz: {enemy.controller.PlayerName} Strzelacz: {shooter}");
                        activeWeapon.Clip1 = 1;

                        var time = (activeWeapon.NextPrimaryAttackTick - Server.TickCount) / 0.08;
                        activeWeapon.NextPrimaryAttackTick = Convert.ToInt32(time + Server.TickCount);

                        Server.NextFrame(() =>
                        {
                            Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
                        });

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
                        Server.PrintToChatAll($"Gracz: {enemy.controller.PlayerName} Strzelacz: {shooter}");
                        activeWeapon.Clip1 = 1;

                        var time = (activeWeapon.NextPrimaryAttackTick - Server.TickCount) / 0.08;
                        activeWeapon.NextPrimaryAttackTick = Convert.ToInt32(time + Server.TickCount);

                        Server.NextFrame(() =>
                        {
                            Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
                        });

                    }
                }
            }
        }

            return HookResult.Continue;
    }


    private void ChangePrimaryWeapon(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null || player.PlayerPawn.Value!.WeaponServices!.MyWeapons == null) return;

        foreach (var gun in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
        {
            if (gun != null && gun.Value != null && gun.Value.IsValid)
            {
                CCSWeaponBaseVData? weaponData = gun.Value.As<CCSWeaponBase>().VData;
                if (weaponData == null) continue;
                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE)
                {
                    Server.NextFrame(() =>
                    {
                        var time = (gun.Value.NextPrimaryAttackTick - Server.TickCount) / 0.08;
                        gun.Value.NextPrimaryAttackTick = Convert.ToInt32(time + Server.TickCount);
                        gun.Value.Clip1 = 1;
                        gun.Value.Clip2 = 0;
                    });

                }

            }
        }
    }


    public void RoundEnd(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }
        if (team1 != null)
        {
            t1!.Clear();
        }
        if (team2 != null)
        {
            t2!.Clear();
        }
        return;
    }


}

