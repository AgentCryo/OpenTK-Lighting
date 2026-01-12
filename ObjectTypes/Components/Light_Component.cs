using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK_Lighting.Helpers;
using System.Runtime.InteropServices;

namespace OpenTK_Lighting.ObjectTypes.Components
{
	internal class Light_Component : Component
	{
		public enum LightType
		{
			Point = 0,
			Directional = 1
		}

		public bool Active = true;
		public int Size = 20;
		[StructLayout(LayoutKind.Sequential, Pack = 16)]
		public struct LightData
		{
			public int type;                 // 4 bytes
			public int shadowMapResolution;  // 4 bytes
			private int padding0;            // 4 bytes padding to align next vec3
			private int padding1;            // 4 bytes padding to fill 16 bytes

			public Vector3 position;         // 12 bytes + 4 padding
			private float padding2;

			public Vector3 direction;        // 12 bytes + 4 padding
			private float padding3;

			public Vector3 color;            // 12 bytes + 4 padding
			private float padding4;

			public float intensity;          // 4 bytes
			public float radius;             // 4 bytes
			private float padding5;          // padding for alignment
			private float padding6;
		}

		public LightData lightData;

		public int ShadowFBO = -1;
		public int ShadowCubeMap = -1;
		public int ShadowMap2D = -1;

		public Matrix4 Projection;

		// Point
		public Matrix4[] ViewMatrices = new Matrix4[6];

		// Directional
		public Matrix4 DirectionalViewMatrix;

		public override void Load()
		{
			InitShadowResources();
		}

		public override void Update(float dt)
		{
			UpdateViewMatrices();
		}

		#region Inspector ImGui
		public override void InspectorIMGUI()
		{
			if (ImGui.CollapsingHeader("Light Component"))
			{
				ImGui.Indent();

				ImGui.Checkbox("Active", ref Active);

				ImGui.Text($"Shadow Map Resolution: {lightData.shadowMapResolution}");

				var color = new System.Numerics.Vector3(lightData.color.X, lightData.color.Y, lightData.color.Z);
				if (ImGui.ColorEdit3("Color", ref color))
				{
					lightData.color = new Vector3(color.X, color.Y, color.Z);
				}

				float intensity = lightData.intensity;
				if (ImGui.DragFloat("Intensity", ref intensity, 0.1f, 0f, 100f))
				{
					lightData.intensity = intensity;
				}

				float radius = lightData.radius;
				if (ImGui.DragFloat("Radius", ref radius, 0.1f, 0f, 10f))
				{
					lightData.radius = radius;
				}

				ImGui.Unindent();
			}
		}
		#endregion

		public void InitShadowResources()
		{
			switch(lightData.type)
			{
				case ((int)LightType.Point):
					Console.WriteLine("Loaded Point");
					ShadowFBO = GL.GenFramebuffer();
					ShadowCubeMap = GL.GenTexture();

					GL.BindTexture(TextureTarget.TextureCubeMap, ShadowCubeMap);
					for (int i = 0; i < 6; i++)
						GL.TexImage2D(
							TextureTarget.TextureCubeMapPositiveX + i,
							0,
							PixelInternalFormat.DepthComponent,
							lightData.shadowMapResolution,
							lightData.shadowMapResolution,
							0,
							PixelFormat.DepthComponent,
							PixelType.Float,
							nint.Zero);

					GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
					GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

					GL.BindFramebuffer(FramebufferTarget.Framebuffer, ShadowFBO);
					GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, ShadowCubeMap, 0);
					GL.DrawBuffer(DrawBufferMode.None);
					GL.ReadBuffer(ReadBufferMode.None);
					GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

					Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1f, 0.1f, 1000f);
					break;
				case ((int)LightType.Directional):
					Console.WriteLine("Loaded Dir");
					ShadowFBO = GL.GenFramebuffer();
					ShadowMap2D = GL.GenTexture();

					GL.BindTexture(TextureTarget.Texture2D, ShadowMap2D);
					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
						lightData.shadowMapResolution, lightData.shadowMapResolution, 0, PixelFormat.DepthComponent, PixelType.Float, nint.Zero);

					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

					float[] borderColor = new float[] { 1, 1, 1, 1 };
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);

					GL.BindFramebuffer(FramebufferTarget.Framebuffer, ShadowFBO);
					GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, ShadowMap2D, 0);
					GL.DrawBuffer(DrawBufferMode.None);
					GL.ReadBuffer(ReadBufferMode.None);
					GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

					Projection = Matrix4.CreateOrthographic(Size, Size, 0.1f, 1000);
					break;
			}
			UpdateViewMatrices();
		}

		#region Update View Matrices
		Vector3[] directions = {
						new Vector3( 1,  0,  0), // +X
						new Vector3(-1,  0,  0), // -X
						new Vector3( 0,  1,  0), // +Y
						new Vector3( 0, -1,  0), // -Y
						new Vector3( 0,  0,  1), // +Z
						new Vector3( 0,  0, -1)  // -Z
					};

		Vector3[] ups = {
						new Vector3(0, -1,  0), // +X
						new Vector3(0, -1,  0), // -X
						new Vector3(0,  0,  1), // +Y
						new Vector3(0,  0, -1), // -Y
						new Vector3(0, -1,  0), // +Z
						new Vector3(0, -1,  0)  // -Z
					};
		public void UpdateViewMatrices()
		{
			switch(lightData.type)
			{
				case ((int)LightType.Point):
					Vector3 pos = Owner.Transform.Position;

					for (int i = 0; i < 6; i++)
						ViewMatrices[i] = Matrix4.LookAt(pos, pos + directions[i], ups[i]);
					break;
				case ((int)LightType.Directional):
					lightData.direction = Owner.Transform.Forward;

					DirectionalViewMatrix = Matrix4.LookAt(getPosition, getPosition + lightData.direction, Vector3.UnitY);
					break;
			}
		}
			
		#endregion

		#region Helpers
		public Vector3 getPosition => Owner.Transform.Position;
		#endregion
	}
}
