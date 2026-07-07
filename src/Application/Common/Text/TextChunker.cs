namespace AiPlatform.Application.Common.Text;

// বড় লেখা ছোট overlapping টুকরো করে (শব্দ-সীমানায় কাটে, overlap রাখে যেন context না ভাঙে)।
public static class TextChunker
{
    public static IReadOnlyList<string> Chunk(string text, int maxChars = 200, int overlapWords = 6)
    {
        var words = text.Replace("\r", " ").Replace("\n", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var result = new List<string>();
        var current = new List<string>();
        var length = 0;

        foreach (var word in words)
        {
            current.Add(word);
            length += word.Length + 1;
            if (length >= maxChars)
            {
                result.Add(string.Join(" ", current));
                current = current.Skip(Math.Max(0, current.Count - overlapWords)).ToList();
                length = current.Sum(w => w.Length + 1);
            }
        }

        if (current.Count > 0)
            result.Add(string.Join(" ", current));

        return result;
    }
}
