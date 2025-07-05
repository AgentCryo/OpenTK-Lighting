using ImGuiNET;
using OpenTK;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK_Lighting.Loaders;
using Image = OpenTK_Lighting.Loaders.Image;
using OpenTK_Lighting.ObjectTypes;
using System.Drawing;
using SysVec4 = System.Numerics.Vector4;
using OpenTK_Lighting.ObjectTypes.Components;
using Object = OpenTK_Lighting.ObjectTypes.Object;
using static System.Formats.Asn1.AsnWriter;

namespace OpenTK_Lighting
{
    internal class MainWindow : GameWindow
	{
		public Scene scene = new();

		#region Rendering Variables
		private int _vao, _vbo, _ibo;

		private Shader _baseShader;

		private Camera _camera;
		Matrix4 _projection;

		private Shader _shadowShader;
		#endregion

		#region Light Rendering Variables
		private bool normalsView = false;
		private bool useColorMaps = true;
		private bool useSpecularMaps = true;
		private bool useNormalMaps = true;
		private bool useShadows = true;

		//List<LightObject> pointLights = new List<LightObject>();
		#endregion

		#region Post Processing Variables
		int PostProcessing_FBO;
		int PostProcessingResolved_FBO;
		int postProcessing_colorTexture, postProcessing_normalTexture, depthTexture;
		int postProcessing_colorTextureResolved, postProcessing_normalTextureResolved, depthTextureResolved;
		int _fsQuadVAO;
		Shader _postProcessingShader;
		Vector3[] ssaoKernel = new Vector3[64];
		Vector3[] ssaoNoise = new Vector3[16];
		int noiseTexture;
		bool useSSAO = true;
		#endregion

		#region Viewport Variables
		int viewportFBO;
		int viewportTexture;
		#endregion

		private ImGuiController _controller;
		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
			: base(gameWindowSettings, nativeWindowSettings)
		{
			_controller = new ImGuiController(this.Size.X, this.Size.Y);
		}

		#region Load
		string basePath = @"C:\Users\chill\source\repos\OpenTK Lighting\Objects\";
		string GetTexturePath(string objectName, string textureName) =>
			Path.Combine(basePath, objectName, "Textures", textureName);

		#region Create MeshRenderer Component
		MeshRenderer_Component CreateMeshRendererComponent(
			(List<float> verts, List<float> norms, List<uint> inds, List<float> uvs) geometry,
			string objectDataName,
			bool useColor = false,
			bool useNormal = false,
			bool useSpecular = false,
			bool flipVerticalNormals = true
		)
		{
			var mesh = new MeshRenderer_Component(
				geometry.verts,
				geometry.norms,
				geometry.inds,
				geometry.uvs
			);

			if (useColor)
				mesh.ColorTexture = Image.LoadTexture(GetTexturePath(objectDataName, "color.png"), Image.TextureType.Color);

			if (useSpecular)
				mesh.SpecularTexture = Image.LoadTexture(GetTexturePath(objectDataName, "specular.png"), Image.TextureType.Specular);

			if (useNormal)
				mesh.NormalTexture = Image.LoadTexture(GetTexturePath(objectDataName, "normal.png"), Image.TextureType.Normal);

			mesh.DisposeBuffers();
			mesh.InitializeBuffers(flipVerticalNormals);

			return mesh;
		}
		#endregion

