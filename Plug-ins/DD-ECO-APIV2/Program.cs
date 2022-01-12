using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DD_ECO_API;

/// <summary>
/// 
/// </summary>
internal static class Program
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="args"></param>
	public static void Main(string[] args)
	{
		CreateHostBuilder(args).Build().Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}