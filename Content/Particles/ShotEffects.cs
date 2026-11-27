using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;
using Terraria.Utilities;
using helluvia01.Content.Items;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Projectiles;
using helluvia01.Content.Particles;

namespace helluvia01.Content.Particles
{
    public static class ShotEffects
    {
        // Перегрузка для локального выстрела и сетевых пакетов (берет профиль из оружия в руках)
        public static void OnShot(Player player, Vector2 muzzle, Vector2 direction)
        {
            if (player.HeldItem?.ModItem is ICustomFireWeapon weapon)
                OnShot(player, weapon.FireProfile, muzzle, direction);
        }

        public static void OnShot(Player player, WeaponFireProfile profile, Vector2 muzzle, Vector2 direction)
        {
            if (Main.dedServ || profile == null)
                return;

            Vector2? ejectPos = profile.EjectionOffset.HasValue
                ? profile.GetWorldPoint(player, direction, profile.EjectionOffset.Value)
                : null;

            if (profile.FireSound.HasValue)
                SoundEngine.PlaySound(profile.FireSound.Value, muzzle);

            player.GetModPlayer<RecoilPlayer>().Start(direction);
            ModContent.GetInstance<ShotVisualSystem>().EmitShot(player, profile, muzzle, direction, ejectPos);

            if (player.whoAmI == Main.myPlayer && profile.ScreenShakeStrength > 0f)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(
                    muzzle,
                    -direction,
                    profile.ScreenShakeStrength,
                    profile.ScreenShakeSpeed,
                    profile.ScreenShakeFrames,
                    uniqueIdentity: profile.Identity
                ));
            }
        }

        public static void OnReady(Player player, WeaponFireProfile profile = null)
        {
            if (Main.dedServ || player.whoAmI != Main.myPlayer)
                return;

            // Если профиль не передан явно (например, из сетевого пакета), берем из текущего оружия в руках
            profile ??= (player.HeldItem?.ModItem as ICustomFireWeapon)?.FireProfile;

            if (profile?.ReadySound != null)
                SoundEngine.PlaySound(profile.ReadySound.Value);
        }
    }

   [Autoload(Side = ModSide.Client)]
    public sealed class ShotVisualSystem : ModSystem
    {
        private class Particle
        {
            public Vector2 Center, Velocity;
            public float Angle, Spin, Gravity;
            public int Age, Life;

            public Action<Particle> UpdateBehavior;
            public Action<Particle, SpriteBatch, Vector2, float> DrawBehavior;
        }

        private const int ParticleLimit = 320;
        private readonly List<Particle> particles = new(ParticleLimit);
        private readonly UnifiedRandom random = new(Environment.TickCount);
        private readonly TrailPass trails = new();

        public override void Load()
        {
            On_Main.DrawPlayers_AfterProjectiles += DrawParticlesAfterPlayers;
        }

        public override void Unload()
        {
            On_Main.DrawPlayers_AfterProjectiles -= DrawParticlesAfterPlayers;
            Main.QueueMainThreadAction(trails.Dispose);
        }

        public override void OnWorldUnload() => particles.Clear();

        private void DrawParticlesAfterPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            // Сначала Terraria рисует всех игроков и оружие в их руках
            orig(self);

            // Сразу поверх них рисуем вспышки, дым и гильзы
            DrawParticles();
        }

        public override void PostDrawTiles()
        {
            trails.Draw();
        }

        private void DrawParticles()
        {
            if (particles.Count == 0)
                return;

            using var bindings = new WorldDrawBindings(Main.instance.GraphicsDevice, saveBuffers: true);
            SpriteBatch batch = Main.spriteBatch;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                foreach (Particle p in particles)
                {
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    float progress = (float)p.Age / p.Life;
                    p.DrawBehavior?.Invoke(p, batch, screenPos, progress);
                }
            }
            finally
            {
                batch.End();
            }
        }

        internal void EmitShot(Player player, WeaponFireProfile profile, Vector2 muzzle, Vector2 aim, Vector2? shellPos)
        {
            if (profile.FlashTexture is { IsLoaded: true })
            {
                Vector2 normal = new(-aim.Y, aim.X);
                for (int i = 0; i < profile.FlashCount; i++)
                {
                    float yShift = profile.FlashSpreadY > 0f
                        ? random.NextFloat(-profile.FlashSpreadY, profile.FlashSpreadY)
                        : 0f;

                    Vector2 flashPos = muzzle + normal * yShift;
                    SpawnFlash(flashPos, aim.ToRotation(), profile.FlashTexture.Value, profile.FlashLightColor, profile.FlashScale);
                }
            }

            if (profile.ShellTexture is { IsLoaded: true } && shellPos.HasValue)
            {
                float side = aim.X < 0 ? -1f : 1f;
                Vector2 eject = new(-side * random.NextFloat(1.5f, 3f), -player.gravDir * random.NextFloat(2.8f, 4.2f));
                SpawnShell(shellPos.Value, eject + player.velocity * 0.25f, aim.ToRotation(), side * 0.25f, player.gravDir, profile.ShellTexture.Value, profile.ShellScale);
            }
            if (profile.SmokeTexture is { IsLoaded: true })
        {
                // Выносим точку спавна дыма чуть вперёд из дула, чтобы центр спрайта не утопал в оружии
                Vector2 smokeOrigin = muzzle + aim * (profile.SmokeForwardOffset);

                for (int i = 0; i < profile.SmokeCount; i++)
                {
                    Vector2 drift = aim.RotatedBy(random.NextFloat(-0.30f, 0.30f)) * random.NextFloat(0.5f, 2.6f);
                    SpawnSmoke(smokeOrigin + drift * (i + 1), drift, random.Next(17, 28), player.gravDir, profile.SmokeTexture.Value, profile.SmokeScale);
                }
            }
        }

        internal void EmitImpact(Projectile pellet, Color? sparkColor = null)
        {
            Color color = sparkColor ?? new Color(255, 165, 54, 0);
            Vector2 backward = -pellet.oldVelocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = backward.RotatedBy(random.NextFloat(-1.45f, 1.45f)) * random.NextFloat(1f, 4.2f);
                SpawnSpark(pellet.Center, velocity, random.Next(6, 12), color);
            }
        }

        private void SpawnFlash(Vector2 pos, float angle, Texture2D texture, Vector3 lightColor, float scale = 1f)
        {
            AddParticle(new Particle
            {
                Center = pos,
                Angle = angle,
                Life = 5,
                UpdateBehavior = p =>
                    Lighting.AddLight(p.Center, lightColor * (1f - (float)p.Age / p.Life)),
                DrawBehavior = (p, batch, screenPos, t) =>
                {
                    float fade = 1f - t;
                    float swell = (1.67f + 0.55f * Math.Min(1f, t * 4f)) * scale;
                    batch.Draw(texture, screenPos, null, Color.White * fade, p.Angle + 0.065f * t,
                        new Vector2(0f, texture.Height * 0.5f), swell * (1f - 0.18f * t), SpriteEffects.None, 0f);
                }
            });
        }

        private void SpawnShell(Vector2 pos, Vector2 vel, float angle, float spin, float grav, Texture2D texture, float scale = 0.85f)
        {
            AddParticle(new Particle
            {
                Center = pos,
                Velocity = vel,
                Angle = angle,
                Spin = spin,
                Gravity = grav,
                Life = 80,
                UpdateBehavior = p =>
                {
                    p.Velocity.Y += 0.26f * p.Gravity;
                    Vector2 step = Collision.TileCollision(p.Center - new Vector2(2f), p.Velocity, 4, 4, gravDir: (int)p.Gravity);
                    p.Center += step;
                    if (step.X != p.Velocity.X) p.Velocity.X *= -0.4f;
                    if (step.Y != p.Velocity.Y)
                    {
                        p.Velocity.Y = Math.Abs(p.Velocity.Y) > 0.9f ? -0.35f * p.Velocity.Y : 0f;
                        p.Velocity.X *= 0.70f;
                        p.Spin *= 0.64f;
                    }
                },
                DrawBehavior = (p, batch, screenPos, t) =>
                {
                    float fade = 1f - t;
                    Color light = Lighting.GetColor(p.Center.ToTileCoordinates());
                    batch.Draw(texture, screenPos, null, light * Math.Min(1f, fade * 5f), p.Angle,
                        texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            });
        }

        private void SpawnSmoke(Vector2 pos, Vector2 vel, int life, float grav, Texture2D texture, float scale = 0.22f)
        {
            AddParticle(new Particle
            {
                Center = pos,
                Velocity = vel,
                Angle = random.NextFloat(MathHelper.TwoPi),
                Spin = random.NextFloat(-0.07f, 0.07f),
                Gravity = grav,
                Life = life,
                UpdateBehavior = p =>
                {
                    p.Velocity *= 0.92f;
                    p.Velocity.Y -= 0.048f * p.Gravity;
                    p.Center += p.Velocity;
                },
                DrawBehavior = (p, batch, screenPos, t) =>
                {
                    float fade = 1f - t;
                    batch.Draw(texture, screenPos, null, new Color(160, 147, 148) * (0.49f * fade),
                        p.Angle, texture.Size() * 0.5f, scale * (1f + 1.9f * t), SpriteEffects.None, 0f);
                }
            });
        }

        private void SpawnSpark(Vector2 pos, Vector2 vel, int life, Color color)
        {
            AddParticle(new Particle
            {
                Center = pos,
                Velocity = vel,
                Angle = vel.ToRotation(),
                Life = life,
                UpdateBehavior = p =>
                {
                    p.Velocity *= 0.90f;
                    p.Velocity.Y += 0.12f;
                    p.Center += p.Velocity;
                    p.Angle = p.Velocity.ToRotation();
                },
                DrawBehavior = (p, batch, screenPos, t) =>
                {
                    float fade = 1f - t;
                    batch.Draw(TextureAssets.MagicPixel.Value, screenPos, new Rectangle(0, 0, 1, 1),
                        color * fade, p.Angle, new Vector2(1f, 0.5f),
                        new Vector2(2f + p.Velocity.Length(), 1.45f * fade), SpriteEffects.None, 0f);
                }
            });
        }

        private void AddParticle(Particle p)
        {
            if (particles.Count < ParticleLimit)
                particles.Add(p);
        }

        public override void PostUpdateDusts()
            {
                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    Particle p = particles[i];
                    if (++p.Age >= p.Life)
                    {
                        particles.RemoveAt(i);
                        continue;
                    }

                    p.UpdateBehavior?.Invoke(p);
                    p.Angle += p.Spin;
                }
            }
        }

    internal sealed class TrailPass : IDisposable
    {
        private const int TriangleVertices = 3;
        private const int QuadVertices = 6;
        private const int BatchQuads = 512;
        private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[BatchQuads * QuadVertices];
        private BasicEffect effect;

        public void Dispose() => effect?.Dispose();

        public void Draw()
        {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            int count = 0;
            bool began = false;
            WorldDrawBindings saved = default;

            Texture2D currentTexture = null;
            BlendState currentBlend = null;

            try
            {
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.ModProjectile is not ITrailProjectile trailProj)
                        continue;

                    TrailSettings settings = trailProj.TrailSettings;
                    if (settings.Texture == null || !settings.Texture.IsLoaded)
                        continue;

                    Vector2 offset = proj.Center - Main.Camera.Center;
                    if (Math.Abs(offset.X) > Main.screenWidth + settings.MaxLength ||
                        Math.Abs(offset.Y) > Main.screenHeight + settings.MaxLength)
                        continue;

                    if (!began)
                    {
                        saved = new WorldDrawBindings(device, saveBuffers: false);
                        began = true;
                        effect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
                        Viewport view = device.Viewport;
                        effect.World = Main.GameViewMatrix.TransformationMatrix;
                        effect.Projection = Matrix.CreateOrthographicOffCenter(0, view.Width, view.Height, 0, 0, 1);
                        device.DepthStencilState = DepthStencilState.None;
                        device.RasterizerState = RasterizerState.CullNone;
                        device.SamplerStates[0] = SamplerState.LinearClamp;
                    }

                    Texture2D tex = settings.Texture.Value;
                    BlendState blend = settings.EffectiveBlendState;

                    if (currentTexture != tex || currentBlend != blend)
                    {
                        Flush(device, ref count);
                        currentTexture = tex;
                        currentBlend = blend;

                        device.BlendState = currentBlend;
                        effect.Texture = currentTexture;
                        effect.CurrentTechnique.Passes[0].Apply();
                    }

                    AppendRibbon(device, proj, settings, ref count);
                }
                Flush(device, ref count);
            }
            finally
            {
                if (began)
                    saved.Dispose();
            }
        }

        private static void GetPath(Projectile proj, float maxLength, Span<Vector2> points, Span<float> distances, out int size)
        {
            points[0] = proj.Center - Main.screenPosition;
            distances[0] = 0f;
            size = 1;
            Vector2 centerOffset = proj.Size * .5f - Main.screenPosition;

            foreach (Vector2 history in proj.oldPos)
            {
                if (history == Vector2.Zero)
                    break;

                int last = size - 1;
                Vector2 delta = history + centerOffset - points[last];
                float length = delta.Length();
                if (length < .015f)
                    continue;
                if (length > maxLength)
                    break;

                float step = Math.Min(length, maxLength - distances[last]);
                points[size] = points[last] + delta * (step / length);
                distances[size++] = distances[last] + step;
                if (step < length || distances[size - 1] >= maxLength)
                    break;
            }
        }

        private void AppendRibbon(GraphicsDevice device, Projectile proj, in TrailSettings settings, ref int count)
        {
            Span<Vector2> points = stackalloc Vector2[proj.oldPos.Length + 1];
            Span<float> distances = stackalloc float[points.Length];
            GetPath(proj, settings.MaxLength, points, distances, out int size);
            if (size < 2)
                return;

            VertexPositionColorTexture prevL = default, prevR = default;
            for (int i = 0; i < size; i++)
            {
                Vector2 tangent = points[Math.Min(i + 1, size - 1)] - points[Math.Max(0, i - 1)];
                tangent = tangent.SafeNormalize(Vector2.UnitX);

                float remaining = 1f - distances[i] / distances[size - 1];
                Vector2 normal = new Vector2(-tangent.Y, tangent.X) * (settings.HeadRadius * remaining);
                Color tint = Color.Lerp(settings.TailColor, settings.HeadColor, remaining) * remaining;

                float u = 1f - remaining;
                VertexPositionColorTexture nextL = new(new Vector3(points[i] + normal, 0f), tint, new Vector2(u, 0f));
                VertexPositionColorTexture nextR = new(new Vector3(points[i] - normal, 0f), tint, new Vector2(u, 1f));

                if (i > 0)
                {
                    if (count == vertices.Length)
                        Flush(device, ref count);
                    vertices[count++] = prevL;
                    vertices[count++] = prevR;
                    vertices[count++] = nextL;
                    vertices[count++] = nextL;
                    vertices[count++] = prevR;
                    vertices[count++] = nextR;
                }
                prevL = nextL;
                prevR = nextR;
            }
        }

        private void Flush(GraphicsDevice device, ref int count)
        {
            if (count != 0)
                device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / TriangleVertices);
            count = 0;
        }
    }

    internal readonly struct WorldDrawBindings : IDisposable
    {
        private readonly GraphicsDevice device;
        private readonly BlendState blend;
        private readonly DepthStencilState depth;
        private readonly RasterizerState rasterizer;
        private readonly SamplerState sampler;
        private readonly Texture texture;
        private readonly VertexBufferBinding[] vertices;
        private readonly IndexBuffer indices;

        internal WorldDrawBindings(GraphicsDevice device, bool saveBuffers)
        {
            this.device = device;
            blend = device.BlendState;
            depth = device.DepthStencilState;
            rasterizer = device.RasterizerState;
            sampler = device.SamplerStates[0];
            texture = device.Textures[0];
            vertices = saveBuffers ? device.GetVertexBuffers() : null;
            indices = device.Indices;
        }

        public void Dispose()
        {
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture;
            if (vertices != null)
            {
                device.SetVertexBuffers(vertices);
                device.Indices = indices;
            }
        }
    }
}