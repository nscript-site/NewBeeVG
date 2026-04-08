using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NewBeeVG.Internal;

public class TTSScriptParser
{
    public static string BuildCode(List<NBTTSClip> clips)
    {
        StringBuilder sb = new StringBuilder();
        var sbNames = new StringBuilder();
        foreach(var clip in clips)
        {
            var text = clip.Text;
            var clipName = $"tts_{clip.Name}";
            sb.AppendLine($"var {clipName} = ttsClip(@\"{text}\", voice: \"{clip.Voice}\", instructions: \"{clip.Instructions}\");");
            if (sbNames.Length > 0) sbNames.Append(",");
            sbNames.Append(clipName);
        }
        sb.AppendLine($"run(stage(bg: Brushes.Black), [{sbNames.ToString()}]);");
        return sb.ToString();
    }

    public static string BuildCode(string markdownFilePath)
    {
        return BuildCode(Parse(markdownFilePath));
    }

    /// <summary>
    /// Parse markdown TTS script into a list of NBTTSClip following project rules:
    /// - Lines that are headings (start with '#') are ignored.
    /// - Voice control directive lines: [@Voice] or [@Voice(Instructions)] set the current voice/instructions (not spoken).
    /// - Clip control directive lines: [#Name] set the current clip name; text from that line until next clip directive is the clip content.
    /// - If a new voice directive appears it replaces the previous voice/instructions for subsequent clips.
    /// </summary>
    public static List<NBTTSClip> Parse(string markdownFilePath)
    {
        if (string.IsNullOrWhiteSpace(markdownFilePath))
            throw new ArgumentNullException(nameof(markdownFilePath));
        if (!File.Exists(markdownFilePath))
            throw new FileNotFoundException("Markdown file not found.", markdownFilePath);

        var voiceRegex = new Regex(@"^\s*\[@([^\]\(]+)(?:\(([^)]*)\))?\]\s*$", RegexOptions.Compiled);
        var clipRegex = new Regex(@"^\s*\[#([^\]]+)\]\s*$", RegexOptions.Compiled);

        var result = new List<NBTTSClip>();
        var bufferLines = new List<string>();

        // defaults
        string currentVoice = QWenVoices.Male_EldricSage;
        string currentInstructions = string.Empty;
        string? currentClipName = null;

        var lines = File.ReadAllLines(markdownFilePath);
        foreach (var raw in lines)
        {
            var line = raw ?? string.Empty;

            // ignore markdown headings
            if (line.TrimStart().StartsWith("#"))
                continue;

            var vm = voiceRegex.Match(line);
            if (vm.Success)
            {
                // finalize nothing; set voice/instructions for subsequent content
                currentVoice = vm.Groups[1].Value.Trim();
                currentInstructions = vm.Groups.Count >= 3 ? (vm.Groups[2].Value ?? string.Empty).Trim() : string.Empty;
                continue;
            }

            var cm = clipRegex.Match(line);
            if (cm.Success)
            {
                // finalize previous clip if buffer has content
                if (bufferLines.Count > 0)
                {
                    var text = NormalizeText(string.Join("\n", bufferLines));
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var name = string.IsNullOrWhiteSpace(currentClipName) ? $"clip{result.Count + 1}" : currentClipName!;
                        var clip = new NBTTSClip(text, currentVoice, "Chinese", currentInstructions, "qwen3-tts-instruct-flash", name, null);
                        result.Add(clip);
                    }
                    bufferLines.Clear();
                }

                currentClipName = cm.Groups[1].Value.Trim();
                continue;
            }

            // normal content line -> accumulate
            bufferLines.Add(line);
        }

        // finalize last buffered clip
        if (bufferLines.Count > 0)
        {
            var text = NormalizeText(string.Join("\n", bufferLines));
            if (!string.IsNullOrWhiteSpace(text))
            {
                var name = string.IsNullOrWhiteSpace(currentClipName) ? $"clip{result.Count + 1}" : currentClipName!;
                var clip = new NBTTSClip(text, currentVoice, "Chinese", currentInstructions, "qwen3-tts-instruct-flash", name, null);
                result.Add(clip);
            }
            bufferLines.Clear();
        }

        return result;
    }


    private static string NormalizeText(string s)
    {
        if (s == null) return string.Empty;
        var lines = s.Replace("\r\n", "\n").Split('\n');
        int start = 0, end = lines.Length - 1;
        while (start <= end && string.IsNullOrWhiteSpace(lines[start])) start++;
        while (end >= start && string.IsNullOrWhiteSpace(lines[end])) end--;
        if (start > end) return string.Empty;
        var selected = new ArraySegment<string>(lines, start, end - start + 1);
        var str = string.Join("", (ReadOnlySpan<string>)selected);
        return str.Trim();
    }
}
