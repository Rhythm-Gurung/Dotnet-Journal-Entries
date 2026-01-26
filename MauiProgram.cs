using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using QuestPDF.Infrastructure;

namespace journalstart;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Configure QuestPDF License
		QuestPDF.Settings.License = LicenseType.Community;

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddSingleton<Services.JournalService>();
		builder.Services.AddScoped<Services.ThemeService>();
		builder.Services.AddSingleton<Services.PdfService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
