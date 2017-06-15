using System;
using Gtk;

namespace gtktfidf
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			wMain win = new wMain ();
			//win.Maximize();
			win.Show ();
			Application.Run ();
		}
	}
}

