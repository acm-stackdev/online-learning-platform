namespace LearnHub.Models.Entities
{
    public class Section
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }

        public Course Course { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
