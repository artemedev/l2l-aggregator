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
using l2l_aggregator.Services.Database.Interfaces;
using l2l_aggregator.Services.Database.Repositories;

namespace l2l_aggregator
{
    public static class ServiceConfigurator
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Регистрируем главную VM (она требует HistoryRouter)
            services.AddSingleton<MainWindowViewModel>();


            // Регистрируем HistoryRouter перед ViewModels
            services.AddSingleton<HistoryRouter<ViewModelBase>>(s =>
                new HistoryRouter<ViewModelBase>(t => (ViewModelBase)s.GetRequiredService(t)));

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
                    databasePath = "/var/lib/medtechtdapp/TEST.fdb";

                    // Убедись, что директория существует и имеет нужные права:
                    Directory.CreateDirectory("/var/lib/medtechtdapp");
                    try
                    {
                        // Firebird работает от имени firebird, так что нужно отдать владельца
                        var dirInfo = new DirectoryInfo("/var/lib/medtechtdapp");
                        dirInfo.SetAccessControl(new System.Security.AccessControl.DirectorySecurity());
                    }
                    catch { /* Если нет прав на chown — не страшно, ручной подход */ }
                }
                return new DatabaseInitializer(databasePath);
            });
        }
    }
}
