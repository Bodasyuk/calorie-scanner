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

	async void OnSaveClicked(object? sender, EventArgs e)
	{
		await AppSettings.SetApiKeyAsync(KeyEntry.Text ?? "");
		AppSettings.Model = ModelEntry.Text ?? "";

		var saved = await AppSettings.GetApiKeyAsync() == (KeyEntry.Text ?? "").Trim();
		SavedLabel.Text = saved ? "Saved." : "The key could not be saved on this device.";
		SavedLabel.IsVisible = true;
	}
}
