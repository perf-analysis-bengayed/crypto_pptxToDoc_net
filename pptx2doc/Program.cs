var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers(); // Ensure controllers are mapped

// Listen on 0.0.0.0:3000 so Docker can expose it
app.Run("http://0.0.0.0:3000");
