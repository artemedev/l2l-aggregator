using l2l_aggregator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace l2l_aggregator
{
    public static class ServiceConfigurator
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
        {
            // Регистрируем главную VM (она требует HistoryRouter)
            services.AddSingleton<MainWindowViewModel>();

        }
    }
}
