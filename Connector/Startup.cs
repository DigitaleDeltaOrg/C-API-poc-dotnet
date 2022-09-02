using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using StorageShared;

namespace Connector;

public class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration       = configuration;
	}

	private IConfiguration Configuration { get; }

	public void ConfigureServices(IServiceCollection services)
	{
		// Default Policy
		services.AddCors(options =>
		{
			options.AddDefaultPolicy(
				builder =>
				{
					builder
						.AllowAnyOrigin()
						.AllowAnyHeader()
						.AllowAnyMethod()
						.SetIsOriginAllowed(isOriginAllowed: _ => true);
				});
		});

		services.AddScoped<IStorageProvider>(x => ActivatorUtilities.CreateInstance<CApiJsonStorage.CApiJsonStore>(x, Configuration.GetValue<string>("StorageFolder"))); 
		services.AddControllers().AddNewtonsoftJson(o => { o.SerializerSettings.Converters.Add(new StringEnumConverter()); });
		services.AddSwaggerGenNewtonsoftSupport();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Connector for C-API (Convenience API)", Version = "v1" }); 
		});
		services.AddSwaggerGenNewtonsoftSupport();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		app.UseSwagger();
		app.UseSwagger();
		app.UseSwaggerUI(c =>
		{
			c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Connector for C-API (Convenience API)");
			c.RoutePrefix = string.Empty;
		});
		app.UseHttpsRedirection();
		app.UseRouting();
		app.UseCors();
		app.UseAuthorization();
		app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
	}
}