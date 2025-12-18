using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace ToDoMauiClient
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Инициализируем состояние кнопок
            UpdateButtonsVisibility();

            // Подписываемся на события навигации
            this.Navigated += OnNavigated;
        }

        private void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            UpdateButtonsVisibility();
            UpdateUserInfo();
        }

        private void UpdateButtonsVisibility()
        {
            // Проверяем наличие токена авторизации
            var hasToken = !string.IsNullOrEmpty(Preferences.Get("AccessToken", null));

            // Показываем кнопку выхода из аккаунта только если пользователь авторизован
            LogoutButton.IsVisible = hasToken;

            // Показываем/скрываем элементы Flyout в зависимости от авторизации
            UpdateFlyoutItemsVisibility(hasToken);
        }

        private void UpdateFlyoutItemsVisibility(bool isLoggedIn)
        {
            // Получаем все FlyoutItem
            var flyoutItems = Items.OfType<FlyoutItem>().ToList();

            // Показываем/скрываем FlyoutItem в зависимости от авторизации
            foreach (var item in flyoutItems)
            {
                item.IsVisible = isLoggedIn;
            }

            // Если не авторизован, скрываем Flyout меню
            FlyoutBehavior = isLoggedIn ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;

            // Если не авторизован, перенаправляем на страницу логина
            if (!isLoggedIn && CurrentState?.Location?.OriginalString != "//login")
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await GoToAsync("///login");
                });
            }
        }

        private void UpdateUserInfo()
        {
            var hasToken = !string.IsNullOrEmpty(Preferences.Get("AccessToken", null));

            if (hasToken)
            {
                // Получаем имя пользователя из Preferences
                var username = Preferences.Get("Username", "Пользователь");
                UserNameLabel.Text = username;
            }
            else
            {
                UserNameLabel.Text = "Гость";
            }
        }

        // Обработчик выхода из аккаунта
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Выход",
                "Вы уверены, что хотите выйти из аккаунта?",
                "Да", "Нет");

            if (confirm)
            {
                // Очищаем токены и данные пользователя
                Preferences.Remove("AccessToken");
                Preferences.Remove("RefreshToken");
                Preferences.Remove("Username");

                // Обновляем состояние
                UpdateButtonsVisibility();

                // Переходим на страницу логина
                await GoToAsync("///login");
            }
        }

        // Обработчик выхода из приложения
        private async void OnExitAppClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Выход из приложения",
                "Вы уверены, что хотите закрыть приложение?",
                "Да", "Нет");

            if (confirm)
            {
                // Выход из приложения
                ExitApplication();
            }
        }

        private void ExitApplication()
        {
            // Метод для выхода из приложения в зависимости от платформы
#if ANDROID
            // Для Android
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif IOS
            // Для iOS нельзя принудительно закрыть приложение
            // Можно просто перейти на стартовую страницу
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Информация", 
                    "Для выхода из приложения нажмите кнопку Home", 
                    "OK");
            });
#elif WINDOWS
            // Для Windows
            Application.Current.Quit();
#else
            // Для других платформ
            Application.Current.Quit();
#endif
        }

        // Публичный метод для обновления профиля из других страниц
        public void UpdateUserProfile(string username)
        {
            UserNameLabel.Text = username;
            Preferences.Set("Username", username);
            UpdateButtonsVisibility();
        }

        // Переопределяем метод для предотвращения навигации назад к логину после авторизации
        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // Если пользователь не авторизован и пытается перейти не на страницу логина/регистрации
            var hasToken = !string.IsNullOrEmpty(Preferences.Get("AccessToken", null));
            var target = args.Target.Location.OriginalString;

            if (!hasToken && !target.Contains("login") && !target.Contains("register"))
            {
                // Отменяем навигацию и перенаправляем на логин
                args.Cancel();
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await GoToAsync("///login");
                });
            }
        }
    }
}