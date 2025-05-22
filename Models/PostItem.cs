using Google.Cloud.Firestore;

namespace ASPNETCore_DB.Models
{
    [FirestoreData]
    public class PostItem
    {
        [FirestoreProperty]
        public string Id { get; set; } // Firestore document ID

        [FirestoreProperty]
        public string ItemName { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public string PosterName { get; set; }

        [FirestoreProperty]
        public string ContactDetails { get; set; }

        [FirestoreProperty]
        public string Course { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        public string ItemType { get; set; }

        [FirestoreProperty]
        public DateTime PostedTime { get; set; }

        // Parameterless constructor
        public PostItem() { }

        [FirestoreProperty]
        public string ImagePath { get; set; }

        [FirestoreProperty]
        public double Latitude { get; set; }  // To store latitude

        [FirestoreProperty]
        public double Longitude { get; set; } // To store longitude


    }
}
