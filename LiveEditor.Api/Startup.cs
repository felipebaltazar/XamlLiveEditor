using LiveEditor.Api.Abstractions;
using LiveEditor.Api.Hub;
using LiveEditor.Api.Infrastructure.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Diagnostics;

namespace LiveEditor.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IConnectionMonitor<LiveEditorHub>, LiveEditorHubConnectionMonitor>();

            services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder.AllowAnyHeader()
                       .AllowAnyMethod()
                       .SetIsOriginAllowed((host) => true)
                       .AllowCredentials();
            }));

            services.AddSwaggerGen(c => SwaggerFactory.CreateSwaggerOptions(c));

            //Adicionando o SignalR self-hosted
            var signalService = services
                .AddSignalR(configuration => configuration.EnableDetailedErrors = Debugger.IsAttached)
                .AddJsonProtocol()
                .AddMessagePackProtocol(options =>
                {
                    options.FormatterResolvers = new List<MessagePack.IFormatterResolver>()
                    {
                        MessagePack.Resolvers.StandardResolver.Instance
                    };
                });

            signalService.AddHubOptions<LiveEditorHub>(options => options.MaximumReceiveMessageSize = 102400000);

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if(!Debugger.IsAttached)
                app.UseHttpsRedirection();

            app.UseStatusCodePagesWithReExecute("/error/{0}");

            app.UseCors("CorsPolicy");

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LiveEditorHub>("/LiveEditor");
                endpoints.MapControllers();
            });

            SwaggerFactory.ConfigureSwagger(app);
        }
    }
}
