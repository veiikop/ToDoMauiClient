using Microsoft.Extensions.Logging;
using ToDoMauiClient.Services;
using ToDoMauiClient.ViewModels;
using ToDoMauiClient.Views;

namespace ToDoMauiClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Сервисы
            builder.Services.AddSingleton<IApiService, ApiService>();

            // Страницы и ViewModel'ы
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();

            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();

            builder.Services.AddTransient<TodoListsPage>();
            builder.Services.AddTransient<TodoListsViewModel>();

            builder.Services.AddTransient<TodoListDetailPage>();
            builder.Services.AddTransient<TodoListDetailViewModel>();

            return builder.Build();
        }
    }
}
