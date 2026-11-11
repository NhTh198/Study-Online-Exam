using Microsoft.EntityFrameworkCore;
using System;

namespace Study.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Notifications> Notifications { get; set; }
    public DbSet<Role> Roles { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Subject> Subjects { get; set; }

    public DbSet<LecturerSubject> LecturerSubjects { get; set; }

    public DbSet<Question> Questions { get; set; }

    public DbSet<QuestionOption> QuestionOptions { get; set; }

    public DbSet<Exam> Exams { get; set; }

    public DbSet<ExamQuestion> ExamQuestions { get; set; }

    public DbSet<ExamSchedule> ExamSchedules { get; set; }

    public DbSet<ExamAttempt> ExamAttempts { get; set; }

    public DbSet<ExamAttemptQuestion> ExamAttemptQuestions { get; set; }

    public DbSet<ExamAttemptOption> ExamAttemptOptions { get; set; }

    public DbSet<StudentAnswer> StudentAnswers { get; set; }

    public DbSet<ExamResult> ExamResults { get; set; }

    
public DbSet<PracticeAttempt> PracticeAttempts { get; set; }

    public DbSet<PracticeAttemptQuestion> PracticeAttemptQuestions { get; set; }

    public DbSet<PracticeAnswer> PracticeAnswers { get; set; }

    public DbSet<PracticeResult> PracticeResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ==========================================
        // ROLE
        // ==========================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        
// =========================================================
// PRACTICE ATTEMPTS
// =========================================================

modelBuilder.Entity<PracticeAttempt>(entity =>
{
    entity.ToTable("PracticeAttempts");

    entity.HasKey(x => x.Id);

    entity.Property(x => x.Status)
        .IsRequired()
        .HasMaxLength(30)
        .HasDefaultValue("InProgress");

    entity.Property(x => x.StartedAt)
        .HasDefaultValueSql("GETDATE()");

    entity.HasIndex(x => x.StudentId);

    entity.HasIndex(x => x.SubjectId);

    // StudentId -> Users.Id
    entity.HasOne(x => x.Student)
        .WithMany()
        .HasForeignKey(x => x.StudentId)
        .OnDelete(DeleteBehavior.Restrict);

    // SubjectId -> Subjects.Id
    entity.HasOne(x => x.Subject)
        .WithMany()
        .HasForeignKey(x => x.SubjectId)
        .OnDelete(DeleteBehavior.Restrict);
});


        // =========================================================
        // PRACTICE ATTEMPT QUESTIONS
        // =========================================================
        modelBuilder.Entity<PracticeAttemptQuestion>(entity =>
        {
            entity.ToTable("PracticeAttemptQuestions");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId
            })
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionOrder
            })
            .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.PracticeAttemptQuestions)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // PRACTICE ANSWERS
        // =========================================================
        modelBuilder.Entity<PracticeAnswer>(entity =>
        {
            entity.ToTable("PracticeAnswers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId
            })
            .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.PracticeAnswers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SelectedOption)
                .WithMany()
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // PRACTICE RESULTS
        // =========================================================
        modelBuilder.Entity<PracticeResult>(entity =>
        {
            entity.ToTable("PracticeResults");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Score)
                .HasPrecision(4, 2);

            entity.Property(x => x.GradedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.AttemptId)
                .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithOne(x => x.Result)
                .HasForeignKey<PracticeResult>(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // USER
        // ==========================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Password)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.StudentCode)
                .HasMaxLength(50);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasIndex(x => x.RoleId);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // SUBJECT
        // ==========================================

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });


        // ==========================================
        // LECTURER SUBJECT
        // ==========================================

        modelBuilder.Entity<LecturerSubject>(entity =>
        {
            entity.ToTable("LecturerSubjects");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.LecturerId,
                x.SubjectId
            })
            .IsUnique();

            entity.HasOne(x => x.Lecturer)
                .WithMany(x => x.LecturerSubjects)
                .HasForeignKey(x => x.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Subject)
                .WithMany(x => x.LecturerSubjects)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // QUESTION
        // ==========================================

// ==========================================
// QUESTION
// ==========================================

