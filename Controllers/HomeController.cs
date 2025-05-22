
using ASPNETCore_DB.Models;

using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASPNETCore_DB.Controllers
{
    public class HomeController : Controller
    {
   

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Privacy() => View();
        public IActionResult Contacts() => View();
        public IActionResult Courses() => View();

       
    }
}
