using Microsoft.AspNetCore.Mvc;

namespace ASPNETCore_DB.Controllers
{
    public class UserDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
