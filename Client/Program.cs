using BlazorAuthDemo.Client;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("BlazorAuthDemo.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// TODO: at the time of writing the Blazor WASM template with authentication for individual users is not working.
// Below is the default implementation which throws exceptions deep in the DI framework when trying to use
// `BaseAddressAuthorizationMessageHandler`. If in the future  this works we will be able to stop manually adding/removing
// the JWT in our auth service.

//_ = builder.Services.AddHttpClient("BlazorAuthDemo.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
//    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
//// Supply HttpClient instances that include access tokens when making requests to the server project
//_ = builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BlazorAuthDemo.ServerAPI"));
//_ = builder.Services.AddApiAuthorization();

builder.Services
    .AddAuthorizationCore()
    .AddBlazoredLocalStorage()// this is scoped
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>()
    ;

await builder.Build().RunAsync();
