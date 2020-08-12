using System.Threading.Tasks;
using ConfArch.Data.Models;
using ConfArch.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfArch.Web.Controllers
{
    [Authorize]
    public class ConferenceController : Controller
    {
        private readonly IConferenceRepository repo;

        public ConferenceController(IConferenceRepository repo)
        {
            this.repo = repo;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Organizer - Conference Overview";
            return View(await repo.GetAll());
        }

        [Authorize("Admin_OR_CanAddConference")]
        public IActionResult Add()
        {
            ViewBag.Title = "Organizer - Add Conference";
            return View(new ConferenceModel());
        }

        [HttpPost]
        [Authorize(Policy = "CanAddConference", AuthenticationSchemes = "Cookies")]
        public async Task<IActionResult> Add(ConferenceModel model)
        {
            if (ModelState.IsValid)
                await repo.Add(model);

            return RedirectToAction("Index");
        }
    }
}
