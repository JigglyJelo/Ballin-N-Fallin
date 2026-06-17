using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
/// <summary>
/// A static utility class that loads banned words from local text files and compiles them into a high-performance Regular Expression.
/// Used to sanitize player names and chat messages.
/// </summary>
public static class WordFilter{
    // The bad word lists are gone from here! Only trolledWords remains.
    public static List<string> trolledWords;
    
    // Both regexes pre-compiled for maximum game performance
    private static Regex chatFilterRegex;
    private static Regex stripRegex = new Regex(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Loads banned substrings and full words from the game's Assets folder and compiles them into a single Regex.
    /// </summary>
    /// <remarks>
    /// Must be called at least once before <see cref="IsBadString"/> can be used.
    /// Automatically handles Godot's .remap file extension swapping in exported builds.
    /// </remarks>
    public static void Initialize(){
        // Check if regex is already built instead of checking the old lists
        if(chatFilterRegex != null) return; 
        
        trolledWords = new List<string>();
        const string PATH = "res://Assets/Text/";
        List<string> tempSubstrings = new List<string>();
        List<string> tempFullWords = new List<string>();
        
        string textPath = PATH + "Banned Substrings.txt";
        if(textPath.Contains(".remap")) textPath = textPath.Replace(".remap","");
        using var fileSub = FileAccess.Open(textPath,FileAccess.ModeFlags.Read);
        if(fileSub != null){
            while(!fileSub.EofReached()){
                string line = fileSub.GetLine();
                if(!string.IsNullOrWhiteSpace(line)) tempSubstrings.Add(line);
            }
        }
        
        textPath = PATH + "Punishment Names.txt";
        if(textPath.Contains(".remap")) textPath = textPath.Replace(".remap","");
        using var fileNames = FileAccess.Open(textPath,FileAccess.ModeFlags.Read);
        if(fileNames != null){
            while(!fileNames.EofReached()){
                string line = fileNames.GetLine();
                if(!string.IsNullOrWhiteSpace(line)) trolledWords.Add(line);
            }
        }
        
        textPath = PATH + "Banned Full Words.txt";
        if(textPath.Contains(".remap")) textPath = textPath.Replace(".remap","");
        using var fileWords = FileAccess.Open(textPath,FileAccess.ModeFlags.Read);
        if(fileWords != null){
            while(!fileWords.EofReached()){
                string line = fileWords.GetLine();
                if(!string.IsNullOrWhiteSpace(line)) tempFullWords.Add(line);
            }
        }

        List<string> regexPatterns = new List<string>();
        
        // Substrings search ANYWHERE in the stripped string (Applies ToLower and escapes directly)
        if(tempSubstrings.Count > 0){
            regexPatterns.Add(string.Join("|", tempSubstrings.Select(w => Regex.Escape(w.ToLower()))));
        }
        
        // Full Words require an EXACT match from start (^) to finish ($)
        if(tempFullWords.Count > 0){
            regexPatterns.Add(string.Join("|", tempFullWords.Select(w => "^" + Regex.Escape(w.ToLower()) + "$")));
        }

        if(regexPatterns.Count > 0){
            string finalPattern = string.Join("|", regexPatterns);
            chatFilterRegex = new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Evaluates a message against the compiled banned word list.
    /// </summary>
    /// <param name="message">The raw input string to check.</param>
    /// <param name="replacementText">The string used to replace stripped characters. Defaults to empty (squishing the string).</param>
    /// <returns>True if the string triggers the filter; otherwise, false.</returns>
    /// <remarks>
    /// Strips out all non-alphanumeric characters before checking to prevent players from bypassing the filter with symbols (e.g., 'b@d w0rd').
    /// </remarks>
    public static bool IsBadString(string message, string replacementText = ""){
        if(string.IsNullOrWhiteSpace(message)) return false;
        if(chatFilterRegex == null) return false;

        // Instantly strips out all spaces and symbols using the compiled regex
        string strippedMessage = stripRegex.Replace(message, replacementText);

        if(chatFilterRegex.IsMatch(strippedMessage)){
            return true;
        }
        return false;
    }
}