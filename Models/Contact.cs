using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace ASPNETCore_DB.Models
{
    [FirestoreData]
    public class Contact
    {
        public string Id { get; set; }  // Document ID for deletion or other operations

        [FirestoreProperty("name")]
        [Required]
        public string Name { get; set; }

        [FirestoreProperty("email")]
        [Required, EmailAddress]
        public string Email { get; set; }

        [FirestoreProperty("message")]
        [Required]
        public string Message { get; set; }

        [FirestoreProperty("timestamp")]
        public DateTime? Timestamp { get; set; }  // Nullable DateTime for the timestamp
    }
}
