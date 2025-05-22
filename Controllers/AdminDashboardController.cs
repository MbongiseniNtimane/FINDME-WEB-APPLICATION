using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASPNETCore_DB.Models;
using System.Collections;

namespace ASPNETCore_DB.Controllers
{
    [Authorize(Roles = "Admin")] // Only admins can access these actions
    public class AdminDashboardController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public AdminDashboardController()
        {
            _firestoreDb = FirestoreDb.Create("findme-v2-a7b1f"); // Your Firestore project ID
        }

        public async Task<IActionResult> Index(string itemType)
        {
            // Firestore query to fetch posts
            Query postQuery = _firestoreDb.Collection("Posts");

            // Optional: Filter by ItemType if provided
            if (!string.IsNullOrEmpty(itemType))
            {
                postQuery = postQuery.WhereEqualTo("ItemType", itemType);
            }

            QuerySnapshot postQuerySnapshot = await postQuery.GetSnapshotAsync();

            List<PostItem> posts = new List<PostItem>();
            foreach (DocumentSnapshot documentSnapshot in postQuerySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    PostItem post = documentSnapshot.ConvertTo<PostItem>();
                    posts.Add(post);
                }
            }

            // Pass the email of the logged-in user and the post list to the view
            var email = User?.Identity?.Name;
            ViewBag.AdminEmail = email;

            return View(posts);
        }

        public async Task<IActionResult> UserManagement()
        {
            var users = await GetAllUsers();
            return View(users);
        }

        // Get all users from Firestore
        private async Task<List<User>> GetAllUsers()
        {
            var users = new List<User>();
            var userCollection = _firestoreDb.Collection("users");
            var snapshot = await userCollection.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var user = doc.ConvertTo<User>();
                users.Add(user);
            }

            return users;
        }

        // GET: AdminDashboard/Edit/{email}
        public async Task<IActionResult> Edit(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty."); // Error if email is invalid
            }

            var user = await GetUserByEmail(email);
            if (user == null)
            {
                return NotFound(); // Return 404 if user is not found
            }

            if (!IsEmailValid(user.Email))
            {
                return BadRequest("Invalid email format."); // Add email format validation
            }

            if (!IsValidRole(user.Role))
            {
                return BadRequest("Invalid role specified."); // Check if the role is valid
            }

            return View(user);
        }

        // POST: AdminDashboard/Edit/{email}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string email, User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedUser); // Return validation errors
            }

            if (!IsEmailValid(updatedUser.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return View(updatedUser);
            }

            if (!IsValidRole(updatedUser.Role))
            {
                ModelState.AddModelError("Role", "Invalid role.");
                return View(updatedUser);
            }

            await UpdateUserInFirestore(email, updatedUser);
            return RedirectToAction("UserManagement");
        }

        // DELETE: AdminDashboard/Delete/{email}
        public async Task<IActionResult> Delete(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            var user = await GetUserByEmail(email);
            if (user == null)
            {
                return NotFound(); // User not found
            }

            return View(user); // Show confirmation view with the user details
        }

        // POST: AdminDashboard/Delete/{email}
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            // Delete the user from Firestore
            await DeleteUserFromFirestore(email);

            // Delete the user's associated posts from Firestore
            await DeleteUserPosts(email);

            return RedirectToAction("UserManagement"); // Redirect to the user management page after deletion
        }

        // Helper method to get a user by email from Firestore
        public async Task<User> GetUserByEmail(string email)
        {
            var usersCollection = _firestoreDb.Collection("users");
            var query = usersCollection.WhereEqualTo("email", email);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count > 0)
            {
                var userDoc = snapshot.Documents.First();
                return userDoc.ConvertTo<User>();
            }

            return null;
        }

        // Delete user from Firestore
        public async Task DeleteUserFromFirestore(string email)
        {
            var usersCollection = _firestoreDb.Collection("users");
            var query = usersCollection.WhereEqualTo("email", email);
            var snapshot = await query.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync(); // Delete each user document
            }
        }

        // Helper method to delete posts related to the user
        public async Task DeleteUserPosts(string posterName)
        {
            var postsCollection = _firestoreDb.Collection("Posts");

            // Query posts based on the 'PosterName' field
            var query = postsCollection.WhereEqualTo("PosterName", posterName); // Use 'PosterName' instead of 'PostedBy'
            var snapshot = await query.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync(); // Delete each post document related to the user
            }
        }


        // Update user in Firestore
        private async Task UpdateUserInFirestore(string email, User updatedUser)
        {
            var docRef = _firestoreDb.Collection("users").Document(email);
            await docRef.SetAsync(updatedUser); // Overwrite existing document
        }

        // Email validation method
        private bool IsEmailValid(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains("@"); // Simple email validation
        }

        // Role validation method (you can add more roles if needed)
        private bool IsValidRole(string role)
        {
            var validRoles = new List<string> { "Admin", "User" }; // List of valid roles
            return validRoles.Contains(role);
        }

        public async Task<IActionResult> Messages()
        {
            var messages = new List<Contact>();

            try
            {
                var snapshot = await _firestoreDb.Collection("messages").GetSnapshotAsync();
                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var contact = document.ConvertTo<Contact>();
                        contact.Id = document.Id; // Assign the document ID for deletion
                        messages.Add(contact);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving messages: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while retrieving messages.";
                return RedirectToAction("Error"); // Ensure this action is defined
            }

            return View(messages); // Return the view with the messages
        }

        // POST: AdminDashboard/DeleteMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Message ID cannot be null or empty.";
                return RedirectToAction("Messages");
            }

            try
            {
                var messageDocRef = _firestoreDb.Collection("messages").Document(id);
                await messageDocRef.DeleteAsync();
                TempData["SuccessMessage"] = "Message deleted successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the message.";
            }

            return RedirectToAction("Messages");
        }

        // Error handling action

        public IActionResult Error()
        {
            return View();
        }
    }

    
}
