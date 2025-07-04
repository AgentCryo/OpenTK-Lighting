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

namespace OpenTK_Lighting
{
    internal class MainWindow : GameWindow
	{

		private List<RenderableObject> _objects = new();

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
		Vector3 pointLightPosition = new Vector3(6, 3, 5);
		Vector3[] directions = {
				new( 1,  0,  0), // +X
				new(-1,  0,  0), // -X
				new( 0,  1,  0), // +Y
				new( 0, -1,  0), // -Y
				new( 0,  0,  1), // +Z
				new( 0,  0, -1), // -Z
			};

		Vector3[] ups = {
				new(0, -1,  0), // +X
				new(0, -1,  0), // -X
				new(0,  0,  1), // +Y
				new(0,  0, -1), // -Y
				new(0, -1,  0), // +Z
				new(0, -1,  0), // -Z
			};

		List<LightObject> pointLights = new List<LightObject>();
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
		public RenderableObject BoxWithFrame;
		public RenderableObject BoxWithFrame2;
		public RenderableObject PlayCube;
		public RenderableObject plane;
		public RenderableObject plane2;
		public RenderableObject lightingText;
		public RenderableObject decorationGizmo;

		public LightObject light1 = new("light1");
		public LightObject light2 = new("light2");
		public LightObject light3 = new("light3");

