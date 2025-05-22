using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using ASPNETCore_DB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;

namespace ASPNETCore_DB.Controllers
{
    public class AccountController : Controller
    {
        private readonly FirestoreDb _firestoreDb; // Firestore database instance
        private readonly IConfiguration _configuration; // Configuration for accessing app settings
        private const string FirebaseApiKey = "AIzaSyD14Cj4kuZslqfx890SgpQQpHOrvc_FcQI"; // Replace with your Firebase API key
        private const string FirestoreProjectId = "findme-v2-a7b1f"; // Firestore project ID
        private const string AdminEmail = "admin@gmail.com"; // Admin email
        private const string AdminPassword = "Test!123"; // Admin password (ensure to secure this)

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;

            // Initialize Firestore connection
            _firestoreDb = FirestoreDb.Create(FirestoreProjectId);

            // Initialize Firebase Admin SDK
            InitializeFirebaseAdmin();

            // Ensure the admin user is created
            InitializeAdminUser().Wait();
        }

        private void InitializeFirebaseAdmin()
        {
            try
            {
                // Initialize Firebase Admin SDK only if not already initialized
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(_configuration["Firebase:ServiceAccountKeyPath"]),
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle initialization errors
                // Consider logging the error
                throw new Exception("Failed to initialize Firebase Admin SDK.", ex);
            }
        }

        private async Task InitializeAdminUser()
        {
            // Check if the admin user exists in Firestore
            if (!await CheckUserInFirestore(AdminEmail))
            {
                try
                {
                    // Create admin user with email verified
                    UserRecordArgs args = new UserRecordArgs()
                    {
                        Email = AdminEmail,
                        EmailVerified = true,
                        Password = AdminPassword,
                        Disabled = false,
                    };
                    UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

                    // Add admin to Firestore
                    await AddUserToFirestore(AdminEmail, "Admin");
                }
                catch (FirebaseAuthException fae)
                {
                    // Handle Firebase Authentication errors
                    // For example, if the user already exists
                    // Log the error as needed
                    Console.WriteLine($"Error creating admin user: {fae.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Unexpected error creating admin user: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "Login";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Check if the user exists in Firestore before proceeding
                if (!await CheckUserInFirestore(model.Email))
                {
                    ModelState.AddModelError(string.Empty, "User does not exist.");
                    return View(model);
                }

                // Firebase sign-in
                var firebaseSignInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}";
                var client = new RestClient();
                var request = new RestRequest(firebaseSignInUrl, Method.Post);
                request.AddJsonBody(new { email = model.Email, password = model.Password, returnSecureToken = true });
                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    // Deserialize the response to get the idToken and other details
                    var signInResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                    string idToken = signInResponse.ContainsKey("idToken") ? signInResponse["idToken"].ToString() : null;

                    if (string.IsNullOrEmpty(idToken))
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login response from authentication server.");
                        return View(model);
                    }

                    // Admin login check before email verification
                    if (model.Email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        // Proceed without email verification
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Email),
                            new Claim(ClaimTypes.Role, "Admin") // Admin role
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToAction("Index", "AdminDashboard");
                    }

                    // Check if email is verified for non-admin users
                    var lookupUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={FirebaseApiKey}";
                    var lookupRequest = new RestRequest(lookupUrl, Method.Post);
                    lookupRequest.AddJsonBody(new { idToken = idToken });
                    RestResponse lookupResponse = await client.ExecuteAsync(lookupRequest);

                    if (lookupResponse.IsSuccessful)
                    {
                        var lookupData = JsonConvert.DeserializeObject<Dictionary<string, object>>(lookupResponse.Content);
                        if (lookupData.TryGetValue("users", out var usersObj) && usersObj is JArray usersArray && usersArray.Count > 0)
                        {
                            var user = usersArray[0] as JObject;
                            bool emailVerified = user["emailVerified"] != null && user["emailVerified"].ToObject<bool>();

                            if (!emailVerified)
                            {
                                ModelState.AddModelError(string.Empty, "Email not verified. Please check your inbox for the verification email.");
                                ViewBag.Email = model.Email; // Pass the email to the view
                                return View("Login"); // Ensure your Login view can handle ViewBag.Email and display the resend link
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Unable to retrieve user information.");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error verifying email status.");
                        return View(model);
                    }

                    // Regular user login
                    var userClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Email),
                        new Claim(ClaimTypes.Role, "User") // Regular user role
                    };

