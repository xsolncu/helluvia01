using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using helluvia01.Content.Items;
using helluvia01.Content.Particles;
using helluvia01.Content.Projectiles;

namespace helluvia01.Content.Mechanics
{
    public sealed class ShotPlayer : ModPlayer
    {
        internal bool Waiting { get; set; }
        internal ulong RequestNumber;

        public override void OnEnterWorld()
        {
            Waiting = false;
            RequestNumber = 0;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                ShotPackets.Query();
        }

        public override void PlayerDisconnect() => RequestNumber = 0;

        internal bool AcceptSequence(ulong sequence)
        {
            if (sequence <= RequestNumber)
                return false;

            RequestNumber = sequence;
            return true;
        }
    }

    public sealed class ShotSession : ModSystem
    {
        private readonly record struct CooldownState(ulong Until, WeaponFireProfile Profile);

        private static readonly Dictionary<string, CooldownState> deadlines = new(StringComparer.OrdinalIgnoreCase);
        public static ulong Tick { get; private set; }

        public override void OnWorldLoad() => Reset();
        public override void OnWorldUnload() => Reset();

        private static void Reset()
        {
            Tick = 0;
            deadlines.Clear();
        }

        public override void PreUpdatePlayers()
        {
            Tick++;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            foreach (Player player in Main.ActivePlayers)
                DeliverReady(player);
        }

        internal static int Remaining(Player player)
        {
            return deadlines.TryGetValue(player.name, out CooldownState state) && state.Until > Tick
                ? (int)(state.Until - Tick)
                : 0;
        }

        private static void DeliverReady(Player player)
        {
            if (player.dead || !deadlines.TryGetValue(player.name, out CooldownState state) || Tick < state.Until)
                return;

            deadlines.Remove(player.name);
            if (Main.netMode == NetmodeID.Server)
                ShotPackets.SendReady(player.whoAmI);
            else
                ShotEffects.OnReady(player, state.Profile);
        }

        internal static bool TryNormalize(Vector2 aim, out Vector2 direction)
        {
            float squared = aim.LengthSquared();
            direction = default;
            if (!float.IsFinite(squared) || squared < 0.000001f)
                return false;

            direction = aim / MathF.Sqrt(squared);
            return true;
        }

     internal static bool TryFire(Player player, Vector2 direction, int damage, float knockback)
{
    if (Main.netMode == NetmodeID.MultiplayerClient || !player.active || player.dead || player.ghost ||
        player.CCed || player.noItems || player.cursed ||
        player.HeldItem.ModItem is not ICustomFireWeapon weapon ||
        (weapon.FireProfile.CooldownTicks.HasValue && Remaining(player) > 0))
        return false;

    WeaponFireProfile profile = weapon.FireProfile;
    Item item = player.HeldItem;

    if (Main.netMode == NetmodeID.Server)
    {
        if (!player.controlUseItem || player.reuseDelay > 0 ||
            (player.mount.Active && player.mount.Type == MountID.Drill) ||
            !CombinedHooks.CanUseItem(player, item) || !CombinedHooks.CanShoot(player, item))
            return false;

        damage = player.GetWeaponDamage(item);
        knockback = player.GetWeaponKnockback(item);
    }

    int free = 0;
    for (int i = 0; i < Main.maxProjectiles && free < profile.PelletCount; i++)
    {
        if (!Main.projectile[i].active)
            free++;
    }
    if (free < profile.PelletCount)
        return false;

    Vector2 origin = player.RotatedRelativePoint(player.MountedCenter);
    Vector2 muzzle = profile.GetWorldPoint(player, direction, profile.MuzzleOffset);
    Vector2 spawn = Collision.CanHitLine(origin, 1, 1, muzzle, 1, 1) ? muzzle : origin;
    int owner = Main.myPlayer;

    int projToShoot = profile.ProjectileType;
    var source = player.GetSource_ItemUse_WithPotentialAmmo(item, profile.AmmoType);

    if (profile.AmmoType != AmmoID.None)
    {
        // dontConsumeAmmo: true, так как расход патронов уже обработан Terraria через CanConsumeAmmo
        if (!player.PickAmmo(item, out int ammoProj, out _, out int ammoDamage, out float ammoKb, out _, dontConsume: true))
            return false;

        damage = ammoDamage;
        knockback = ammoKb;
        projToShoot = profile.OverrideProjectileType > 0
            ? profile.OverrideProjectileType
            : ammoProj;
    }

    DeliverReady(player);

    float spreadRad = MathHelper.ToRadians(profile.SpreadDegrees);
    Vector2 normal = new(-direction.Y, direction.X);

    for (int i = 0; i < profile.PelletCount; i++)
    {
        float angle = spreadRad * (Main.rand.NextFloat() - 0.5f);
        Vector2 velocity = direction.RotatedBy(angle) * profile.PelletSpeed;

        Vector2 pelletPos = spawn;
        if (profile.PelletSpreadY > 0f)
            pelletPos += normal * Main.rand.NextFloat(-profile.PelletSpreadY, profile.PelletSpreadY);

        Projectile.NewProjectile(source, pelletPos, velocity, projToShoot, damage, knockback, owner);
    }

    // Спавн второй пули ПП и других кастомных эффектов выстрела

    if (profile.CooldownTicks.HasValue)
        deadlines[player.name] = new CooldownState(Tick + (ulong)profile.CooldownTicks.Value, profile);

    if (Main.netMode == NetmodeID.Server)
        ShotPackets.BroadcastShot(player.whoAmI, muzzle, direction);
    else
        ShotEffects.OnShot(player, profile, muzzle, direction);

    return true;
}
    }
}