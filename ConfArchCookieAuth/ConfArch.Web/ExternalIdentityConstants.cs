using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConfArch.Web
{
    public static class ExternalAuthenticationDefaults
    {
        // QUESTA COSTANTE VIENE USATA PER CONFIGURARE IL Challange NEL Startup.cs 
        //+ NEL AccountController.cs LoginWithGoogle PER FARE GIRO DI AUTH CUSTOM CON PROVIDER ESTERNO -> POI TRASFORMATO IN UTENTE INTERNO (APP_CLAIMS)
        public const string AuthenticationScheme = "ext-google"; //ERA: "ExternalIdentity";
    }
}
