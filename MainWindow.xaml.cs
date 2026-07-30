using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Graphics;

namespace PhoneAudioLink
{
	public class DeviceItem
	{
		public string Name { get; set; } = string.Empty;
		public string Id { get; set; } = string.Empty;
		public DeviceInformation? DeviceInfo { get; set; }
	}

	public sealed partial class MainWindow : Window
	{
		public ObservableCollection<DeviceInformation> devices { get; } = new ObservableCollection<DeviceInformation>();
		private Dictionary<string, AudioPlaybackConnection> audioPlaybackConnections = new Dictionary<string, AudioPlaybackConnection>();
		private DeviceWatcher? deviceWatcher;

		public MainWindow()
		{
			this.InitializeComponent();

			this.AppWindow.Resize(new SizeInt32(450, 600));

			var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary);
			if (displayArea != null)
			{
				var centeredPosition = new PointInt32(
					(displayArea.WorkArea.Width - this.AppWindow.Size.Width) / 2,
					(displayArea.WorkArea.Height - this.AppWindow.Size.Height) / 2
				);
				this.AppWindow.Move(centeredPosition);
			}

			this.AppWindow.Closing += AppWindow_Closing;
		}

		private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			// cancel closing operation
			args.Cancel = true;


			// hide the window
			sender.Hide();
		}

		private void OpenMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			this.AppWindow.Show();
		}

		private void ExitMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			foreach (var connection in audioPlaybackConnections.Values)
			{
				connection.Dispose();
			}
			audioPlaybackConnections.Clear();

			if (deviceWatcher != null)
			{
				deviceWatcher.Added -= DeviceWatcher_Added;
				deviceWatcher.Removed -= DeviceWatcher_Removed;
				deviceWatcher.Stop();
			}

			TrayIcon.Dispose();

			Application.Current.Exit();
		}

		private void MainGrid_Loaded(object sender, RoutedEventArgs e)
		{
			deviceWatcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());

			// Register event handlers before starting the watcher. 
			deviceWatcher.Added += DeviceWatcher_Added;
			deviceWatcher.Removed += DeviceWatcher_Removed;

			deviceWatcher.Start();
		}

		private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				devices.Add(args);
			});
		}

		private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				var device = devices.FirstOrDefault(d => d.Id == args.Id);
				if (device != null)
				{
					devices.Remove(device);
				}
			});
		}

		private async void EnableAudioPlaybackConnectionButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedDevice = DeviceListView.SelectedItem as DeviceInformation;
			if (selectedDevice == null)
			{
				SetStatus("Select a device");
				return;
			}
			EnableAudioPlaybackConnectionButton.IsEnabled = false;
			LoadingSpinner.Visibility = Visibility.Visible;
			LoadingSpinner.IsActive = true;

			try
			{
				if (audioPlaybackConnections.TryGetValue(selectedDevice.Id, out var existingConnection))
				{
					existingConnection.StateChanged -= Connection_StateChanged;
					existingConnection.Dispose();
					audioPlaybackConnections.Remove(selectedDevice.Id);

					SetStatus("Disconnected");
					EnableAudioPlaybackConnectionButton.Content = "Connect";
				}
				else
				{
					foreach (var oldConnection in audioPlaybackConnections.Values)
					{
						oldConnection.StateChanged -= Connection_StateChanged;
						oldConnection.Dispose();
					}
					audioPlaybackConnections.Clear();

					AudioPlaybackConnection connection = AudioPlaybackConnection.TryCreateFromId(selectedDevice.Id);
					if (connection != null)
					{
						audioPlaybackConnections[selectedDevice.Id] = connection;
						connection.StateChanged += Connection_StateChanged;

						SetStatus("Connecting…");

						await Task.Run(() => connection.Start());
						await connection.OpenAsync();

						LoadingSpinner.IsActive = false;
						LoadingSpinner.Visibility = Visibility.Collapsed;
						EnableAudioPlaybackConnectionButton.Content = "Disconnect";
					}
					else
					{
						SetStatus("Unavailable", $"Windows would not create a connection for {selectedDevice.Name}.");
						EnableAudioPlaybackConnectionButton.Content = "Connect";
					}
				}

			}
			catch (Exception ex)
			{
				SetStatus("Connection failed", ex.Message);
				EnableAudioPlaybackConnectionButton.Content = "Connect";
			}
			finally
			{
				LoadingSpinner.IsActive = false;
				LoadingSpinner.Visibility = Visibility.Collapsed;
				EnableAudioPlaybackConnectionButton.IsEnabled = true;
			}
		}

		private void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedDevice = DeviceListView.SelectedItem as DeviceInformation;

			if (selectedDevice == null)
			{
				EnableAudioPlaybackConnectionButton.IsEnabled = false;
				EnableAudioPlaybackConnectionButton.Content = "Connect";
				return;
			}

			EnableAudioPlaybackConnectionButton.IsEnabled = true;

			if (audioPlaybackConnections.ContainsKey(selectedDevice.Id))
			{
				EnableAudioPlaybackConnectionButton.Content = "Disconnect";
			}
			else
			{
				EnableAudioPlaybackConnectionButton.Content = "Connect";
			}
		}
		private void Connection_StateChanged(AudioPlaybackConnection sender, object args)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				SetStatus(sender.State == AudioPlaybackConnectionState.Opened
					? "Connected"
					: "Disconnected");

				if (sender.State != AudioPlaybackConnectionState.Opened)
				{
					EnableAudioPlaybackConnectionButton.Content = "Connect";
				}
			});
		}

		private void SetStatus(string text, string? detail = null)
		{
			ConnectionState.Text = text;
			ToolTipService.SetToolTip(ConnectionState, detail);
		}
	}
}
