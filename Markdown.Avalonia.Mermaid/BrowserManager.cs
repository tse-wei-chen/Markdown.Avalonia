using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Markdown.Avalonia.Mermaid
{
	/// <summary>
	/// Manages the lifecycle of the Playwright browser instance.
	/// This ensures we only have one browser process running and handles the initial installation/launch.
	/// </summary>
	public static class BrowserManager
	{
		private static IPlaywright? _playwright;
		private static IBrowser? _browser;
		private static IBrowserContext? _context;
		private static readonly SemaphoreSlim _browserLock = new(1, 1);

		/// <summary>
		/// Ensures a Chromium instance and Context is launched and ready.
		/// </summary>
		/// <param name="onStatusUpdate">Optional callback to report status strings (e.g., for UI updates).</param>
		/// <returns>The active IBrowserContext instance.</returns>
		public static async Task<IBrowserContext?> EnsureBrowserContextAsync(Action<string>? onStatusUpdate = null)
		{
			if (_context != null) return _context;

			await _browserLock.WaitAsync();
			try
			{
				if (_context != null) return _context;

				onStatusUpdate?.Invoke("Initializing internal browser engine...");
				_playwright ??= await Playwright.CreateAsync();

				if (_browser == null || !_browser.IsConnected)
				{
					onStatusUpdate?.Invoke("Launching browser...");
					try
					{
						_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
						{
							Headless = true,
							Args = new[] { "--no-sandbox", "--disable-gpu" }
						});
					}
					catch (PlaywrightException)
					{
						onStatusUpdate?.Invoke("Installing browser binaries (one-time)...");
						var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
						if (exitCode != 0) throw new Exception($"Install failed: {exitCode}");

						_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
						{
							Headless = true,
							Args = new[] { "--no-sandbox", "--disable-gpu" }
						});
					}
				}

				_context = await _browser.NewContextAsync(new BrowserNewContextOptions
				{
					DeviceScaleFactor = 2.0f
				});

				return _context;
			}
			finally
			{
				_browserLock.Release();
			}
		}
	}
}
