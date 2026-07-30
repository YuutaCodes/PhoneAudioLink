using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneAudioLink
{
	public static class Program
	{
		private const string InstanceKey = "PhoneAudioLink.Main";

		[STAThread]
		private static int Main(string[] args)
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			if (IsRedirectedToRunningInstance())
			{
				return 0;
			}

			Application.Start(p =>
			{
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				_ = new App();
			});

			return 0;
		}

		/// <summary>
		/// Claims the single-instance key. If another process already holds it, the activation is
		/// handed over to that process and this one should exit.
		/// </summary>
		private static bool IsRedirectedToRunningInstance()
		{
			var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
			var keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

			if (keyInstance.IsCurrent)
			{
				keyInstance.Activated += (_, e) => (Application.Current as App)?.OnRedirectedActivation(e);
				return false;
			}

			RedirectActivationTo(activationArgs, keyInstance);
			return true;
		}

		private static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
		{
			// RedirectActivationToAsync must not block the STA thread outright, so pump COM while waiting.
			IntPtr redirectEvent = CreateEvent(IntPtr.Zero, true, false, null);

			_ = Task.Run(() =>
			{
				keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
				SetEvent(redirectEvent);
			});

			_ = CoWaitForMultipleObjects(CWMO_DEFAULT, INFINITE, 1, new[] { redirectEvent }, out _);
			CloseHandle(redirectEvent);
		}

		private const uint CWMO_DEFAULT = 0;
		private const uint INFINITE = 0xFFFFFFFF;

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetEvent(IntPtr hEvent);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("ole32.dll")]
		private static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, ulong nHandles, IntPtr[] pHandles, out uint dwIndex);
	}
}
