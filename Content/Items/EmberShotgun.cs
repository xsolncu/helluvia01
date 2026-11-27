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
    public sealed class EmberShotgun : CustomFireGun
    {
        public static readonly Vector2 GripPixel = new(27, 28);
        public static readonly Vector2 MuzzleFromGrip = new(65, -9);
        public static readonly Vector2 EjectionFromGrip = new(18, -7);

        public const int Damage = 30;
        public const float Knockback = 9f;
        public const float WeaponScale = 0.8f;
        public const int TriggerAnimationTicks = 1;

        public static WeaponFireProfile Profile { get; private set; }
        public override WeaponFireProfile FireProfile => Profile;

        public override void SetStaticDefaults()
        {
            Profile = new WeaponFireProfile
            {
                Identity = "helluvia01.EmberShotgun",
                AutoReuse = false, // Одиночный клик (как было в CanUseItem)
                CooldownTicks = 90,
                ProjectileType = ModContent.ProjectileType<EnergyPellet>(),
                PelletCount = 7,
                SpreadDegrees = 24f,
                PelletSpeed = 12f,

                // Геометрия
                GripOffset = GripPixel,
                MuzzleOffset = MuzzleFromGrip,
                EjectionOffset = EjectionFromGrip,
                Scale = WeaponScale,

                // Камера и звуки
                ScreenShakeStrength = 1.7f,
                FireSound = Main.dedServ ? null : new SoundStyle("helluvia01/Content/Sounds/ShotgunFire") { MaxInstances = 8 },
                ReadySound = Main.dedServ ? null : new SoundStyle("helluvia01/Content/Sounds/ShotgunReady") { MaxInstances = 1 },

                // Частицы (остальные параметры вроде ShellScale = 0.85f и SmokeScale = 0.22f подхватятся по умолчанию)
                FlashTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunFlash"),
                ShellTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunShell"),
                SmokeTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunSmoke")
            };
        }

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
            Item.useAnimation = 20;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<EnergyPellet>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);
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