using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

namespace APIMocker
{
  public static class ApiMocker
  {
    public class Macro 
    {
      public string RegexStr { get; set; }
      public string RegexHeaderStr { get; set; }
      public Func<string[], string> Run { get; set; }
    }

    private static Random random = new Random();
    private static ConcurrentDictionary<string, int> sequences = new ConcurrentDictionary<string, int>();

    private static Macro[] macros = {
      new Macro {
        RegexStr = @"\{\s*Random\s*\(\s*(?:[0-9]*|[0-9]\s*\,\s*[0-9]*)\s*\)\s*}",
        RegexHeaderStr = @"\{\s*Random\s*\(",
        Run = numbers => (numbers.Length == 2 && int.TryParse(numbers[0], out int minValue)
            && int.TryParse(numbers[1], out int maxValue) && (minValue < maxValue)) ? 
              random.Next(minValue, maxValue).ToString() :
              (
                (numbers.Length == 1 && int.TryParse(numbers[0], out maxValue)) ? 
                random.Next(maxValue).ToString() : random.Next().ToString()
              )
      },
      new Macro {
        RegexStr = @"\{\s*DateTime\s*\(\s*.*\s*\)\s*}",
        RegexHeaderStr = @"\{\s*DateTime\s*\(",
        Run = format => format.Length == 1 ?
          DateTime.Now.ToString(format[0]) :
          DateTime.Now.ToString()
      },
      new Macro {
        RegexStr = @"\{\s*Sequence\s*\(\s*.*\s*\)\s*}",
        RegexHeaderStr = @"\{\s*Sequence\s*\(",
        Run = seqKeyParams => {
          var key = seqKeyParams.Length == 0 ? "" : seqKeyParams[0];
          return sequences.AddOrUpdate(key, x => 1, (_, x) => x + 1).ToString();
        }
      }
    };

    public static string Replace(string responseBodyString, List<Replace> replaces)
    {
      const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
      char[] randomTailChars = {' ', ')', '}'};

      foreach (var replace in replaces)
      {
        if (!replace.Replacement.Contains("{"))
        {
          responseBodyString = Regex.Replace(responseBodyString, replace.Pattern, replace.Replacement);
          continue;
        }
        var macro = macros.FirstOrDefault(m => Regex.IsMatch(replace.Replacement, m.RegexStr, regexOptions));
        if (macro != null)
        {
          var randomRegex = macro.RegexStr;
          var replaceWith = replace.Replacement;
          if (Regex.IsMatch(replace.Replacement, randomRegex, regexOptions))
          {
            var numbersPart = Regex.Replace(replace.Replacement, macro.RegexHeaderStr, "").TrimEnd(randomTailChars);
            var numbers = numbersPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
            replaceWith = macro.Run(numbers);
          }
        responseBodyString = Regex.Replace(responseBodyString, replace.Pattern, replaceWith);
        }
      }
      
      return responseBodyString;
    }
  }
}