using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace OpenTK_Lighting.ObjectTypes
{
	public class LightObject
	{
		public string Name;
		public Vector3 Position = Vector3.Zero;

		public bool Active = true;
		public int ShadowMapResolution = 2048;
		public Vector3 Color = Vector3.One;
		public float Intensity = 1.0f;

		public int ShadowFBO = -1;
		public int DepthCubeMap = -1;

		public Matrix4 Projection;
		public Matrix4[] ViewMatrices = new Matrix4[6];

		public LightObject(string name) { Name = name; }

		// Initialize shadow resources for this light
		public void InitShadowResources()
		{
			ShadowFBO = GL.GenFramebuffer();
			DepthCubeMap = GL.GenTexture();

			GL.BindTexture(TextureTarget.TextureCubeMap, DepthCubeMap);
			for (int i = 0; i < 6; i++)
				GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.DepthComponent, ShadowMapResolution, ShadowMapResolution, 0,
					PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, ShadowFBO);
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, DepthCubeMap, 0);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			GL.TextureParameter(DepthCubeMap, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
			GL.TextureParameter(DepthCubeMap, TextureParameterName.TextureCompareFunc, (int)All.Less);

			Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1f, 0.1f, 1000f);
			UpdateViewMatrices();
		}

		public void UpdateViewMatrices()
		{
			//Vector3[] directions = new Vector3[]
			//{
			//Vector3.UnitX, -Vector3.UnitX,
			//Vector3.UnitY, -Vector3.UnitY,
			//Vector3.UnitZ, -Vector3.UnitZ
			//};

			//Vector3[] ups = new Vector3[]
			//{
			//Vector3.UnitY, Vector3.UnitY,
			//Vector3.UnitZ, -Vector3.UnitZ,
			//Vector3.UnitY, Vector3.UnitY
			//};

			//for (int i = 0; i < 6; i++)
			//	ViewMatrices[i] = Matrix4.LookAt(Position, Position + directions[i], ups[i]);

			Vector3[] directions = {
				new Vector3( 1,  0,  0), // +X
				new Vector3(-1,  0,  0), // -X
				new Vector3( 0,  1,  0), // +Y
				new Vector3( 0, -1,  0), // -Y
				new Vector3( 0,  0,  1), // +Z
				new Vector3( 0,  0, -1), // -Z
			};

			Vector3[] ups = {
				new Vector3(0, -1,  0), // +X
				new Vector3(0, -1,  0), // -X
				new Vector3(0,  0,  1), // +Y
				new Vector3(0,  0, -1), // -Y
				new Vector3(0, -1,  0), // +Z
				new Vector3(0, -1,  0), // -Z
			};

			for (int i = 0; i < 6; i++)
				ViewMatrices[i] = Matrix4.LookAt(Position, Position + directions[i], ups[i]);
		}
	}

}
