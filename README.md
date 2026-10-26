# Calorie Scanner

Take a photo of a meal and get an estimate of calories, protein, carbs and fat. Built with .NET MAUI (iPhone and Android). The AI part uses the Gemini API with your own free key.

You can correct the weight of each food and the totals update. Estimates are rough: a photo cannot show oil, sauces or exact weight.

## Run on an iPhone (needs a Mac)

1. Install **Xcode** from the App Store and open it once to accept the licence.
2. Install the **.NET 10 SDK** and the iOS workload:
   ```bash
   dotnet workload install maui-ios
   ```
3. Change `ApplicationId` in `CalorieScanner.csproj` to something unique, e.g. `com.yourname.caloriescanner`.
4. In Xcode, sign in with your Apple ID (Settings > Accounts). A free Apple ID works, but the app expires after 7 days; the paid developer account ($99/year) removes that.
5. Open the folder in **VS Code** with the *C# Dev Kit* and *.NET MAUI* extensions, pick your iPhone as the target and run. Or from a terminal:
   ```bash
   dotnet build -f net10.0-ios -t:Run
   ```
   The iPhone must be plugged in, unlocked and in Developer Mode (Settings > Privacy & Security).
6. Open the app > **Settings**, paste your Gemini key from <https://aistudio.google.com> and save.

## Notes

- The key is kept in the phone's secure storage (Keychain). It is never in the code or in git.
- The default model is `gemini-3.1-flash-lite`. If Google retires it, change the model name in Settings.
- The free tier has daily limits and Google may use free-tier prompts to improve its models. Check the current terms.
- Android: `dotnet workload install maui-android`, then `dotnet build -f net10.0-android -t:Run`.
