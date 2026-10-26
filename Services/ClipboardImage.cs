#if WINDOWS
using Windows.ApplicationModel.DataTransfer;

namespace CalorieScanner.Services;

/// <summary>Pasting a picture is a desktop feature: MAUI's Clipboard only handles text, so Windows uses its own API.</summary>
public static class ClipboardImage
{
	/// <summary>The picture on the clipboard as image bytes, or null when the clipboard holds no picture.</summary>
	public static async Task<byte[]?> GetAsync()
	{
		var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
		if (!content.Contains(StandardDataFormats.Bitmap)) return null;

		var reference = await content.GetBitmapAsync();
		using var source = await reference.OpenReadAsync();
		using var stream = source.AsStreamForRead();
		using var copy = new MemoryStream();
		await stream.CopyToAsync(copy);
		return copy.ToArray();
	}

	/// <summary>True when Ctrl+V should paste a picture (so a text paste into the hint box is left alone).</summary>
	public static bool HasImageNow() =>
		Windows.ApplicationModel.DataTransfer.Clipboard.GetContent().Contains(StandardDataFormats.Bitmap);
}
#endif
