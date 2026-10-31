using Net.NowhereAtAll.Zaturnava.Server.Composition;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
_ = builder.Services.AddZaturnava(builder.Configuration);
_ = builder.Services.AddOpenApi();
_ = builder.Services.AddHostedMcp();

WebApplication app = builder.Build();
_ = app.UseExceptionHandler();
_ = app.UseCors();
_ = app.UseRateLimiter();
_ = app.UseAuthentication();
_ = app.MapOpenApi();
_ = app.MapScalarApiReference(IndexEndpoints.ReferencePrefix);
_ = app.MapIndex();
_ = app.MapGames();
_ = app.MapSignIn();
_ = app.MapHostedMcp();
app.Run();
