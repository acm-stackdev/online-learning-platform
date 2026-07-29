namespace LearnHub.Models.DTOs.Course
{
    public class LessonSummaryDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public ContentType ContentType { get; set; }
        public string ContentUrl { get; set; }
        public int Duration { get; set; }
        public int Order { get; set; }
    }

    public class SectionSummaryDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public List<LessonSummaryDto> Lessons { get; set; } = new();
    }

    public class CourseDetailDto : CourseListItemDto
    {
        public List<SectionSummaryDto> Sections { get; set; } = new();
    }
}
