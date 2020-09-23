using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Cricinfo.Api.Controllers;
using Cricinfo.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Cricinfo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
                options.Filters.Add(new ConsumesAttribute("application/json"));
                options.Filters.Add(new ProducesAttribute("application/json"));
                options.Filters.Add(new ProducesResponseTypeAttribute(Status400BadRequest));
                options.Filters.Add(new ProducesResponseTypeAttribute(Status500InternalServerError));
            });

            services.AddSwaggerGen(setupAction =>
            {
                setupAction.SwaggerDoc(
                    "LibraryOpenAPISpecification",
                    new OpenApiInfo
                    {
                        Title = "Cricinfo.API - Documentation",
                        Version = Configuration.GetValue<string>("APIVersion")
                    });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                setupAction.IncludeXmlComments(xmlPath);

                setupAction.UseInlineDefinitionsForEnums();
            });

            services.AddScoped<ICricInfoCommandService>(sp =>
            {
                return new CricInfoCommandService<MatchController>(
                    Configuration.GetConnectionString("PostgresConnection"),
                    sp.GetRequiredService<ILogger<MatchController>>());
            });

            services.AddScoped<ICricInfoQueryService>(sp =>
            {
                return new CricInfoQueryService<MatchController>(
                    Configuration.GetConnectionString("PostgresConnection"),
                    sp.GetRequiredService<ILogger<MatchController>>());
            });

            services.AddHealthChecks()
                .AddCheck("API Healthcheck", () => HealthCheckResult.Healthy())
                .AddCheck("SQL Healthcheck", () =>
                {
                    using var conn = new Npgsql.NpgsqlConnection(Configuration.GetConnectionString("PostgresConnection"));
                    try
                    {
                        conn.Open();
                        return HealthCheckResult.Healthy();
                    }
                    catch (Npgsql.NpgsqlException)
                    {
                        return HealthCheckResult.Degraded();
                    }
                });

            services.AddHealthChecksUI().AddInMemoryStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(setupAction =>
            {
                setupAction.SwaggerEndpoint("/swagger/LibraryOpenAPISpecification/swagger.json", "Documentation");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/api/health", new HealthCheckOptions
                {
                    ResultStatusCodes = new Dictionary<HealthStatus, int>
                    {
                        { HealthStatus.Healthy, 200 },
                        { HealthStatus.Degraded, 500 },
                        { HealthStatus.Unhealthy, 503 }
                    },
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecksUI();
            });
        }
    }
}
