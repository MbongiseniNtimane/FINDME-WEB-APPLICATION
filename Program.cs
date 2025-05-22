using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Firebase initialization
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(@"D:\WIL FINAL PROJECT -BRAINIACS\FIND ME Web\Find me\ASPNETCore_DB (2)\ASPNETCore_DB\findme-v2-a7b1f-firebase-adminsdk-qql8g-3764e50b67.json"),
});

// Set environment variable for Firebase credentials
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"D:\WIL FINAL PROJECT -BRAINIACS\FIND ME Web\Find me\ASPNETCore_DB (2)\ASPNETCore_DB\findme-v2-a7b1f-firebase-adminsdk-qql8g-3764e50b67.json");
builder.Services.AddDistributedMemoryCache();
// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Make cookie accessible only to the server
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});

// Configure Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Set this if you want JWT as the challenge
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";   // Redirect to login page if unauthorized
    options.LogoutPath = "/Account/Logout"; // Handle logout path
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Session expiration time
    options.SlidingExpiration = true; // Extend session on activity
})
.AddJwtBearer(options =>
{
    options.Authority = "https://securetoken.google.com/findme-v2-a7b1f"; // Replace with your Firebase project ID
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://securetoken.google.com/findme-v2-a7b1f", // Firebase project ID
        ValidateAudience = true,
        ValidAudience = "findme-v2-a7b1f", // Firebase project ID
        ValidateLifetime = true
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Add this before UseAuthentication

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "admin",
     pattern: "Admin/{controller=AdminDashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "user",
     pattern: "User/{controller=UserDashboard}/{action=Index}/{id?}");
app.Run();

