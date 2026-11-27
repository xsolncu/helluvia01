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
    public sealed class Spas12 : CustomFireGun
    {
        public static readonly Vector2 GripPixel = new(35, 39); // Подставь точный пиксель рукояти из PNG

        // Разница (MuzzlePixel - GripPixel):
        // Если дуло на X = 118, Y = 26, то: 118 - 38 = 80, 26 - 36 = -10
        public static readonly Vector2 MuzzleFromGrip = new(80, -10);

        // Разница (EjectionPixel - GripPixel):
        // Если окно выброса на X = 58, Y = 27, то: 58 - 38 = 20, 27 - 36 = -9
        public static readonly Vector2 EjectionFromGrip = new(20, -9);

        public const int Damage = 30;
        public const float Knockback = 9f;
        public const float WeaponScale = 0.73f;
        public const int TriggerAnimationTicks = 1;

        public static WeaponFireProfile Profile { get; private set; }
        public override WeaponFireProfile FireProfile => Profile;


        public override void SetStaticDefaults()
        {
            Profile = new WeaponFireProfile
            {
                Identity = "helluvia01.Spas12",
                AutoReuse = true, 
                ProjectileType = ModContent.ProjectileType<EnergyPellet>(),
                PelletCount = 5,
                SpreadDegrees = 18f,
                PelletSpeed = 12f,

                // Геометрия
                GripOffset = GripPixel,
                MuzzleOffset = MuzzleFromGrip,
                EjectionOffset = EjectionFromGrip,
                Scale = WeaponScale,

                // Камера и звуки
                ScreenShakeStrength = 5.7f,
                FireSound = Main.dedServ ? null : new SoundStyle("helluvia01/Content/Sounds/ShotgunFire") { MaxInstances = 8 },

                // Частицы (остальные параметры вроде ShellScale = 0.85f и SmokeScale = 0.22f подхватятся по умолчанию)
                FlashTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunFlash"),
                ShellTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunShell"),
                SmokeTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/ShotgunSmoke"),
                SmokeScale = 1f
            };
        }

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 67;
            Item.scale = WeaponScale;
            Item.damage = Damage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = Knockback;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = 35;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<EnergyPellet>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);
        }
         public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        
    }

}