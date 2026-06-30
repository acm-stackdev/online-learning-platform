namespace LearnHub.Models
{
    public class Course
    {
        public long Id { get; set; }
        public long InstructorId { get; set; }
        public string Title { get; set;}
        public string Description { get; set;}
        public string ThumbnailUrl { get; set;}
        public string Category { get; set;}
        public bool IsPublished { get; set;}
        public DateTime CreatedAt { get; set;}

        public User Instructor { get; set;}
        public ICollection<Section> Sections { get; set;} = new List<Section>();
        public ICollection<Enrollment> Enrollments { get; set;} = new List<Enrollment>();
    }
}
