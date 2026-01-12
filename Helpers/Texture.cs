using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_Lighting.Helpers
{
	public static class Texture
	{
		public static int CreateBlankDepthTexture(int size = 1)
		{
			int tex = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, tex);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24,
				size, size, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			int fbo = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, tex, 0);

			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			GL.ClearDepth(1.0);
			GL.Clear(ClearBufferMask.DepthBufferBit);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.DeleteFramebuffer(fbo);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			return tex;
		}

		public static int CreateBlankCubemapTexture(int size = 1)
		{
			int tex = GL.GenTexture();
			GL.BindTexture(TextureTarget.TextureCubeMap, tex);

			for (int i = 0; i < 6; i++)
			{
				GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0,
					PixelInternalFormat.DepthComponent24, size, size, 0,
					PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			}

			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

			int fbo = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

			for (int i = 0; i < 6; i++)
			{
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
					TextureTarget.TextureCubeMapPositiveX + i, tex, 0);

				GL.DrawBuffer(DrawBufferMode.None);
				GL.ReadBuffer(ReadBufferMode.None);
				GL.ClearDepth(1.0);
				GL.Clear(ClearBufferMask.DepthBufferBit);
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.DeleteFramebuffer(fbo);

			GL.BindTexture(TextureTarget.TextureCubeMap, 0);

			return tex;
		}
	}
}
