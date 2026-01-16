using Appdev_Group_8.Data;
using Appdev_Group_8.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionString");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
    });


builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        db.Database.Migrate();

        if (!db.Users.Any(u => u.Email == "admin01"))
        {
            var admin = new User
            {
                FullName = "Admin",
                Email = "admin01",
                SchoolId = "ADMIN001",
                Role = UserRole.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, "pass123");
            db.Users.Add(admin);
            db.SaveChanges();
        }

        if (!db.Users.Any(u => u.Email == "user01"))
        {
            var user = new User
            {
                FullName = "Test User 1",
                Email = "user01",
                SchoolId = "123",
                Role = UserRole.User
            };
            user.PasswordHash = hasher.HashPassword(user, "pass123");
            db.Users.Add(user);
            db.SaveChanges();
        }
        if (!db.Users.Any(u => u.Email == "user02"))
        {
            var user = new User
            {
                FullName = "Test User 2",
                Email = "user02",
                SchoolId = "123",
                Role = UserRole.User
            };
            user.PasswordHash = hasher.HashPassword(user, "pass124");
            db.Users.Add(user);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
