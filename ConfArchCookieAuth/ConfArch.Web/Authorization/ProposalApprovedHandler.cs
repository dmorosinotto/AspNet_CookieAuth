using System.Threading.Tasks;
using ConfArch.Data.Models;
using Microsoft.AspNetCore.Authorization;

namespace ConfArch.Web.Authorization
{
    public class ProposalNotApprovedHandler : AuthorizationHandler<ProposalRequirement, ProposalModel> //NON SERVE REGISTRARE HANDLER PERCHE' VIENE USATO A RUNTIME
    {
        //CUSTOM LOGIC DI VERIFICA DI UN REQUIREMENT BASATO SU RISORSE - OSSIA LOCICA CONTROLLA A RUNTIME STATO DI UNA RISORSA
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProposalRequirement requirement, ProposalModel resource)
        {
            if (!resource.Approved)
            {   //MARCA SUCCESS SOLO SE PROPOSAL NON E' APPROVATA -> USATA NELLA POLICY CanEditProposal SOLO SE NON APPROVATA
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}