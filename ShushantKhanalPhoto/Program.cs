using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Azure SQL — connection string comes from user-secrets (local) or app settings (Azure)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));  // free-tier Azure SQL auto-pauses, so retry while it wakes up

builder.Services.AddScoped<IEmailService, EmailService>();

// Cloudinary hosts the image files; the DB only stores their URLs
builder.Services.AddSingleton(_ => new Cloudinary(new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"])) { Api = { Secure = true } });

// Images dropped into Cloudinary's portfolio/<Category> folders show up in the gallery automatically
builder.Services.AddScoped<CloudinarySyncService>();
builder.Services.AddHostedService<CloudinarySyncWorker>();

// API support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create/upgrade the database schema on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();  // enables the [Route] attribute-based API controllers

app.Run();