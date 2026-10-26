using CalorieScanner.Models;
using CalorieScanner.Services;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CalorieScanner;

public partial class MainPage : ContentPage
{
	const float MaxImageSide = 1024f;

	readonly GeminiClient _gemini = new();
	byte[]? _jpeg;
	List<MealItem> _items = [];

	public MainPage()
	{
		InitializeComponent();
#if WINDOWS
		Loaded += (_, _) => AddPasteShortcut();
#else
		// pasting a picture is only built for Windows; phones use the camera and photo library
		PasteButton.IsVisible = false;
		ButtonRow.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];
#endif
	}

#if WINDOWS
	// Ctrl+V pastes a picture, but only when the clipboard holds one, so pasting text into the hint box still works
	void AddPasteShortcut()
	{
		if (Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement view) return;

		var shortcut = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
		{
			Key = Windows.System.VirtualKey.V,
			Modifiers = Windows.System.VirtualKeyModifiers.Control,
		};
		shortcut.Invoked += async (_, args) =>
		{
			if (!ClipboardImage.HasImageNow()) return;
			args.Handled = true;
			await PasteAsync();
		};
		view.KeyboardAccelerators.Add(shortcut);
	}
#endif

	async void OnPasteClicked(object? sender, EventArgs e)
	{
#if WINDOWS
		await PasteAsync();
#else
		await Task.CompletedTask; // the button is hidden on phones
#endif
	}

#if WINDOWS
	async Task PasteAsync()
	{
		try
		{
			var bytes = await ClipboardImage.GetAsync();
			if (bytes is null)
			{
				ShowStatus("No picture on the clipboard. Copy a photo first, then press Paste.");
				return;
			}
			await using var stream = new MemoryStream(bytes);
			UseImage(stream);
		}
		catch (Exception ex)
		{
			ShowStatus($"Could not paste the picture. {ex.Message}");
		}
	}
#endif

	async void OnSettingsClicked(object? sender, EventArgs e) => await Navigation.PushAsync(new SettingsPage());

	async void OnTakePhotoClicked(object? sender, EventArgs e)
	{
		if (!MediaPicker.Default.IsCaptureSupported)
		{
			ShowStatus("This device has no camera. Use \"Choose photo\" instead.");
			return;
		}
		await LoadPhotoAsync(() => MediaPicker.Default.CapturePhotoAsync());
	}

	async void OnPickPhotoClicked(object? sender, EventArgs e) =>
		await LoadPhotoAsync(async () =>
			(await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 })).FirstOrDefault());

	async Task LoadPhotoAsync(Func<Task<FileResult?>> getPhoto)
	{
		try
		{
			var photo = await getPhoto();
			if (photo is null) return; // user cancelled

			await using var stream = await photo.OpenReadAsync();
			UseImage(stream);
		}
		catch (PermissionException)
		{
			ShowStatus("Camera or photo access is off. Allow it in the phone's Settings.");
		}
		catch (Exception ex)
		{
			ShowStatus($"Could not load the photo. {ex.Message}");
		}
	}

	/// <summary>Shrinks a picture (taken, chosen or pasted) and shows it, ready to be analysed.</summary>
	void UseImage(Stream stream)
	{
		using var image = PlatformImage.FromStream(stream);
		using IImage small = image.Downsize(MaxImageSide, true);
		_jpeg = small.AsBytes(ImageFormat.Jpeg, 0.8f);

		var bytes = _jpeg;
		Preview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
		PlaceholderLabel.IsVisible = false;
		AnalyzeButton.IsEnabled = true;
		ClearResult();
	}

	async void OnAnalyzeClicked(object? sender, EventArgs e)
	{
		if (_jpeg is null) return;

		SetBusy(true);
		try
		{
			var key = await AppSettings.GetApiKeyAsync();
			_items = await _gemini.AnalyzeAsync(_jpeg, HintEntry.Text ?? "", key, AppSettings.Model);
			ShowResult();
		}
		catch (GeminiException ex)
		{
			ShowStatus(ex.Message);
		}
		catch (Exception ex)
		{
			ShowStatus($"Something went wrong. {ex.Message}");
		}
		finally
		{
			SetBusy(false);
		}
	}

	void ShowResult()
	{
		StatusLabel.IsVisible = false;
		BindableLayout.SetItemsSource(ItemsLayout, _items);
		TotalsCard.IsVisible = true;
		if (_items.Count == 0) ShowStatus("No food found in the photo. Try a closer shot.");
		UpdateTotals();
	}

	void ClearResult()
	{
		_items = [];
		BindableLayout.SetItemsSource(ItemsLayout, null);
		TotalsCard.IsVisible = false;
		StatusLabel.IsVisible = false;
	}

	void OnGramsChanged(object? sender, TextChangedEventArgs e) => UpdateTotals();

	void UpdateTotals()
	{
		TotalCaloriesLabel.Text = $"{_items.Sum(i => i.Calories):0} kcal";
		TotalMacrosLabel.Text =
			$"Protein {_items.Sum(i => i.Protein):0} g  ·  Carbs {_items.Sum(i => i.Carbs):0} g  ·  Fat {_items.Sum(i => i.Fat):0} g";
	}

	void SetBusy(bool busy)
	{
		Busy.IsVisible = Busy.IsRunning = busy;
		AnalyzeButton.IsEnabled = !busy && _jpeg is not null;
		TakeButton.IsEnabled = PickButton.IsEnabled = PasteButton.IsEnabled = !busy;
	}

	void ShowStatus(string message)
	{
		StatusLabel.Text = message;
		StatusLabel.IsVisible = true;
	}
}
