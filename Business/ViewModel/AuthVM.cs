namespace Business.ViewModel
{
    public class ForgotPasswordVM
    {
        public string email { get; set; } = string.Empty;
    }

    public class ResetPasswordVM
    {
        public string token { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
    }
}
