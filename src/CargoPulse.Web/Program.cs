using CargoPulse.ServiceDefaults;
using CargoPulse.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddCargoPulseServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddOutputCache();

var app = builder.Build();

app.UseCargoPulseHttpDefaults();
app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapCargoPulseDefaultEndpoints();

app.Run();

public partial class Program;
