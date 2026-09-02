using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VocabBotWeb.Api.Models;

namespace VocabBotWeb.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Word> Words => Set<Word>();
    public DbSet<UserWord> UserWords => Set<UserWord>();
    public DbSet<UserDeck> UserDecks => Set<UserDeck>();
    public DbSet<GrammarStat> GrammarStats => Set<GrammarStat>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<MockTestResult> MockTestResults => Set<MockTestResult>();
    public DbSet<MockPromptProgress> MockPromptProgress => Set<MockPromptProgress>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // важно вызвать первым — настраивает таблицы Identity

        // ---------- Word ----------
        b.Entity<Word>(e =>
        {
            e.ToTable("words");
            e.Property(w => w.WordText).HasMaxLength(200).IsRequired();
            e.Property(w => w.Level).HasMaxLength(50).IsRequired();
            e.Property(w => w.Topic).HasMaxLength(100).HasDefaultValue("general");
            e.Property(w => w.DefinitionEn).HasDefaultValue("");

            // Быстрый выбор "новые слова уровня/темы, которых нет у пользователя" —
            // это самый частый запрос бота (экран "Учить слова").
            e.HasIndex(w => new { w.Level, w.Topic });
            e.HasIndex(w => w.OwnerUserId);

            e.HasOne(w => w.Owner)
                .WithMany(u => u.OwnedWords)
                .HasForeignKey(w => w.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade); // удалили юзера — его свои слова тоже уходят
        });

        // ---------- UserWord ----------
        b.Entity<UserWord>(e =>
        {
            e.ToTable("user_words");
            e.HasKey(uw => new { uw.UserId, uw.WordId });
            e.Property(uw => uw.EFactor).HasDefaultValue(2.5);
            e.Property(uw => uw.Interval).HasDefaultValue(0);
            e.Property(uw => uw.Repetitions).HasDefaultValue(0);

            // Главный запрос сессии повторения: "мои слова, next_review <= сейчас".
            e.HasIndex(uw => new { uw.UserId, uw.NextReview });

            e.HasOne(uw => uw.User)
                .WithMany(u => u.UserWords)
                .HasForeignKey(uw => uw.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(uw => uw.Word)
                .WithMany(w => w.UserWords)
                .HasForeignKey(uw => uw.WordId)
                .OnDelete(DeleteBehavior.Cascade); // удалили слово (свою карточку) — прогресс по нему тоже
        });

        // ---------- UserDeck ----------
        b.Entity<UserDeck>(e =>
        {
            e.ToTable("user_decks");
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();

            // В боте PK был (user_id, name) — здесь суррогатный Id, но уникальность
            // имени колоды в рамках пользователя сохраняем явным индексом.
            e.HasIndex(d => new { d.UserId, d.Name }).IsUnique();

            e.HasOne(d => d.User)
                .WithMany(u => u.UserDecks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- GrammarStat ----------
        b.Entity<GrammarStat>(e =>
        {
            e.ToTable("grammar_stats");
            e.HasKey(g => new { g.UserId, g.Topic });
            e.Property(g => g.Topic).HasMaxLength(50);

            e.HasOne(g => g.User)
                .WithMany(u => u.GrammarStats)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- StudyPlan (1:1 c User) ----------
        b.Entity<StudyPlan>(e =>
        {
            e.ToTable("study_plans");
            e.HasKey(sp => sp.UserId);
            e.Property(sp => sp.Exam).HasMaxLength(20);

            e.HasOne(sp => sp.User)
                .WithOne(u => u.StudyPlan)
                .HasForeignKey<StudyPlan>(sp => sp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- MockTestResult ----------
        b.Entity<MockTestResult>(e =>
        {
            e.ToTable("mock_test_results");
            e.Property(m => m.Exam).HasMaxLength(20);
            e.Property(m => m.TestType).HasMaxLength(30);
            e.Property(m => m.ScaledScore).HasMaxLength(20);

            // Экран "Прогресс" читает историю по пользователю, свежие сверху.
            e.HasIndex(m => new { m.UserId, m.TakenAt });

            e.HasOne(m => m.User)
                .WithMany(u => u.MockTestResults)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- MockPromptProgress ----------
        b.Entity<MockPromptProgress>(e =>
        {
            e.ToTable("mock_prompt_progress");
            e.HasKey(p => new { p.UserId, p.Exam, p.Section, p.PromptId });
            e.Property(p => p.Exam).HasMaxLength(20);
            e.Property(p => p.Section).HasMaxLength(20);

            e.HasOne(p => p.User)
                .WithMany(u => u.MockPromptProgress)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- ApplicationUser: доп. ограничения на профильные поля ----------
        b.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.InterfaceLanguage).HasMaxLength(5).HasDefaultValue("ru");
            e.Property(u => u.Level).HasMaxLength(50).HasDefaultValue("Beginner / A1-A2");
            e.Property(u => u.Direction).HasMaxLength(10).HasDefaultValue("EN_RU");
            e.Property(u => u.DirectionTarget).HasMaxLength(5).HasDefaultValue("en");
            e.Property(u => u.WordsPerDay).HasDefaultValue(20);
            e.Property(u => u.HighestLevelPassedIndex).HasDefaultValue(-1);
            e.Property(u => u.UnlockedAchievements).HasDefaultValue("");
            e.Property(u => u.IsNotificationEnabled).HasDefaultValue(true);
        });
    }
}
