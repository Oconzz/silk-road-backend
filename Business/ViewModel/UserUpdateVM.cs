namespace Business.ViewModel
{
    public class CreateUserVM
    {
        public string? email { get; set; }
        public string? password { get; set; }
        public string? fullName { get; set; }
        public string? phone { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? zipcode { get; set; }
        public string? country { get; set; }
        // Base64 encoded image data (optionally with data URL prefix). Will be resized and uploaded to S3.
        public string? personalPhotoBase64 { get; set; }
        // Output: current URLs stored in DB
        public string? personalPhoto { get; set; }
        public string? personalPhotoThumbnail { get; set; }
    }

    public class UserUpdateVM : CreateUserVM
    {
        public Guid id { get; set; }
    }
}
