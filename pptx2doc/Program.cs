// var builder = WebApplication.CreateBuilder(args);

// // Ajoutez la configuration CORS
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAllOrigins",
//         builder =>
//         {
//             builder.AllowAnyOrigin()
//                    .AllowAnyMethod()
//                    .AllowAnyHeader();
//         });
// });

// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();

// var app = builder.Build();

// // Utilisez CORS
// app.UseCors("AllowAllOrigins");

// app.UseStaticFiles();
// app.UseRouting();
// app.UseAuthorization();
// app.MapControllers(); // Assurez-vous que les contrôleurs sont mappés

// // Écoutez sur 0.0.0.0:3000 pour que Docker puisse l'exposer
// app.Run("http://0.0.0.0:3000");



var builder = WebApplication.CreateBuilder(args);

// Ajouter la configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.WithOrigins("https://192.168.100.250") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configurer Kestrel pour écouter en HTTP et HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
  

    // Endpoint HTTPS sur le port 443 en utilisant le certificat SSL
    // Remplacez "votre_mot_de_passe" par le mot de passe réel de votre certificat PFX
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("/app/ssl_certificates/your_certificate.pfx", "test123");
    });
});

var app = builder.Build();

// Utiliser CORS
app.UseCors("AllowAllOrigins");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers(); // S'assurer que les contrôleurs sont mappés

// Lancer l'application. Les endpoints configurés dans Kestrel seront utilisés.
app.Run("https://192.168.100.250:443");

