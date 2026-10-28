using Net.NowhereAtAll.Zaturnava.Server.Composition;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
_ = builder.Services.AddZaturnava(builder.Configuration);

WebApplication app = builder.Build();
_ = app.UseExceptionHandler();
_ = app.UseCors();
_ = app.MapGames();
app.Run();
