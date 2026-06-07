using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ox.SleepingGods.Data;
using Ox.SleepingGods.Data.Schema;
using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods
{
	using BrowserAPI;

	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			builder.Logging.SetMinimumLevel(LogLevel.Trace);

			// Silence render logs
			builder.Logging.AddFilter("Microsoft", LogLevel.Information);

			builder.Services.AddScoped(sp => new HttpClient());// { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

			builder.Services.AddBrowserAPI();
			builder.Services.ConfigureWithIndexedDb();
			builder.Services.AddSingleton<Management>((s) =>
			{
				using (var scope = s.CreateScope())
				{
					Management mgmt = new(scope.ServiceProvider.GetRequiredService<IndexedDBManager>());
					Task.Run(mgmt.LoadAsync);   // Kick this off
					return mgmt;
				}
			});
			builder.Services.AddTransient<KeywordRepository>();
			builder.Services.AddTransient<LocationRepository>();
			builder.Services.AddTransient<KeywordLocationRepository>();
			builder.Services.AddTransient<EtlManager>();
			builder.Services.AddSingleton<MessageManager>();
			builder.Services.AddSingleton<HomeStateManager>();
			builder.Services.AddSingleton<SyncChannel>();
			builder.Services.AddScoped<SyncMonitor>();

			var app = builder.Build();

			// Spawn the sync monitor
			await using var syncScope = app.Services.CreateAsyncScope();
			await using var syncMonitor = syncScope.ServiceProvider.GetRequiredService<SyncMonitor>();

			await Task.Yield();

			await syncMonitor.InitialImportAsync();

			// Fire up the app itself
			await app.RunAsync();
		}
	}
}
