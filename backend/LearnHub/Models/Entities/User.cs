namespace LearnHub.Models.Entities
{
    public enum Role
    {
        Student,
        Instructor,
        Admin
    }

    public enum PresenceStatus
    {
        Online,
        Busy
    }

    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? GoogleId { get; set; }
        public Role Role { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsSuspended { get; set; } = false;
        public string? AvatarUrl { get; set; }
        public PresenceStatus PresenceStatus { get; set; } = PresenceStatus.Online;
        public DateTime? LastActiveAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Course> CoursesTaught { get; set; } = new List<Course>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
