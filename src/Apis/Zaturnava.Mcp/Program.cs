using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.NowhereAtAll.Zaturnava.Mcp;

// Only an MCP client starting this program and talking over its stdin and stdout runs this file, so
// it is not measured (ZaturnavaUnmeasuredFile in Zaturnava.Mcp.Test); the server it builds is.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
_ = builder.Logging.AddConsole(console => console.LogToStandardErrorThreshold = LogLevel.Trace);
_ = builder.Services.AddZaturnavaMcp(builder.Configuration).WithStdioServerTransport();
await builder.Build().RunAsync();
