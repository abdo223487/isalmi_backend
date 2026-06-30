using Microsoft.EntityFrameworkCore;
using IslamiApi.Models;

namespace IslamiApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AzkarItem> AzkarItems => Set<AzkarItem>();
    public DbSet<Fatwa> Fatwas => Set<Fatwa>();
    public DbSet<Hadith> Hadiths => Set<Hadith>();
    public DbSet<Sira> Siras => Set<Sira>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Admin
        modelBuilder.Entity<Admin>(e =>
        {
            e.ToTable("admins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Property(x => x.IsAdmin).HasDefaultValue(false);
            e.Property(x => x.IsRevoked).HasDefaultValue(false);
        });

        // AzkarItem
        modelBuilder.Entity<AzkarItem>(e =>
        {
            e.ToTable("azkar_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Category);
        });

        // Fatwa
        modelBuilder.Entity<Fatwa>(e =>
        {
            e.ToTable("fatwa");
            e.HasKey(x => x.Id);
        });

        // Hadith
        modelBuilder.Entity<Hadith>(e =>
        {
            e.ToTable("hadiths");
            e.HasKey(x => x.Id);
        });

        // Sira
        modelBuilder.Entity<Sira>(e =>
        {
            e.ToTable("sira");
            e.HasKey(x => x.Id);
        });

        // ChatConversation
        modelBuilder.Entity<ChatConversation>(e =>
        {
            e.ToTable("chat_conversations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Status).HasDefaultValue("pending");
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.ToTable("chat_messages");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ConversationId);
        });
    }
}
