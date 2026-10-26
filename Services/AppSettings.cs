namespace CalorieScanner.Services;

/// <summary>The API key lives in the phone's secure storage (Keychain on iPhone), never in the code.</summary>
public static class AppSettings
{
	const string KeyName = "gemini_api_key";
	const string ModelName = "gemini_model";
	// In a test with a meal photo these two answered 3 of 3 times in 2-8 s; the other models tried timed out or returned 503
	public const string DefaultModel = "gemini-3.1-flash-lite-preview";
	public const string FallbackModel = "gemini-3-flash-preview";
	// earlier defaults: 2.5-flash is closed to new users, 3.8-flash and 3.1-flash-lite were overloaded; people who saved them move to the new default
	static readonly string[] RetiredModels = ["gemini-2.5-flash", "gemini-3.8-flash", "gemini-3.1-flash-lite"];

	public static async Task<string> GetApiKeyAsync()
	{
		try { return await SecureStorage.Default.GetAsync(KeyName) ?? ""; }
		catch { return ""; } // secure storage can be unavailable (e.g. simulator without keychain)
	}

	public static async Task SetApiKeyAsync(string key)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(key)) SecureStorage.Default.Remove(KeyName);
			else await SecureStorage.Default.SetAsync(KeyName, key.Trim());
		}
		catch { /* nothing sensible to do; caller shows the "not saved" case via a re-read */ }
	}

	/// <summary>"system", "light" or "dark".</summary>
	public static string Theme
	{
		get => Preferences.Default.Get("theme", "system");
		set => Preferences.Default.Set("theme", value);
	}

	public static void ApplyTheme()
	{
		// CALSCAN_THEME is a developer override used to preview each theme without changing the system setting
		var choice = Environment.GetEnvironmentVariable("CALSCAN_THEME") ?? Theme;
		if (Application.Current is { } app)
			app.UserAppTheme = choice switch { "light" => AppTheme.Light, "dark" => AppTheme.Dark, _ => AppTheme.Unspecified };
	}

	public static string Model
	{
		get
		{
			var saved = Preferences.Default.Get(ModelName, DefaultModel);
			return RetiredModels.Contains(saved) ? DefaultModel : saved;
		}
		set => Preferences.Default.Set(ModelName, string.IsNullOrWhiteSpace(value) ? DefaultModel : value.Trim());
	}
}
