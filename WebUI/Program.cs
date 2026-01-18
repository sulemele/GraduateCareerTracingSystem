using BusinessLogic.Implementations;
using BusinessLogic.Interfaces;
using DataAccess.DataBase;
using EmailManager.Models;
using EmailManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
using WebUI.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddDbContext<DatabaseEntity>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDefaultIdentity<IdentityUser>(
               options =>
               {
                   options.SignIn.RequireConfirmedAccount = true;
                   // Require 2FA for all users
                   options.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider;

               }).AddRoles<IdentityRole>()
               .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpClient();


builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();

//HERE IREGISTERED THE INTERFACE AND MAPPED WITH REPOSITORY
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));


builder.Services.AddMemoryCache();
// Add session for temporary data storage
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService2, EmailService2>();

builder.Services.AddResponseCaching();
//builder.Services.AddNotyf(config =>
//{
//    config.DurationInSeconds = 10;
//    config.IsDismissable = true;
//    config.Position = NotyfPosition.TopRight;
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();

app.UseAuthentication();
//app.UseMiddleware<Enforce2FAMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();
//app.UseNotyf();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();
app.UseSession();


app.Run();
