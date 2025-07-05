using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace OpenTK_Lighting.ObjectTypes.Components
{
	internal class Light_Component : Component
	{
		public bool Active = true;
		public int ShadowMapResolution = 512;
		public Vector3 Color = Vector3.One;
		public float Intensity = 32f;
		public float Radius = 0.4f;

		public int ShadowFBO = -1;
		public int DepthCubeMap = -1;

		public Matrix4 Projection;
		public Matrix4[] ViewMatrices = new Matrix4[6];

		public override void Load()
		{
			InitShadowResources();
		}

		public override void Update(float dt)
		{
			UpdateViewMatrices();
		}

		public override void InspectorIMGUI()
		{
			if (ImGui.CollapsingHeader("Light Component"))
			{
				ImGui.Indent();

				ImGui.Checkbox("Active", ref Active);

				ImGui.Text($"Shadow Map Resolution: {ShadowMapResolution}");

				var color = new System.Numerics.Vector3(Color.X, Color.Y, Color.Z);
				if (ImGui.ColorEdit3("Color", ref color))
				{
					Color = new Vector3(color.X, color.Y, color.Z);
				}

				float intensity = Intensity;
				if (ImGui.DragFloat("Intensity", ref intensity, 0.1f, 0f, 100f))
				{
					Intensity = intensity;
				}

				float radius = Radius;
				if (ImGui.DragFloat("Radius", ref radius, 0.1f, 0f, 10f))
				{
					Radius = radius;
				}

				ImGui.Unindent();
			}
		}

		public void InitShadowResources()
		{
			ShadowFBO = GL.GenFramebuffer();
			DepthCubeMap = GL.GenTexture();

			GL.BindTexture(TextureTarget.TextureCubeMap, DepthCubeMap);
			for (int i = 0; i < 6; i++)
				GL.TexImage2D(
					TextureTarget.TextureCubeMapPositiveX + i,
					0,
					PixelInternalFormat.DepthComponent,
					ShadowMapResolution,
					ShadowMapResolution,
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
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, DepthCubeMap, 0);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1f, 0.1f, 1000f);
			UpdateViewMatrices();
		}

		public void UpdateViewMatrices()
		{
			Vector3 pos = Owner.Transform.Position;

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

			for (int i = 0; i < 6; i++)
				ViewMatrices[i] = Matrix4.LookAt(pos, pos + directions[i], ups[i]);
		}

		#region Helpers
		public Vector3 getPosition => Owner.Transform.Position;
		#endregion
	}
}
