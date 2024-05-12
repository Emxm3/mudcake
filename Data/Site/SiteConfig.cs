using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudCake.Data.Converters;
using System.Text.Json;

namespace MudCake.core.Data.Site
{
    public class SiteConfig : ISiteConfig
    {
        /// <summary>
        /// Get or sets the usage of Dark theme
        /// </summary>
        public bool IsDarkTheme { get; set; } = true;

        /// <summary>
        /// The raw string name of the app
        /// </summary>
        public string AppName { get; set; } = "MudCake";

        /// <summary>
        /// The HTML markup used to render the Apps name on the site
        /// </summary>
        public MarkupString AppNameMarkup { get; set; } = new MarkupString("<strong>Mud</strong>Cake");

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions()
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

        /// <summary>
        /// Connection string used by the site
        /// </summary>
        public string? Connectionstring { get; set; }

        /// <summary>
        /// Swagger Uri if implemented
        /// </summary>
        public string? SwaggerUri { get; internal set; }

        /// <summary>
        /// The theme to apply to the site. 
        /// Includes Dark theme
        /// </summary>
        public MudTheme? Theme { get; set; }

        public SiteConfig()
        {
            JsonSerializerOptions.Converters.Add(new DateOnlyConverter());
            JsonSerializerOptions.Converters.Add(new TimeOnlyConverter());
        }
    }
}
