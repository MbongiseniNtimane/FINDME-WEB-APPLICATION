//using System.ComponentModel.DataAnnotations;
//using Google.Cloud.Firestore;


//namespace ASPNETCore_DB.Models
//{
//    [FirestoreData]
//    public class Message
//    {
//        [FirestoreProperty("name")]
//        [Required]
//        public string Name { get; set; }

//        [FirestoreProperty("email")]
//        [Required, EmailAddress]
//        public string Email { get; set; }

//        [FirestoreProperty("message")]
//        [Required]
//        public string Content { get; set; }

//        [FirestoreProperty("timestamp")]
//        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
//    }
//}
