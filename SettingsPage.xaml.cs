using CalorieScanner.Services;

namespace CalorieScanner;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		ModelEntry.Text = AppSettings.Model;
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

		var saved = await AppSettings.GetApiKeyAsync() == (KeyEntry.Text ?? "").Trim();
		SavedLabel.Text = saved ? "Saved." : "The key could not be saved on this device.";
		SavedLabel.IsVisible = true;
	}
}
