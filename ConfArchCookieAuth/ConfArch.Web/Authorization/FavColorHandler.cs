using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ConfArch.Web.Authorization
{
    public class FavColorHandler : AuthorizationHandler<FavColorRequirement> //L'HANDLER VA REGISTRATO NEI services DEL DI PER POTERLO USARE NELLE POLICY
    {
        //CUSTOM LOGIC DI VERIFICA DI UN REQUIREMENT TOTALEMTEN CUSTOM
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FavColorRequirement requirement)
        {
            if (!context.User.HasClaim(c => c.Type == "FavoriteColor"))
            {
                return Task.CompletedTask; //SE NON HO IL CLAIM PROCEDO SENZA DAR ESITO COSI CONSENTO PIU' REQUIREMENT IN OR
            }

            //ALTRIMENTI CONTROLLO SE C'E' CONTROLLO SE SODDISFA IL REQUIREMENTE RICHIESTO
            var favColor = context.User.FindFirst(c => c.Type == "FavoriteColor").Value;
            if (favColor == requirement.FavColor)
            {
                context.Succeed(requirement); //SE VERIFICO IL CRITERI DEVO MARCARE SUCCESS PASSANDO IL requirement SODDISFATTO!
            }
            else
            {
                context.Fail(); //MARCO COME FAIL --> IMPLICA CHE TUTTI I CRITERI IN AND/OR FALLIRANNO!
            }
            return Task.CompletedTask;
        }
    }
}