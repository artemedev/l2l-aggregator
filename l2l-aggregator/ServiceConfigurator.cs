using Avalonia.SimpleRouter;
using FastReport.Barcode;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Infrastructure.OsIntegration.Firebird;
using l2l_aggregator.Services.AggregationService;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Repositories;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.Notification;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using l2l_aggregator.Services.ScannerService.Interfaces;
using l2l_aggregator.Services.ScannerService;

namespace l2l_aggregator
{
    public static class ServiceConfigurator
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
        {
            // Регистрация базовых сервисов
            //services.AddSingleton<Preferences>();
            services.AddSingleton<SessionService>();
            services.AddSingleton<DataApiService>();
            //services.AddSingleton<ImageProcessingHelper>();
            services.AddSingleton<DmScanService>();
            services.AddSingleton<TemplateService>();
            services.AddSingleton<ScannerListenerService>();
            services.AddSingleton<ScannerInputService>();
            //services.AddSingleton<DMProcessingService>();
            services.AddSingleton<ImageHelper>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Регистрируем главную VM (она требует HistoryRouter)
            services.AddSingleton<MainWindowViewModel>();

            // Регистрация ViewModels (они зависят от HistoryRouter)
            services.AddTransient<InitializationViewModel>();
            services.AddTransient<AuthViewModel>();
            services.AddTransient<TaskListViewModel>();
            services.AddTransient<TaskDetailsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AggregationViewModel>();
            services.AddTransient<CameraSettingsViewModel>();


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
            //// Регистрируем Refit API-клиенты
            //var refitSettings = new RefitSettings
            //{
            //    HttpMessageHandlerFactory = () => new HttpClientHandler
            //    {
            //        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            //    }
            //};
            //services.AddRefitClient<ITaskApi>(refitSettings)
            //        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://172.16.3.196:5005"));
            //services.AddRefitClient<IAuthApi>(refitSettings)
            //        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://172.16.3.196:5005"));
            // Регистрируем работу с api
            services.AddSingleton<ApiClientFactory>();
            services.AddSingleton<DataApiService>();
            services.AddSingleton<DeviceCheckService>(); 
            services.AddSingleton<ConfigurationLoaderService>(); 
            services.AddSingleton<IScannerPortResolver>(PlatformResolverFactory.CreateScannerResolver());
        }
    }
}
