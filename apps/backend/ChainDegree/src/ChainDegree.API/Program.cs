using ChainDegree.Infrastructure.Configurations;
using DotNetEnv;
namespace ChainDegree.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                DotNetEnv.Env.TraversePath().Load();
            }
            builder.Configuration.AddEnvironmentVariables();

            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(JwtOptions.SectionName));

            builder.Services.Configure<BesuOptions>(
                builder.Configuration.GetSection("Blockchain:Besu"));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
