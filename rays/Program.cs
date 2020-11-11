using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Windowing.Desktop;

namespace rays
{
	class Program
	{
		static void Main(string[] args)
		{
			var gws = new GameWindowSettings()
			{
				IsMultiThreaded = false
			};
			var nws = new NativeWindowSettings()
			{
				Size = new OpenTK.Mathematics.Vector2i(1600, 900)
				
			};


			using (Game game = new Game(gws, nws))
			{
				game.VSync = OpenTK.Windowing.Common.VSyncMode.On;
				game.Run();
				
			}
		}
	}
}
