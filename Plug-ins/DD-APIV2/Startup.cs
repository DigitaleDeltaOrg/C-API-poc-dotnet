using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Shared.SwaggerHelpers;

namespace DD_API;

/// <summary>
/// Called by Program to bootstrap the configuration process.
/// </summary>
public class Startup
{
	/// <summary>
	/// Startup code.
	/// </summary>
	/// <param name="configuration"></param>
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	/// <summary>
	/// Configuration.
	/// </summary>
	private IConfiguration Configuration { get; }
		
	/// <summary>
	/// Configure services.
	/// </summary>
	/// <param name="services">Service collection instance.</param>
	public void ConfigureServices(IServiceCollection services)
	{
		IEnumerable<string> mimeTypes         = new[] { "text/plain", "text/html", "text/css", "font/woff2", "application/javascript", "application/json", "image/x-icon", "image/png", "application/vnd.geo+json" };
		var                 localAssemblyXml  = $"{Assembly.GetExecutingAssembly().GetName().Name}.XML";
		const string?       remoteAssemblyXml = "Common.XML"; // Holds the XML for the common library, containing the C-API structures.
			
		services.AddHttpClient();
		services.AddControllers().AddNewtonsoftJson(o =>
		{
			o.SerializerSettings.Converters.Add(new StringEnumConverter());
		});
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic DD-API v2 (Digital Delta-API) plug-in for C-API (Convenience API)", Version = "v1" }); 
			c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, localAssemblyXml)); 
			c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, remoteAssemblyXml));
			c.SchemaFilter<EnumTypesSchemaFilter>(Path.Combine(AppContext.BaseDirectory, localAssemblyXml));
			c.SchemaFilter<EnumTypesSchemaFilter>(Path.Combine(AppContext.BaseDirectory, remoteAssemblyXml));
			c.DocumentFilter<EnumTypesDocumentFilter>();
		});
		services.AddSwaggerGenNewtonsoftSupport();
		services.AddResponseCompression(options =>
		{
			options.EnableForHttps = true;
			options.MimeTypes      = mimeTypes;
			options.Providers.Add<BrotliCompressionProvider>();
			options.Providers.Add<GzipCompressionProvider>();
		});
		services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
		services.Configure<GzipCompressionProviderOptions>(options => options.Level   = CompressionLevel.Fastest);
		services.AddCors(options => options.AddDefaultPolicy(builder => builder.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true)));
	}
		
	/// <summary>
	/// Activate components.
	/// </summary>
	/// <param name="app">Application builder</param>
	/// <param name="env">Environment</param>
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		app.UseResponseCompression();
		app.UseSwagger();
		app.UseSwaggerUI(c =>
		{
			c.SwaggerEndpoint("/swagger/v1/swagger.json", "DD-API v1");
			c.RoutePrefix = string.Empty;
		});
		app.UseHttpsRedirection();
		app.UseRouting();
		app.UseAuthorization();
		app.UseCors();
		app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
	}
}