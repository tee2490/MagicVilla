global using MagicVilla_VillaAPI.Data;
global using MagicVilla_VillaAPI;
using MagicVilla_VillaAPI.Repository.IRepostiory;
using MagicVilla_VillaAPI.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using MagicVilla_ClassLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.EntityFrameworkCore;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#region Important ต้องอยู่เรียงลำดับกัน และนำมาวางไว้ข้างบนสุด มิฉนั้นติดปัญหา [Authorize] ไม่ผ่าน
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
     .AddEntityFrameworkStores<ApplicationDbContext>();
#endregion Important

builder.Services.AddControllers(option =>
{
    option.CacheProfiles.Add("Default30",
       new CacheProfile()
       {
           Duration = 30
       });
}).AddNewtonsoftJson();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(x => {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = "https://magicvilla-api.com",
            ValidAudience = "https://test.com",
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAutoMapper(typeof(MappingConfig));

//ใช้ AutoFac ลงทะเบียนโดยอัตโนมัติกรณีมีหลายๆ Service
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(containerBuilder =>
{
    containerBuilder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
    .Where(t => t.Name.EndsWith("Repository") || t.Name.EndsWith("Test"))
    .AsImplementedInterfaces()
    .InstancePerLifetimeScope(); // เทียบเท่า AddScoped;
}));

builder.Services.AddResponseCaching();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

ApplyMigration();

app.Run();


void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}