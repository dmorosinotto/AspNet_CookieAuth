using System.Threading.Tasks;
using ConfArch.Data.Models;
using ConfArch.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfArch.Web.Controllers
{

    public class ProposalController : Controller
    {
        private readonly IConferenceRepository conferenceRepo;
        private readonly IProposalRepository proposalRepo;

        private readonly IAuthorizationService authService;

        public ProposalController(IConferenceRepository conferenceRepo, IProposalRepository proposalRepo, IAuthorizationService authService)
        {
            this.conferenceRepo = conferenceRepo;
            this.proposalRepo = proposalRepo;
            this.authService = authService;
        }

        [Authorize("LoveRed")]
        public async Task<IActionResult> Index(int conferenceId)
        {
            var conference = await conferenceRepo.GetById(conferenceId);
            ViewBag.Title = $"Speaker - Proposals For Conference {conference.Name} {conference.Location}";
            ViewBag.ConferenceId = conferenceId;

            return View(await proposalRepo.GetAllForConference(conferenceId));
        }


        [Authorize(Roles = "Admin,Speaker")] //USO DI AUTHORIZATION + RUOLI DEFAULT
        public IActionResult AddProposal(int conferenceId)
        {
            ViewBag.Title = "Speaker - Add Proposal";
            return View(new ProposalModel { ConferenceId = conferenceId });
        }

        [HttpPost]
        [Authorize("IsSpeaker")] //USO DI AUTHORIZATION CON CUSTOM POLICY
        public async Task<IActionResult> AddProposal(ProposalModel proposal)
        {
            if (ModelState.IsValid)
                await proposalRepo.Add(proposal);
            return RedirectToAction("Index", new { conferenceId = proposal.ConferenceId });
        }

        [Authorize(Roles = "Admin")] //USO DI AUTHORIZATION + RUOLI DEFAULT
        public async Task<IActionResult> Approve(int proposalId)
        {
            var proposal = await proposalRepo.Approve(proposalId);
            return RedirectToAction("Index", new { conferenceId = proposal.ConferenceId });
        }

        public async Task<IActionResult> EditProposal(ProposalModel proposal)
        {
            //VERIFICA A RUNTIME DELLA RESOURCE POLICY CanEditProposal CON PASSAGGIO DELLA proposal <- resource da controllare
            var result = await authService.AuthorizeAsync(User, proposal, "CanEditProposal");
            if (result.Succeeded)
            {
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Account");
            }

        }
    }
}
