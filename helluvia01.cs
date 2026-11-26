using Microsoft.Xna.Framework;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using helluvia01.Content.Items;
using helluvia01.Content.Mechanics;
using helluvia01.Content.Particles;
using helluvia01.Content.Projectiles;

namespace helluvia01
{
    public class helluvia01 : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShotPackets.Receive(reader, whoAmI);
        }
    }

    internal static class ShotPackets
    {
        private enum Kind : byte { Query, Request, State, Shot, Ready }

        private static ModPacket Begin(Kind kind)
        {
            ModPacket packet = ModContent.GetInstance<global::helluvia01.helluvia01>().GetPacket();
            packet.Write((byte)kind);
            return packet;
        }

        internal static void Query() => Begin(Kind.Query).Send();

        internal static void Request(ulong sequence, Vector2 direction)
        {
            ModPacket packet = Begin(Kind.Request);
            packet.Write(sequence);
            packet.WriteVector2(direction);
            packet.Send();
        }

        private static void SendState(Player player)
        {
            ModPacket packet = Begin(Kind.State);
            packet.Write((ushort)ShotSession.Remaining(player));
            packet.Send(player.whoAmI);
        }

        internal static void BroadcastShot(int shooter, Vector2 muzzle, Vector2 direction)
        {
            ModPacket packet = Begin(Kind.Shot);
            packet.Write((byte)shooter);
            packet.WriteVector2(muzzle);
            packet.WriteVector2(direction);
            packet.Send();
        }

        internal static void SendReady(int target) => Begin(Kind.Ready).Send(target);

        internal static void Receive(BinaryReader reader, int sender)
        {
            Kind kind = (Kind)reader.ReadByte();
            if (Main.netMode == NetmodeID.Server)
            {
                if ((uint)sender >= Main.maxPlayers)
                    return;

                Player player = Main.player[sender];
                if (kind == Kind.Query)
                    SendState(player);
                else if (kind == Kind.Request)
                {
                    ulong sequence = reader.ReadUInt64();
                    Vector2 aim = reader.ReadVector2();
                    if (!player.GetModPlayer<ShotPlayer>().AcceptSequence(sequence))
                        return;

                    if (!ShotSession.TryNormalize(aim, out Vector2 direction) ||
                        !ShotSession.TryFire(player, direction, 0, 0f))
                        SendState(player);
                }
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ShotPlayer local = Main.LocalPlayer.GetModPlayer<ShotPlayer>();
            switch (kind)
            {
                case Kind.State:
                    local.Waiting = reader.ReadUInt16() != 0;
                    break;
                case Kind.Shot:
                    int shooter = reader.ReadByte();
                    Vector2 muzzle = reader.ReadVector2();
                    Vector2 direction = reader.ReadVector2();
                    if (shooter >= Main.maxPlayers)
                        return;
                    if (shooter == Main.myPlayer)
                        local.Waiting = true;
                    ShotEffects.OnShot(Main.player[shooter], muzzle, direction);
                    break;
                case Kind.Ready:
                    local.Waiting = false;
                    ShotEffects.OnReady(Main.LocalPlayer);
                    break;
            }
        }
    }
}
