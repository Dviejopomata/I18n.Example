using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace I18n.Example
{
    public class SectionLiteralDictionary : Dictionary<string, LiteralDictionary>
    {
        public new void Add(string section, LiteralDictionary author)
        {
            base.Add(section, author);
        }

        public new LiteralDictionary this[string literal]
        {
            get => base[literal];
            set => base[literal] = value;
        }
    }
    public class LiteralDictionary : Dictionary<string, LiteralLanguageDictionary>
    {
        public new void Add(string literal, LiteralLanguageDictionary author)
        {
            base.Add(literal, author);
        }

        public new LiteralLanguageDictionary this[string literal]
        {
            get => base[literal];
            set => base[literal] = value;
        }
    }

    public class LiteralLanguageDictionary : Dictionary<string, string>
    {
        public new void Add(string en, string author)
        {
            base.Add(en, author);
        }

        public new string this[string literal]
        {
            get => base[literal];
            set => base[literal] = value;
        }
    }

    public class MyStringLocalizer : IStringLocalizer
    {
        private readonly SectionLiteralDictionary _data;

        public MyStringLocalizer()
        {
            var client = new HttpClient();
            var resp = client.GetAsync($"https://i18n.nextagilesoft.com/i18n/nextagilesoft_printv3_prod").Result;
            var str = resp.Content.ReadAsStringAsync().Result;
            _data = JsonConvert.DeserializeObject<SectionLiteralDictionary>(str);
        }
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var list = new List<LocalizedString>();
            var cultureCode = CultureInfo.CurrentCulture.Name;
            foreach (var item in _data)
            {
                foreach (var literal in item.Value)
                {

                    foreach (var translation in literal.Value)
                    {
                        if (translation.Key == cultureCode)
                        {
                            list.Add(new LocalizedString($"{item.Key}:{item.Value}", translation.Value));
                        }
                    }
                }
            }
            return list;
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        LocalizedString IStringLocalizer.this[string name] => new LocalizedString(name, _GetValue(name));

        LocalizedString IStringLocalizer.this[string name, params object[] arguments]
        {
            get
            {
                var value = _GetValue(name);
                return new LocalizedString(name, string.Format(value, arguments));
            }
        }

        private string _GetValue(string name)
        {
            if (name.Contains(":"))
            {
                var chunks = name.Split(":");
                var section = chunks[0];
                var key = chunks[1];
                var lng = CultureInfo.CurrentCulture.Name;
                var value = _data[section][key][lng];
                return value ?? name;
            }

            return "";
        }
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {

                var supportedCultures = new[]
                {
                    new CultureInfo("es"),
                    new CultureInfo("en")
                };

                // State what the default culture for your application is. This will be used if no specific culture
                // can be determined for a given request.
                options.DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en");

                // You must explicitly state which cultures your application supports.
                // These are the cultures the app supports for formatting numbers, dates, etc.
                options.SupportedCultures = supportedCultures;

                // These are the cultures the app supports for UI strings, i.e. we have localized resources for.
                options.SupportedUICultures = supportedCultures;

                // You can change which providers are configured to determine the culture for requests, or even add a custom
                // provider with your own logic. The providers will be asked in order to provide a culture for each request,
                // and the first to provide a non-null result that is in the configured supported cultures list will be used.
                // By default, the following built-in providers are configured:
                // - QueryStringRequestCultureProvider, sets culture via "culture" and "ui-culture" query string values, useful for testing
                // - CookieRequestCultureProvider, sets culture via "ASPNET_CULTURE" cookie
                // - AcceptLanguageHeaderRequestCultureProvider, sets culture via the "Accept-Language" request header
                //options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
                //{
                //  // My custom request culture logic
                //  return new ProviderCultureResult("en");
                //}));
            });
            services.AddSingleton<IStringLocalizer, MyStringLocalizer>();

            services.AddMvc()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization(o =>
                {
                    o.DataAnnotationLocalizerProvider = (type, factory) => new MyStringLocalizer();
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
