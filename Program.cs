using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using MudCake.core.Data;
using MudCake.core.Data.Services;
using MudCake.core.Pages.Nav;
using MudCake.core.SignalR;
using MudCake.Data.SignalR;
using System.Diagnostics;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;


public static class Program
{
    static DirectoryInfo? pluginDirectory;
    static Assembly? thisAssembly;
    static Assembly? coreAssembly;
    static Assembly[]? pluginAssemblies;
    static Uri? mudcake_uri;

    public static void Main(string[] args)
    {
        var app = CreateBuilder(args)            
                    .Build();

        ConfigureApp(app);

        app.Run();

    }

    private static Uri GetUri(WebApplicationBuilder builder) => new(builder.Configuration["MUDCAKE_URI"]!);

    private static void ConfigureApp(WebApplication app)
    {
        ConfigureForEnvironments(app);

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();
        app.MapControllers();

        //Register hubs
        AddHubs(thisAssembly!, app);
        AddHubs(coreAssembly!, app);

        foreach (var plugin in pluginAssemblies ?? Enumerable.Empty<Assembly>())
        {
            AddHubs(plugin, app);
        }

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
    }

    private static WebApplicationBuilder CreateBuilder(string[] args)
    {
        WebApplicationOptions options = new WebApplicationOptions()
        {
            EnvironmentName = "Development",
            Args = args
        };
        
        var builder = WebApplication.CreateBuilder(options);


        if (builder.Configuration["MUDCAKE_URI"] == null)
            throw new ApplicationException("Mudcake Uri not defined");

        Debug.WriteLine($"[*] Building App");

        SetupInfrastructure();

        InitialiseAssemblies();

        mudcake_uri = GetUri(builder);

        SetupAssets(builder);

        //Add core dataservice
        Debug.WriteLine($"   [*] Loading assembly: {thisAssembly.FullName}");
        AddScopedServicesFromAssemblies([thisAssembly!, typeof(IScopedService).Assembly], builder.Services);
        AddNavigation(thisAssembly!, builder.Services);

        ImportPlugins(builder.Services);


        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddHttpClient("signalr", options => {
            options.BaseAddress = mudcake_uri;
        })
        .ConfigurePrimaryHttpMessageHandler(_ =>
        {
            var handler = new HttpClientHandler() //Test this, but then test with a DI version that works with the main
            {
                UseDefaultCredentials = true,
                Credentials = System.Net.CredentialCache.DefaultCredentials,
                AllowAutoRedirect = true,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
            };
            handler.ServerCertificateCustomValidationCallback += ReturnTrue;

            return handler;
        });

        builder.Services.AddHttpClient(Options.DefaultName, o => {
            o.BaseAddress = mudcake_uri;
        });

        //HttpClient
        builder.Services.AddScoped(x => new HttpClient(x.GetService<HttpClientHandler>() ?? new HttpClientHandler())
        {
            BaseAddress = mudcake_uri,
        });

        //HttpClientHandler
        builder.Services.AddScoped(options => {

            var handler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
                Credentials = System.Net.CredentialCache.DefaultCredentials,
                AllowAutoRedirect = true,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
            };

            handler.ServerCertificateCustomValidationCallback += ReturnTrue;

            return handler;
        });

        // CONTROLLERS
        builder.Services.AddMvc(options => options.EnableEndpointRouting = false)
                        .AddJsonOptions(o => {
                            o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                            o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        });

        builder.Services.AddMudServices();


