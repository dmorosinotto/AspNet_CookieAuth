using Microsoft.AspNetCore.Authorization;

namespace ConfArch.Web.Authorization
{
    public class FavColorRequirement : IAuthorizationRequirement //L'INTERFACCIA E' SOLO UN PURO SEGNAPOSTO

    { //E' UNA CLASSE QUALSIASI CHE FA DA CONTENITORE DI DATI (costrutture -> property) LA LOGICA E' NELL'Handler
        public FavColorRequirement(string color)
        {
            FavColor = color;
        }

        public string FavColor { get; set; }
    }
}