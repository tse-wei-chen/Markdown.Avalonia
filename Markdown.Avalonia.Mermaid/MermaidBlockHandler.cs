using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Styling;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;

namespace Markdown.Avalonia.Mermaid
{
	public class MermaidBlockHandler : IContainerBlockHandler, IMdAvPlugin
	{
		public static bool EnableMermaidRendering { get; set; } = true;
		public string Theme { get; set; } = "default";
		public string BackgroundColor { get; set; } = "transparent";

		public void Setup(SetupInfo info)
		{
			var cs = new ContainerSwitch();
			cs["mermaid"] = this;
			info.SetOnce(cs);

			InjectBlockOverride(info);
		}

		private void InjectBlockOverride(SetupInfo info)
		{
			var prop = typeof(SetupInfo).GetProperty("BlockOverrides", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (prop != null)
			{
				if (prop.GetValue(info) is IList list)
				{
					IBlockOverride? inner = null;
					foreach (var item in list)
					{
						if (item is IBlockOverride ibo && ibo.ParserName == "CodeBlocksWithLangEvaluator")
						{
							inner = ibo;
							break;
						}
					}

					if (inner != null)
					{
						list.Remove(inner);
					}

					info.Register(new MermaidFencedBlockOverride(inner, this));
				}
			}
		}

		public Border? ProvideControl(string assetPathRoot, string blockName, string lines)
		{
			if (!blockName.Trim().Equals("mermaid", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var border = new Border()
			{
				Child = new TextBlock() { Text = "Initializing Mermaid...", FontStyle = FontStyle.Italic, Foreground = Brushes.Gray, Margin = new Thickness(5) }
			};

			var theme = ResolveTheme();
			var bgColor = BackgroundColor;

			if (!EnableMermaidRendering)
			{
				return border;
			}

			Task.Run(() => PerformRenderAsync(border, lines, theme, bgColor));

			return border;
		}

		private string ResolveTheme()
		{
			var theme = Theme;
			if (string.Equals(theme, "default", StringComparison.OrdinalIgnoreCase))
			{
				bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
				if (!isDark && Application.Current?.RequestedThemeVariant == ThemeVariant.Dark)
				{
					isDark = true;
				}

				if (isDark)
				{
					return "dark";
				}
			}
			return theme;
		}

		private async Task PerformRenderAsync(Border border, string code, string theme, string bgColor)
		{
			try
			{
				var context = await BrowserManager.EnsureBrowserContextAsync(status =>
				{
					Dispatcher.UIThread.Post(() =>
					{
						if (border.Child is TextBlock tb) tb.Text = status;
					});
				});

				if (context == null) return;

				Dispatcher.UIThread.Post(() =>
				{
					if (border.Child is TextBlock tb) tb.Text = "Rendering Mermaid Diagram...";
				});

				var renderer = new MermaidRenderer();
				byte[]? pngBytes = await renderer.RenderPngAsync(context, code, theme, bgColor);
				
				if (pngBytes == null || pngBytes.Length == 0)
				{
					Dispatcher.UIThread.Post(() => border.Child = CreateErrorControl("Rendered Image was empty."));
					return;
				}

				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					try
					{
						using var stream = new MemoryStream(pngBytes);
						var bitmap = new Bitmap(stream);
						var img = new Image
						{
							Source = bitmap,
							Stretch = Stretch.Uniform
						};
						border.Child = img;
					}
					catch (Exception ex)
					{
						border.Child = CreateErrorControl($"Error loading image: {ex.Message}");
					}
				});
			}
			catch (Exception ex)
			{
				Dispatcher.UIThread.Post(() => border.Child = CreateErrorControl($"Error: {ex.Message}"));
			}
		}

		private Control CreateErrorControl(string message)
		{
			return new TextBlock { Text = message, Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap };
		}
	}
}
