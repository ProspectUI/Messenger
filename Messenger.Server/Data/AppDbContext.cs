using Microsoft.EntityFrameworkCore;
using Messenger.Shared.Models;

namespace Messenger.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMember>()
            .HasKey(cm => new { cm.ChatId, cm.UserId });

        modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.Chat)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.ChatId);

        modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.ChatMembers)
            .HasForeignKey(cm => cm.UserId);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
