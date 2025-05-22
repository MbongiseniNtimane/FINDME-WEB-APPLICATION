using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace ASPNETCore_DB.Models
{
    [FirestoreData] // Annotate the class for Firestore serialization
    public class UserProfile
    {
        [FirestoreProperty("name")] // Matches lowercase field name in Firestore
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [FirestoreProperty("email")] // Matches lowercase field name in Firestore
        [Display(Name = "Email")]
        public string Email { get; set; }

        [FirestoreProperty("mobile")] // Matches lowercase field name in Firestore
        [Display(Name = "Mobile")]
        public string Mobile { get; set; }

        [FirestoreProperty("gender")] // Matches lowercase field name in Firestore
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [FirestoreProperty("dob")] // Matches lowercase field name in Firestore
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [FirestoreProperty("profilePicture")] // Matches lowercase field name in Firestore
        [Display(Name = "Profile Picture")]
        public string ProfilePictureUrl { get; set; }
    }
}
