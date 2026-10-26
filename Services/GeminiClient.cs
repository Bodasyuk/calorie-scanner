using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CalorieScanner.Models;

namespace CalorieScanner.Services;

public class GeminiException(string message) : Exception(message);

public class GeminiClient
{
	// timeouts are enforced per call with a cancellation token, so there is no HttpClient-wide limit
	static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

	// nobody should wait longer than this for a photo; slow or busy models are skipped in favour of the next one
	static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds(12);
	static readonly string[] FallbackModels = [AppSettings.DefaultModel, "gemini-flash-lite-latest"];

	const string Prompt =
		"You are a nutrition assistant. Look at the photo of a meal and list every distinct food or drink you can see. " +
		"For each, estimate the visible portion in grams (or millilitres for drinks) and the nutrition for that portion. " +
		"Be realistic about cooking oil and sauces. If the user gave a hint, trust it over the picture. " +
		"If the photo contains no food, return an empty list.";

	public async Task<List<MealItem>> AnalyzeAsync(byte[] jpeg, string hint, string apiKey, string model, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new GeminiException("Add your Gemini API key in Settings first.");

		var text = string.IsNullOrWhiteSpace(hint) ? Prompt : $"{Prompt}\nUser hint: {hint.Trim()}";
		var body = new JsonObject
		{
			["contents"] = new JsonArray(new JsonObject
			{
				["parts"] = new JsonArray(
					new JsonObject { ["text"] = text },
					new JsonObject { ["inline_data"] = new JsonObject { ["mime_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(jpeg) } })
			}),
			["generationConfig"] = new JsonObject
			{
				["temperature"] = 0.2,
				["responseMimeType"] = "application/json",
				["responseSchema"] = Schema(),
			},
		};

		using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
		budget.CancelAfter(TotalBudget);

		string? lastError = null;
		foreach (var candidate in new[] { model }.Concat(FallbackModels).Distinct())
		{
			var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(candidate)}:generateContent";

			// the key goes in a header, not the URL, so it doesn't end up in logs
			using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
			request.Headers.Add("x-goog-api-key", apiKey);

			try
			{
				using var response = await Http.SendAsync(request, budget.Token);
				var json = await response.Content.ReadAsStringAsync(budget.Token);
				if (response.IsSuccessStatusCode)
					return Parse(json);

				// busy (5xx) or retired (404) model: try the next one; anything else (bad key, quota) won't improve
				var status = (int)response.StatusCode;
				lastError = Describe(status, json);
				if (status is not (404 or 500 or 502 or 503 or 504))
					throw new GeminiException(lastError);
			}
			catch (HttpRequestException) { throw new GeminiException("No connection. Check your internet and try again."); }
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				throw new GeminiException("The AI is too slow right now. Please try again.");
			}
		}

		throw new GeminiException(lastError ?? "Google's AI is busy right now. Please try again in a minute.");
	}

	/// <summary>Models this key can call for text+image answers, fastest-looking first.</summary>
	public async Task<List<string>> ListModelsAsync(string apiKey, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new GeminiException("Enter your Gemini API key first.");

		using var request = new HttpRequestMessage(HttpMethod.Get,
			"https://generativelanguage.googleapis.com/v1beta/models?pageSize=200");
		request.Headers.Add("x-goog-api-key", apiKey);

		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromSeconds(10));

		HttpResponseMessage response;
		try { response = await Http.SendAsync(request, timeout.Token); }
		catch (HttpRequestException) { throw new GeminiException("No connection. Check your internet and try again."); }
		catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new GeminiException("The request timed out. Try again."); }

		var json = await response.Content.ReadAsStringAsync(timeout.Token);
		if (!response.IsSuccessStatusCode)
			throw new GeminiException(Describe((int)response.StatusCode, json));

		string[] skip = ["image", "tts", "embedding", "audio", "live", "imagen", "veo", "robotics", "aqa", "computer"];
		var models = JsonNode.Parse(json)?["models"]?.AsArray() ?? [];
		return models
			.Where(m => m?["supportedGenerationMethods"]?.AsArray().Any(x => x?.GetValue<string>() == "generateContent") == true)
			.Select(m => (m?["name"]?.GetValue<string>() ?? "").Replace("models/", ""))
			.Where(n => n.StartsWith("gemini", StringComparison.OrdinalIgnoreCase) && !skip.Any(s => n.Contains(s, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(n => n.Contains("lite") ? 0 : n.Contains("flash") ? 1 : 2) // lite/flash answer fastest
			.ThenByDescending(n => n)
			.Distinct()
			.ToList();
	}

	static JsonObject Schema() => new()
	{
		["type"] = "OBJECT",
		["properties"] = new JsonObject
		{
			["items"] = new JsonObject
			{
				["type"] = "ARRAY",
				["items"] = new JsonObject
				{
					["type"] = "OBJECT",
					["properties"] = new JsonObject
					{
						["name"] = new JsonObject { ["type"] = "STRING" },
						["grams"] = new JsonObject { ["type"] = "NUMBER" },
						["calories"] = new JsonObject { ["type"] = "NUMBER" },
						["protein_g"] = new JsonObject { ["type"] = "NUMBER" },
						["carbs_g"] = new JsonObject { ["type"] = "NUMBER" },
						["fat_g"] = new JsonObject { ["type"] = "NUMBER" },
					},
					["required"] = new JsonArray("name", "grams", "calories", "protein_g", "carbs_g", "fat_g"),
				},
			},
		},
		["required"] = new JsonArray("items"),
	};

	static List<MealItem> Parse(string responseJson)
	{
		try
		{
			var root = JsonNode.Parse(responseJson);
			var text = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
			if (string.IsNullOrWhiteSpace(text))
				throw new GeminiException("The AI returned no answer. Try another photo.");

			var items = JsonNode.Parse(text)?["items"]?.AsArray() ?? [];
			return items.Select(i => MealItem.FromEstimate(
				i?["name"]?.GetValue<string>() ?? "Unknown",
				Num(i, "grams"), Num(i, "calories"), Num(i, "protein_g"), Num(i, "carbs_g"), Num(i, "fat_g"))).ToList();
		}
		catch (JsonException)
		{
			throw new GeminiException("Could not read the AI answer. Try again.");
		}
	}

	static double Num(JsonNode? node, string name) => node?[name]?.GetValue<double>() ?? 0;

	static string Describe(int status, string body)
	{
		var message = "";
		try { message = JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>() ?? ""; } catch (JsonException) { }

		return status switch
		{
			400 or 403 when message.Contains("API key", StringComparison.OrdinalIgnoreCase) => "The API key was rejected. Check it in Settings.",
			429 => "Free daily limit reached. Try again later.",
			500 or 502 or 503 or 504 => "Google's AI is busy right now (tried several times). Wait a minute and try again.",
			404 => $"Model not found. Check the model name in Settings. {message}",
			_ => $"Request failed ({status}). {message}".Trim(),
		};
	}
}