		protected override void OnLoad()
		{
			base.OnLoad();

			#region Objects

			#region Cube Data
			List<float> cubeVertices = new List<float>
			{
				// Front face
				-0.5f, -0.5f,  0.5f,
				0.5f, -0.5f,  0.5f,
				0.5f,  0.5f,  0.5f,
				-0.5f,  0.5f,  0.5f,

				// Back face
				0.5f, -0.5f, -0.5f,
				-0.5f, -0.5f, -0.5f,
				-0.5f,  0.5f, -0.5f,
				0.5f,  0.5f, -0.5f,

				// Left face
				-0.5f, -0.5f, -0.5f,
				-0.5f, -0.5f,  0.5f,
				-0.5f,  0.5f,  0.5f,
				-0.5f,  0.5f, -0.5f,

				// Right face
				0.5f, -0.5f,  0.5f,
				0.5f, -0.5f, -0.5f,
				0.5f,  0.5f, -0.5f,
				0.5f,  0.5f,  0.5f,

				// Top face
				-0.5f,  0.5f,  0.5f,
				0.5f,  0.5f,  0.5f,
				0.5f,  0.5f, -0.5f,
				-0.5f,  0.5f, -0.5f,

				// Bottom face
				-0.5f, -0.5f, -0.5f,
				0.5f, -0.5f, -0.5f,
				0.5f, -0.5f,  0.5f,
				-0.5f, -0.5f,  0.5f
			};

			List<float> cubeNormals = new List<float>
			{
				// Front
				0f, 0f, 1f,
				0f, 0f, 1f,
				0f, 0f, 1f,
				0f, 0f, 1f,

				// Back
				0f, 0f, -1f,
				0f, 0f, -1f,
				0f, 0f, -1f,
				0f, 0f, -1f,

				// Left
				-1f, 0f, 0f,
				-1f, 0f, 0f,
				-1f, 0f, 0f,
				-1f, 0f, 0f,

				// Right
				1f, 0f, 0f,
				1f, 0f, 0f,
				1f, 0f, 0f,
				1f, 0f, 0f,

				// Top
				0f, 1f, 0f,
				0f, 1f, 0f,
				0f, 1f, 0f,
				0f, 1f, 0f,

				// Bottom
				0f, -1f, 0f,
				0f, -1f, 0f,
				0f, -1f, 0f,
				0f, -1f, 0f
			};

			List<uint> cubeIndices = new List<uint>
			{
				// Front
				0, 1, 2, 2, 3, 0,
				// Back
				4, 5, 6, 6, 7, 4,
				// Left
				8, 9,10,10,11, 8,
				// Right
				12,13,14,14,15,12,
				// Top
				16,17,18,18,19,16,
				// Bottom
				20,21,22,22,23,20
			};
			List<float> cubeTexCoords = new List<float>
			{
				// Front face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f,

				// Back face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f,

				// Left face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f,

				// Right face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f,

				// Top face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f,

				// Bottom face
				0f, 0f,
				1f, 0f,
				1f, 1f,
				0f, 1f
			};

			#endregion
			#region Plane Data
			List<float> planeVertices = new List<float>
			{
				// Positions (XZ-plane)
				-10f, 0f, -10f,  // Bottom-left
				10f, 0f, -10f,  // Bottom-right
				10f, 0f,  10f,  // Top-right
				-10f, 0f,  10f   // Top-left
			};

			List<float> planeNormals = new List<float>
			{
				// Upward-facing normals
				0f, 1f, 0f,
				0f, 1f, 0f,
				0f, 1f, 0f,
				0f, 1f, 0f
			};

			List<uint> planeIndices = new List<uint>
			{
				2, 1, 0,
				0, 3, 2
			};
			List<float> planeTexCoords = new List<float>
			{
				0f, 0f,
				5f, 0f,
				5f, 5f,
				0f, 5f,
			};

			#endregion

			#region Box With Frame
			var boxWithFrame = new Object();
			boxWithFrame.Name = "Box With Frame";

			var meshComponent = CreateMeshRendererComponent(
				geometry: (cubeVertices, cubeNormals, cubeIndices, cubeTexCoords),
				objectDataName: "BoxWithFrame",
				useColor: true,
				useNormal: true,
				useSpecular: true,
				flipVerticalNormals: false
			);

			meshComponent.SpecularStrength = 8;
			boxWithFrame.AddComponent(meshComponent);

			scene.Objects.Add(boxWithFrame);
			#endregion

			#region Play Cube
			var playCube = new Object();
			playCube.Name = "Play Cube";

			var playCubeMesh = CreateMeshRendererComponent(
				geometry: (cubeVertices, cubeNormals, cubeIndices, cubeTexCoords),
				objectDataName: "PlayCube",
				useColor: true,
				useNormal: true
			);
			playCube.AddComponent(playCubeMesh);
			playCube.Transform.Position = new Vector3(2, 0, 0);

			scene.Objects.Add(playCube);
			#endregion

			#region Floor
			var floor = new Object();
			floor.Name = "Floor";

			var floorMesh = CreateMeshRendererComponent(
				geometry: (planeVertices, planeNormals, planeIndices, planeTexCoords),
				objectDataName: "Bricks",
				useColor: true,
				useNormal: true,
				flipVerticalNormals: false
			);
			floor.AddComponent(floorMesh);
			floor.Transform.Position = new Vector3(0, -0.5f, 0);
			floorMesh.SpecularStrength = 1;

			scene.Objects.Add(floor);
			#endregion

			#region Lighting Text
			var (verts, inds, uvs, norms) = OBJ_Parser.ParseOBJFile(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Lighting Text\Mesh\LightingText.obj");
			var lightingText = new Object();
			lightingText.Name = "Lighting Text";

			var lightingTextMesh = CreateMeshRendererComponent(
				geometry: (verts, norms, inds, uvs),
				objectDataName: "Lighting Text",
				useColor: true
			);
			lightingText.AddComponent(lightingTextMesh);
			lightingText.Transform.Position = new Vector3(-2.5f, 0.25f, 0.55f);
			lightingText.Transform.Scale = new Vector3(3, 3, 3);
			lightingText.Transform.Rotation = new Vector3(0, 10.7f, 9.45f);

			scene.Objects.Add(lightingText);
			#endregion

			#region Decoration Gizmo
			(verts, inds, uvs, norms) = OBJ_Parser.ParseOBJFile(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Decoration Gizmo\Mesh\DecorationGizmo.obj");
			var decorationGizmo = new Object();
			decorationGizmo.Name = "Decoration Gizmo";

			var decorationGizmoMesh = CreateMeshRendererComponent(
				geometry: (verts, norms, inds, uvs),
				objectDataName: "Decoration Gizmo",
				useColor: true
			);
			decorationGizmo.AddComponent(decorationGizmoMesh);
			decorationGizmo.Transform.Position = new Vector3(5f, -0.5f, -1.0f);
			decorationGizmo.Transform.Rotation = new Vector3(0, -105f, 0);

			scene.Objects.Add(decorationGizmo);
			#endregion

			#endregion

			#region Light Init
			_shadowShader = new Shader(
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\vertex.glsl",
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\fragment.glsl"
			);

			#region Light 1
			var lightObj1 = new Object();
			lightObj1.Name = "light1";
			lightObj1.Transform.Position = new Vector3(0, 4, 3);

			var lightComp1 = new Light_Component();
			lightComp1.Color = new Vector3(1, 0, 0);
			lightComp1.Intensity = 32f;
			lightComp1.Radius = 0.4f;
			lightComp1.ShadowMapResolution = 2048 / 4;

			lightObj1.AddComponent(lightComp1);
			scene.Objects.Add(lightObj1);
			#endregion

			#region Light 2
			var lightObj2 = new Object();
			lightObj2.Name = "light2";
			lightObj2.Transform.Position = new Vector3(0.25f, 4, 3);

			var lightComp2 = new Light_Component();
			lightComp2.Color = new Vector3(0, 1, 0);
			lightComp2.Intensity = 32f;
			lightComp2.Radius = 0.4f;
			lightComp2.ShadowMapResolution = 2048 / 4;

			lightObj2.AddComponent(lightComp2);
			scene.Objects.Add(lightObj2);
			#endregion

			#region Light 3
			var lightObj3 = new Object();
			lightObj3.Name = "light3";
			lightObj3.Transform.Position = new Vector3(0.5f, 4, 3);

			var lightComp3 = new Light_Component();
			lightComp3.Color = new Vector3(0, 0, 1);
			lightComp3.Intensity = 32f;
			lightComp3.Radius = 0.4f;
			lightComp3.ShadowMapResolution = 2048 / 4;

			lightObj3.AddComponent(lightComp3);
			scene.Objects.Add(lightObj3);
			#endregion

			#endregion

			#region Base Init
			_baseShader = new Shader(
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Base\vertex.glsl",
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Base\fragment.glsl"
			);

			_camera = new Camera(new Vector3(0, 0, 10));
			_projection = Matrix4.CreatePerspectiveFieldOfView(
				MathHelper.DegreesToRadians(60f),
				Size.X / (float)Size.Y,
				0.1f,
				1000f
			);
			#endregion

			#region Post Processing Init
			_postProcessingShader = new Shader(
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\PostProcessing\vertex.glsl",
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\PostProcessing\fragment.glsl"
			);
			int samples = 4;

			postProcessing_colorTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2DMultisample, postProcessing_colorTexture);
			GL.TexImage2DMultisample((TextureTargetMultisample)TextureTarget.Texture2DMultisample, samples, PixelInternalFormat.Rgba8, Size.X, Size.Y, true);
			GL.BindTexture(TextureTarget.Texture2DMultisample, 0);

			postProcessing_normalTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2DMultisample, postProcessing_normalTexture);
			GL.TexImage2DMultisample((TextureTargetMultisample)TextureTarget.Texture2DMultisample, samples, PixelInternalFormat.Rgba16f, Size.X, Size.Y, true);
			GL.BindTexture(TextureTarget.Texture2DMultisample, 0);

			depthTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2DMultisample, depthTexture);
			GL.TexImage2DMultisample((TextureTargetMultisample)TextureTarget.Texture2DMultisample, samples, PixelInternalFormat.DepthComponent24, Size.X, Size.Y, true);
			GL.BindTexture(TextureTarget.Texture2DMultisample, 0);

