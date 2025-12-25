using DocAttestation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocAttestation.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicantProfile> ApplicantProfiles { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
    public DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public DbSet<WorkflowHistory> WorkflowHistory { get; set; }
    public DbSet<QRVerificationToken> QRVerificationTokens { get; set; }
    public DbSet<QRScanLog> QRScanLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RegisteredDevice> RegisteredDevices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicantProfile Configuration
        builder.Entity<ApplicantProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CNICHash).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CNICHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.EncryptedCNIC).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.ApplicantProfile)
                .HasForeignKey<ApplicantProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Application Configuration
        builder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApplicationNumber).IsUnique();
            entity.Property(e => e.ApplicationNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DocumentHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).HasConversion<int>();
            
            entity.HasOne(e => e.ApplicantProfile)
                .WithMany(p => p.Applications)
                .HasForeignKey(e => e.ApplicantProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkflowStep Configuration
        builder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.WorkflowSteps)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.AssignedToUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkflowHistory Configuration
        builder.Entity<WorkflowHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Level).HasConversion<int>();
            entity.Property(e => e.PreviousStatus).HasConversion<int>();
            entity.Property(e => e.NewStatus).HasConversion<int>();
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.WorkflowHistory)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ActionByUser)
                .WithMany()
                .HasForeignKey(e => e.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QRVerificationToken Configuration
        builder.Entity<QRVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EncryptedToken);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.QRVerificationTokens)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QRScanLog Configuration
        builder.Entity<QRScanLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.QRVerificationToken)
                .WithMany(q => q.ScanLogs)
                .HasForeignKey(e => e.QRVerificationTokenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken Configuration
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment Configuration
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Payments)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationDocument Configuration
        builder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DocumentHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).HasConversion<int>();
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Documents)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.VerifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RegisteredDevice Configuration
        builder.Entity<RegisteredDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.DeviceTokenHash);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceTokenHash).IsRequired().HasMaxLength(64);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

