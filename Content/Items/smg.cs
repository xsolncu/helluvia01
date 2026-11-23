using System;
using helluvia01.Content.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace helluvia01.Content.Items
{
	// This is a basic item template.
	// Please see tModLoader's ExampleMod for every other example:
	// https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod
	public class smg : ModItem
	{
        private readonly UnifiedRandom random = new(Environment.TickCount);

		public new string LocalizationCategory => "Items.Weapons.Ranged";
		 private static readonly float XYInaccuracy = 8.25f;
		 private static readonly float XYInaccuracy2 = 8.25f;
		 public static int AmmoSavedPercent = 50;
		// The Display Name and Tooltip of this item can be edited in the 'Localization/en-US_Mods.helluvia01.hjson' file.
		public override void SetDefaults()
		{
			Item.shoot = ProjectileID.Bullet;
			Item.damage = 3;
			Item.DamageType = DamageClass.Ranged;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.shootSpeed = 55;
			Item.noMelee = true;
			Item.useAmmo = 	AmmoID.Bullet;

			Item.crit = 40;
		  	Item.UseSound = SoundID.Item40;

			Item.useAnimation = Item.useTime = 5;
			Item.knockBack = 0;
			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.color = Color.White;
		}


		public override Vector2? HoldoutOffset() => new Vector2(-20, 9);

		public static void OnShot(Player player, Vector2 muzzle, Vector2 direction)
		{
			
		}


		public override bool Shoot(
		Player player,
		EntitySource_ItemUse_WithAmmo source,
		Vector2 position,
		Vector2 velocity,
		int type,
		int damage,
		float knockback)
		{
		if (type == ProjectileID.Bullet)
			type = ProjectileID.BulletHighVelocity;

		velocity.X += Main.rand.NextFloat(-XYInaccuracy, XYInaccuracy);
		velocity.Y += Main.rand.NextFloat(-XYInaccuracy, XYInaccuracy);

		Vector2 vel = velocity;
		Vector2 muzzle = position + Vector2.Normalize(velocity) * 35f;

		velocity.X += Main.rand.NextFloat(-XYInaccuracy, XYInaccuracy);
		velocity.Y += Main.rand.NextFloat(-XYInaccuracy, XYInaccuracy);
		Vector2 vel2 = velocity;



		Projectile shot = Projectile.NewProjectileDirect(source, position, vel, type, damage, knockback, player.whoAmI);

		for (int i = 0; i <= 1; i++)
		{
		float yOffset = Main.rand.NextFloat(-6f, 6f);

			Projectile.NewProjectile(
		source, position, Vector2.Zero, ModContent.ProjectileType<WhiteCrossProjectile>(),
		0, 0f, player.whoAmI,
		player.direction,
		yOffset);
		}


		Vector2 secondPosition = position;
		secondPosition.Y += 5f; // up; use +10f to move it down

		Projectile shot2 = Projectile.NewProjectileDirect(source, secondPosition, vel2, type, damage, knockback, player.whoAmI);


		var dust = Dust.NewDustPerfect(muzzle, DustID.InfernoFork,
				velocity * 0.1f, 100, Color.Orange, 1.2f);
			dust.noGravity = true;
			dust.scale = 50;




		Vector2 mouseWorld = Main.MouseWorld + new Vector2(2f, 2f);

			if (player.whoAmI == Main.myPlayer)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(muzzle, -vel, 0.1f,
				0.1f, 35, uniqueIdentity: "helluvia01.Smg"));
				Vector2 eject = new(-side * random.NextFloat(1.5f, 3f), -player.gravDir * random.NextFloat(2.8f, 4.2f));
            Add(Kind.Shell, EmberShotgun.WorldPoint(player, aim, EmberShotgun.EjectionFromGrip), eject + player.velocity * .25f,
                aim.ToRotation(), 80, side * .25f, player.gravDir);
		return false;

		}

		public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.Next(100) >= AmmoSavedPercent;

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 10);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();

		}
	}
}
