using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Particles;
using helluvia01.Content.Projectiles;

namespace helluvia01.Content.Items
{
    public sealed class Smg : CustomFireGun
    {
        public static readonly Vector2 GripPixel = new(43, 20);
        public static readonly Vector2 MuzzleFromGrip = new(44, -4);
        public static readonly Vector2 EjectionFromGrip = new(16, -4);

        public const int Damage = 3;
        public const float Knockback = 0f;
        public const float WeaponScale = 0.56f;

        public static WeaponFireProfile Profile { get; private set; }
        public override WeaponFireProfile FireProfile => Profile;

        public new string LocalizationCategory => "Items.Weapons.Ranged";


        public override void SetStaticDefaults()
        {
            Profile = new WeaponFireProfile
            {
                Identity = "helluvia01.Smg",
				AutoReuse = true,
				CooldownTicks = null, // Работает на стандартных useTime и autoReuse
				AmmoType = AmmoID.Bullet,
				AmmoSaveChance = 50,
				OverrideProjectileType = ModContent.ProjectileType<LowCalBullet>(),
				ProjectileType = ProjectileID.Bullet,
				PelletCount = 2,
				PelletSpreadY = 3.5f,
				SpreadDegrees = 17f,
				PelletSpeed = 35f,

                // Геометрия
                GripOffset = GripPixel,
                MuzzleOffset = MuzzleFromGrip,
                EjectionOffset = EjectionFromGrip,
                Scale = WeaponScale,

                // Легкая быстрая отдача для ПП
                RecoilReturnTicks = 8,
                RecoilKickPixels = 2.5f,
                RecoilLiftPixels = 0.8f,
                RecoilKickAngle = 0.08f,

                // Камера и звук
                ScreenShakeStrength = 0.1f,
                ScreenShakeSpeed = 0.1f,
                ScreenShakeFrames = 35,
                FireSound = Main.dedServ ? null : SoundID.Item40,
                ReadySound = null, // ПП не требует звука перезарядки затвора

                // Текстуры (можно указать текстуры ПП или оставить null, если не нужны)
                ShellTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/LowCalBulletShell"),
                ShellScale = 0.30f,

                FlashTexture = Main.dedServ ? null : ModContent.Request<Texture2D>("helluvia01/Content/Particles/gunflash"),
				FlashCount = 2,
				FlashSpreadY = 9f, // Разброс по локальной оси Y (между верхним и нижним стволом)
				FlashScale = 0.95f,

            };
        }

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 54;
            Item.scale = WeaponScale;
            Item.damage = Damage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = Knockback;
            Item.useStyle = ItemUseStyleID.Shoot;
          	Item.useTime = Item.useAnimation = 5;
    		Item.autoReuse = true;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 40;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 1);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-20, 9);

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}