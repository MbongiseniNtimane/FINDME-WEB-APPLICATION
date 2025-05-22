using ASPNETCore_DB.Models;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;

namespace ASPNETCore_DB.Controllers
{
    public class ContactController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public ContactController()
        {
            _firestoreDb = FirestoreDb.Create("findme-v2-a7b1f");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.Timestamp = DateTime.UtcNow;

                // Store the data in Firestore
                DocumentReference docRef = _firestoreDb.Collection("messages").Document();
                await docRef.SetAsync(contact);

                // Set a success message in TempData
                TempData["SuccessMessage"] = "Your message has been sent successfully!";

                // Redirect to thank you page or back to the form
                return RedirectToAction("ThankYou");
            }

            return View("Index", contact);  // Return to the form if validation fails
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
