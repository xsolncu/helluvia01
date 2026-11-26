using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Particles;
using helluvia01.Content.Projectiles;

namespace helluvia01.Content.Items
{
    public sealed class Snipergun : ModItem
    {
        public static readonly Vector2 GripPixel = new(27, 28);
        public static readonly Vector2 MuzzleFromGrip = new(65, -9);
        public static readonly Vector2 EjectionFromGrip = new(18, -7);

        public static Vector2 WorldPoint(Player player, Vector2 aim, Vector2 offsetPixels)
        {
            int facing = aim.X == 0f ? player.direction : System.Math.Sign(aim.X);
            Vector2 normal = new(-aim.Y, aim.X);
            Vector2 offset = aim * offsetPixels.X + normal * (offsetPixels.Y * facing * player.gravDir);
            return player.RotatedRelativePoint(player.MountedCenter) + offset * WeaponScale;
        }

        public const int Damage = 110;
        public const int PelletCount = 1;
        public const float SpreadDegrees = 0f;
        public const int CooldownTicks = 90;
        public const float Knockback = 25f;
        public const float PelletStepSpeed = 22f;
        public const float WeaponScale = 0.8f;
        public const int TriggerAnimationTicks = 1;

        public override string Texture => "helluvia01/Content/Items/Snipergun";

        public override void SetDefaults()
        {
            Item.width = 94;
            Item.height = 54;
            Item.scale = WeaponScale;
            Item.damage = Damage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = Knockback;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = TriggerAnimationTicks;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<EnergyPellet>();
            Item.shootSpeed = PelletStepSpeed;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-16f, 0f);

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer && !PlayerInput.Triggers.JustPressed.MouseLeft)
                return false;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return player.whoAmI != Main.myPlayer || !player.GetModPlayer<ShotPlayer>().Waiting;

            return ShotSession.Remaining(player) == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
            Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer || !ShotSession.TryNormalize(velocity, out Vector2 direction))
                return false;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Vector2 origin = player.RotatedRelativePoint(player.MountedCenter);
                Vector2 muzzle = Snipergun.WorldPoint(player, direction, Snipergun.MuzzleFromGrip);
                Vector2 spawn = Collision.CanHitLine(origin, 1, 1, muzzle, 1, 1) ? muzzle : origin;
                int pelletType = ModContent.ProjectileType<EnergyPellet>();
            }
            else
            {
                ShotPlayer state = player.GetModPlayer<ShotPlayer>();
                if (state.Waiting)
                    return false;

                state.Waiting = true;
                // Send the selected slot and use button before the shot request.
                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
                ShotPackets.Request(++state.RequestNumber, direction);
            }

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Shotgun)
                .AddIngredient(ItemID.SoulofLight, 32)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
