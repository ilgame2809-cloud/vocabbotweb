using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VocabBotWeb.Api.Data;
using VocabBotWeb.Api.Models;

namespace VocabBotWeb.Api.Services;

/// <summary>
/// DTO ровно под структуру Data/Seed/words.json — та же схема, что была
/// в SeedWord бота, только с JSON-атрибутами вместо record с позиционными
/// параметрами (в JSON поля лежат в camelCase, .NET по умолчанию ждёт
/// PascalCase).
/// </summary>
public class SeedWordDto
{
    [JsonPropertyName("level")] public string Level { get; set; } = "";
    [JsonPropertyName("word")] public string Word { get; set; } = "";
    [JsonPropertyName("transcription")] public string? Transcription { get; set; }
    [JsonPropertyName("partOfSpeech")] public string? PartOfSpeech { get; set; }
    [JsonPropertyName("translation")] public string? Translation { get; set; }
    [JsonPropertyName("translationUz")] public string? TranslationUz { get; set; }
    [JsonPropertyName("exampleEn")] public string? ExampleEn { get; set; }
    [JsonPropertyName("exampleRu")] public string? ExampleRu { get; set; }
    [JsonPropertyName("exampleUz")] public string? ExampleUz { get; set; }
    [JsonPropertyName("synonyms")] public string? Synonyms { get; set; }
    [JsonPropertyName("definitionEn")] public string DefinitionEn { get; set; } = "";
    [JsonPropertyName("topic")] public string Topic { get; set; } = "general";
}

/// <summary>
/// Перенос WordSeed.cs из бота: словарь по-прежнему лежит в JSON (не в C#),
/// та же валидация на дубликаты/пустые поля при загрузке. Отличие от бота —
/// там это был статический List&lt;SeedWord&gt; в памяти, здесь это реально
/// записывается в Postgres, и делается это один раз при первом старте:
/// повторные деплои не создают дублей (проверка по паре Level+WordText,
/// как и было в исходной проверке на дубликаты).
/// </summary>
public static class WordSeedService
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var seedWords = LoadSeedWords();

        // Быстрый путь: если системных слов (без владельца) уже столько же
        // или больше, чем в JSON — считаем, что сидинг уже прошёл, и не
        // тратим время на построчную сверку при каждом старте контейнера.
        var existingCount = await db.Words.CountAsync(w => w.OwnerUserId == null);
        if (existingCount >= seedWords.Count) return;

        var existingKeys = await db.Words
            .Where(w => w.OwnerUserId == null)
            .Select(w => new { w.Level, w.WordText })
            .ToListAsync();
        var existingSet = existingKeys
            .Select(k => (k.Level, Word: k.WordText.Trim().ToLowerInvariant()))
            .ToHashSet();

        var toInsert = seedWords
            .Where(w => !existingSet.Contains((w.Level, w.Word.Trim().ToLowerInvariant())))
            .Select(w => new Word
            {
                Level = w.Level,
                WordText = w.Word,
                Transcription = w.Transcription,
                PartOfSpeech = w.PartOfSpeech,
                Translation = w.Translation,
                TranslationUz = w.TranslationUz,
                ExampleEn = w.ExampleEn,
                ExampleRu = w.ExampleRu,
                ExampleUz = w.ExampleUz,
                Synonyms = w.Synonyms,
                DefinitionEn = w.DefinitionEn,
                Topic = w.Topic,
                OwnerUserId = null,
            })
            .ToList();

        if (toInsert.Count == 0) return;

        db.Words.AddRange(toInsert);
        await db.SaveChangesAsync();
    }

    private static List<SeedWordDto> LoadSeedWords()
    {
        var assembly = typeof(WordSeedService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("words.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Не найден встроенный ресурс words.json. Проверь, что " +
                "Data/Seed/words.json подключён как <EmbeddedResource> в " +
                "VocabBotWeb.Api.csproj.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var words = JsonSerializer.Deserialize<List<SeedWordDto>>(stream, options)
            ?? throw new InvalidOperationException("Data/Seed/words.json пуст или не удалось разобрать.");

        var duplicates = words
            .GroupBy(w => (w.Level, Word: w.Word.Trim().ToLowerInvariant()))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Level} / {g.Key.Word}")
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "Data/Seed/words.json содержит дубликаты (level, word): " + string.Join(", ", duplicates));
        }

        var blank = words.Where(w =>
            string.IsNullOrWhiteSpace(w.Level) || string.IsNullOrWhiteSpace(w.Word) ||
            string.IsNullOrWhiteSpace(w.Translation)).ToList();
        if (blank.Count > 0)
        {
            throw new InvalidOperationException(
                $"Data/Seed/words.json содержит {blank.Count} запись(ей) с пустым level/word/translation.");
        }

        return words;
    }
}
