namespace LearnHub.Models.Entities
{
    public enum ContentType
    {
        Video,
        Pdf
    }

    public class Lesson
    {
        public long Id { get; set; }
        public long SectionId { get; set; }
        public string Title { get; set; }
        public ContentType ContentType { get; set; }
        public string ContentUrl { get; set; }
        public int Duration { get; set; }
        public int Order { get; set; }

        public Section Section { get; set; }
        public ICollection<LessonProgress> LessonRecords { get; set; } = new List<LessonProgress>();
    }
}