modelBuilder.Entity<Question>(entity =>
{
    entity.ToTable("Questions");

    entity.HasKey(x => x.Id);

    entity.Property(x => x.Topic)
        .HasMaxLength(100);

    entity.Property(x => x.Content)
        .IsRequired();

    entity.Property(x => x.Difficulty)
        .HasMaxLength(30);

    // CreatedBy có thể NULL đối với câu hỏi Practice
    entity.Property(x => x.CreatedBy)
        .IsRequired(false);

    // Question -> Subject
    // Dùng đúng navigation Subject.Questions
    entity.HasOne(x => x.Subject)
        .WithMany(x => x.Questions)
        .HasForeignKey(x => x.SubjectId)
        .OnDelete(DeleteBehavior.Restrict);

    // Question -> User
    // Dùng đúng navigation User.CreatedQuestions
    // CreatedBy có thể NULL
    entity.HasOne(x => x.Creator)
        .WithMany(x => x.CreatedQuestions)
        .HasForeignKey(x => x.CreatedBy)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.Restrict);
});



        // ==========================================
        // QUESTION OPTION
        // ==========================================

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.ToTable("QuestionOptions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.IsCorrect)
                .HasDefaultValue(false);

            entity.HasIndex(x => x.QuestionId);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ==========================================
        // EXAM
        // ==========================================

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exams");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.SubjectId);

            entity.HasIndex(x => x.CreatedBy);

            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.Subject)
                .WithMany(x => x.Exams)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Creator)
                .WithMany(x => x.CreatedExams)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM QUESTION
        // ==========================================

        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.ToTable("ExamQuestions");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.ExamId,
                x.QuestionId
            })
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.ExamId,
                x.QuestionOrder
            })
            .IsUnique();

            entity.HasOne(x => x.Exam)
                .WithMany(x => x.ExamQuestions)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.ExamQuestions)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM SCHEDULE
        // ==========================================

        modelBuilder.Entity<ExamSchedule>(entity =>
        {
            entity.ToTable("ExamSchedules");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.DurationMinutes)
                .IsRequired();

            entity.Property(x => x.GraceMinutes)
                .HasDefaultValue(15);

            entity.Property(x => x.ExamKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.IsPublished)
                .HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.ExamId)
                .IsUnique();

            entity.HasIndex(x => x.CreatedByAdmin);

            entity.HasOne(x => x.Exam)
                .WithOne(x => x.Schedule)
                .HasForeignKey<ExamSchedule>(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Admin)
                .WithMany(x => x.CreatedSchedules)
                .HasForeignKey(x => x.CreatedByAdmin)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM ATTEMPT
        // ==========================================

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.ToTable("ExamAttempts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(x => new
            {
                x.ExamId,
                x.StudentId
            })
            .IsUnique();

            entity.HasIndex(x => x.StudentId);

            entity.HasIndex(x => x.ExamId);

            entity.HasOne(x => x.Exam)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Student)
                .WithMany(x => x.ExamAttempts)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM ATTEMPT QUESTION
        // ==========================================

        modelBuilder.Entity<ExamAttemptQuestion>(entity =>
        {
            entity.ToTable("ExamAttemptQuestions");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId
            })
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionOrder
            })
            .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.ExamAttemptQuestions)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.AttemptQuestions)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM ATTEMPT OPTION
        // ==========================================

        modelBuilder.Entity<ExamAttemptOption>(entity =>
        {
            entity.ToTable("ExamAttemptOptions");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId,
                x.OptionId
            })
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId,
                x.DisplayOrder
            })
            .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.ExamAttemptOptions)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.AttemptOptions)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Option)
                .WithMany(x => x.AttemptOptions)
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // STUDENT ANSWER
        // ==========================================

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.ToTable("StudentAnswers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.AttemptId,
                x.QuestionId
            })
            .IsUnique();

            entity.HasIndex(x => x.SelectedOptionId);

            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.StudentAnswers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.StudentAnswers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SelectedOption)
                .WithMany(x => x.StudentAnswers)
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // EXAM RESULT
        // ==========================================

        modelBuilder.Entity<ExamResult>(entity =>
        {
            entity.ToTable("ExamResults");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Score)
                .HasPrecision(4, 2);

            entity.Property(x => x.GradedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.AttemptId)
                .IsUnique();

            entity.HasOne(x => x.Attempt)
                .WithOne(x => x.Result)
                .HasForeignKey<ExamResult>(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Notifications>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.Type)
                .HasMaxLength(50);

            entity.Property(x => x.IsRead)
                .HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => x.ExamId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Exam)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}