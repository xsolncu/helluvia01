using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using helluvia01.Content.Items;
using helluvia01.Content.Particles;
using helluvia01.Content.Mechanics;

namespace helluvia01.Content.Projectiles
{
    public abstract class ServerAuthProjectile : ModProjectile
    {
        protected Player shooter;
        private string shooterName;

        protected virtual Color? ImpactSparkColor => null;

        protected bool ShooterPresent => shooter != null && shooter.active &&
            ReferenceEquals(Main.player[shooter.whoAmI], shooter) && shooter.name == shooterName;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.netMode == NetmodeID.Server &&
                source is IEntitySource_WithStatsFromItem itemUse &&
                itemUse.Item.ModItem is ICustomFireWeapon)
            {
                shooter = itemUse.Player;
                shooterName = shooter.name;
            }
        }

        public override bool? CanDamage() => Main.netMode switch
        {
            NetmodeID.MultiplayerClient => false,
            NetmodeID.Server when Projectile.owner != Main.maxPlayers || shooter == null => false,
            _ => null
        };

        public override bool CanHitPvp(Player target) => false;

        public override bool? CanHitNPC(NPC target)
        {
            if (Main.netMode == NetmodeID.Server && ShooterPresent &&
                !shooter.CanNPCBeHitByPlayerOrPlayerProjectile(target, Projectile))
                return false;

            return null;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.netMode == NetmodeID.Server && ShooterPresent)
                target.PlayerInteraction(shooter.whoAmI);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ && timeLeft > 0)
                ModContent.GetInstance<ShotVisualSystem>().EmitImpact(Projectile, ImpactSparkColor);
        }
    }
}