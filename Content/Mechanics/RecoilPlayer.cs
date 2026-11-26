using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using helluvia01.Content.Items;

namespace helluvia01.Content.Mechanics
{
    public class RecoilPlayer : ModPlayer
    {
        public float RecoilAngle;

        private WeaponFireProfile activeProfile;
        private Vector2 aim;
        private float kickProgress;
        private int slot;

        public override void PostUpdate()
        {
            UpdateWeaponPose();
            if (!Player.controlUseItem)
                RecoilAngle = Math.Max(0f, RecoilAngle - MathHelper.ToRadians(1f));
        }

        internal void Start(Vector2 direction)
        {
            if (Player.dead || Player.HeldItem?.ModItem is not ICustomFireWeapon weapon)
            {
                kickProgress = 0f;
                return;
            }

            activeProfile = weapon.FireProfile;
            slot = Player.selectedItem;
            aim = direction;
            kickProgress = MathHelper.Clamp(kickProgress + 0.65f, 0f, 1f);
        }

        internal bool Visible => kickProgress > 0.01f && activeProfile != null &&
            Player.active && !Player.dead && !Player.noItems && !Player.CCed &&
            Player.selectedItem == slot &&
            Player.HeldItem?.ModItem is ICustomFireWeapon weapon &&
            weapon.FireProfile == activeProfile;

        // Кастомный слой включается только для оружия, у которого явно задан GripOffset (например, EmberShotgun)
        internal bool HasCustomDraw => Visible && activeProfile.GripOffset.HasValue;

        private void UpdateWeaponPose()
        {
            if (!Visible)
            {
                kickProgress = 0f;
                return;
            }

            float decayPerTick = 1f / Math.Max(1, activeProfile.RecoilReturnTicks);
            kickProgress = Math.Max(0f, kickProgress - decayPerTick);

            GetPose(out _, out float angle, out _);
            Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, angle - MathHelper.PiOver2);
            Player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        }

        public override void UpdateDead() => kickProgress = 0f;

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (HasCustomDraw)
                PlayerDrawLayers.HeldItem.Hide();
        }

        internal void GetPose(out Vector2 grip, out float angle, out bool flip)
        {
            float kick = kickProgress * (2f - kickProgress);

            int side = aim.X == 0f ? Player.direction : aim.X < 0f ? -1 : 1;
            float orientation = side * Player.gravDir;

            angle = aim.ToRotation() - orientation * activeProfile.RecoilKickAngle * kick;
            grip = Player.RotatedRelativePoint(Player.MountedCenter)
                - aim * (activeProfile.RecoilKickPixels * kick)
                - Vector2.UnitY * (Player.gravDir * activeProfile.RecoilLiftPixels * kick);
            flip = orientation < 0f;
        }

        internal WeaponFireProfile ActiveProfile => activeProfile;
    }

    public sealed class CustomWeaponDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new Between(PlayerDrawLayers.SolarShield, PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
            drawInfo.shadow == 0f &&
            !drawInfo.drawPlayer.invis &&
            drawInfo.drawPlayer.GetModPlayer<RecoilPlayer>().HasCustomDraw;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            RecoilPlayer recoil = player.GetModPlayer<RecoilPlayer>();
            WeaponFireProfile profile = recoil.ActiveProfile;
            if (profile?.GripOffset == null)
                return;

            int itemType = player.HeldItem.type;
            Main.instance.LoadItem(itemType);
            var asset = TextureAssets.Item[itemType];
            if (asset == null || !asset.IsLoaded)
                return;

            Texture2D gun = asset.Value;
            recoil.GetPose(out Vector2 grip, out float angle, out bool flip);

            Vector2 origin = profile.GripOffset.Value;
            if (flip)
                origin.Y = gun.Height - origin.Y;

            Vector2 drawPosition = grip + drawInfo.Position - player.position - Main.screenPosition;
            Color light = Lighting.GetColor(grip.ToTileCoordinates());

            drawInfo.DrawDataCache.Add(new DrawData(
                gun,
                drawPosition,
                null,
                player.HeldItem.GetAlpha(light),
                angle,
                origin,
                profile.Scale,
                flip ? SpriteEffects.FlipVertically : SpriteEffects.None,
                0f
            ));
        }
    }
}