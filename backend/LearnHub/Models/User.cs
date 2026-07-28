namespace LearnHub.Models
{


        public enum Role{
            Student,
            Instructor,
            Admin
        }

        public class User
        {
            public long Id { get; set;}
            public string Username { get; set;}
            public string Email { get; set;}
            public string? PasswordHash { get; set;}
            public string? GoogleId { get; set;}
            public Role Role { get; set;}
            public bool IsEmailVerified { get; set; } = false;
            public DateTime CreatedAt { get; set;}

            public ICollection<Course> EnrolledCourses {get; set;} = new List<Course>();
            public ICollection<Enrollment> Enrollments {get; set;} = new List<Enrollment>();
        }
}