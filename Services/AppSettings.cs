namespace CalorieScanner.Services;

/// <summary>The API key lives in the phone's secure storage (Keychain on iPhone), never in the code.</summary>
public static class AppSettings
{
	const string KeyName = "gemini_api_key";
	const string ModelName = "gemini_model";
	public const string DefaultModel = "gemini-2.5-flash";

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
		get => Preferences.Default.Get(ModelName, DefaultModel);
		set => Preferences.Default.Set(ModelName, string.IsNullOrWhiteSpace(value) ? DefaultModel : value.Trim());
	}
}
