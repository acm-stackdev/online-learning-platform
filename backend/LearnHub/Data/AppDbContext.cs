using Microsoft.EntityFrameworkCore;
using LearnHub.Models;

namespace LearnHub.Data
{
    public class AppDbContext : DbContext
    {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<LessonProgress> LessonProgress { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<VerificationToken> VerificationTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder){
        //for unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        //for unique google account linkage (Postgres allows multiple NULLs in a unique index)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleId)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VerificationToken>()
            .HasOne(vt => vt.User)
            .WithMany()
            .HasForeignKey(vt => vt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VerificationToken>()
            .Property(vt => vt.Purpose)
            .HasConversion<string>();


        //for unique duplicate enrollments
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();

        //for unique progress records
        modelBuilder.Entity<LessonProgress>()
            .HasIndex(lp => new { lp.EnrollmentId, lp.LessonId })
            .IsUnique();

        //store enum as strings
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Course>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Lesson>()
            .Property(l => l.ContentType)
            .HasConversion<string>();

        modelBuilder.Entity<Certificate>()
            .HasIndex(c => c.EnrollmentId).IsUnique();

    }
    }
}