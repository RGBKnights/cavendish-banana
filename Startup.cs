using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;
using ReverseMarkdown;
using System;
using System.IO;

[assembly: FunctionsStartup(typeof(CavendishBanana.Startup))]

namespace CavendishBanana
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }
        
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOpenAIService().ConfigureHttpClient(c => {
                c.Timeout = TimeSpan.FromMinutes(5);
            });

            builder.Services.AddSingleton<Converter>(sp => {
                var config = new Config
                {
                    // Include the unknown tag completely in the result (default as well)
                    // UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough,
                    UnknownTags = Config.UnknownTagsOption.Bypass,
                    // generate GitHub flavoured markdown, supported for BR, PRE and table tags
                    GithubFlavored = true,
                    // will ignore all comments
                    RemoveComments = true,
                    // remove markdown output for links where appropriate
                    SmartHrefHandling = true
                };
                return new Converter(config);
            });
        }
    }
}