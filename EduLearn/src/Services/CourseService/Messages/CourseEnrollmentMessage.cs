using System;

namespace EduLearn.CourseService.Messages
{
    public class CourseEnrollmentMessage
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string EventType { get; set; } = "CourseEnrolled";
    }

    public class CourseUpdatedMessage
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string EventType { get; set; } = "CourseUpdated";
    }

    public class CourseDeletedMessage
    {
        public Guid CourseId { get; set; }
        public DateTime DeletedAt { get; set; }
        public string EventType { get; set; } = "CourseDeleted";
    }
}
