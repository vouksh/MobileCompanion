using CommunityToolkit.Maui;
using InputKit.Shared.Controls;
using UraniumUI;
using UraniumUI.Icons.MaterialSymbols;

namespace MobileCompanion.Phone
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUIBlurs()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddMaterialSymbolsFonts();

                });

            builder.Services.AddCommunityToolkitDialogs();
            return builder.Build();
        }
    }
}
