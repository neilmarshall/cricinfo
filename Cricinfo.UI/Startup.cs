using Cricinfo.Api.Client;
using Cricinfo.UI.Healthchecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Cricinfo.Services.IdentityStore;
using Cricinfo.Services.IdentityStore.Models;

namespace Cricinfo.UI
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
            services.AddAuthorization(options =>
            {
                options.AddPolicy("CanAddScorecard", policy => policy.RequireClaim("CanAddScorecard", "true"));
                options.AddPolicy("CanAddTeam", policy => policy.RequireClaim("CanAddTeam", "true"));
                options.AddPolicy("CanAddUser", policy => policy.RequireClaim("CanAddUser", "true"));
                options.AddPolicy("CanManagePermissions", policy => policy.RequireClaim("CanManagePermissions", "true"));
            });

            services
                .AddRazorPages(options =>
                {
                    options.Conventions.AllowAnonymousToPage("/Index");
                    options.Conventions.AuthorizeFolder("/Teams", "CanAddTeam");
                    options.Conventions.AuthorizeFolder("/Scorecard", "CanAddScorecard");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/ManagePermissions", "CanManagePermissions");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Register", "CanAddUser");
                })
                .AddMvcOptions(options => options.Filters.Add(new AuthorizeFilter()))
                .AddSessionStateTempDataProvider();
            services.AddSession();

            services.AddIdentity<ApplicationUser, ApplicationRole>(setup =>
            {
                setup.Password.RequireNonAlphanumeric = false;
            });
            services.AddTransient<IUserStore<ApplicationUser>>(sp =>
            {
                return new CricInfoUserStore<Areas.Identity.Pages.Account.LoginModel>(
                    Configuration.GetConnectionString("PostgresConnection"),
                    sp.GetRequiredService<ILogger<Areas.Identity.Pages.Account.LoginModel>>());
            });
            services.AddTransient<IRoleStore<ApplicationRole>, CricInfoRoleStore>();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Unauthorized";
            });

            services.AddHttpClient<ICricinfoApiClient, CricinfoApiClient>(client =>
            {
                client.BaseAddress = new Uri(Configuration["cricInfoAPIURL"]);
            });

            services.AddHealthChecks()
                .AddCheck("UI Healthcheck", () => HealthCheckResult.Healthy())
                .AddTypeActivatedCheck<APIHealthCheck>("API Healthcheck", Configuration["cricInfoAPIURL"], Configuration["HealthcheckEndpoint"]);

            services.AddHealthChecksUI(setupSettings =>
            {
                setupSettings.AddHealthCheckEndpoint("Healthchecks", Configuration["HealthcheckEndpoint"]);
                setupSettings.SetEvaluationTimeInSeconds(300);
            })
                .AddInMemoryStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHealthChecks(Configuration["HealthcheckEndpoint"], new HealthCheckOptions
                {
                    ResultStatusCodes = new Dictionary<HealthStatus, int>
                    {
                        { HealthStatus.Healthy, 200 },
                        { HealthStatus.Unhealthy, 500 },
                        { HealthStatus.Degraded, 503 },
                    },
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecksUI();
            });
        }
    }
}
