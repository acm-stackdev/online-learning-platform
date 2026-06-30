namespace LearnHub.Models
{

public class Enrollment
{
        public long Id { get; set;}
        public long StudentId { get; set;}
        public long CourseId { get; set;}
        public bool IsCompleted { get; set;}
        public DateTime EnrolledAt { get; set;}

        public User Student { get; set;}
        public Course Course { get; set;}
        public ICollection<LessonProgress> ProgressRecords { get; set; } = new List<LessonProgress>();
        public Certificate? Certificate { get; set;}
    }
}