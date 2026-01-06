

using BugTrackr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTrackr.Infrastructure.Persistence;

public class BugTrackrDbContext : DbContext
{
    public BugTrackrDbContext(DbContextOptions<BugTrackrDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<MessageStatus> MessageStatuses { get; set; }
    public DbSet<TypingStatus> TypingStatuses { get; set; }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Composite keys
        modelBuilder.Entity<ProjectUser>()
            .HasKey(pu => new { pu.ProjectId, pu.UserId });

        modelBuilder.Entity<IssueLabel>()
            .HasKey(il => new { il.IssueId, il.LabelId });

        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Reporter)
            .WithMany(u => u.ReportedIssues)
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict); // prevent cascade delete loops

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Assignee)
            .WithMany(u => u.AssignedIssues)
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull); // optional relationship
                                               // Chat Room Configuration
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .IsRequired();

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Chat Participant Configuration
        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();

            entity.Property(e => e.Role)
                .HasConversion<string>()
                .IsRequired();

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Participants)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Chat Message Configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .IsRequired();

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReplyToMessage)
                .WithMany()
                .HasForeignKey(e => e.ReplyToId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Message Status Configuration
        modelBuilder.Entity<MessageStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.HasOne(e => e.Message)
                .WithMany(m => m.MessageStatuses)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Typing Status Configuration
        modelBuilder.Entity<TypingStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Password Reset Token Configuration
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type)
            .HasConversion<string>()
            .IsRequired();
            entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);
            entity.Property(e => e.Message)
           .IsRequired()
           .HasMaxLength(1000);

            entity.Property(e => e.Priority)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create Index for efficient queries
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.GroupKey);
        });
    }
}
