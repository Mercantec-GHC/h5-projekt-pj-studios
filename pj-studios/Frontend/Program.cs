using Frontend;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
	var handler = sp.GetRequiredService<JwtAuthorizationMessageHandler>();
	handler.InnerHandler = new HttpClientHandler();

	return new HttpClient(handler)
	{
		BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
	};
});
builder.Services.AddScoped<AuthenticationService>();

await builder.Build().RunAsync();
