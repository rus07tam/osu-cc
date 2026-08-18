using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using System.IO;

namespace osucc.Core
{
    /// <summary>
    /// Builds <see cref="Texture"/> instances from raw bytes or a stream, resolving the
    /// renderer from the live game dependencies. Texture creation must run on the update thread.
    /// </summary>
    public static class TextureHelper
    {
        /// <summary>Creates a texture from PNG bytes; <c>null</c> if the game/renderer is unavailable or decoding fails.</summary>
        public static Texture? FromBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            using var stream = new MemoryStream(data);
            return FromStream(stream);
        }

        public static Func<IRenderer?>? RendererProvider { get; set; }

        /// <summary>Creates a texture from a stream; <c>null</c> if the game/renderer is unavailable or decoding fails.</summary>
        public static Texture? FromStream(Stream stream)
        {
            try
            {
                var renderer = RendererProvider?.Invoke();
                return renderer == null ? null : Texture.FromStream(renderer, stream);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"TextureHelper: failed to create texture: {ex.Message}");
                return null;
            }
        }
    }
}
