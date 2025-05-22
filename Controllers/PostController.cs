using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using ASPNETCore_DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ASPNETCore_DB.Controllers
{
    public class PostController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private const string CollectionName = "Posts"; // Firestore collection name

        public PostController()
        {
            _firestoreDb = FirestoreDb.Create("findme-v2-a7b1f"); // Your Firestore project ID
        }

        // Get all posts (filterable by item type)
        public async Task<IActionResult> Index(string itemType = "Lost")
        {
            Query query = _firestoreDb.Collection(CollectionName).WhereEqualTo("ItemType", itemType);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            List<PostItem> posts = new List<PostItem>();
            foreach (var document in snapshot.Documents)
            {
                PostItem post = document.ConvertTo<PostItem>();
                post.Id = document.Id; // Assign Firestore document ID to the post
                posts.Add(post);
            }

            return View(posts);
        }

        // Create a new post
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(PostItem post, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Check if an image is uploaded
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Set a unique filename using the current timestamp
                    var fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                    var extension = Path.GetExtension(ImageFile.FileName);
                    fileName = fileName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + extension;

                    // Set the path to save the image in the wwwroot/images directory
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    // Save the image to the server
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    // Update the post object with the image path
                    post.ImagePath = "/images/" + fileName;
                }

                // Set PostedTime to UTC
                post.PostedTime = DateTime.UtcNow;

                // Save post to Firestore
                DocumentReference docRef = await _firestoreDb.Collection(CollectionName).AddAsync(post);
                post.Id = docRef.Id; // Get the document ID

                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }


        // Edit a post
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                PostItem post = snapshot.ConvertTo<PostItem>();
                post.Id = snapshot.Id;
                return View(post);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, PostItem updatedPost, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing document
                DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(id);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    return NotFound();
                }

                PostItem existingPost = snapshot.ConvertTo<PostItem>();

                // Process the new image file if one is uploaded
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Define the path where the image will be saved (example path, adjust as needed)
                    string imagePath = Path.Combine("wwwroot/uploads", imageFile.FileName);

                    // Save the image to the defined path
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Update the image path in the post
                    updatedPost.ImagePath = "/uploads/" + imageFile.FileName;
                }
                else
                {
                    // Keep the existing image if no new file is uploaded
                    updatedPost.ImagePath = existingPost.ImagePath;
                }

                // Set the update time and overwrite the post in Firestore
                updatedPost.PostedTime = DateTime.UtcNow;
                await docRef.SetAsync(updatedPost, SetOptions.Overwrite);

                return RedirectToAction(nameof(Index));
            }

            return View(updatedPost);
        }



        // Delete a post
        public async Task<IActionResult> Delete(string id)
        {
            DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                await docRef.DeleteAsync();
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }

        // Share a post (returns post details as a shareable string)
        public async Task<IActionResult> Share(string id)
        {
            DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                PostItem post = snapshot.ConvertTo<PostItem>();
                string postDetails = $@"
                Item Name: {post.ItemName}
                Description: {post.Description}
                Posted By: {post.PosterName}
                Contact: {post.ContactDetails}
                Course: {post.Course}
                Category: {post.Category}
                Item Type: {post.ItemType}
                Posted {DateTime.Now.Subtract(post.PostedTime).Minutes} minutes ago
                ";
                return Content(postDetails);
            }
            return NotFound();
        }
    }
}