        //SIGNALR
        builder.Services.AddSignalR();
        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        });

        builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();

        Debug.WriteLine("[*] App built successfully.");

        return builder;
    }

    private static void SetupAssets(WebApplicationBuilder builder)
    {
        builder.WebHost.UseWebRoot("wwwroot").UseStaticWebAssets();
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
    }

    private static void InitialiseAssemblies()
    {
        Debug.WriteLine($"   [*] Initialising Assemblies");

        // Set up assembly references
        thisAssembly = Assembly.GetExecutingAssembly();

        Debug.WriteLine($"      [+] {thisAssembly.GetName().Name}");

        coreAssembly = typeof(IScopedService).Assembly;
        Debug.WriteLine($"      [+] {coreAssembly.GetName().Name}");

        pluginAssemblies = pluginDirectory!.EnumerateFiles()
            .Where(f => f.Extension == ".dll")
            .Select(f => AssemblyLoadContext.Default.LoadFromAssemblyPath(f.FullName))
            .ToArray()
            ;
        Debug.WriteLine($"      [*] Plugins:");
        if(pluginAssemblies.Any())
        {
            Debug.WriteLine($"         [*] {string.Join("\n         [+] ", pluginAssemblies.Select(p => p.GetName().Name))}");
        }
        else
        {
            Debug.WriteLine($"         [x] No plugins found!");
        }
    }

    private static void SetupInfrastructure()
    {
        pluginDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
        Debug.WriteLine($"   [*] Setting up plugin directory");
        if (!pluginDirectory.Exists)
        {
            Debug.WriteLine($"      [*] Plugin directory not found. Creating.");
            pluginDirectory.Create();
        }
        else
        {
            Debug.WriteLine($"      [*] Plugin directory already exists");
        }

        Debug.WriteLine($"      [*] Plugin directory: {pluginDirectory}");
    }


    #region Helper Functions

    static void ConfigureForEnvironments(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            Debug.WriteLine($"[*] Configuring app for DEVELOPMENT");

        }
        else
        {
            // Configure the HTTP request pipeline.

            Debug.WriteLine($"[*] Configuring app for PRODUCTION!");

            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            app.UseResponseCompression();
        }


    }

    static void ImportPlugins(IServiceCollection services)
    {
        foreach (var assembly in pluginAssemblies ?? Enumerable.Empty<Assembly>())
        {
            Debug.WriteLine($"   [*] Loading assembly: {assembly.FullName}");
            try
            {
                // Optionally, keep track of loaded assemblies to avoid duplicates
                // e.g., store assembly.FullName in a HashSet<string>
                
                AddDataServices(assembly, services);
                AddNavigation(assembly, services);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading assembly: {ex.Message}");
                // Handle the error (e.g., log it, show a message to the user)
            }
        }

    }

    static void AddNavigation(Assembly assembly, IServiceCollection services)
    {

        var navPages = assembly
                            .GetTypes()
                            .Where(t => t.IsAssignableTo(typeof(INavigationData)))
                            .ToList()
                            ;

        Debug.WriteLine($"      [*] Routes Found:");
        navPages.ForEach(p => {
            Debug.WriteLine($"         [+] {p.Name}");
            services.AddScoped(typeof(INavigationData), p);
        } );
    }

    static void AddDataServices(Assembly assembly, IServiceCollection services) => AddScopedServicesFromAssemblies([assembly], services);

    static void AddScopedServicesFromAssemblies(Assembly[] assemblies, IServiceCollection services)
    {
        var dataServices = assemblies.SelectMany(a => a.GetTypes())
                                .Where(t => t.IsAssignableTo(typeof(IScopedService)))
                                .Where(t => !t.IsAbstract || t.IsInterface)
                                .ToList();

        var classes = dataServices.Where(ds => ds.IsClass);
        var interfaces = dataServices.Where(ds => ds.IsInterface);

        Debug.WriteLine($"      [*] Scoped Services Found:");
        classes.Join(interfaces
                , c => c.GetInterfaces().FirstOrDefault()
                , i => i
                , (c, i) => new { Class = c, Interface = i }
                )
                .ToList()
                .ForEach(ci => {
                    Debug.WriteLine($"         [+] {ci.Interface.Name} ({ci.Class.Name})");
                    services.AddScoped(ci.Interface, ci.Class);
                })
                ;

    }

    static void AddHubs(Assembly assembly, WebApplication webApp)
    {
        Debug.WriteLine($"   [*] Searching for Hubs in {assembly.GetName().Name}");
        var hubs = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(Hub)))
            .ToList();

        if (hubs.Count == 0) //Use Count = 0 for lists
            return;

        Debug.WriteLine($"      [*] Hubs Found:");

        hubs.ForEach(t =>
            { 
                Debug.WriteLine($"         [+] {t.Name}");
                typeof(Extensions)
                    .GetMethod(nameof(Extensions.AddHub), BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(t)
                    .Invoke(null, [webApp]);
            })
            ;
    }

    static bool ReturnTrue(HttpRequestMessage message, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors) => true;

    #endregion

}