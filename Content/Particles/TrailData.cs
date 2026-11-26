using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace helluvia01.Content.Particles
{
    public readonly record struct TrailSettings(
        Asset<Texture2D> Texture,
        float MaxLength,
        float HeadRadius,
        Color HeadColor,
        Color TailColor,
        BlendState BlendState = null)
    {
        public BlendState EffectiveBlendState => BlendState ?? BlendState.Additive;
    }

    public interface ITrailProjectile
    {
        TrailSettings TrailSettings { get; }
    }
}