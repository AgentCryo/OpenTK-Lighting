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
		public enum TextureType
		{
			Color,
			Normal,
			Specular
		}

		public static int LoadTexture(string imagePath, TextureType type = TextureType.Color)
		{
			try
			{
				using var stream = File.OpenRead(imagePath);
				StbImage.stbi_set_flip_vertically_on_load(1);
				var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

				int internalFormat;
				int pixelFormat = (int)PixelFormat.Rgba;

				switch (type)
				{
					case TextureType.Normal:
						internalFormat = (int)PixelInternalFormat.Rgba;
						break;
					case TextureType.Specular:
					case TextureType.Color:
					default:
						internalFormat = (int)PixelInternalFormat.SrgbAlpha;
						break;
				}

				int textureHandle = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, textureHandle);

				GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)internalFormat,
					image.Width, image.Height, 0,
					(PixelFormat)pixelFormat, PixelType.UnsignedByte, image.Data);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
					(int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
					(int)TextureMagFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, 16f);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
					(int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
					(int)TextureWrapMode.Repeat);

				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

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
