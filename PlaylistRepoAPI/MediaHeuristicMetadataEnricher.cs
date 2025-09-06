using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PlaylistRepoAPI;

/// <summary>
/// Infer contents of contents based on common YT playlist naming schemes.
/// </summary>
public class MediaHeuristicMetadataEnricher : IMetadataEnricher
{
	public Task<MediaDTO?> TryEnrich(MediaDTO media)
	{
		if (Try_Provided(media, out MediaDTO? modified)) goto exit;
		if (Try_SoundtrackParenthesis(media, out modified)) goto exit;
		if (Try_Soundtrack(media, out modified)) goto exit;
		if (Try_FromKeyword(media, out modified)) goto exit;
		if (Try_MusicKeyword(media, out modified)) goto exit;
		exit: return Task.FromResult(modified);
	}

	#region utils
	/// <summary>
	/// Common seperator characters
	/// </summary>
	public static readonly char[] SEPERATORS = ['-', '\u2012', '\u2013', '\u2014', '\u2015', '|', '·', '\uFF02', '\u0022', '\u201C', '\u201D', '\u201E', '\u201F'];

	/// <summary>
	/// Common quote characters
	/// </summary>
	public static readonly char[] QUOTES = ['\u0022', '\u00AB', '\u00BB', '\u201C', '\u201D', '\u201E', '\uFF02'];

	/// <summary>
	/// Characters used with trim to clean up metadata
	/// </summary>
	public static readonly char[] CLEAN_UP_TRIM = [' ', '\n', '\t', '\r', '.', ':', '\uFF1A', '-', '\u2012', '\u2013', '\u2014', '\u2015', '|', '·', '\uFF02', '\u0022', '\u201C', '\u201D', '\u201E', '\u201F'];

	/// <summary>
	/// Prepositions that are usually not at the end of a title
	/// </summary>
	public static readonly string[] PREPOSITIONS = [
		"at", "by", "during", "for", "from", "in", "onto", "of",
			"to", "with", "than", "through", "throughout", "towards"
	];

	/// <summary>
	/// Split a sentence by only the first divider. If there is no split, second half will be "".
	/// </summary>
	/// <param name="sentence">Sentence to split</param>
	/// <returns>(first half, second half)</returns>
	public static (string, string) SplitFirst(string sentence)
	{
		for (int i = 0; i < sentence.Length; i++)
		{
			char c = sentence[i];
			if (SEPERATORS.Contains(c))
			{
				string first = sentence[..i].Trim();
				string last = sentence[(i + 1)..].Trim();

				return (first, last);
			}
		}

		return (sentence.Trim(), "");
	}

	/// <summary>
	/// Create an array with lines seperated out from a description
	/// </summary>
	/// <param name="description"></param>
	/// <returns></returns>
	public static string[] LineifyDescription(string description)
	{
		return description.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
	}

