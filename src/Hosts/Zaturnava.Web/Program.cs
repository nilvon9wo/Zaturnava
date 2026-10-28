using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Net.NowhereAtAll.Zaturnava.Web;

// Only a browser's .NET runtime can run this file, so it is not measured
// (ZaturnavaUnmeasuredFile in Zaturnava.Web.Test); everything it calls is.
WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
_ = builder.Services.AddZaturnavaWeb(builder.Configuration, builder.HostEnvironment.BaseAddress);
await builder.Build().RunAsync();
