using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_Lighting.Loaders
{
    public static class Image
    {
        public static int loadTexture(string ImageFilePath)
        {
            int width, height;
            int textureHandle;

            try
            {
                using (var stream = File.OpenRead(ImageFilePath))
                {
					StbImage.stbi_set_flip_vertically_on_load(1);

					var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    width = image.Width;
                    height = image.Height;

                    textureHandle = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, textureHandle);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height,
                      0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, 16.0f);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                    //GL.BindTexture(TextureTarget.Texture2D, 0);
                }

                return textureHandle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture: {ex.Message}");
                return -1;
            }
        }
    }
}
