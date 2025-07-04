﻿using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK_Lighting;
class Program
{
	[STAThread]
	static void Main()
	{
		var nativeSettings = new NativeWindowSettings()
		{
			Size = new Vector2i(1280, 720),
			Title = "Lighting OpenTK"
		};
		nativeSettings.WindowState = WindowState.Fullscreen;
		using var window = new MainWindow(GameWindowSettings.Default, nativeSettings);
		window.Run();
	}
}