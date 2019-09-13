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

namespace rays
{
	class Game : GameWindow
	{
		public static Random RANDOM = new Random();
		List<Sphere> Spheres = new List<Sphere>();

		// camera, mvp, frustrum corners
		Camera camera;
		MouseState current, previous;
		Matrix4 mvp, model, view, projection;

		// Uniforms 
		// Perspective & frustrum
		int mvp_id, camera_id;
		int rayBottomLeft, rayBottomRight, rayTopLeft, rayTopRight;

		// Scene objects
		int skybox_id; // No implemented
		int sphereUniform;

		// Buffers
		uint texture, texture_vao, texture_vbo;

		// Shaders
		int texture_shader;
		int rays_compute_shader;


		float[] texturedata;

		public Game(int width, int height) : base(width, height, GraphicsMode.Default, "", GameWindowFlags.FixedWindow)
		{
			this.Size = new System.Drawing.Size(Width, Height);
			VSync = VSyncMode.On;
			CursorVisible = false;
			MouseMove += (s, a) =>
			{
				//this.Title = String.Format("Mouse position: {0},{1}", a.X.ToString(), a.Y.ToString());
				//Mouse.SetPosition(1920 / 2, 1080 / 2);
			};
			KeyPress += (s, a) =>
			{
				Console.WriteLine(a.KeyChar);
			};
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			//GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
			GL.Enable(EnableCap.DepthTest);

			// Create spheres
			var smallspheresize = 0.5f;
			for (var x = -smallspheresize * 10.0f; x <= smallspheresize * 10.0f; x += smallspheresize + 0.5f)
			{
				for (var z = -smallspheresize * 10.0f; z <= smallspheresize * 10.0f; z += smallspheresize + 0.5f)
				{
					Spheres.Add(new Sphere(z, smallspheresize, x*1.1f, smallspheresize));

				}
			}
			var bigspheresize = 1f;
			Spheres.Add(new Sphere(0.0f, bigspheresize + smallspheresize + 1.0f , 0.0f, bigspheresize));

			Console.WriteLine(Spheres.Count);

			// Create texture vao
			GL.GenVertexArrays(1, out texture_vao);
			GL.BindVertexArray(texture_vao);

			texturedata = new float[]
			{
				//! Positions			texture coords
				-1.0f, -1.0f, 0.0f, //! bottom left corner
				-1.0f,  1.0f, 0.0f, //! top left corner
				 1.0f,  1.0f, 0.0f, //! top right corner
				 1.0f, -1.0f, 0.0f, //! bottom right corner
			};

			// Texture vbo
			GL.GenBuffers(1, out texture_vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, texture_vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * texturedata.Length, texturedata, BufferUsageHint.DynamicDraw);

			// Vertex position attributes
			// These can only be used after binding a VBO since the glVertexAttribPointer function sets the currently bound GL_ARRAY_BUFFER as a source buffer for this attribute
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

			mvp_id = GL.GetUniformLocation(texture_shader, "MVP");

			// Initialise compute shader uniforms
			camera_id = GL.GetUniformLocation(rays_compute_shader, "eye");
			rayBottomLeft = GL.GetUniformLocation(rays_compute_shader, "rayBottomLeft");
			rayBottomRight = GL.GetUniformLocation(rays_compute_shader, "rayBottomRight");
			rayTopLeft = GL.GetUniformLocation(rays_compute_shader, "rayTopLeft");
			rayTopRight = GL.GetUniformLocation(rays_compute_shader, "rayTopRight");
			sphereUniform = GL.GetUniformLocation(rays_compute_shader, "spheres");
			//skybox_id = GL.GetUniformLocation(rays_compute_shader, "skybox");
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			current = Mouse.GetState();

			var delta = new Vector2(previous.X - current.X, previous.Y - current.Y);

			camera.Aim(delta, e.Time);
			camera.Move(e.Time);

			previous = current;

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
			GL.Uniform4(sphereUniform, result.Length, result);

			// SPHERE BOBS
			var last = Spheres.Last();
			foreach(Sphere sphere in Spheres)
			{
				if (sphere.Equals(last))
					break;
				if (sphere.Position.Y > 1.0f || sphere.Position.Y < 0.5f)
				{
					if (sphere.Direction.Y > 0)
						sphere.Position.Y = 1.0f;
					else
						sphere.Position.Y = 0.5f;
					sphere.Direction *= -1;
				}
				var translate = model * Matrix4.CreateTranslation(sphere.Direction) * new Vector4(sphere.Direction, 1.0f);
				var normal = new Vector3(translate);
				sphere.Position += normal * (float)e.Time / 2;
			}

			if (Keyboard.GetState().IsKeyDown(Key.Escape))
				Exit();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			this.Title = this.RenderFrequency.ToString();
			// Compute scene
			GL.UseProgram(rays_compute_shader);
			GL.DispatchCompute(Width, Height, 1);
			GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

			// Draw quad
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(texture_shader);
			GL.UniformMatrix4(mvp_id, false, ref mvp);
			GL.BindVertexArray(texture_vao);
			GL.DrawArrays(PrimitiveType.Quads, 0, 4); 

			 SwapBuffers();
		}
	}
}
