using Google.Cloud.Firestore;

namespace ASPNETCore_DB.Models
{
    [FirestoreData] // Marks the class as Firestore-compatible
    public class User
    {
        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; }

    }
}