                    var userClaimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(userClaimsIdentity));

                    return RedirectToAction("Index", "UserDashboard");
                }
                else
                {
                    // Handle Firebase login errors
                    var firebaseError = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                    if (firebaseError.ContainsKey("error"))
                    {
                        var errorDetails = firebaseError["error"] as JObject;
                        var errorMessage = errorDetails["message"]?.ToString();

                        // Handle specific Firebase error messages
                        ModelState.AddModelError(string.Empty, errorMessage switch
                        {
                            "INVALID_PASSWORD" => "Incorrect password.",
                            "EMAIL_NOT_FOUND" => "User does not exist.",
                            _ => "Login failed. Please check your credentials."
                        });
                    }
                }
            }

            return View(model);
        }

        // Check if user exists in Firestore
        private async Task<bool> CheckUserInFirestore(string email)
        {
            DocumentReference docRef = _firestoreDb.Collection("users").Document(email);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists;
        }

        // Add user to Firestore
        private async Task AddUserToFirestore(string email, string role)
        {
            DocumentReference docRef = _firestoreDb.Collection("users").Document(email);
            await docRef.SetAsync(new { email = email, role = role });
        }

        // Add admin to Firebase Authentication
        private async Task AddAdminToFirebase()
        {
            // This method is no longer needed as admin is created in InitializeAdminUser using Admin SDK
            // You can remove this method if not used elsewhere
        }

        // Check if user exists in Firebase Authentication
        private async Task<bool> CheckUserInFirebase(string email)
        {
            try
            {
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                return userRecord != null;
            }
            catch (FirebaseAuthException)
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUp());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUp model)
        {
            // Prevent signing up with admin email
            if (model.Email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Cannot sign up with admin email.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var firebaseSignUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseApiKey}";
                var client = new RestClient();
                var request = new RestRequest(firebaseSignUpUrl, Method.Post);
                request.AddJsonBody(new { email = model.Email, password = model.Password, returnSecureToken = true });
                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    // Deserialize the response to get the idToken
                    var signUpResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                    if (signUpResponse.TryGetValue("idToken", out var idTokenObj))
                    {
                        string idToken = idTokenObj.ToString();

                        // Send verification email
                        var sendOobCodeUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={FirebaseApiKey}";
                        var sendRequest = new RestRequest(sendOobCodeUrl, Method.Post);
                        sendRequest.AddJsonBody(new
                        {
                            requestType = "VERIFY_EMAIL",
                            idToken = idToken,
                            continueUrl = Url.Action("VerifyEmail", "Account", null, Request.Scheme)
                        });
                        RestResponse sendResponse = await client.ExecuteAsync(sendRequest);

                        if (sendResponse.IsSuccessful)
                        {
                            await AddUserToFirestore(model.Email, "User");
                            ViewBag.Message = "Registration successful! A verification email has been sent to your email address. Please verify to log in.";
                            return View("Login"); // Redirect to Login view with message
                        }
                        else
                        {
                            // Handle errors in sending verification email
                            var errorDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(sendResponse.Content);
                            var errorMessage = errorDetails["error"] is JObject errorObj
                                ? errorObj["message"]?.ToString()
                                : "Error sending verification email.";
                            ModelState.AddModelError(string.Empty, errorMessage);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error processing sign-up response.");
                    }
                }
                else
                {
                    var errorDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                    var errorMessage = errorDetails["error"] is JObject errorObj
                        ? errorObj["message"]?.ToString()
                        : "An unknown error occurred.";

                    ModelState.AddModelError(string.Empty, errorMessage switch
                    {
                        "EMAIL_EXISTS" => "This email address is already in use. Please use a different email.",
                        _ => "Error during sign-up: " + errorMessage
                    });
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid email.");
                return View();
            }

            var firebaseForgotPasswordUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={FirebaseApiKey}";
            var client = new RestClient();
            var request = new RestRequest(firebaseForgotPasswordUrl, Method.Post);
            request.AddJsonBody(new { requestType = "PASSWORD_RESET", email = email });
            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                ViewBag.Message = "Password reset instructions have been sent to your email.";
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Error sending reset instructions. Try again.");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string mode, string oobCode, string lang = null, string continueUrl = null)
        {
            if (mode != "verifyEmail" || string.IsNullOrEmpty(oobCode))
            {
                return BadRequest("Invalid verification request.");
            }

            var applyActionCodeUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:applyActionCode?key={FirebaseApiKey}";
            var client = new RestClient();
            var request = new RestRequest(applyActionCodeUrl, Method.Post);
            request.AddJsonBody(new { oobCode = oobCode });
            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                ViewBag.Message = "Your email has been successfully verified. You can now log in.";
                return View("Login"); // Redirect to Login view with success message
            }
            else
            {
                var errorDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                var errorMessage = errorDetails["error"] is JObject errorObj
                    ? errorObj["message"]?.ToString()
                    : "Error verifying email.";

                ViewBag.Message = $"Email verification failed: {errorMessage}";
                return View("Error"); // Redirect to an Error view with message
            }
        }

        [HttpGet]
        public IActionResult ResendVerificationEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid email.");
                return View();
            }

            // To resend verification email, the user needs to be authenticated.
            // Since we don't have the user's password, consider using Firebase Admin SDK for this functionality.
            // Alternatively, inform the user to log in (if possible) and resend from their profile.

            ModelState.AddModelError(string.Empty, "Unable to resend verification email. Please log in and request a new verification email from your profile.");
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData.Clear();
            HttpContext.Session.Clear(); // Clear the session
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        //public IActionResult Contact()
        //{
          //  return View();
        //}

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(Contact model)
        {
            if (ModelState.IsValid)
            {
                // Create a new document in Firestore
                DocumentReference docRef = _firestoreDb.Collection("messages").Document();
                await docRef.SetAsync(new
                {
                    Name = model.Name,
                    Email = model.Email,
                    Message = model.Message
                });

                ViewBag.Message = "Your message has been sent successfully!";
                return View();
            }

            
            return View(model);
        }

        public IActionResult AboutUs()
        {
            return View();
        }




    }
}
    