	/// <summary>
	/// Check for if word is inside sentence. Insures that word is isolated with only whitespace, an edge, or symbols surounding it.
	/// </summary>
	/// <param name="word">Word to check</param>
	/// <param name="sentence">Sentence to check for word</param>
	/// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
	/// <returns></returns>
	public static bool IsStandaloneWord(string word, string sentence, out string usedWord)
	{
		int index = sentence.IndexOf(word, StringComparison.InvariantCultureIgnoreCase);
		usedWord = word;

		if (index == -1)
		{
			return false;
		}

		if (index != 0 && char.IsLetterOrDigit(sentence[index - 1]))
		{
			return false;
		}

		if (index + word.Length < sentence.Length - 1 && char.IsLetterOrDigit(sentence[index + word.Length]))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Calls the 'contains' method on sentence with InvariantCultureIgnoreCase. In same format as IsStandaloneWord.
	/// </summary>
	/// <param name="word">Word to check</param>
	/// <param name="sentence">Sentence to check for word</param>
	/// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
	/// <returns></returns>
	public static bool IsWord(string word, string sentence, out string usedWord)
	{
		usedWord = word;

		if (sentence.Contains(word, StringComparison.InvariantCultureIgnoreCase))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Calls the 'starts with' method on sentence with InvariantCultureIgnoreCase. In same format as IsStandaloneWord.
	/// </summary>
	/// <param name="word">Word to check</param>
	/// <param name="sentence">Sentence to check for word</param>
	/// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
	/// <returns></returns>
	public static bool StartsWithStandaloneWord(string word, string sentence, out string usedWord)
	{
		usedWord = word;

		if (!sentence.StartsWith(word, StringComparison.InvariantCultureIgnoreCase))
		{
			return false;
		}

		if (word.Length < sentence.Length - 1 && char.IsLetterOrDigit(sentence[word.Length]))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Return a piece of a string starting at the specified character
	/// </summary>
	/// <param name="sentence">String to split</param>
	/// <param name="target">Character to start at</param>
	/// <param name="hit">Index of hit character</param>
	/// <returns></returns>
	public static string CutLeftToChar(string sentence, char target, out int hit)
	{
		sentence = sentence.Trim();

		for (int i = sentence.Length - 1; i >= 0; i--)
		{
			if (sentence[i] == target)
			{
				hit = i;
				return sentence[(i + 1)..];
			}
		}

		hit = -1;
		return sentence;
	}

	/// <summary>
	/// Is a string a container for a number?
	/// </summary>
	/// <param name="section">String to check</param>
	/// <returns>True if number body</returns>
	public static bool IsNumberBody(string section)
	{
		section = section.Trim('(', ')');
		section = section.TrimStart('#');
		section = section.Trim(CLEAN_UP_TRIM);

		return IsNumber(section);
	}

	/// <summary>
	/// Checks to see if a string is entirely digits
	/// </summary>
	/// <param name="section"></param>
	/// <returns></returns>
	public static bool IsNumber(string section)
	{
		return section.All(char.IsDigit);
	}

	public static bool Contains(string[] span, string word)
	{
		foreach (string s in span)
		{
			if (s.Equals(word, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static string ProcessTitle(string bit, string album)
	{
		bit = bit.Trim();
		var words = bit.Split(' ', '\t', '\r');

		if (StartsWithStandaloneWord("track", bit, out _) ||
			StartsWithStandaloneWord("song", bit, out _) ||
			StartsWithStandaloneWord("part", bit, out _))
		{

			if (words.Length > 1 && IsNumberBody(words[1]))
			{
				return $"{bit} - {album}";
			}
		}

		bit = bit.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

		if (words.Length >= 2 && IsNumberBody(words[^1]) && !Contains(PREPOSITIONS, words[^2]))
		{
			bit = bit.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
		}

		bit = bit.TrimEnd('(');
		bit = bit.TrimStart(')');


		return bit;
	}
	#endregion

	/// <summary>
	/// Provided to youtube in description
	/// </summary>
	private static bool Try_Provided(MediaDTO media, [NotNullWhen(true)] out MediaDTO? modified)
	{
		modified = media.Clone();

		if (media.Description.Contains("Provided to YouTube by"))
		{
			string[] lines = LineifyDescription(media.Description);
			Debug.Assert(lines[0].Contains("Provided to YouTube by"));

			string[] performers = lines[1].Split('·')[1..];
			for (int i = 0; i < performers.Length; i++)
			{
				performers[i] = performers[i].Trim();
			}

			modified.Artists = performers;
			modified.Album = lines[2].Trim();

			return true;
		}

		return false;
	}

	/// <summary>
	/// Soundtrack indication inside of parenthesis
	/// </summary>
	private static bool Try_SoundtrackParenthesis(MediaDTO media, [NotNullWhen(true)] out MediaDTO? modified)
	{
		modified = media.Clone();
		if (IsStandaloneWord("OST", media.Title, out string usedWord) || IsStandaloneWord("O.S.T.", media.Title, out usedWord) || IsStandaloneWord("Soundtrack", media.Title, out usedWord))
		{
			int index = media.Title.IndexOf(usedWord, StringComparison.InvariantCultureIgnoreCase);
			if (index < 0 || index + usedWord.Length >= media.Title.Length || media.Title[index + usedWord.Length] != ')')
			{
				return false;
			}

			string album = CutLeftToChar(media.Title[..index], '(', out int left);
			if (usedWord == "O.S.T.")
			{
				album = album.Replace("O.S.T.", "OST");
			}

			string a = "";

			foreach (string word in album.Split(' '))
			{
				string trimmedWord = word.Trim(CLEAN_UP_TRIM);

				if (trimmedWord.Equals("OST", StringComparison.InvariantCultureIgnoreCase) ||
					trimmedWord.Equals("Soundtrack", StringComparison.InvariantCultureIgnoreCase))
				{
					break;
				}

				a += word + " ";
			}

			album = a + "Soundtrack";
			string title = media.Title[..left].Trim(CLEAN_UP_TRIM);
			modified.Album = album;
			modified.Title = title;

			return true;
		}

		return false;
	}

	/// <summary>
	/// General soundtrack indication
	/// </summary>
	private static bool Try_Soundtrack(MediaDTO media, [NotNullWhen(true)] out MediaDTO? modified)
	{
		modified = media.Clone();
		if (IsStandaloneWord("OST", media.Title, out string usedWord) || IsStandaloneWord("O.S.T", media.Title, out usedWord) || IsStandaloneWord("Soundtrack", media.Title, out usedWord))
		{
			var bits = media.Title.Split(SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

			int i;
			for (i = 0; i < bits.Length; i++)
			{
				if (IsStandaloneWord(usedWord, bits[i], out _)) break;
			}

			int soundtrackIndex = i;
			if (i < bits.Length)
			{
				string album = bits[i].Trim().Trim(CLEAN_UP_TRIM);
				int blacklist = -1;

				if (album == usedWord)
				{
					i--;
					if (i < 0)
					{
						return false;
					}

					blacklist = i;

					album = bits[i].Trim();
					if (album.Length == 0)
					{
						return false;
					}
				}

				if (usedWord.Equals("O.S.T", StringComparison.InvariantCultureIgnoreCase))
				{
					album = album.Replace("O.S.T", "OST", StringComparison.InvariantCultureIgnoreCase);
				}

				string a = "";
				foreach (string word in album.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
				{
					string trimmedWord = word.Trim(CLEAN_UP_TRIM);
					if (trimmedWord.Equals("OST", StringComparison.InvariantCultureIgnoreCase) ||
						trimmedWord.Equals("Soundtrack", StringComparison.InvariantCultureIgnoreCase))
					{
						break;
					}

					a += word + " ";
				}

				modified.Album = a + "Soundtrack";
				i++;
				string? title = null;
				for (; i < bits.Length; i++)
				{
					if (i == blacklist) continue;
					string bit = ProcessTitle(bits[i], modified.Album);
					if (string.IsNullOrWhiteSpace(bit) || IsNumberBody(bit)) continue;
					title = bit;
					break;
				}
				i = soundtrackIndex - 1;
				if (title == null)
				{
					for (; i >= 0; i--)
					{
						if (i == blacklist) continue;
						string bit = ProcessTitle(bits[i], modified.Album);
						if (string.IsNullOrWhiteSpace(bit) || IsNumberBody(bit)) continue;
						title = bit;
						break;
					}
				}

				if (title == null) return false;
				modified.Title = title.Trim(CLEAN_UP_TRIM);
			}

			return true;
		}

		return false;
	}

	private static bool Try_FromKeyword(MediaDTO media, [NotNullWhen(true)] out MediaDTO? modified)
	{
		modified = media.Clone();
		if (IsStandaloneWord("From", media.Title, out string usedWord))
		{
			if (!media.Title.Contains($"({usedWord}", StringComparison.InvariantCultureIgnoreCase)) return false;

			string[] bits = media.Title.Split("(", StringSplitOptions.TrimEntries);
			string title = bits[0].Trim();
			string album = bits[1][usedWord.Length..^1];

			foreach (char q in QUOTES)
			{
				album = album.Replace(q.ToString(), "");
			}

			album = album.Trim();
			modified.Title = title;
			modified.Album = album;
			return true;
		}

		return false;
	}

	private static bool Try_MusicKeyword(MediaDTO media, [NotNullWhen(true)] out MediaDTO? modified)
	{
		modified = media.Clone();
		if (IsWord("Music ", media.Description, out string usedWord) || IsWord("Title ", media.Description, out usedWord))
		{
			var lines = LineifyDescription(media.Description).AsEnumerable();
			var possibleLines = lines.Where(l => l.StartsWith(usedWord, StringComparison.InvariantCultureIgnoreCase));
			if (!possibleLines.Any())
			{
				throw new FormatException("Not real 'music keyword'");
			}

			string titleLine = possibleLines.First();
			titleLine = titleLine[usedWord.Length..];
			(string title, string album) = SplitFirst(titleLine);

			title = title.Trim(CLEAN_UP_TRIM);
			album = album.Trim(CLEAN_UP_TRIM);
			if (!media.Title.Contains(title))
			{
				throw new FormatException("Not real 'music keyword'");
			}

			modified.Title = title;
			modified.Album = string.IsNullOrWhiteSpace(album) ? "" : album;
			return true;
		}

		return false;
	}
}
