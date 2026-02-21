using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Models;

namespace GradingSystem.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Class> Classes { get; set; } = default!;
        public DbSet<Student> Students { get; set; } = default!;
        public DbSet<Subject> Subjects { get; set; } = default!;
        public DbSet<Grade> Grades { get; set; } = default!;
        public DbSet<Attendance> Attendances { get; set; } = default!;
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; } = default!;
        public DbSet<Teacher> Teachers { get; set; } = default!;
        public DbSet<ClassSubject> ClassSubjects { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // CLASS
            builder.Entity<Class>(entity =>
            {
                entity.Property(c => c.Name).IsRequired().HasMaxLength(20);
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // STUDENT
            builder.Entity<Student>(entity =>
            {
                entity.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.LastName).IsRequired().HasMaxLength(100);
                entity.HasOne(s => s.Class)
                    .WithMany(c => c.Students)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TEACHER
            builder.Entity<Teacher>(entity =>
            {
                entity.Property(t => t.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(t => t.LastName).IsRequired().HasMaxLength(100);
                entity.HasOne(t => t.User)
                    .WithOne(u => u.Teacher)
                    .HasForeignKey<Teacher>(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // APPLICATION USER → STUDENT
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Student)
                .WithMany()
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            // SUBJECT
            builder.Entity<Subject>(entity =>
            {
                entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(s => s.Name).IsUnique();
            });

            // CLASS SUBJECT
            builder.Entity<ClassSubject>(entity =>
            {
                entity.HasOne(cs => cs.Class)
                    .WithMany(c => c.ClassSubject)
                    .HasForeignKey(cs => cs.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cs => cs.Subject)
                    .WithMany(s => s.ClassSubjects)
                    .HasForeignKey(cs => cs.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cs => cs.Teacher)
                    .WithMany(t => t.ClassSubjects)
                    .HasForeignKey(cs => cs.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // GRADE
            builder.Entity<Grade>(entity =>
            {
                entity.Property(g => g.Value).IsRequired().HasPrecision(3, 2);
                entity.Property(g => g.Type).IsRequired().HasMaxLength(50);
                entity.Property(g => g.Comment).HasMaxLength(500);
                entity.HasOne(g => g.Student)
                    .WithMany(s => s.Grades)
                    .HasForeignKey(g => g.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(g => g.Subject)
                    .WithMany(s => s.Grades)
                    .HasForeignKey(g => g.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ATTENDANCE
            builder.Entity<Attendance>(entity =>
            {
                entity.Property(a => a.Status).IsRequired().HasMaxLength(50);
                entity.HasIndex(a => new { a.StudentId, a.SubjectId, a.Date }).IsUnique();
                entity.HasOne(a => a.Student)
                    .WithMany(s => s.Attendances)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Subject)
                    .WithMany(s => s.Attendances)
                    .HasForeignKey(a => a.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SCHEDULE SLOT
            builder.Entity<ScheduleSlot>(entity =>
            {
                entity.Property(ss => ss.DayOfWeek).IsRequired().HasMaxLength(20);
                entity.HasIndex(ss => new { ss.ClassId, ss.DayOfWeek, ss.PeriodNumber }).IsUnique();
                entity.HasOne(ss => ss.Class)
                    .WithMany(c => c.ScheduleSlots)
                    .HasForeignKey(ss => ss.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ss => ss.Subject)
                    .WithMany(s => s.ScheduleSlots)
                    .HasForeignKey(ss => ss.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}