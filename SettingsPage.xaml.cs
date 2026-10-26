using CalorieScanner.Services;

namespace CalorieScanner;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		// a tidy column no wider than 560, but never wider than the screen (small phones)
		SizeChanged += (_, _) => { if (Width > 0) Column.WidthRequest = Math.Min(Width, 560); };
		ModelEntry.Text = AppSettings.Model;
		ThemePicker.ItemsSource = new[] { "System default", "Light", "Dark" };
		ThemePicker.SelectedIndex = AppSettings.Theme switch { "light" => 1, "dark" => 2, _ => 0 };
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		KeyEntry.Text = await AppSettings.GetApiKeyAsync();
	}

	async void OnPickModelClicked(object? sender, EventArgs e)
	{
		PickModelButton.IsEnabled = false;
		try
		{
			var models = await new GeminiClient().ListModelsAsync((KeyEntry.Text ?? "").Trim());
			if (models.Count == 0)
			{
				await DisplayAlertAsync("No models", "This key has no models that can read photos.", "OK");
				return;
			}
			var choice = await DisplayActionSheetAsync("Choose a model", "Cancel", null, models.Take(12).ToArray());
			if (!string.IsNullOrEmpty(choice) && choice != "Cancel")
			{
				ModelEntry.Text = choice;
				SavedLabel.Text = "Tap Save to use it.";
				SavedLabel.IsVisible = true;
			}
		}
		catch (GeminiException ex)
		{
			await DisplayAlertAsync("Could not load models", ex.Message, "OK");
		}
		finally
		{
			PickModelButton.IsEnabled = true;
		}
	}

	async void OnSaveClicked(object? sender, EventArgs e)
	{
		await AppSettings.SetApiKeyAsync(KeyEntry.Text ?? "");
		AppSettings.Model = ModelEntry.Text ?? "";
		AppSettings.Theme = ThemePicker.SelectedIndex switch { 1 => "light", 2 => "dark", _ => "system" };
		AppSettings.ApplyTheme();

		var saved = await AppSettings.GetApiKeyAsync() == (KeyEntry.Text ?? "").Trim();
		SavedLabel.Text = saved ? "Saved." : "The key could not be saved on this device.";
		SavedLabel.IsVisible = true;
	}
}
