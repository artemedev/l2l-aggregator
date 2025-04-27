using Avalonia.SimpleRouter;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services.Notification;
using l2l_aggregator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using l2l_aggregator.Services.Database;
using System.IO;
using System;
using l2l_aggregator.Infrastructure.OsIntegration.Firebird;
using l2l_aggregator.Services.Database.Repositories;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using l2l_aggregator.Services.Database.Interfaces;
using Refit;
using System.Net.Http;

namespace l2l_aggregator
{
    public static class ServiceConfigurator
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Регистрируем главную VM (она требует HistoryRouter)
            services.AddSingleton<MainWindowViewModel>();

            // Регистрация ViewModels (они зависят от HistoryRouter)
            services.AddTransient<InitializationViewModel>();


            // Регистрируем HistoryRouter перед ViewModels
            services.AddSingleton<HistoryRouter<ViewModelBase>>(s =>
                new HistoryRouter<ViewModelBase>(t => (ViewModelBase)s.GetRequiredService(t)));
            
            // Регистрируем работу с бд
            services.AddSingleton<IUserAuthRepository, UserAuthRepository>();
            services.AddSingleton<IConfigRepository, ConfigRepository>();
            services.AddSingleton<IRegistrationDeviceRepository, RegistrationDeviceRepository>();

            services.AddSingleton<DatabaseService>();
            services.AddSingleton<DatabaseInitializer>(sp =>
            {
                string databasePath;
                //= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TEST.fdb");
                if (OperatingSystem.IsWindows())
                {
                    databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TEST.fdb");
                }
                else
                {
                    LinuxFirebirdPermissionFixer.EnsureFirebirdDirectoryAccess();
                    // Используем системную директорию, куда firebird точно имеет доступ
                    databasePath = "/var/lib/l2l_aggregator/TEST.fdb";

                }
                return new DatabaseInitializer(databasePath);
            });
            // Регистрируем Refit API-клиенты
            var refitSettings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }
            };
            services.AddRefitClient<ITaskApi>(refitSettings)
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://172.16.3.196:5005"));
            services.AddRefitClient<IAuthApi>(refitSettings)
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://172.16.3.196:5005"));
        }
    }
}