		#region Create Renderable Object
		string basePath = @"C:\Users\chill\source\repos\OpenTK Lighting\Objects\";
		string GetTexturePath(string objectName, string textureName) => Path.Combine(basePath, objectName, "Textures", textureName);
		RenderableObject CreateRenderableObject(
			string name,
			(List<float> verts, List<float> norms, List<uint> inds, List<float> uvs) geometry,
			string objectDataName,
			bool useColor = false,
			bool useNormal = false,
			bool useSpecular = false,
			bool flipVerticalNormals = true
		){
			var obj = new RenderableObject(name, geometry.verts, geometry.norms, geometry.inds, geometry.uvs);
			if (useColor)
				obj.colorTexture = Image.LoadTexture(GetTexturePath(objectDataName, "color.png"), Image.TextureType.Color);

			if (useSpecular)
				obj.specularTexture = Image.LoadTexture(GetTexturePath(objectDataName, "specular.png"), Image.TextureType.Specular);

			if (useNormal)
				obj.normalTexture = Image.LoadTexture(GetTexturePath(objectDataName, "normal.png"), Image.TextureType.Normal);

			obj.DisposeBuffers();
			obj.InitializeBuffers(flipVerticalNormals);
			return obj;
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
			BoxWithFrame = CreateRenderableObject(
				name: "Box With Frame",
				geometry: (cubeVertices, cubeNormals, cubeIndices, cubeTexCoords),
				objectDataName: "BoxWithFrame",
				useColor: true,
				useNormal: true,
				useSpecular: true,
				flipVerticalNormals: false
			);
			BoxWithFrame.specularStrength = 8;
			_objects.Add(BoxWithFrame);
			#endregion

			#region Play Cube
			PlayCube = CreateRenderableObject(
				name: "Play Cube",
				geometry: (cubeVertices, cubeNormals, cubeIndices, cubeTexCoords),
				objectDataName: "PlayCube",
				useColor: true,
				useNormal: true
			);
			PlayCube.Position = new Vector3(2, 0, 0);
			_objects.Add(PlayCube);
			#endregion

			#region Floor
			plane = CreateRenderableObject(
				name: "Floor",
				geometry: (planeVertices, planeNormals, planeIndices, planeTexCoords),
				objectDataName: "Bricks",
				useColor: true,
				useNormal: true,
				flipVerticalNormals: false
			);
			plane.Position = new Vector3(0, -0.5f, 0);
			plane.specularStrength = 1;
			_objects.Add(plane);
			#endregion

			#region Lighting Text
			var (verts, inds, uvs, norms) = OBJ_Parser.ParseOBJFile(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Lighting Text\Mesh\LightingText.obj");
			lightingText = CreateRenderableObject(
				name: "Lighting Text",
				geometry: (verts, norms, inds, uvs),
				objectDataName: "Lighting Text",
				useColor: true
			);
			lightingText.Position = new Vector3(-2.5f, 0.25f, 0.55f);
			lightingText.Scale = new Vector3(3, 3, 3);
			lightingText.Rotation = new Vector3(0, 10.7f, 9.45f);
			_objects.Add(lightingText);
			#endregion

			#region Decoration Gizmo
			(verts, inds, uvs, norms) = OBJ_Parser.ParseOBJFile(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Decoration Gizmo\Mesh\DecorationGizmo.obj");
			decorationGizmo = CreateRenderableObject(
				name: "Decoration Gizmo",
				geometry: (verts, norms, inds, uvs),
				objectDataName: "Decoration Gizmo",
				useColor: true
			);
			decorationGizmo.Position = new Vector3(5f, -0.5f, -1.0f);
			decorationGizmo.Rotation = new Vector3(0, -90 - 15, 0);
			_objects.Add(decorationGizmo);
			#endregion

			#endregion

			#region Light Init
			_shadowShader = new Shader(
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\vertex.glsl",
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\fragment.glsl"
			);

			//light1.Position = new Vector3(0, 4, 3);
			//light1.Color = new Vector3(1,1,1);
			//light1.InitShadowResources();
			//pointLights.Add(light1);

			light1.Position = new Vector3(0, 4, 3);
			light1.Color = new Vector3(1, 0, 0);
			light1.InitShadowResources();
			pointLights.Add(light1);

			light2.Position = new Vector3(0.25f, 4, 3);
			light2.Color = new Vector3(0, 1, 0);
			light2.InitShadowResources();
			pointLights.Add(light2);

			light3.Position = new Vector3(0.5f, 4, 3);
			light3.Color = new Vector3(0, 0, 1);
			light3.InitShadowResources();
			pointLights.Add(light3);
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

				// Scale samples so they're more aligned closer to center
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
			// Create SSAO FBO
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

			// Create single-sample textures for resolved FBO
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

			// Create single-sample framebuffer for resolved MSAA
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
		}
		#endregion

		#region Update
		bool mouseGrabbedToggle = true;
		float lightValue = 0;
		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			base.OnUpdateFrame(args);

			BoxWithFrame.Rotation.Y -= (float)args.Time * 60;
			//pointLightPosition.X = float.Sin(lightValue)*6;
			//pointLightPosition.Z = float.Cos(lightValue+=(float)args.Time/12)*6;

			foreach(LightObject light in pointLights) light.UpdateViewMatrices();

			if (KeyboardState.IsKeyPressed(Keys.Tab)) mouseGrabbedToggle ^= true;
			CursorState = mouseGrabbedToggle ? CursorState.Grabbed : CursorState.Normal;
			_camera.UpdateInput(KeyboardState, MouseState, (float)args.Time, mouseGrabbedToggle);

			if (KeyboardState.IsKeyDown(Keys.Escape))
				Close();
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

			#region Shadow

			foreach (var light in pointLights)
			{
				if (!light.Active || !useShadows)
					continue;

				GL.Viewport(0, 0, light.ShadowMapResolution, light.ShadowMapResolution);
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, light.ShadowFBO);

				_shadowShader.Use();
				GL.UniformMatrix4(_shadowShader.GetUniform("uLightProjection"), false, ref light.Projection);
				GL.Uniform3(_shadowShader.GetUniform("uLightPos"), ref light.Position);

				int location = GL.GetUniformLocation(_shadowShader.Handle, "uLightView");
				GL.UniformMatrix4(location, light.ViewMatrices.Length, false, ref light.ViewMatrices[0].Row0.X);

				GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, light.DepthCubeMap, 0);
				GL.Clear(ClearBufferMask.DepthBufferBit);

				foreach (var obj in _objects)
				{
					obj.Render(_shadowShader, 6);
					_drawCalls++;
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

				// Bind shadow cubemap to texture unit;
				GL.ActiveTexture(TextureUnit.Texture0 + i);
				GL.BindTexture(TextureTarget.TextureCubeMap, light.DepthCubeMap);
				GL.Uniform1(_baseShader.GetUniform($"shadowMaps[{i}]"), i);

				GL.Uniform3(_baseShader.GetUniform($"lightPositions[{i}]"), ref light.Position);
				GL.Uniform3(_baseShader.GetUniform($"lightColors[{i}]"), ref light.Color);
				GL.Uniform1(_baseShader.GetUniform($"lightIntensities[{i}]"), light.Intensity);
				GL.Uniform1(_baseShader.GetUniform($"lightActives[{i}]"), light.Active ? 1 : 0);
				GL.Uniform1(_baseShader.GetUniform($"lightSizes[{i}]"), light.Radius);
			}

			GL.Uniform3(_baseShader.GetUniform("uCameraPos"), ref _camera.Position);
			GL.Uniform1(_baseShader.GetUniform("normalView"), normalsView ? 1 : 0);
			GL.Uniform1(_baseShader.GetUniform("useShadows"), useShadows ? 1 : 0);
			GL.Uniform1(_baseShader.GetUniform("material.shininess"), 32.0f);

			foreach (var obj in _objects)
			{
				#region Color
				GL.Uniform3(_baseShader.GetUniform("material.color"), obj.baseColor);
				if (obj.colorTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), useColorMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture1 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, obj.colorTexture);
					GL.Uniform1(_baseShader.GetUniform("material.colorTexture"), 1 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), 0);
				}
				#endregion
				#region Specular
				if (obj.specularTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), useSpecularMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture2 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, obj.specularTexture);
					GL.Uniform1(_baseShader.GetUniform("material.specularTexture"), 2 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), 0);
				}
				#endregion
				#region Normal
				if (obj.normalTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), useNormalMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture3 + pointLights.Count);
					GL.BindTexture(TextureTarget.Texture2D, obj.normalTexture);
					GL.Uniform1(_baseShader.GetUniform("material.normalTexture"), 3 + pointLights.Count);
				}
				else
				{
					GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), 0);
				}
				#endregion
				GL.Uniform1(_baseShader.GetUniform("material.specular"), obj.specularStrength);
				obj.Render(_baseShader);
				_drawCalls++;
			}

			// Done rendering base scene to multisampled FBO
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

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, viewportFBO);  // bind resolved single-sample FBO
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
			double currentTime = GLFW.GetTime(); // or use a Stopwatch
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

		public void ImGUI_Render()
		{
			var io = ImGui.GetIO();
			var viewport = ImGui.GetMainViewport();

			// === Main Dockspace Window ===
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

			// === Viewport Window (optional) ===
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
				// Window is wider than image aspect — fit height
				imageSize.Y = avail.Y;
				imageSize.X = imageSize.Y * textureAspect;
			}
			else
			{
				// Window is taller than image aspect — fit width
				imageSize.X = avail.X;
				imageSize.Y = imageSize.X / textureAspect;
			}

			// Center image
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

			// === Objects ===
			ImGui.Begin("Objects");
			if (ImGui.CollapsingHeader("Scene Objects"))
			{
				ImGui.Indent();
				foreach (var obj in _objects)
				{
					ImGui.PushID(obj.Name);

					if (ImGui.CollapsingHeader($"Object: {obj.Name}"))
					{
						ImGui.Indent();

						if (ImGui.CollapsingHeader("Transform"))
						{
							ImGui.Indent();
							ImGui.Text("Position");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("X", ref obj.Position.X, 0.05f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("Y", ref obj.Position.Y, 0.05f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("Z", ref obj.Position.Z, 0.05f);

							ImGui.Text("Rotation");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("RX", ref obj.Rotation.X, 0.05f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("RY", ref obj.Rotation.Y, 0.05f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("RZ", ref obj.Rotation.Z, 0.05f);

							ImGui.Text("Scale");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("SX", ref obj.Scale.X, 0.025f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("SY", ref obj.Scale.Y, 0.025f);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat("SZ", ref obj.Scale.Z, 0.025f);
							ImGui.Unindent();
						}

						ImGui.DragFloat("Specular Strength", ref obj.specularStrength);

						if (!useColorMaps || obj.TextureCoordinants == null)
						{
							var color3 = new System.Numerics.Vector3(obj.baseColor.X, obj.baseColor.Y, obj.baseColor.Z);
							if (ImGui.ColorEdit3("Base Color", ref color3))
							{
								obj.baseColor = (Vector3)color3;
							}
						}

						ImGui.Unindent();
					}

					ImGui.PopID();
				}
				ImGui.Unindent();
			}
			ImGui.End();

			// === Lights ===
			ImGui.Begin("Lights");
			if (ImGui.CollapsingHeader("Scene Lights"))
			{
				ImGui.Indent();
				foreach (var light in pointLights)
				{
					ImGui.PushID(light.Name);
					if (ImGui.CollapsingHeader($"Light: {light.Name}"))
					{
						ImGui.Indent();
						ImGui.Checkbox("Active", ref light.Active);

						ImGui.Text("Position");
						ImGui.SameLine();
						ImGui.SetNextItemWidth(70);
						ImGui.DragFloat("X", ref light.Position.X, 0.05f);
						ImGui.SameLine();
						ImGui.SetNextItemWidth(70);
						ImGui.DragFloat("Y", ref light.Position.Y, 0.05f);
						ImGui.SameLine();
						ImGui.SetNextItemWidth(70);
						ImGui.DragFloat("Z", ref light.Position.Z, 0.05f);

						var lightColor = (System.Numerics.Vector3)light.Color;
						if (ImGui.ColorEdit3("Color", ref lightColor))
						{
							light.Color = (Vector3)lightColor;
						}

						ImGui.DragFloat("Intensity", ref light.Intensity, 0.05f, 0.0f, 100f);
						ImGui.DragFloat("Radius", ref light.Radius, 0.05f, 0.0f, 10.0f);
						ImGui.Unindent();
					}
					ImGui.PopID();
				}
				ImGui.Unindent();
			}
			ImGui.End();
		}
		#endregion
	}
}
