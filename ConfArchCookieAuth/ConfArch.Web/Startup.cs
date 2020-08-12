using ConfArch.Data;
using ConfArch.Data.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfArch.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(o => o.Filters.Add(new AuthorizeFilter()));
            services.AddScoped<IConferenceRepository, ConferenceRepository>();
            services.AddScoped<IProposalRepository, ProposalRepository>();
            services.AddScoped<IAttendeeRepository, AttendeeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddDbContext<ConfArchDbContext>(options =>
                //options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                //    assembly => assembly.MigrationsAssembly(typeof(ConfArchDbContext).Assembly.FullName)));
                // IO PREFERISCO USARE INMEMORY PERCHE NON HO SQL SERVER SU MAC
                options.UseInMemoryDatabase("ConfArchDb"));

            //CONFIGUAZIONE DELL'AUTHENTICATION
            services.AddAuthentication(/*"defaultSchema" oppure */ o =>
            {   //QUI POSSO CUSTOMIZZARE PROPREITA DELL'AUTHENTICATION -> SCHEMA DI DEFAULT USATO X [Authorize] DI DEFAULT
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; //="Cookies" 
                //o.DefaultAuthenticateScheme = "Cookies"; //SCHEMA USATO PER Authenticate -> LOGIN
                //o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; //SCHEMA USATO PER Challenge
            })
                .AddCookie(o =>
                {   //PERSONALIZZAZIONE DEL COOKIE AUTH
                    //o.LoginPath = "/Account/LoginWithGoogle"; // QUI POSSO CUSTOMIZZARE URL DELLA PAGINE LOGIN - POTREI FARE DIRETTAMENTE LOGIN VERSO GOOGLE
                    //o.DataProtectionProvider=...; // DATA PROVIDER USATO PER CRIPTARE/PROTEGGERE COOKIE
                    //o.Cookie.Name=... .Domain=... // PROPRIETA' DEL COOKIE DA EMETTERE
                    o.Events = new CookieAuthenticationEvents
                    { //ESEMPIO DI INTERCETTAZIONE DEGLI EVENTI DI LIFECYCLE DELL'AUTH PER LOGICA CUSTOM
                        OnValidatePrincipal = (ctx) =>
                        {
                            //STAMPO LE CREDENZIALI CORRENTI RICAVATE DAL COOKIE
                            System.Console.WriteLine($"{System.DateTime.Now} INTERCETTO OnValidatePrincipal - Scheme={ctx.Scheme.Name}");
                            foreach (var claim in ctx.Principal.Claims)
                            {
                                System.Console.WriteLine($"{claim.Type} =\t{claim.Value}");
                            }
                            //POSSO FORZARE LOGOUT (ad esempio controllando expire) --> ctx.RejectPrincipal();
                            //OPPURE CAMBIARE/MANIPOLARE CLAIM --> ctx.ReplacePrincipal(newPrincipal);
                            System.Console.WriteLine($"- ExpireTimeSpan: {ctx.Options.ExpireTimeSpan}\n- ExpiresUtc: {ctx.Properties.ExpiresUtc}\n- IssuedUtc: {ctx.Properties.IssuedUtc}\n- IsPersistent: {ctx.Properties.IsPersistent}\n- RedirectUri: {ctx.Properties.RedirectUri}");
                            return System.Threading.Tasks.Task.CompletedTask;
                        }
                    };
                })
                .AddCookie(ExternalAuthenticationDefaults.AuthenticationScheme /* SPECIFICO UN NOME CUSTOM PER CookieScheme="ext-google" PER GESTIRE LOGIN ESTERNA Google -> POI TRASFORMO IN APP_CLAIMS VEDI AccountController.GoogleLoginCallback */)
                .AddGoogle(o =>
                {
                    o.SignInScheme = ExternalAuthenticationDefaults.AuthenticationScheme; //SPECIFICO QUI SCHEMA CUSTOM="ext-google" PER FARE AUTH EXT -> POI TRASFORMATA CON CUSTOM (APP_CLAIMS)
                    o.ClientId = Configuration["Google:ClientId"]; //QUESTO PARAMETRO ANDREBBE MESSO IN App Secret
                    o.ClientSecret = Configuration["Google:ClientSecret"]; //QUESTO PARAMETRO ANDREBBE MESSO IN App Secret
                });

            //CONFIGURAZIONE DELL'AUTHORIZAZION
            services.AddAuthorization(options =>
            {   //ESEMPI DI POLICY CUSTOM PER [Authorize]
                options.AddPolicy("IsLoggedIn", policy => policy.RequireAuthenticatedUser()); //CONTROLLA CHE SIA LOGGATO  
                options.AddPolicy("IsSpeaker", policy => policy.RequireRole("Speaker")); //CONTROLLA CHE ABBIA CERTO RUOLO
                options.AddPolicy("CanAddConference", policy => policy.RequireClaim("AddConference")); //CONTROLLA CHE ABBIA CERTO CLAIM
                options.AddPolicy("Admin_OR_CanAddConference", policy => policy.RequireAssertion(ctx =>
                {   // CONTROLLA UNA FUNZIONE CHE DEVE TORNARE true COMBINANDO DIVERSI CONTROLLI RUOLO OR CLAIM
                    return ctx.User.IsInRole("Admin") || ctx.User.HasClaim(c => c.Type == "AddConference");
                }));
                options.AddPolicy("LoveRed", policy => policy.AddRequirements(new Authorization.FavColorRequirement("red"))); //UTILIZZO DI CUSTOM REQUIREMENT
                options.AddPolicy("CanEditProposal", policy => policy.AddRequirements(new Authorization.ProposalRequirement())); //UTILIZZO DI RESOURCE REQUIREMENT
            });

            //REGISTRO CUSTOM REQUIREMENT / HANDLER PER POTERLO USARE NELLE POLICY
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Authorization.FavColorHandler>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Conference}/{action=Index}/{id?}");
            });


            // NOTE: this must go at the end of Configure to SEED INMEMORY DB WITH EF
            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var serviceScope = serviceScopeFactory.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<ConfArchDbContext>();
                dbContext.Database.EnsureCreated();
            }


        }
    }
}
