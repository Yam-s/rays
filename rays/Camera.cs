using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;

namespace rays
{
	/*public class Camera
	{

		// Position, & Direction
		public Vector3 Position;
		public Vector3 CameraTarget;
		public Vector3 Direction;

		// Axis
		public Vector3 Up;
		public Vector3 Right;
		public Vector3 Forward;	


		public Camera()
		{
			Position = new Vector3(0.0f, 5.0f, 5.0f);
			CameraTarget = new Vector3(0.0f, 3.0f, 0.0f);
			Direction = CameraTarget - Position;

			var worldUp = new Vector3(0.0f, 1.0f, 0.0f);
			Right = Vector3.Normalize(Vector3.Cross(worldUp, Direction));
			Up = Vector3.Cross(Direction, Right);
			Forward = -Direction;
		}

		public Vector3 GetNormalizedDeviceRay(float x, float y, Matrix4 VP)
		{
			// Invert View and Projection matrix
			var invertedProjectionView = Matrix4.Invert(VP);
			// Convert from clipping space (device) coordinates to world space by multiplying by the inverse of VP matrix
			var transform = new Vector4(x, y, 0.0f, 1.0f) * invertedProjectionView;
			// Divide by w (this is called a perspective-divide)
			var perspective = new Vector3(transform.X / transform.W, transform.Y / transform.W, transform.Z / transform.W);
			// Now we return the direction vector of the given x/y coordinates from the camera position.
			return new Vector3(perspective.X, perspective.Y, perspective.Z) - Position;
		}
	}*/

	class Camera
	{
		GameWindow window;
		float sens = 0.0025f;

		float hAngle = 3.14f;
		float vAngle = 0.0f;

		public Vector3 position { get; private set; } = new Vector3(1, 1, 0);
		public Vector3 direction { get; private set; } = new Vector3(0, 0, 0);
		public Vector3 right { get; private set; } = new Vector3(0, 0, 0);
		public Vector3 up { get; private set; } = new Vector3(0, 0, 0);

		public Camera(GameWindow gamewindow)
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
			if (Keyboard.GetState().IsKeyDown(Key.W))
			{
				position += direction * (float)time * 3f;
			}
			if (Keyboard.GetState().IsKeyDown(Key.A))
			{
				position -= right * (float)time * 3f;
			}
			if (Keyboard.GetState().IsKeyDown(Key.S))
			{
				position -= direction * (float)time * 3f;
			}
			if (Keyboard.GetState().IsKeyDown(Key.D))
			{
				position += right * (float)time * 3f;
			}
		}

		public Vector3 GetNormalizedDeviceRay(float x, float y, Matrix4 VP)
		{
			// Invert View and Projection matrix
			var invertedProjectionView = Matrix4.Invert(VP);
			// Convert from clipping space (device) coordinates to world space by multiplying by the inverse of VP matrix
			var transform = new Vector4(x, y, 0.0f, 1.0f) * invertedProjectionView;
			// Divide by w (this is called a perspective-divide)
			var perspective = new Vector3(transform.X / transform.W, transform.Y / transform.W, transform.Z / transform.W);
			// Now we return the direction vector of the given x/y coordinates from the camera position.
			return new Vector3(perspective.X, perspective.Y, perspective.Z) - position;
		}
	}
}
