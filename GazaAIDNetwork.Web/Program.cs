using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services;
using GazaAIDNetwork.Infrastructure.Services.CycleAidService;
using GazaAIDNetwork.Infrastructure.Services.DivisionService;
using GazaAIDNetwork.Infrastructure.Services.FamilyService;
using GazaAIDNetwork.Infrastructure.Services.UserService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<User, IdentityRole>(
    options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 0;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredUniqueChars = 0;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); ;

builder.Services.AddControllersWithViews();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services
    .AddMvc()
    .AddRazorPagesOptions(options =>
    options.Conventions.AddAreaPageRoute("Identity", "/Account/Login", ""));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDivisionService, DivisionService>();
builder.Services.AddScoped<IRepositoryAudit, RepositoryAudit>();
builder.Services.AddScoped<IDisabilityService, DisabilityService>();
builder.Services.AddScoped<IDiseaseService, DiseaseService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IDisplacedService, DisplacedService>();
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<ICycleAidService, CycleAidService>();
builder.Services.AddScoped<IInfoRepresentativeService, InfoRepresentativeService>();
builder.Services.AddScoped<IProjectAidService, ProjectAidService>();
builder.Services.AddScoped<IOrderAidService, OrderAidService>();

builder.Services.AddRazorPages();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Auto logout after 1 hour
    options.SlidingExpiration = true; // Reset timeout on user activity
    options.LoginPath = "/Identity/Account/Login"; // Ensure this matches your route
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

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
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 401) // Unauthorized
    {
        response.Redirect("/Identity/Account/Login");
    }
    else if (response.StatusCode == 403) // Forbidden
    {
        response.Redirect("/Identity/Account/AccessDenied");
    }
});

app.Run();











