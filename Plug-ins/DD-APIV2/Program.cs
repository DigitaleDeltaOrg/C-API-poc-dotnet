using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DD_API;

/// <summary>
/// Program entry class.
/// </summary>
internal static class Program
{
	/// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Arguments passed through the command line.</param>
	public static void Main(string[] args)
	{
		CreateHostBuilder(args).Build().Run();
	}

	/// <summary>
	/// Helper for creating the application instance.
	/// </summary>
	/// <param name="args">Arguments passed through the command line.</param>
	/// <returns></returns>
	private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}