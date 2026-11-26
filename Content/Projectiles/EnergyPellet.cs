using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Particles;

namespace helluvia01.Content.Projectiles
{
    public sealed class EnergyPellet : ServerAuthProjectile, ITrailProjectile
     {
        private const int UpdatesPerTick = 3;
        private const int LifetimeTicks = 44;
        private const int TrailSamples = 16;
        private const int HitboxPixels = 12;
        private const float DefaultScale = 1.40f;
        

        private static Asset<Texture2D> trailTexture;
        private static Asset<Texture2D> glowHead;
        private static Asset<Texture2D> glowTail;
        private static Asset<Texture2D> glowTailLarge;


        public TrailSettings TrailSettings => new(
            Texture: trailTexture,
            MaxLength: 78f,
            HeadRadius: 1.6f,
            HeadColor: new Color(255, 217, 124),
            TailColor: new Color(243, 68, 16),
            BlendState: BlendState.Additive
        );

        public override string Texture => "helluvia01/Content/Projectiles/EnergyPellet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailSamples;
            ProjectileID.Sets.TrailingMode[Type] = 0;

            if (!Main.dedServ)
            {
                trailTexture = ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunTrailGradient");
                glowHead = ModContent.Request<Texture2D>("helluvia01/Content/Particles/GlowHead");
                glowTail = ModContent.Request<Texture2D>("helluvia01/Content/Particles/GlowTail");
                glowTailLarge = ModContent.Request<Texture2D>("helluvia01/Content/Particles/GlowTailLarge");
            }
        }

        public override void Unload()
        {
            trailTexture = null;
            glowHead = null;
            glowTail = null;
            glowTailLarge = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = HitboxPixels;
            Projectile.scale = DefaultScale; // Устанавливаем масштаб по умолчанию
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = UpdatesPerTick - 1;
            Projectile.timeLeft = LifetimeTicks * UpdatesPerTick;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Fracture.Apply(target);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D core = TextureAssets.Projectile[Type].Value;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Масштаб свечения масштабируется пропорционально Projectile.scale
            float glowScale = 0.11f * Projectile.scale;
            Color glowTint = new Color(255, 220, 140);
            Vector2 glowOrigin = new Vector2(171f, 64f);

            if (glowTailLarge is { IsLoaded: true })
            {
                Main.EntitySpriteDraw(glowTailLarge.Value, drawPosition, null, glowTint * 0.05f,
                    rotation, glowOrigin, glowScale * 0.75f, SpriteEffects.None, 0f);
            }

            if (glowTail is { IsLoaded: true })
            {
                Main.EntitySpriteDraw(glowTail.Value, drawPosition, null, glowTint * 0.25f,
                    rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }

            if (glowHead is { IsLoaded: true })
            {
                Main.EntitySpriteDraw(glowHead.Value, drawPosition, null, glowTint * 0.50f,
                    rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }

            // Используем Projectile.scale вместо жесткой единицы
            Main.EntitySpriteDraw(
                core,
                drawPosition,
                null,
                Color.White,
                rotation,
                core.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}