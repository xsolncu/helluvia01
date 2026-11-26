using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using helluvia01.Content.Mechanics;

namespace helluvia01.Content.Items
{
 public abstract class CustomFireGun : ModItem, ICustomFireWeapon
    {
        public abstract WeaponFireProfile FireProfile { get; }

        public override bool CanUseItem(Player player)
        {
            // Если оружие использует стандартный таймер Terraria (CooldownTicks == null)
            if (!FireProfile.CooldownTicks.HasValue)
                return true;

            // Логика для оружия с кастомным кулдауном (как EmberShotgun)
            if (!FireProfile.AutoReuse && player.whoAmI == Main.myPlayer && !PlayerInput.Triggers.JustPressed.MouseLeft)
                return false;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return player.whoAmI != Main.myPlayer || !player.GetModPlayer<ShotPlayer>().Waiting;

            return ShotSession.Remaining(player) == 0;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) =>
            Main.rand.Next(100) >= FireProfile.AmmoSaveChance;

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
                // Блокируем ожидание ответа только если у оружия включен кастомный кулдаун
                if (FireProfile.CooldownTicks.HasValue && state.Waiting)
                    return false;

                if (FireProfile.CooldownTicks.HasValue)
                    state.Waiting = true;

                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
                ShotPackets.Request(++state.RequestNumber, direction);
            }

            return false;
        }
    }
}