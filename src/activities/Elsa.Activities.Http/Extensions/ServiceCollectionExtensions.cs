using System;
using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.JavaScript;
using Elsa.Activities.Http.Liquid;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers.Request;
using Elsa.Activities.Http.Parsers.Response;
using Elsa.Activities.Http.Services;
using Elsa.Options;
using Elsa.Scripting.JavaScript.Providers;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddHttpActivities(this ElsaOptionsBuilder options, Action<HttpActivityOptions>? configureOptions = default, Action<IHttpClientBuilder>? configureHttpClient = default)
        {
            options.Services.AddHttpServices(configureOptions, configureHttpClient);
            options.AddHttpActivitiesInternal();
            return options;
        }

        public static IServiceCollection AddHttpServices(this IServiceCollection services, Action<HttpActivityOptions>? configureOptions = default, Action<IHttpClientBuilder>? configureHttpClient = default)
        {
            if (configureOptions != null) 
                services.Configure(configureOptions);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            var httpClientBuilder = services.AddHttpClient(nameof(SendHttpRequest));
            configureHttpClient?.Invoke(httpClientBuilder);

            services.AddAuthorizationCore();

            services
                .AddSingleton<IHttpRequestBodyParser, DefaultHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, JsonHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, FormHttpRequestBodyParser>()
                .AddSingleton<IHttpResponseContentReader, PlainTextHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, JsonRawHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, TypedHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, ExpandoObjectHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, JTokenHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, FileHttpResponseContentReader>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
                .AddSingleton<AllowAnonymousHttpEndpointAuthorizationHandler>()
                .AddSingleton(sp => sp.GetRequiredService<IOptions<HttpActivityOptions>>().Value.HttpEndpointAuthorizationHandlerFactory(sp))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<HttpActivityOptions>>().Value.HttpEndpointWorkflowFaultHandlerFactory(sp))
                .AddBookmarkProvider<HttpEndpointBookmarkProvider>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(ConfigureJavaScriptEngine))
                .AddLiquidFilter<SignalUrlFilter>("signal_url")
                .AddJavaScriptTypeDefinitionProvider<HttpTypeDefinitionProvider>()
                .AddSingleton<IActivityTypeDefinitionRenderer, HttpEndpointTypeDefinitionRenderer>()
                .AddDataProtection();

            return services;
        }

        private static ElsaOptionsBuilder AddHttpActivitiesInternal(this ElsaOptionsBuilder options) =>
            options
                .AddActivity<HttpEndpoint>()
                .AddActivity<WriteHttpResponse>()
                .AddActivity<SendHttpRequest>()
                .AddActivity<Redirect>();
    }
}