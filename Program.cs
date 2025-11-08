using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SphinxQueryCore.Net
{
	public class Program
    {
		public static void Main(string[] args)
		{
			var app = CreateHostBuilder(args).Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
			{
				Predicate = healthCheck => true
			});

			app.MapHealthChecks("/healthz/live", new HealthCheckOptions
			{
				Predicate = _ => false
			});

			app.MapControllerRoute(
				name: "default", 
				pattern: "/sphinxquery", 
				defaults: new { controller = "Home", action = "Index" });

			app.UseStaticFiles();
			app
				.UseRouting()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapDefaultControllerRoute();
				});


			app.Run();
		}

		public static WebApplicationBuilder CreateHostBuilder(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Logging
				.AddConsole()
				.AddDebug()
				.AddOpenTelemetry(options => options
					.SetResourceBuilder(ResourceBuilder
						.CreateDefault()
						.AddService(
							serviceName: "sphinxquery",
							serviceVersion: "1.0.3"
						)
					)
				);

			builder.Services.AddControllersWithViews();

			builder.Services.AddHealthChecks();

			builder.Services.AddOpenTelemetry()
				.WithMetrics(cfg => cfg.AddAspNetCoreInstrumentation())
				.WithTracing(cfg => cfg
					.AddAspNetCoreInstrumentation()
					.AddConnectorNet())
				.UseOtlpExporter();

			return builder;
		}
	}
}
