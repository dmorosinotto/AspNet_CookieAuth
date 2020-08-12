using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ConfArch.Data.Repositories;
using ConfArch.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfArch.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository userRepository;

        public AccountController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [AllowAnonymous] //QUESTO E' ENTRYPOINT "/Account/Login" DI DEFAULT SE RICHIESTO [Authorize] MA NON HO CookieAuth -> LoginPage
        public IActionResult Login(string returnUrl = "/") //PARAMETRO returnUrl=PAGINA INTERNA A CUI TORNARE DOPO IL LOGIN LETTO DA QUERYSTRING
        {
            return View(new LoginModel { ReturnUrl = returnUrl }); //MOSTRO PAGINA INTERNA -> FORM LOGIN CON SETTATO returnUrl
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginModel model) //ARRIVO QUI QUANDO FACCIO SUBMIT FORM DI LOGIN - uso CookieAuth A MANO CON UTENTE LOCALI
        {
            //CONTROLLO UTENTE LOCALI IN BASE utente + password HASh - VEDI CODICE INTERNO AL REPOSITORY!!
            var user = userRepository.GetByUsernameAndPassword(model.Username, model.Password);
            if (user == null)
                return Unauthorized(); //SE NON TROVO UTENTE TORNA 401 = Unauthorized

            //SE CREDENZIALI VALIDE ALLORA CREO CLAIMS -> Identity PER IL CookieSchema -> PRINCIPAL --> POI FINISCE IN HttpContext.User
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), //SID DELL'UTENTE
                new Claim(ClaimTypes.Name, user.Name),  //CLAIM DI DEFAULT PER IL NOME UTENTE
                new Claim(ClaimTypes.Role, user.Role),  //CLAIM DI DEFAULT PER I RUOLI ()
                //new Claim(ClaimTypes.Role, "Speaker"),  //EVENTUALI ALTRI RUOLI VANNO AGGUNTI SEPARATAMENTE
                //new Claim("Permission", "AddConference"),
                new Claim("FavoriteColor", user.FavoriteColor) //CLAIM TOTALMENTE CUSTOM DA LEGARE A UTENTE X POLICY
            };

            var identity = new ClaimsIdentity(claims, //QUI DEVO SPECIFICARE LO SCHEMA DELL'AUTH PER CUI VALGONO QUESTI CLAIMS
                CookieAuthenticationDefaults.AuthenticationScheme); //QUI LO EMETTO PER LO SCHEMA DI DEFAULT="Cookies"
            var principal = new ClaimsPrincipal(identity); //ALLA FINE CREO IL PRINCIPAL

            await HttpContext.SignInAsync( //QUESTA CHIAMATA ALLA FINE FINISCE IL SIGNIN CON CLAIMS PASSATE
                CookieAuthenticationDefaults.AuthenticationScheme, //IMPORTANTE RIPORTARE LO SCHEMA GIUSTO QUI USA DEFAULT="Cookies"
                principal,
                new AuthenticationProperties { IsPersistent = model.RememberLogin }); //SETTA PROPRIETA' PERSISTENT VOLENDO SI PUO' SETTARE ANCHE DURATA ExpireUtc / Sliding / AllowRefresh

            return LocalRedirect(model.ReturnUrl); //IMPORTANTE USARE LocalRedirect PER EVITARE ATTACHI CHE PASSANO ReturnUrl a siti malevoli esterni (SI ASSICURA DI SALTARE A PAGINE INTERNE)
        }

        [AllowAnonymous] //QUESTA ACTION VIENE LANCIATA QUANDO PREMO PULSANTE LoginWithGoogle (MA POTREI IMPOSTARLA COME DEFAULT LoginPage su Startup.cs)
        public IActionResult LoginWithGoogle(string returnUrl = "/") //PARAMETRO returnUrl=PAGINA INTERNA A CUI TORNARE DOPO IL LOGIN LETTO DA QUERYSTRING
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleLoginCallback"), //="Account/GoogleLoginCallback" QUESTO E' L'URL CONFIGURATO IN Google COME URL PER BECCARE CALLBACK AuthToken
                Items =
                {
                    { "returnUrl", returnUrl } //QUESTO INVECE E' IL RETURNURL DELLA PAGINA A CUI SALTARE DOPO AVVENUTA LOGIN -> REDIRECT PAGINA INTERNA DA CUI SON PARTITO
                }
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme); //QUESTO FA PARTIRE IL Challenge --> PROCEDURA LOGIN SU SCHEMA Google (flusso OAuth autentificazione esterna)
        }

        [AllowAnonymous] //QUEST E' ENTRYPOINT "/Account/GoogleLoginCallback" DI RITORNO DELL'AUTENTIFICAZIONE ESTERNA OAuth -> IMPOSATATA SU Google 
        public async Task<IActionResult> GoogleLoginCallback()
        {
            //QUANDO HO CONFIGURATO AddGoogle HO SPECIFICATO SCHEMA CUSTOM "ext-google" PER IL COOKIE
            var result = await HttpContext.AuthenticateAsync( //QUI VADO A FORNZARE Authenticate -> COSI POSSO OTTENERE CLAIMS TORNATI DA GOOGLE
                ExternalAuthenticationDefaults.AuthenticationScheme);

            // read google identity from the temporary cookie
            var externalClaims = result.Principal.Claims.ToList();
            //SE VOLESSI STAMPARE CLAIMS TORNATI DA GOOGLE
            System.Console.WriteLine("CLAIMS TORNATI DA Google");
            foreach (var claim in result.Principal.Claims)
            {
                System.Console.WriteLine($"{claim.Type} =\t{claim.Value}");
            }

            // DAI CLAIMS DI Google VADO AD USARE SID (oppure potrei usare ClaimTypes.Email)
            var subjectIdClaim = externalClaims.FirstOrDefault(
                x => x.Type == ClaimTypes.NameIdentifier); //DEVO AVERLO ASSOCIATO/LEGATO IN QUALCHE MODO AI MIEI Users
            var subjectValue = subjectIdClaim.Value; //LEGGO IL SID DI GOOGLE CHE HO 

            // LOGICA DI VALIDAZIONE/CONTROLLO UTENTE INTERNO (CHE DEVO AVER ASSOCIATO AL SID DI GOOGLE vedi fake repository)
            var user = userRepository.GetByGoogleId(subjectValue);

            if (user == null) return Forbid(); //SE NON HO LEGATO UTENTE INTERNO TORNO 403 = FORBID o 401 = UNAUTHORIZED

            // A QUESTO PUNTO POSSO CREARE APP_CLAIMS LEGATE A UTENTE LOCALE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), //TENGO SID GOOGLE, MA POTREI USARE Email O ALTRO COME IDENTIFICATIVO
                new Claim(ClaimTypes.Name, user.Name), //NOME UTENTE INTERNO (Claim di default)
                new Claim(ClaimTypes.Role, user.Role), //RUOLO INTERNO (Claim di default)
                new Claim("FavoriteColor", user.FavoriteColor) //CLAIM TOTALMENTE CUSTOM X POLICY
            };

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme); //CREO IDENTITY PER LO SCHEMA Cookies DI DEFAULT
            var principal = new ClaimsPrincipal(identity); //E IL PRINCIPAL --> ALLA FINE STO TRASFORMANDO UTENTE Google -> COME SE FACESSI CookieAuth LOCALE!!

            // delete temporary cookie used during google authentication
            await HttpContext.SignOutAsync( //IMPORTANTE ALLA FINE DEVO CANCELLARE COOKIE TEMPARNEO "ext-google" USATO DA Google
                ExternalAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync( //E DEVO IN FINE FARE SignIn CON APP_CLAIMS PER SCHEMA "Cookies" CHE E' IL DEFAULT LOCALE
                CookieAuthenticationDefaults.AuthenticationScheme, principal); //ALLA FINE STO TRASFORMANDO UTENTE Google -> COME SE FACESSI CookieAuth LOCALE!!

            // QUI RICAVO IL redirectUrl DAI PARAMETRI AGGIUNTIVI PASSATI A Google QUANDO HO INIZIATO IL Challenge NELLA LoginWithGoogle
            return LocalRedirect(result.Properties.Items["returnUrl"]); //IMPORTANTE USARE LocalRedirect PER EVITARE ATTACHI CHE PASSANO ReturnUrl a siti malevoli esterni (SI ASSICURA DI SALTARE A PAGINE INTERNE)
        }

        public async Task<IActionResult> Logout() //LOGOUT FINAL EBASTA FARLO SULLO SCHEMA DEFAULT="Cookies"
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); //ELIMINA IL COOKIE LOCALE
            return Redirect("/");
        }
    }
}
