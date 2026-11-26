using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace helluvia01.Content.Mechanics
{
    public interface ICustomFireWeapon
    {
        WeaponFireProfile FireProfile { get; }
    }

    public class WeaponFireProfile
    {
        public string Identity { get; init; }

        // Режим огня и боеприпасы
        public bool AutoReuse { get; init; } = false;
        public int AmmoType { get; init; } = AmmoID.None;
        public int AmmoSaveChance { get; init; } = 0; // Например, 50 для 50% экономии
        public int OverrideProjectileType { get; init; } = 0; // Если > 0, заменяет ЛЮБОЙ снаряд от патронов на указанный
        // Баллистика
        public int ProjectileType { get; init; }
        public int PelletCount { get; init; } = 1;
        public float PelletSpreadY { get; init; } = 0f; // Смещение каждой дробинки/пули поперек ствола (для двустволок и парных ПП)
        public float SpreadDegrees { get; init; } = 0f;
        public float PelletSpeed { get; init; } = 12f;
        public int? CooldownTicks { get; init; } = null;

        // Геометрия и хват
        public Vector2? GripOffset { get; init; } = null;
        public Vector2 MuzzleOffset { get; init; }
        public Vector2? EjectionOffset { get; init; }
        public float Scale { get; init; } = 1f;

        // Анимация отдачи в руках
        public int RecoilReturnTicks { get; init; } = 10;
        public float RecoilKickPixels { get; init; } = 4.2f;
        public float RecoilLiftPixels { get; init; } = 1.6f;
        public float RecoilKickAngle { get; init; } = 0.19f;

        // Звуки и камера
        public SoundStyle? FireSound { get; init; }
        public SoundStyle? ReadySound { get; init; }
        public float ScreenShakeStrength { get; init; } = 1.7f;
        public float ScreenShakeSpeed { get; init; } = 1f;
        public int ScreenShakeFrames { get; init; } = 15;

        // Визуал
        public Asset<Texture2D> FlashTexture { get; init; }
        public int FlashCount { get; init; } = 1;       // Сколько вспышек за выстрел (2 для парных ПП)
        public float FlashSpreadY { get; init; } = 0f;  // Разброс вспышек поперек ствола в пикселях
        public float FlashScale { get; init; } = 1f;    // Масштаб вспышки
        public Asset<Texture2D> ShellTexture { get; init; }
        public float ShellScale { get; init; } = 0.85f;
        public Asset<Texture2D> SmokeTexture { get; init; }
        public Vector3 FlashLightColor { get; init; } = new(0.92f, 0.46f, 0.12f);
        public Color SparkColor { get; init; } = new(255, 165, 54, 0);
        public int SmokeCount { get; init; } = 4;

        

        // Хук для кастомных эффектов при выстреле (например, спавн WhiteCrossProjectile или второй пули со смещением)
        public Action<Player, IEntitySource, Vector2, Vector2, int, int, float> OnCustomShoot { get; init; }

        public Vector2 GetWorldPoint(Player player, Vector2 aim, Vector2 offsetPixels)
        {
            int facing = aim.X == 0f ? player.direction : Math.Sign(aim.X);
            Vector2 normal = new(-aim.Y, aim.X);
            Vector2 offset = aim * offsetPixels.X + normal * (offsetPixels.Y * facing * player.gravDir);
            return player.RotatedRelativePoint(player.MountedCenter) + offset * Scale;
        }
        
    }
}