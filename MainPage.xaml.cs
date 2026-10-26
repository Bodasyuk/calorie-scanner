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
	}

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
			using var image = PlatformImage.FromStream(stream);
			using IImage small = image.Downsize(MaxImageSide, true);
			_jpeg = small.AsBytes(ImageFormat.Jpeg, 0.8f);

			Preview.Source = ImageSource.FromStream(() => new MemoryStream(_jpeg));
			PlaceholderLabel.IsVisible = false;
			AnalyzeButton.IsEnabled = true;
			ClearResult();
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
		TakeButton.IsEnabled = PickButton.IsEnabled = !busy;
	}

	void ShowStatus(string message)
	{
		StatusLabel.Text = message;
		StatusLabel.IsVisible = true;
	}
}
