using AgenticBlazer.Components;
using Microsoft.EntityFrameworkCore;
using AgenticBlazer.Data;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace AgenticBlazer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddAzureKeyVault(
                new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
                new DefaultAzureCredential());

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration["DefaultConnection"]));

            builder.Services.AddScoped(x =>
                new AgenticBlazer.Services.UserService(
                    x.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                    new SecretClient(
                        new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
                        new DefaultAzureCredential())));
            
            builder.Services.AddScoped<AgenticBlazer.Services.UserState>();
            builder.Services.AddScoped<AgenticBlazer.Services.PoemService>();

            var app = builder.Build();

            // Ensure database is created
            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbFactory.CreateDbContext();
                db.Database.EnsureCreated();
                try 
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD Theme nvarchar(max) DEFAULT 'light'");
                    db.Database.ExecuteSqlRaw("UPDATE Users SET Theme = 'light' WHERE Theme IS NULL");
                } 
                catch { } // Ignore if column already exists
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
