using System.ComponentModel;

namespace CalorieScanner.Models;

/// <summary>
/// One food found in the photo. The AI estimates the nutrition for its guessed weight;
/// we keep it per gram so the user can correct the weight and everything rescales.
/// </summary>
public class MealItem : INotifyPropertyChanged
{
	double _grams;

	public string Name { get; init; } = "";
	public double CaloriesPerGram { get; init; }
	public double ProteinPerGram { get; init; }
	public double CarbsPerGram { get; init; }
	public double FatPerGram { get; init; }

	public double Grams
	{
		get => _grams;
		set
		{
			if (Math.Abs(_grams - value) < 1e-9) return;
			_grams = Math.Max(0, value);
			foreach (var p in new[] { nameof(Grams), nameof(Calories), nameof(Protein), nameof(Carbs), nameof(Fat), nameof(Summary) })
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
		}
	}

	public double Calories => Grams * CaloriesPerGram;
	public double Protein => Grams * ProteinPerGram;
	public double Carbs => Grams * CarbsPerGram;
	public double Fat => Grams * FatPerGram;

	public string Summary => $"{Calories:0} kcal  ·  P {Protein:0} g  ·  C {Carbs:0} g  ·  F {Fat:0} g";

	public static MealItem FromEstimate(string name, double grams, double calories, double protein, double carbs, double fat)
	{
		var perGram = grams > 0 ? 1 / grams : 0;
		return new MealItem
		{
			Name = name,
			CaloriesPerGram = calories * perGram,
			ProteinPerGram = protein * perGram,
			CarbsPerGram = carbs * perGram,
			FatPerGram = fat * perGram,
			Grams = grams,
		};
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}
