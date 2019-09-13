using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace rays
{
	class Program
	{
		static void Main(string[] args)
		{
			using (Game game = new Game(1280, 720))
			{
				
				game.Run(120);
				
			}
		}
	}
}
