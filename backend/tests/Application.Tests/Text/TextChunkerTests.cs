using AiPlatform.Application.Common.Text;
using Xunit;

namespace AiPlatform.Application.Tests.Text;

public class TextChunkerTests
{
    [Fact]
    public void ShortText_ReturnsSingleChunk()
    {
        var chunks = TextChunker.Chunk("short text here", maxChars: 200);
        Assert.Single(chunks);
        Assert.Equal("short text here", chunks[0]);
    }

    [Fact]
    public void LongText_SplitsIntoMultipleChunks()
    {
        var text = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"word{i}"));
        var chunks = TextChunker.Chunk(text, maxChars: 50, overlapWords: 3);
        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public void ConsecutiveChunks_ShareOverlapWords()
    {
        var text = string.Join(" ", Enumerable.Range(1, 60).Select(i => $"w{i}"));
        var chunks = TextChunker.Chunk(text, maxChars: 40, overlapWords: 4);

        var firstWords = chunks[0].Split(' ');
        var secondWords = chunks[1].Split(' ');
        // দ্বিতীয় chunk প্রথমটার শেষ কিছু শব্দ দিয়ে শুরু (context ধরে রাখে)
        Assert.Contains(firstWords[^1], secondWords);
    }
}
