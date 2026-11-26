using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace helluvia01.Content.Projectiles
{
    public class WhiteCrossProjectile : ModProjectile
    {
        private const int Duration = 4;
        private const float DistanceFromPlayer = 68f;

        public override string Texture => "helluvia01/Content/Particles/gunflash";

        public override void SetDefaults()
        {
            Projectile.width = 57;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Duration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.damage = 0;
            Projectile.hide = true; // Обязательно для работы DrawBehind!
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Берем игрока из source (работает и в одиночной игре, и на сервере), либо по owner
            Player player = source is EntitySource_Parent { Entity: Player p }
                ? p
                : (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers ? Main.player[Projectile.owner] : null);

            // Направление передаем через ai[0] (угол выстрела), чтобы не зависеть от Main.MouseWorld на сервере
            float angle = Projectile.ai[0];
            Vector2 direction = angle.ToRotationVector2();

            Projectile.scale = 1.1f;

            Vector2 baseCenter = player != null ? player.MountedCenter : Projectile.Center;
            Vector2 center = baseCenter + direction * DistanceFromPlayer;

            Projectile.Center = new Vector2(center.X, center.Y + Projectile.ai[1]);
            Projectile.rotation = angle;
            Projectile.spriteDirection = direction.X >= 0f ? 1 : -1;
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Используем предзагруженный ассет снаряда вместо ModContent.Request в каждом кадре
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 origin = texture.Size() / 2f;
            SpriteEffects effects = Projectile.spriteDirection == -1
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            Vector2 position = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                position,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            Color glowColor = new Color(255, 235, 180, 0) * 0.18f;
            for (int i = 0; i < 3; i++)
            {
                float scale = 1.2f + i * 0.45f;
                float opacity = 0.88f - i * 0.24f;

                Main.EntitySpriteDraw(
                    texture,
                    position,
                    null,
                    glowColor * opacity,
                    Projectile.rotation,
                    origin,
                    scale,
                    effects,
                    0
                );
            }

            return false;
        }
    }
}