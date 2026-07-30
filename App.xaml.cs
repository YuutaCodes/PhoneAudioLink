using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhoneAudioLink
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		private Window? _window;

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			_window = new MainWindow();
			_window.Activate();
		}

		/// <summary>
		/// Invoked on the running instance when another launch was redirected here.
		/// Restores the window instead of opening a second one.
		/// </summary>
		internal void OnRedirectedActivation(Microsoft.Windows.AppLifecycle.AppActivationArguments args)
		{
			_window?.DispatcherQueue.TryEnqueue(() =>
			{
				if (_window is null)
				{
					return;
				}

				_window.AppWindow.Show();
				_window.Activate();
				SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(_window));
			});
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
