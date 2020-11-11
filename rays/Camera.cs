using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace rays
{
	class Camera
	{
		Game window;
		float sens = 0.0025f;

		float hAngle = 3.14f;
		float vAngle = 0.0f;

		public Vector3 position { get; private set; } = new Vector3(1, 1, 0);
		public Vector3 direction { get; private set; } = new Vector3(0, 0, 0);
		public Vector3 right { get; private set; } = new Vector3(0, 0, 0);
		public Vector3 up { get; private set; } = new Vector3(0, 0, 0);

		public Camera(Game gamewindow)
		{
			window = gamewindow;
		}

		public void Aim(Vector2 delta, double time)
		{
			hAngle += (float)(sens * delta.X);
			vAngle += (float)(sens * delta.Y);

			vAngle = (float)Math.Max(-(Math.PI / 2), Math.Min(Math.PI / 2, vAngle));

			direction = new Vector3(
				(float)(Math.Cos(vAngle) * Math.Sin(hAngle)),
				(float)(Math.Sin(vAngle)),
				(float)(Math.Cos(vAngle) * Math.Cos(hAngle)));

			right = new Vector3(
				(float)(Math.Sin(hAngle - (Math.PI / 2f))),
				0,
				(float)(Math.Cos(hAngle - (Math.PI / 2f))));

			up = Vector3.Cross(right, direction);
		}

		public void Move(double time)
		{
			var kb = window.KeyboardState;
			if (kb.IsKeyDown(Keys.W))
			{
				position += direction * (float)time * 3f;
			}
			if (kb.IsKeyDown(Keys.A))
			{
				position -= right * (float)time * 3f;
			}
			if (kb.IsKeyDown(Keys.S))
			{
				position -= direction * (float)time * 3f;
			}
			if (kb.IsKeyDown(Keys.D))
			{
				position += right * (float)time * 3f;
			}
		}

		public Vector3 GetNormalizedDeviceRay(float x, float y, Matrix4 VP)
		{
			var invertedProjectionView = Matrix4.Invert(VP);
			var transform = new Vector4(x, y, 0.0f, 1.0f) * invertedProjectionView;
			var perspective = new Vector3(transform.X / transform.W, transform.Y / transform.W, transform.Z / transform.W);
			return new Vector3(perspective.X, perspective.Y, perspective.Z) - position;
		}
	}
}
