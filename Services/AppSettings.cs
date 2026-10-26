namespace CalorieScanner.Services;

/// <summary>The API key lives in the phone's secure storage (Keychain on iPhone), never in the code.</summary>
public static class AppSettings
{
	const string KeyName = "gemini_api_key";
	const string ModelName = "gemini_model";
	public const string DefaultModel = "gemini-3.1-flash-lite"; // answered a photo in ~4 s in testing
	// earlier defaults: 2.5-flash is closed to new users, 3.8-flash was overloaded; people who saved them move to the new default
	static readonly string[] RetiredModels = ["gemini-2.5-flash", "gemini-3.8-flash"];

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
