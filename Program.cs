using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Login;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//relacion usuario , role
builder.Services.AddIdentity<Usuario, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Crear roles y admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await CrearRolesYAdmin(services);
}

app.Run();

async Task CrearRolesYAdmin(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<Usuario>>();

    string[] roles = { "Administrador", "Conductor" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"✅ Rol '{role}' creado");
        }
    }

    var adminEmail = "administrador234@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new Usuario
        {
            UserName = adminEmail,
            Email = adminEmail,
            Estado = true,
            EmailConfirmed = true
        };
        
        // 🔴 CONTRASEÑA SIMPLE (sin @)
        var result = await userManager.CreateAsync(adminUser, "Admin123@hola");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
            Console.WriteLine("✅ Administrador creado: administrador234@gmail.com / Admin123@hola");
        }
        else
        {
            Console.WriteLine("❌ Errores:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   {error.Description}");
            }
        }
    }
    else
    {
        Console.WriteLine("✅ Admin ya existe");
    }
}