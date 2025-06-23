using ImGuiNET;
using OpenTK;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK_Lighting.Loaders;
using Image = OpenTK_Lighting.Loaders.Image;

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
		private int pointShadowMapFBO, depthCubeMap;
		int shadowMapWidth = 2048*2, shadowMapHeight = 2048*2;
		Matrix4[] _shadowView = new Matrix4[6];
		Matrix4 _shadowProjection;
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
			BoxWithFrame = new RenderableObject("Box With Frame", cubeVertices, cubeNormals, cubeIndices, cubeTexCoords);
			BoxWithFrame.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\color.png");
			BoxWithFrame.specularTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\specular.png");
			BoxWithFrame.normalTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\normal.png");
			BoxWithFrame.DisposeBuffers();
			BoxWithFrame.InitializeBuffers(true);
			_objects.Add(BoxWithFrame);
			#endregion

			#region Box With Frame 2
			BoxWithFrame2 = new RenderableObject("Box With Frame 2", cubeVertices, cubeNormals, cubeIndices, cubeTexCoords) { Position = new Vector3(0, 5, 0) };
			BoxWithFrame2.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\color.png");
			BoxWithFrame2.specularTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\specular.png");
			BoxWithFrame2.normalTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\BoxWithFrame\Textures\normal.png");
			BoxWithFrame2.DisposeBuffers();
			BoxWithFrame2.InitializeBuffers(true);
			_objects.Add(BoxWithFrame2);
			#endregion

			#region Play Cube
			PlayCube = new RenderableObject("Play Cube", cubeVertices, cubeNormals, cubeIndices, cubeTexCoords) { Position = new Vector3(2, 0, 0) };
			PlayCube.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\PlayCube\Textures\color.png");
			PlayCube.normalTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\PlayCube\Textures\normal.png");
			PlayCube.DisposeBuffers();
			PlayCube.InitializeBuffers(true);
			_objects.Add(PlayCube);
			#endregion

			#region Floor
			plane = new RenderableObject("Floor", planeVertices, planeNormals, planeIndices, planeTexCoords) { Position = new Vector3(0, -0.5f, 0), Rotation = new Vector3(0, 0, 0) };
			plane.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Bricks\Textures\color.png");
			plane.normalTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Bricks\Textures\normal.png");
			plane.DisposeBuffers();
			plane.InitializeBuffers(false);
			_objects.Add(plane);
			#endregion

			#region Ceiling
			plane2 = new RenderableObject("Ceiling", planeVertices, planeNormals, planeIndices, planeTexCoords) { Position = new Vector3(0, 5.5f, 0), Rotation = new Vector3(180, 0, 0) };
			plane2.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Bricks\Textures\color.png");
			plane2.normalTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Bricks\Textures\normal.png");
			plane2.DisposeBuffers();
			plane2.InitializeBuffers(false);
			_objects.Add(plane2);
			#endregion

			#region Lighting Text
			var (verts, inds, uvs, norms) = OBJ_Parser.ParseOBJFile(@"C:\\Users\\chill\\source\\repos\\OpenTK Lighting\\Objects\\Lighting Text\\Mesh\\LightingText.obj");
			lightingText = new RenderableObject("Lighting Text", verts, norms, inds, uvs) { Position = new Vector3(-2.5f, 0.25f, 0.55f), Scale = new Vector3(3,3,3), Rotation = new Vector3(0,10.7f,9.45f)};
			lightingText.colorTexture = Image.loadTexture(@"C:\Users\chill\source\repos\OpenTK Lighting\Objects\Lighting Text\Textures\color.png");
			lightingText.DisposeBuffers();
			lightingText.InitializeBuffers(true);
			_objects.Add(lightingText);
			#endregion

			#endregion

			#region Shadow Rendering
			_shadowShader = new Shader(
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\vertex.glsl",
				@"C:\Users\chill\source\repos\OpenTK Lighting\Shaders\Shadow\fragment.glsl"
			);

			pointShadowMapFBO = GL.GenFramebuffer();
			depthCubeMap = GL.GenTexture();

			GL.BindTexture(TextureTarget.TextureCubeMap, depthCubeMap);
			for (int i = 0; i < 6; i++)
				GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.DepthComponent, shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, pointShadowMapFBO);
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthCubeMap, 0);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			_shadowProjection = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90.0f), 1.0f, 0.1f, 1000.0f);

			for (int i = 0; i < 6; i++)
				_shadowView[i] = Matrix4.LookAt(pointLightPosition, pointLightPosition + directions[i], ups[i]);
			#endregion

			#region Base Rendering
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

			_vao = GL.GenVertexArray();
			_vbo = GL.GenBuffer();
			_ibo = GL.GenBuffer();

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
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
			pointLightPosition.X = float.Sin(lightValue)*6;
			pointLightPosition.Z = float.Cos(lightValue+=(float)args.Time/12)*6;
			for (int i = 0; i < 6; i++)
				_shadowView[i] = Matrix4.LookAt(pointLightPosition, pointLightPosition + directions[i], ups[i]);

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
		private float _memoryUsageMB;
		private int _drawCalls;
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			base.OnRenderFrame(args);
			_drawCalls = 0;

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			_controller.Update(this, (float)args.Time);

			#region Shadow
			//GL.CullFace(TriangleFace.Front);
			GL.Viewport(0, 0, shadowMapWidth, shadowMapHeight);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, pointShadowMapFBO);

			_shadowShader.Use();
			GL.UniformMatrix4(_shadowShader.GetUniform("uLightProjection"), false, ref _shadowProjection);
			GL.Uniform3(_shadowShader.GetUniform("uLightPos"), ref pointLightPosition);

			int location = GL.GetUniformLocation(_shadowShader.Handle, "uLightView");
			GL.UniformMatrix4(location, _shadowView.Length, false, ref _shadowView[0].Row0.X);

			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthCubeMap, 0);
			GL.Clear(ClearBufferMask.DepthBufferBit);

			foreach (var obj in _objects) {
				obj.Render(_shadowShader, 6);
				_drawCalls++;
			}
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			//GL.CullFace(TriangleFace.Back);
			#endregion

			#region Base Rendering
			GL.Viewport(0, 0, Size.X, Size.Y);
			_baseShader.Use();
			Matrix4 view = _camera.GetViewMatrix();
			GL.UniformMatrix4(_baseShader.GetUniform("uView"), false, ref view);
			GL.UniformMatrix4(_baseShader.GetUniform("uProjection"), false, ref _projection);

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.TextureCubeMap, depthCubeMap);
			GL.Uniform1(_baseShader.GetUniform("shadowMap"), 0);
			GL.Uniform3(_baseShader.GetUniform("uLightPos"), ref pointLightPosition);
			GL.Uniform3(_baseShader.GetUniform("uCameraPos"), ref _camera.Position);
			GL.Uniform1(_baseShader.GetUniform("normalView"), normalsView ? 1 : 0);
			GL.Uniform1(_baseShader.GetUniform("useShadows"), useShadows ? 1 : 0);
			
			GL.Uniform1(_baseShader.GetUniform("material.specular"), 0.3f);
			GL.Uniform1(_baseShader.GetUniform("material.shininess"), 32.0f);

			foreach (var obj in _objects)
			{
				#region Color
				GL.Uniform3(_baseShader.GetUniform("material.color"), obj.baseColor);
				if (obj.colorTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), useColorMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture1);
					GL.BindTexture(TextureTarget.Texture2D, obj.colorTexture);
					GL.Uniform1(_baseShader.GetUniform("material.colorTexture"), 1);
				} else { GL.Uniform1(_baseShader.GetUniform("material.useColorTexture"), 0); }
				#endregion
				#region Specular
				if (obj.specularTexture != -1) {
					GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), useSpecularMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture2);
					GL.BindTexture(TextureTarget.Texture2D, obj.specularTexture);
					GL.Uniform1(_baseShader.GetUniform("material.specularTexture"), 2);
				} else { GL.Uniform1(_baseShader.GetUniform("material.useSpecularTexture"), 0); }
				#endregion
				#region Normal
				if (obj.normalTexture != -1)
				{
					GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), useNormalMaps ? 1 : 0);
					GL.ActiveTexture(TextureUnit.Texture3);
					GL.BindTexture(TextureTarget.Texture2D, obj.normalTexture);
					GL.Uniform1(_baseShader.GetUniform("material.normalTexture"), 3);
				}
				else { GL.Uniform1(_baseShader.GetUniform("material.useNormalTexture"), 0); }
				#endregion
				obj.Render(_baseShader);
				_drawCalls++;
			}
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
		public void ImGUI_Render()
		{
			ImGui.Begin("Stats");
			ImGui.Text($"FPS: {_fps:F2}");
			ImGui.Text($"Frame Time: {(1000.0f / _fps):F2} ms");
			ImGui.Text($"Draw Calls: {_drawCalls}");
			ImGui.End();

			ImGui.Begin("Lighting");
			if (ImGui.CollapsingHeader("Settings"))
			{
				ImGui.Indent();
				ImGui.Checkbox("Use Color Maps", ref useColorMaps);
				ImGui.Checkbox("Use Specular Maps", ref useSpecularMaps);
				ImGui.Checkbox("Use Normal Maps", ref useNormalMaps);
				ImGui.Checkbox("Use Shadows", ref useShadows);
				ImGui.Unindent();
			}
			if (ImGui.CollapsingHeader("Objects"))
			{
				ImGui.Indent();
				foreach (RenderableObject obj in _objects)
				{
					if (ImGui.CollapsingHeader($"{obj.Name}"))
					{
						ImGui.Indent();

						if (ImGui.CollapsingHeader($"Transform##{obj.Name}"))
						{
							ImGui.Indent();
							#region Position
							ImGui.AlignTextToFramePadding();
							ImGui.Text("Position ");
							ImGui.SameLine();
							ImGui.AlignTextToFramePadding();
							ImGui.Text("X");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##PosX{obj.Name}", ref obj.Position.X, 0.05f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Y");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##PosY{obj.Name}", ref obj.Position.Y, 0.05f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Z");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##PosZ{obj.Name}", ref obj.Position.Z, 0.05f, float.MinValue, float.MaxValue, "%.2f");
							#endregion
							#region Rotation
							ImGui.AlignTextToFramePadding();
							ImGui.Text("Rotation ");
							ImGui.SameLine();
							ImGui.AlignTextToFramePadding();
							ImGui.Text("X");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##RotX{obj.Name}", ref obj.Rotation.X, 0.05f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Y");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##RotY{obj.Name}", ref obj.Rotation.Y, 0.05f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Z");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##RotZ{obj.Name}", ref obj.Rotation.Z, 0.05f, float.MinValue, float.MaxValue, "%.2f");
							#endregion
							#region Scale
							ImGui.AlignTextToFramePadding();
							ImGui.Text("Scale ");
							ImGui.SameLine();
							ImGui.AlignTextToFramePadding();
							ImGui.Text("X");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##ScaleX{obj.Name}", ref obj.Scale.X, 0.025f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Y");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##ScaleY{obj.Name}", ref obj.Scale.Y, 0.025f, float.MinValue, float.MaxValue, "%.2f");

							ImGui.SameLine();
							ImGui.Text("Z");
							ImGui.SameLine();
							ImGui.SetNextItemWidth(70);
							ImGui.DragFloat($"##ScaleZ{obj.Name}", ref obj.Scale.Z, 0.025f, float.MinValue, float.MaxValue, "%.2f");
							#endregion
							ImGui.Unindent();
						}
						if (useColorMaps != true || obj.TextureCoordinants == null) {
							ImGui.PushItemWidth(300);
							var color4 = new System.Numerics.Vector4(obj.baseColor.X, obj.baseColor.Y, obj.baseColor.Z, 1.0f);
							if (ImGui.ColorPicker4($"Color Picker##{obj.Name}", ref color4))
							{
								obj.baseColor = new Vector3(color4.X, color4.Y, color4.Z);
							}
							ImGui.PopItemWidth();
						}
						ImGui.Unindent();
					}
				}
				ImGui.Unindent();
			}
			ImGui.End();
		}
		#endregion
	}
}
