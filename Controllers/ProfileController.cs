using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ASPNETCore_DB.Models;
using Firebase.Storage;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;

namespace ASPNETCore_DB.Controllers
{
    public class ProfileController : Controller
    {
        private readonly string _firebaseStorageUrl = "gs://findme-v2-a7b1f.appspot.com";
        private readonly FirestoreDb _firestore;

        public ProfileController()
        {
            _firestore = FirestoreDb.Create("findme-v2-a7b1f"); // Replace with your Firebase Project ID
        }

        public async Task<IActionResult> Index()
        {
            // Retrieve email from the identity set during login
            var userEmail = User.Identity.Name;
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userProfile = await GetUserProfile(userEmail);
            if (userProfile == null)
            {
                return RedirectToAction("Error", "Home"); // Handle profile not found
            }
            return View(userProfile);
        }


        public async Task<IActionResult> Edit()
        {
            var userProfile = await GetUserProfile(User.Identity.Name);
            if (userProfile == null)
            {
                ViewBag.ErrorMessage = "Profile not found.";
                return View("Error");
            }
            return View(userProfile);
        }


        [HttpPost]
        public async Task<IActionResult> Update(UserProfile model, IFormFile profilePicture)
        {
            if (ModelState.IsValid)
            {
                // Ensure DateOfBirth is in UTC if it's not null
                if (model.DateOfBirth.HasValue)
                {
                    if (model.DateOfBirth.Value.Kind != DateTimeKind.Utc)
                    {
                        // Convert to UTC if not already in UTC
                        model.DateOfBirth = DateTime.SpecifyKind(model.DateOfBirth.Value, DateTimeKind.Utc);
                    }
                }

                // Handle profile picture upload if provided
                if (profilePicture != null && profilePicture.Length <= 5 * 1024 * 1024) // 5 MB limit
                {
                    model.ProfilePictureUrl = await UploadProfilePicture(profilePicture);
                }
                else if (profilePicture != null && profilePicture.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfilePicture", "The profile picture must be less than 5 MB.");
                    return View("Edit", model);
                }

                // Update user profile in Firebase
                await UpdateUserProfile(model);
                return RedirectToAction("Index");
            }
            return View("Edit", model); // Return the view with validation errors
        }


        private async Task<string> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("No file uploaded.");
                return null; // Early exit if no file is provided
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0; // Reset stream position

                // Unique file name based on current ticks
                var fileName = $"{DateTime.Now.Ticks}.jpg";

                // Firebase Storage reference
                var storage = new FirebaseStorage(_firebaseStorageUrl);

                // Uploading the file
                var uploadTask = storage.Child("profile_pictures").Child(fileName).PutAsync(stream);

                // Awaiting upload completion
                var downloadUrl = await uploadTask;

                Console.WriteLine($"Profile picture uploaded successfully: {downloadUrl}");

                return downloadUrl; // Return the download URL
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading profile picture: " + ex.Message);
                return null; // Return null if upload fails
            }
        }



        private async Task<UserProfile> GetUserProfile(string email)
        {
            try
            {
                var docRef = _firestore.Collection("profile").Document(email);
                var snapshot = await docRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<UserProfile>();
                }
                else
                {
                    Console.WriteLine($"Document with email {email} does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching profile: {ex.Message}");
            }
            return null;
        }

        private async Task UpdateUserProfile(UserProfile model)
        {
            // Create a reference to the document using the email
            var docRef = _firestore.Collection("profile").Document(model.Email);

            // Check if the document exists
            var snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                // Document exists, prepare to update only specified fields
                var updates = new Dictionary<string, object>
        {
            { "name", model.Name }, // Use lowercase as per Firestore
            { "email", model.Email },
            { "mobile", model.Mobile },
            { "gender", model.Gender },
            { "dob", model.DateOfBirth },
            { "profilePicture", model.ProfilePictureUrl }
        };

                // Log the update attempt
                Console.WriteLine($"Updating profile for: {model.Email} with values: {string.Join(", ", updates.Select(u => $"{u.Key}: {u.Value}"))}");

                // Update the existing document
                await docRef.UpdateAsync(updates);
            }
            else
            {
                // Document does not exist, log this and consider creating a new profile if needed
                Console.WriteLine($"No existing profile found for: {model.Email}. Document will not be created.");
                // Uncomment the following line if you want to create a new profile in case of no existing document
                // await docRef.SetAsync(model, SetOptions.MergeAll);
            }
        }



    }
}
