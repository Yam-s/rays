using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rays.Shapes
{
	public class Sphere
	{
		public Vector3 Position;
		public float Radius;

		
		public Vector3 Direction;
		public float Velocity;

		public Sphere(float x, float y, float z, float Radius)
		{
			Position = new Vector3(x, y, z);
			this.Radius = Radius;

			Direction = new Vector3(0.0f, 0.0f, 0.0f);
			Velocity = 1;
		}
	}
}
