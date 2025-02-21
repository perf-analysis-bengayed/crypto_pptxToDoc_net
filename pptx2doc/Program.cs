var builder = WebApplication.CreateBuilder(args);

// Ajoutez la configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Utilisez CORS
app.UseCors("AllowAllOrigins");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers(); // Assurez-vous que les contrôleurs sont mappés

// Écoutez sur 0.0.0.0:3000 pour que Docker puisse l'exposer
app.Run("http://0.0.0.0:3000");
