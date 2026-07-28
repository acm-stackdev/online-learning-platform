namespace LearnHub.Models
{
    public enum TokenPurpose
    {
        EmailVerification,
        PasswordReset
    }

    public class VerificationToken
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string TokenHash { get; set; }
        public TokenPurpose Purpose { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public User User { get; set; }
    }
}
