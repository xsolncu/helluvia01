using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Particles;
using helluvia01.Content.Projectiles;

namespace helluvia01.Content.Items
{
    public sealed class EmberShotgun : ModItem, ICustomFireWeapon
    {
        public static readonly Vector2 GripPixel = new(27, 28);
        public static readonly Vector2 MuzzleFromGrip = new(65, -9);
        public static readonly Vector2 EjectionFromGrip = new(18, -7);

        public const int Damage = 30;
        public const float Knockback = 9f;
        public const float WeaponScale = 0.8f;
        public const int TriggerAnimationTicks = 1;

        public static WeaponFireProfile Profile { get; private set; }
        public WeaponFireProfile FireProfile => Profile;

        public override string Texture => "helluvia01/Content/Items/EmberShotgun";

        public override void SetStaticDefaults()
        {
            Profile = new WeaponFireProfile
            {
                Identity = "helluvia01.EmberShotgun",
                ProjectileType = ModContent.ProjectileType<EnergyPellet>(),
                PelletCount = 7,
                SpreadDegrees = 24f,
                PelletSpeed = 12f,
                CooldownTicks = 90,
                GripOffset = GripPixel, // <-- Добавлено для отрисовки в руках
                MuzzleOffset = MuzzleFromGrip,
                EjectionOffset = EjectionFromGrip,
                Scale = WeaponScale,
                ScreenShakeStrength = 1.7f,
                FireSound = Main.dedServ ? null : new SoundStyle("helluvia01/Content/Sounds/ShotgunFire") { MaxInstances = 8 },
                ReadySound = Main.dedServ ? null : new SoundStyle("helluvia01/Content/Sounds/ShotgunReady") { MaxInstances = 1 },
                FlashTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunFlash"),
                ShellTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunShell"),
                SmokeTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunSmoke")
            };
        }

        public static Vector2 WorldPoint(Player player, Vector2 aim, Vector2 offsetPixels) =>
            Profile.GetWorldPoint(player, aim, offsetPixels);

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
            Item.shootSpeed = 12f;
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
                ShotSession.TryFire(player, direction, damage, knockback);
            }
            else
            {
                ShotPlayer state = player.GetModPlayer<ShotPlayer>();
                if (state.Waiting)
                    return false;

                state.Waiting = true;
                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
                ShotPackets.Request(++state.RequestNumber, direction);
            }

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Shotgun)
                .AddIngredient(ItemID.SoulofLight, 12)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}