using System.Reflection;
using System.Text;
using BlazorAuthDemo.Server.Data;
using BlazorAuthDemo.Server.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());

// Add services to the container.
string connectionString = builder.Configuration["ConnectionStrings:DefaultConntection"];

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

builder.Services
    .AddAuthentication(o =>
    {
        // see https://github.com/dotnet/aspnetcore/issues/9039#issuecomment-773408772
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "https://localhost",
            ValidAudience = "https://localhost",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("af1097912a99481f839b0490b875eaf1")), // TODO: save this in secrets and get from config
        };
        // TODO: looks like we'll need this if we use SignalR
        // https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-6.0#built-in-jwt-authentication
        //o.Events = new()
        //{
        //    OnMessageReceived = context =>
        //    {
        //        string token = context.Request.Query["access_token"];
        //        context.Token = token;
        //        return Task.CompletedTask;
        //    }
        //};
    });

_ = builder.Services.AddEndpointsApiExplorer();
_ = builder.Services.AddControllers();
_ = builder.Services.AddRazorPages();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
}
else
{
    _ = app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
_ = app.MapFallback("/api/{*path}", () => Results.NotFound()); // deals with bad requests to api properly
app.MapFallbackToFile("index.html");

app.Run();
