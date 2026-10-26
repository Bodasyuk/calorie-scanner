using Microsoft.Extensions.Logging;

namespace CalorieScanner;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if WINDOWS
		// Windows draws its own border and focus underline inside every Entry; the pages already wrap
		// text fields in a Material-style outline, so remove the native one to avoid a double border.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
		{
			var clear = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			var none = new Microsoft.UI.Xaml.Thickness(0);
			var box = handler.PlatformView;
			box.BorderThickness = none;
			foreach (var key in new[] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
			                            "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused" })
				box.Resources[key] = clear;
			box.Resources["TextControlBorderThemeThickness"] = none;
			box.Resources["TextControlBorderThemeThicknessFocused"] = none;
		});

		// same for the drop-down in Settings
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
		{
			var clear = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			var none = new Microsoft.UI.Xaml.Thickness(0);
			var box = handler.PlatformView;
			box.BorderThickness = none;
			foreach (var key in new[] { "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed", "ComboBoxBackgroundFocused",
			                            "ComboBoxBorderBrush", "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed", "ComboBoxBorderBrushFocused" })
				box.Resources[key] = clear;
			box.Resources["ComboBoxBorderThemeThickness"] = none;
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
