using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using K4ArenaSharedApi;
using Microsoft.Extensions.Logging;

namespace K4ArenaNades;

[MinimumApiVersion(205)]
public class PluginK4ArenaNades : BasePlugin
{
    public static int RoundTypeID { get; private set; } = -1;
    public override string ModuleName => "K4-Arenas Addon - Nades";
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
            RoundTypeID = checkAPI.AddSpecialRound("PANZERFAUST", 1, true, RoundStart, RoundEnd);
            RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
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

    public void RoundStart(List<CCSPlayerController>? team1, List<CCSPlayerController>? team2)
    {
        if (team1 == null || team2 == null) { return; }

        foreach (var p in team1)
        {
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(CsItem.AWP);
            t1!.Add(p);
        }
        foreach (var p in team2)
        {
            p.RemoveWeapons();
            p.GiveNamedItem(CsItem.Knife);
            p.GiveNamedItem(CsItem.AWP);
            t2!.Add(p);
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

    public HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot || player.IsHLTV || !player.PawnIsAlive) return HookResult.Continue;
        var playerPawn  = player!.PlayerPawn.Value;
        var bullet = @event;

        var bulletPos = new Vector
        {
            X = bullet.X,
            Y = bullet.Y,
            Z = bullet.Z
        };

        if(t1!.Contains(player) || t2!.Contains(player))
        {
            // SuicideBomber by TRAV on CSS discord: https://discord.com/channels/1160907911501991946/1198323604748767492/1198323608385245254
            var heProjectile = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (heProjectile == null || !heProjectile.IsValid) return HookResult.Continue;
            var node = playerPawn!.CBodyComponent!.SceneNode;
            Vector pos = node!.AbsOrigin;
            pos.Z += 10;
            heProjectile.TicksAtZeroVelocity = 100;
            heProjectile.TeamNum = playerPawn.TeamNum;
            heProjectile.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(heProjectile.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            _ = heProjectile.OwnerEntity == player.PlayerPawn;
            heProjectile.Damage = 200f;
            heProjectile.DmgRadius = 350;
            heProjectile.Teleport(bulletPos, node!.AbsRotation, new Vector(0, 0, -10));
            heProjectile.DispatchSpawn();
            heProjectile.AcceptInput("InitializeSpawnFromWorld", player.PlayerPawn.Value!, player.PlayerPawn.Value!, "");
            heProjectile.DetonateTime = 0;
            Schema.SetSchemaValue(heProjectile.Handle, "CBaseGrenade", "m_hThrower", player.Pawn.Raw);
        }

        return HookResult.Continue;
    }


    private void OnTick()
    {

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.Pawn == null || !player.Pawn.IsValid) return;
            if (t1!.Contains(player) || t2!.Contains(player))
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || pawn.WeaponServices == null) continue;

                var weapon = pawn.WeaponServices.ActiveWeapon?.Value;
                if (weapon == null) continue;

                var ActiveWeaponName = weapon.DesignerName;

                if (ActiveWeaponName.Contains("weapon_ssg08") || ActiveWeaponName.Contains("weapon_awp")
                || ActiveWeaponName.Contains("weapon_scar20") || ActiveWeaponName.Contains("weapon_g3sg1"))
                {
                    player.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.NextSecondaryAttackTick = Server.TickCount + 500;
                    var buttons = player.Buttons;
                }
            }
        }
    }


}

