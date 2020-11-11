using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using rays.Shapes;
using rays.Handlers;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace rays
{
	class Game : GameWindow
	{
		public static int random_seed = 2839472;
		public static Random RANDOM = new Random(random_seed);
		List<Sphere> Spheres = new List<Sphere>();

		// camera, mvp, frustrum corners
		Camera camera;
		Matrix4 mvp, model, view, projection;

		// Uniforms 
		// Perspective & frustrum
		int camera_id;
		int rayBottomLeft, rayBottomRight, rayTopLeft, rayTopRight;

		// Scene objects
		int skybox_id; // Not implemented
		int sphereUniform;

		// Buffers
		uint texture, texture_vao, texture_vbo;

		// Shaders
		int texture_shader;
		int rays_compute_shader;


		float[] texturedata;

		int Width = 1600;
		int Height = 900;


		private static DebugProc _debugProcCallback = DebugCallback;
		private static GCHandle _debugProcCallbackHandle;

		public Game(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings) { }

		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string messageString = Marshal.PtrToStringAnsi(message, length);

			Console.WriteLine($"{severity} {type} | {messageString}");

			if (type == DebugType.DebugTypeError)
			{
				throw new Exception(messageString);
			}
		}

		protected override void OnLoad()
		{
			// Debug stuff
			_debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

			GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			//GL.Enable(EnableCap.DebugOutputSynchronous);
			// End debug stuff


			//GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
			GL.Enable(EnableCap.DepthTest);

			// Create spheres
			var bigspheresize = 1f;
			Spheres.Add(new Sphere(0.0f, bigspheresize + 1.0f, 0.0f, bigspheresize));
			Spheres.Add(new Sphere(0.5f, bigspheresize + 1.0f, -5.0f, bigspheresize));
			Spheres.Last().Velocity.X = 1.0f;

			texturedata = new float[]
			{
				//! Positions			texture coords
				-1.0f, -1.0f, 0.0f, //! bottom left corner
				-1.0f,  1.0f, 0.0f, //! top left corner
				 1.0f,  1.0f, 0.0f, //! top right corner
				 1.0f, -1.0f, 0.0f, //! bottom right corner
			};

			// Create texture vao
			GL.GenVertexArrays(1, out texture_vao);
			GL.BindVertexArray(texture_vao);

			// Texture vbo
			GL.GenBuffers(1, out texture_vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, texture_vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * texturedata.Length, texturedata, BufferUsageHint.DynamicDraw);

			// Vertex position attributes
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			GL.EnableVertexArrayAttrib(texture_vao, 0);

			// Create texture to draw pixels onto
			GL.GenTextures(1, out texture);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureParameterName.ClampToEdge });
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureParameterName.ClampToEdge });
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)All.Linear });
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)All.Linear });
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
			GL.BindImageTexture(0, texture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

			texture_shader = ShaderHandler.LoadShader("vertex.shader", "fragment.shader");
			rays_compute_shader = ShaderHandler.LoadSingleShader("compute.shader", ShaderType.ComputeShader);

			//! Send texture resolution to fragment shader
			GL.UseProgram(texture_shader);
			GL.Uniform1(GL.GetUniformLocation(texture_shader, "width"), (float)Width);
			GL.Uniform1(GL.GetUniformLocation(texture_shader, "height"), (float)Height);

			// Initialise Camera
			camera = new Camera(this);

			// Initialise compute shader uniforms
			GL.UseProgram(rays_compute_shader);
			GL.Uniform1(GL.GetUniformLocation(rays_compute_shader, "_randseed"), (float)random_seed);
			camera_id = GL.GetUniformLocation(rays_compute_shader, "camera");
			rayBottomLeft = GL.GetUniformLocation(rays_compute_shader, "rayBottomLeft");
			rayBottomRight = GL.GetUniformLocation(rays_compute_shader, "rayBottomRight");
			rayTopLeft = GL.GetUniformLocation(rays_compute_shader, "rayTopLeft");
			rayTopRight = GL.GetUniformLocation(rays_compute_shader, "rayTopRight");
			sphereUniform = GL.GetUniformLocation(rays_compute_shader, "spheres");
			//skybox_id = GL.GetUniformLocation(rays_compute_shader, "skybox");

			base.OnLoad();
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			var delta = new Vector2(MouseState.PreviousX - MouseState.X, MouseState.PreviousY - MouseState.Y);

			camera.Aim(delta, e.Time);
			camera.Move(e.Time);

			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), (float)Width / (float)Height, 0.01f, 100f);
			view = Matrix4.LookAt(
			camera.position,
			camera.position + camera.direction,
			camera.up
			);
			model = Matrix4.Identity;

			mvp = model * view * projection;

			// Compute rays to frustrum corners & update uniforms
			GL.UseProgram(rays_compute_shader);
			Vector3 TopLeft = camera.GetNormalizedDeviceRay(-1.0f, 1.0f, view * projection);
			Vector3 TopRight = camera.GetNormalizedDeviceRay(1.0f, 1.0f, view * projection);
			Vector3 BottomLeft = camera.GetNormalizedDeviceRay(-1.0f, -1.0f, view * projection);
			Vector3 BottomRight = camera.GetNormalizedDeviceRay(1.0f, -1.0f, view * projection);

			var position = camera.position;
			GL.Uniform3(camera_id, ref position);
			GL.Uniform3(rayTopLeft, ref TopLeft);
			GL.Uniform3(rayTopRight, ref TopRight);
			GL.Uniform3(rayBottomLeft, ref BottomLeft);
			GL.Uniform3(rayBottomRight, ref BottomRight);

			var result = Spheres.SelectMany(sphere => new float[] { sphere.Position.X, sphere.Position.Y, sphere.Position.Z, sphere.Radius }).ToArray();

			// This prevents crashing..?
			/*for (int i = 0; i < result.Length; i += 4)
			{
				Console.WriteLine($"Starting at {(i / 4) + 1}: x[{result[i]}] y[{result[i + 1]}] z[{result[i + 2]}] radius[{result[i + 3]}]");
			}*/

			GL.Uniform4(sphereUniform, result.Length, result);

			// Move second sphere
			Vector4 translate;
			float radius = 10f;
			Spheres[1].Speed = 150f;
			var angle = (float)Math.PI / 2 - ((float)Math.Acos(Spheres[1].Speed * (float)e.Time / (2 * radius)));
			translate = model * (Matrix4.CreateRotationY(angle) * new Vector4(Spheres[1].Velocity, 1.0f));

			Spheres[1].Velocity = new Vector3(translate).Normalized() * Spheres[1].Speed;
			Spheres[1].Position += new Vector3(Spheres[1].Velocity) * (float)e.Time / 4;

			if (KeyboardState.IsKeyDown(Keys.Escape))
				Close();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			this.Title = "yeet";
			// Compute scene
			GL.UseProgram(rays_compute_shader);
			GL.DispatchCompute(Width, Height, 1);
			GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

			// Draw quad
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(texture_shader);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.BindVertexArray(texture_vao);

			//GL.DrawArrays(PrimitiveType.Quads, 0, 4);
			GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

			SwapBuffers();

			base.OnRenderFrame(e);
		}
	}
}