			PostProcessing_FBO = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, PostProcessing_FBO);

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, postProcessing_colorTexture, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2DMultisample, postProcessing_normalTexture, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2DMultisample, depthTexture, 0);

			DrawBuffersEnum[] drawBuffers = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
			GL.DrawBuffers(drawBuffers.Length, drawBuffers);

			var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception($"Framebuffer not complete: {status}");
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			_fsQuadVAO = GL.GenVertexArray();

			Random rnd = new Random();
			for (int i = 0; i < 64; i++)
			{
				Vector3 sample = new Vector3(
					(float)(rnd.NextDouble() * 2.0 - 1.0),
					(float)(rnd.NextDouble() * 2.0 - 1.0),
					(float)(rnd.NextDouble())
				);
				sample = Vector3.Normalize(sample);
				sample *= (float)rnd.NextDouble();

				float scale = i / 64.0f;
				scale = MathHelper.Lerp(0.1f, 1.0f, scale * scale);
				ssaoKernel[i] = sample * scale;
			}

			for (int i = 0; i < 16; i++)
			{
				ssaoNoise[i] = new Vector3(
					(float)(rnd.NextDouble() * 2.0 - 1.0),
					(float)(rnd.NextDouble() * 2.0 - 1.0),
					0.0f
				);
			}

			noiseTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
			int noiseSize = (int)Math.Sqrt(ssaoNoise.Length);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, noiseSize, noiseSize, 0,
						  PixelFormat.Rgb, PixelType.Float, ssaoNoise);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			int ssaoFBO = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoFBO);

			int ssaoColorBuffer = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, Size.X, Size.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoColorBuffer, 0);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				Console.WriteLine("SSAO Framebuffer not complete!");
			}

			postProcessing_colorTextureResolved = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, postProcessing_colorTextureResolved);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			postProcessing_normalTextureResolved = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, postProcessing_normalTextureResolved);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			depthTextureResolved = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, depthTextureResolved);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, Size.X, Size.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			PostProcessingResolved_FBO = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, PostProcessingResolved_FBO);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, postProcessing_colorTextureResolved, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, postProcessing_normalTextureResolved, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTextureResolved, 0);

			DrawBuffersEnum[] drawBuffers2 = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
			GL.DrawBuffers(drawBuffers2.Length, drawBuffers2);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception("PostProcessing resolved framebuffer not complete");
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			#endregion

			#region Viewport Init
			viewportFBO = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, viewportFBO);

			viewportTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, viewportTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, viewportTexture, 0);

			DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0 };
			GL.DrawBuffers(attachments.Length, attachments);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception("SSAO Framebuffer is not complete!");
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			#endregion

			_vao = GL.GenVertexArray();
			_vbo = GL.GenBuffer();
			_ibo = GL.GenBuffer();

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Multisample);
			GL.LineWidth(1);

			SetImGUIStyle();

			scene.Load();
		}

		#region ImGui Styling
		public void SetImGUIStyle()
		{
			ImGuiStylePtr style = ImGui.GetStyle();

			style.FrameRounding = 3.0f;
			style.WindowRounding = 3.0f;
			style.GrabRounding = 5.0f;

			var colors = style.Colors;
			Vector4[] imguiColors = new Vector4[]
			{
				new Vector4(200/255f, 220/255f, 230/255f, 1f),      // Text: soft light cyan-blue
				new Vector4(120/255f, 130/255f, 140/255f, 1f),      // TextDisabled: muted grayish blue
				new Vector4(18/255f, 30/255f, 38/255f, 0.95f),      // WindowBg: very dark blue-gray
				new Vector4(0f, 0f, 0f, 0f),                         // ChildBg: transparent
				new Vector4(25/255f, 40/255f, 50/255f, 0.95f),      // PopupBg: dark blue-gray
				new Vector4(80/255f, 90/255f, 95/255f, 0.5f),       // Border: soft gray-blue
				new Vector4(0f, 0f, 0f, 0f),                         // BorderShadow: transparent
				new Vector4(35/255f, 60/255f, 90/255f, 0.55f),      // FrameBg: dark cyan-blue
				new Vector4(45/255f, 80/255f, 125/255f, 0.45f),     // FrameBgHovered: medium cyan-blue
				new Vector4(55/255f, 100/255f, 150/255f, 0.7f),     // FrameBgActive: stronger cyan-blue
				new Vector4(12/255f, 22/255f, 30/255f, 1f),         // TitleBg: very dark blue-gray
				new Vector4(35/255f, 60/255f, 80/255f, 1f),         // TitleBgActive: dark cyan-blue
				new Vector4(0f, 0f, 0f, 0.5f),                       // TitleBgCollapsed: semi-transparent black
				new Vector4(30/255f, 50/255f, 70/255f, 1f),         // MenuBarBg: dark cyan-blue gray
				new Vector4(5/255f, 5/255f, 5/255f, 0.5f),           // ScrollbarBg: almost black transparent
				new Vector4(90/255f, 95/255f, 100/255f, 1f),        // ScrollbarGrab: gray
				new Vector4(110/255f, 115/255f, 120/255f, 1f),      // ScrollbarGrabHovered: lighter gray
				new Vector4(55/255f, 110/255f, 155/255f, 1f),       // ScrollbarGrabActive: cyan-blue
				new Vector4(90/255f, 95/255f, 100/255f, 0.7f),      // CheckMark: muted gray
				new Vector4(75/255f, 90/255f, 100/255f, 0.7f),      // SliderGrab: gray-blue
				new Vector4(55/255f, 110/255f, 155/255f, 0.7f),     // SliderGrabActive: cyan-blue
				new Vector4(70/255f, 75/255f, 80/255f, 0.7f),       // Button: dark gray-blue
				new Vector4(100/255f, 110/255f, 120/255f, 0.7f),    // ButtonHovered: lighter gray-blue
				new Vector4(50/255f, 100/255f, 150/255f, 0.62f),    // ButtonActive: medium cyan-blue
				new Vector4(95/255f, 100/255f, 105/255f, 0.62f),    // Header: muted gray-blue
				new Vector4(75/255f, 80/255f, 85/255f, 0.62f),      // HeaderHovered: darker gray-blue
				new Vector4(50/255f, 100/255f, 150/255f, 1f),       // HeaderActive: bright cyan-blue
				new Vector4(80/255f, 85/255f, 90/255f, 0.5f),       // Separator: gray
				new Vector4(20/255f, 80/255f, 140/255f, 0.75f),     // SeparatorHovered: bright cyan-blue
				new Vector4(50/255f, 100/255f, 150/255f, 1f),       // SeparatorActive: bright cyan-blue
				new Vector4(50/255f, 100/255f, 150/255f, 0.25f),    // ResizeGrip: light cyan-blue transparent
				new Vector4(50/255f, 100/255f, 150/255f, 0.7f),     // ResizeGripHovered: stronger cyan-blue
				new Vector4(50/255f, 100/255f, 150/255f, 1f),       // ResizeGripActive: strong cyan-blue
				new Vector4(60/255f, 65/255f, 70/255f, 0.85f),      // Tab: dark gray-blue
				new Vector4(80/255f, 90/255f, 100/255f, 0.8f),      // TabHovered: medium gray-blue
				new Vector4(50/255f, 100/255f, 150/255f, 1f),       // TabActive: bright cyan-blue
				new Vector4(20/255f, 30/255f, 40/255f, 0.97f),      // TabUnfocused: very dark gray-blue
				new Vector4(30/255f, 70/255f, 100/255f, 1f),        // TabUnfocusedActive: muted cyan-blue
				new Vector4(150/255f, 150/255f, 150/255f, 1f),      // PlotLines: soft gray
				new Vector4(0.4f, 0.75f, 1f, 1f),                    // PlotLinesHovered: bright cyan
				new Vector4(200/255f, 180/255f, 50/255f, 1f),       // PlotHistogram: warm yellow
				new Vector4(1f, 0.6f, 0f, 1f),                       // PlotHistogramHovered: orange
				new Vector4(40/255f, 40/255f, 45/255f, 1f),         // TableHeaderBg: dark gray-blue
				new Vector4(70/255f, 75/255f, 80/255f, 1f),         // TableBorderStrong: gray-blue
				new Vector4(55/255f, 55/255f, 60/255f, 1f),         // TableBorderLight: dark gray
				new Vector4(0f, 0f, 0f, 0f),                         // TableRowBg: transparent
				new Vector4(1f, 1f, 1f, 0.05f),                      // TableRowBgAlt: very subtle white tint
				new Vector4(50/255f, 100/255f, 150/255f, 0.35f),    // TextSelectedBg: translucent cyan-blue
				new Vector4(1f, 1f, 0f, 0.9f),                       // DragDropTarget: bright yellow
				new Vector4(50/255f, 100/255f, 150/255f, 1f),       // NavHighlight: bright cyan-blue
				new Vector4(1f, 1f, 1f, 0.7f),                       // NavWindowingHighlight: white translucent
				new Vector4(0.8f, 0.8f, 0.8f, 0.2f),                 // NavWindowingDimBg: light gray transparent
				new Vector4(0.8f, 0.8f, 0.8f, 0.35f)                 // ModalWindowDimBg: light gray semi-transparent
			};

			HashSet<int> skipIndexes = new HashSet<int>()
			{
				(int)ImGuiCol.DragDropTarget,
				(int)ImGuiCol.PlotHistogramHovered,
				(int)ImGuiCol.PlotHistogram,
				(int)ImGuiCol.PlotLines,
				(int)ImGuiCol.PlotLinesHovered
			};

			for (int i = 0; i < imguiColors.Length; i++)
			{
				if (skipIndexes.Contains(i))
				{
					continue;
				}
				else
				{
					//colors[i] = (SysVec4)imguiColors[i];
				}
			}
		}
		#endregion
		#endregion

		#region Update
		bool mouseGrabbedToggle = true;
		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			base.OnUpdateFrame(args);
			
			//foreach(LightObject light in pointLights) light.UpdateViewMatrices();

			if (KeyboardState.IsKeyPressed(Keys.Tab)) mouseGrabbedToggle ^= true;
			CursorState = mouseGrabbedToggle ? CursorState.Grabbed : CursorState.Normal;
			_camera.UpdateInput(KeyboardState, MouseState, (float)args.Time, mouseGrabbedToggle);

			if (KeyboardState.IsKeyDown(Keys.Escape))
				Close();

			scene.Update((float)args.Time);
		}
		#endregion

		#region Controls (Mouse & Keyboard)
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			var io = ImGui.GetIO();
			io.MouseWheel += e.OffsetY; 
		}

		protected override void OnTextInput(TextInputEventArgs e)
		{
			var io = ImGui.GetIO();
			io.AddInputCharacter((uint)e.Unicode);
		}
		#endregion

		#region Render
		private double _frameTime;
		private double _lastTime;
		private int _frameCount;
		private float _fps;
		private int _drawCalls;
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			base.OnRenderFrame(args);
			_drawCalls = 0;

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			_controller.Update(this, (float)args.Time);

			var pointLights = scene.Objects
				.Select(o => o.Components.OfType<Light_Component>().FirstOrDefault())
				.Where(light => light != null)
				.ToList();

			#region Shadow

			foreach (var light in pointLights)
			{
				if (!useShadows) continue;

				GL.Viewport(0, 0, light.ShadowMapResolution, light.ShadowMapResolution);
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, light.ShadowFBO);

				_shadowShader.Use();
				GL.UniformMatrix4(_shadowShader.GetUniform("uLightProjection"), false, ref light.Projection);
				GL.Uniform3(_shadowShader.GetUniform("uLightPos"), light.getPosition);

				int location = GL.GetUniformLocation(_shadowShader.Handle, "uLightView");
				GL.UniformMatrix4(location, light.ViewMatrices.Length, false, ref light.ViewMatrices[0].Row0.X);

				GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, light.DepthCubeMap, 0);
				GL.Clear(ClearBufferMask.DepthBufferBit);

				foreach (var obj in scene.Objects)
				{
					var meshRenderer = obj.Components.OfType<MeshRenderer_Component>().FirstOrDefault();
					if(meshRenderer != null) {
						meshRenderer.Render(_shadowShader, 6);
						_drawCalls++;
					}
				}

				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			}
			#endregion

			#region Base Rendering

			GL.Viewport(0, 0, Size.X, Size.Y);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, PostProcessing_FBO);
			GL.DrawBuffers(2, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			_baseShader.Use();
			Matrix4 view = _camera.GetViewMatrix();
			GL.UniformMatrix4(_baseShader.GetUniform("uView"), false, ref view);
			GL.UniformMatrix4(_baseShader.GetUniform("uProjection"), false, ref _projection);

			GL.Uniform1(_baseShader.GetUniform("numPointLights"), pointLights.Count);

			for (int i = 0; i < pointLights.Count; i++)
			{
				var light = pointLights[i];

				GL.ActiveTexture(TextureUnit.Texture0 + i);
				GL.BindTexture(TextureTarget.TextureCubeMap, light.DepthCubeMap);
				GL.Uniform1(_baseShader.GetUniform($"shadowMaps[{i}]"), i);

				GL.Uniform3(_baseShader.GetUniform($"lightPositions[{i}]"), light.getPosition);
				GL.Uniform3(_baseShader.GetUniform($"lightColors[{i}]"), ref light.Color);
				GL.Uniform1(_baseShader.GetUniform($"lightIntensities[{i}]"), light.Intensity);
				GL.Uniform1(_baseShader.GetUniform($"lightActives[{i}]"), light.Active ? 1 : 0);
				GL.Uniform1(_baseShader.GetUniform($"lightSizes[{i}]"), light.Radius);
			}

			GL.Uniform3(_baseShader.GetUniform("uCameraPos"), ref _camera.Position);
			GL.Uniform1(_baseShader.GetUniform("normalView"), normalsView ? 1 : 0);
			GL.Uniform1(_baseShader.GetUniform("useShadows"), useShadows ? 1 : 0);
			GL.Uniform1(_baseShader.GetUniform("material.shininess"), 32.0f);

			foreach (var obj in scene.Objects)
			{
				var meshRenderer = obj.Components.OfType<MeshRenderer_Component>().FirstOrDefault();
				if (meshRenderer == null)
					continue;

				#region Color
				GL.Uniform3(_baseShader.GetUniform("material.color"), meshRenderer.BaseColor);
				if (meshRenderer.ColorTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), useColorMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture1 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, meshRenderer.ColorTexture);
					GL.Uniform1(_baseShader.GetUniform("material.colorTexture"), 1 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), 0);
				}
				#endregion
				#region Specular
				if (meshRenderer.SpecularTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), useSpecularMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture2 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, meshRenderer.SpecularTexture);
					GL.Uniform1(_baseShader.GetUniform("material.specularTexture"), 2 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), 0);
				}
				#endregion
				#region Normal
				if (meshRenderer.NormalTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), useNormalMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture3 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, meshRenderer.NormalTexture);
					GL.Uniform1(_baseShader.GetUniform("material.normalTexture"), 3 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), 0);
				}
				#endregion
				GL.Uniform1(_baseShader.GetUniform("material.specular"), meshRenderer.SpecularStrength);
				meshRenderer.Render(_baseShader);
				_drawCalls++;
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			#endregion

			#region MSAA Resolve (Blit multisampled FBO to single-sample FBO)

			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, PostProcessing_FBO);             // multisample FBO
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, PostProcessingResolved_FBO);     // single-sample FBO

			GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			GL.BlitFramebuffer(
				0, 0, Size.X, Size.Y,
				0, 0, Size.X, Size.Y,
				ClearBufferMask.ColorBufferBit,
				BlitFramebufferFilter.Nearest);

			GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment1);
			GL.BlitFramebuffer(
				0, 0, Size.X, Size.Y,
				0, 0, Size.X, Size.Y,
				ClearBufferMask.ColorBufferBit,
				BlitFramebufferFilter.Nearest);

			GL.BlitFramebuffer(
				0, 0, Size.X, Size.Y,
				0, 0, Size.X, Size.Y,
				ClearBufferMask.DepthBufferBit,
				BlitFramebufferFilter.Nearest);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			#endregion

			#region Post Processing

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, viewportFBO);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			_postProcessingShader.Use();

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, postProcessing_colorTextureResolved);
			GL.Uniform1(_postProcessingShader.GetUniform("colorTexture"), 0);

			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, postProcessing_normalTextureResolved);
			GL.Uniform1(_postProcessingShader.GetUniform("normalTexture"), 1);

			GL.ActiveTexture(TextureUnit.Texture2);
			GL.BindTexture(TextureTarget.Texture2D, depthTextureResolved);
			GL.Uniform1(_postProcessingShader.GetUniform("depthTexture"), 2);

			GL.ActiveTexture(TextureUnit.Texture3);
			GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
			GL.Uniform1(_postProcessingShader.GetUniform("noiseTexture"), 3);

			int kernelLocation = GL.GetUniformLocation(_postProcessingShader.Handle, "samples");
			for (int i = 0; i < ssaoKernel.Length; i++)
			{
				GL.Uniform3(kernelLocation + i, ref ssaoKernel[i]);
			}

			Matrix4 inverseProjection = Matrix4.Invert(_projection);
			GL.UniformMatrix4(_postProcessingShader.GetUniform("projection"), false, ref _projection);
			GL.UniformMatrix4(_postProcessingShader.GetUniform("inverseProjection"), false, ref inverseProjection);

			Vector2 noiseScale = new Vector2(Size.X / (int)Math.Sqrt(ssaoNoise.Length), Size.Y / (int)Math.Sqrt(ssaoNoise.Length));
			GL.Uniform2(_postProcessingShader.GetUniform("noiseScale"), ref noiseScale);

			GL.Uniform1(_postProcessingShader.GetUniform("useSSAO"), useSSAO ? 1 : 0);

			GL.BindVertexArray(_fsQuadVAO);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			GL.BindVertexArray(0);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			#endregion

			#region ImGUI STATS
			double currentTime = GLFW.GetTime();
			_frameCount++;

			if (currentTime - _lastTime >= 1.0)
			{
				_fps = _frameCount / (float)(currentTime - _lastTime);
				_frameCount = 0;
				_lastTime = currentTime;
			}
			#endregion

			ImGUI_Render();
			_controller.Render();

			SwapBuffers();
		}
		#endregion

		#region Debug GUI

		private bool _viewportFullscreen = false;
		private bool _hasSavedWindowState = false;
		private System.Numerics.Vector2 _savedWindowPos = new(100, 100);
		private System.Numerics.Vector2 _savedWindowSize = new(800, 600);
		private bool _applyWindowRestore = false;

		private Object _selectedObject = null;
		public void ImGUI_Render()
		{
			var io = ImGui.GetIO();
			var viewport = ImGui.GetMainViewport();

			#region Main Space Dock Window
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

			ImGuiWindowFlags hostWindowFlags = ImGuiWindowFlags.NoDocking
				| ImGuiWindowFlags.NoTitleBar
				| ImGuiWindowFlags.NoCollapse
				| ImGuiWindowFlags.NoResize
				| ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoBringToFrontOnFocus
				| ImGuiWindowFlags.NoNavFocus
				| ImGuiWindowFlags.MenuBar;

			ImGui.SetNextWindowPos(viewport.WorkPos);
			ImGui.SetNextWindowSize(viewport.WorkSize);
			ImGui.SetNextWindowViewport(viewport.ID);

			ImGui.Begin("MainDockSpace", hostWindowFlags);
			ImGui.PopStyleVar(2);

			uint dockspaceID = ImGui.GetID("DockSpace");
			ImGui.DockSpace(dockspaceID, System.Numerics.Vector2.Zero, ImGuiDockNodeFlags.None);
			ImGui.End();
			#endregion

			#region Viewport Window
			ImGuiWindowFlags viewportFlags;
			if (_viewportFullscreen)
			{
				viewportFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
				ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
				ImGui.SetNextWindowSize(io.DisplaySize);
			}
			else
			{
				viewportFlags = ImGuiWindowFlags.None;
			}

			ImGui.Begin("Viewport", viewportFlags);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);

			var avail = ImGui.GetContentRegionAvail();
			float windowAspect = avail.X / avail.Y;
			float textureAspect = (float)Size.X / Size.Y;

			System.Numerics.Vector2 imageSize;

			if (windowAspect > textureAspect)
			{
				imageSize.Y = avail.Y;
				imageSize.X = imageSize.Y * textureAspect;
			}
			else
			{
				imageSize.X = avail.X;
				imageSize.Y = imageSize.X / textureAspect;
			}

			var cursorPos = ImGui.GetCursorPos();
			float offsetX = (avail.X - imageSize.X) * 0.5f;
			float offsetY = (avail.Y - imageSize.Y) * 0.5f;
			ImGui.SetCursorPos(new System.Numerics.Vector2(cursorPos.X + offsetX, cursorPos.Y + offsetY));

			var imageTopLeftScreen = ImGui.GetCursorScreenPos();

			ImGui.Image((IntPtr)viewportTexture, imageSize, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));

			ImGui.SetCursorScreenPos(new System.Numerics.Vector2(imageTopLeftScreen.X + 10, imageTopLeftScreen.Y +10));
			if (ImGui.Button(_viewportFullscreen ? "Window" : "Fullscreen"))
			{
				if (!_viewportFullscreen)
				{
					_savedWindowPos = ImGui.GetWindowPos();
					_savedWindowSize = ImGui.GetWindowSize();
					_hasSavedWindowState = true;
				}
				else
				{
					_applyWindowRestore = true;
				}
				_viewportFullscreen = !_viewportFullscreen;
			}

			bool isDocked = ImGui.GetWindowDockID() != 0;
			if (!_viewportFullscreen && _applyWindowRestore && _hasSavedWindowState && !isDocked)
			{
				ImGui.SetWindowPos(_savedWindowPos);
				ImGui.SetWindowSize(_savedWindowSize);
				_applyWindowRestore = false;
			}
			ImGui.PopStyleVar();
			ImGui.End();
			#endregion

			// === Stats ===
			ImGui.Begin("Stats");
			ImGui.Text($"FPS: {_fps:F2}");
			ImGui.Text($"Frame Time: {(1000.0f / _fps):F2} ms");
			ImGui.Text($"Draw Calls: {_drawCalls}");
			ImGui.End();

			// === Lighting ===
			ImGui.Begin("Lighting");
			if (ImGui.CollapsingHeader("Settings"))
			{
				ImGui.Indent();
				ImGui.Checkbox("Use Color Maps", ref useColorMaps);
				ImGui.Checkbox("Use Specular Maps", ref useSpecularMaps);
				ImGui.Checkbox("Use Normal Maps", ref useNormalMaps);
				ImGui.Checkbox("Use Shadows", ref useShadows);
				ImGui.Checkbox("Use SSAO", ref useSSAO);
				ImGui.Unindent();
			}
			ImGui.End();

			#region Hierarchy
			ImGui.Begin("Hierarchy");
			foreach (var obj in scene.Objects)
			{
				ImGui.PushID(obj.Name);
				if (ImGui.Selectable(obj.Name, _selectedObject == obj))
				{
					_selectedObject = obj;
				}
				ImGui.PopID();
			}
			ImGui.End();
			#endregion

			#region Inspector
			ImGui.Begin("Inspector");

			if (_selectedObject != null)
			{
				_selectedObject.InspectorIMGUI();
			}
			else
			{
				ImGui.Text("No object selected.");
			}

			ImGui.End();
			#endregion
		}

		System.Numerics.Vector4 ColorFromHtml(string html, float alpha)
		{
			var c = System.Drawing.ColorTranslator.FromHtml(html);
			return new System.Numerics.Vector4(c.R / 255f, c.G / 255f, c.B / 255f, alpha);
		}

		#endregion
	}
}
