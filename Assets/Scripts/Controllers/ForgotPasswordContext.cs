namespace UI
{
    // The forgot-password flow is split across chunk controllers, so the email
    // and the verified OTP travel through this shared state.
    public static class ForgotPasswordContext
    {
        public static string Email;
        public static string Otp;
    }
}
