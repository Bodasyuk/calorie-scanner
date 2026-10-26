using Microsoft.Extensions.DependencyInjection;

namespace CalorieScanner;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Services.AppSettings.ApplyTheme();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}