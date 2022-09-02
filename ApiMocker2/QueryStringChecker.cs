using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIMocker
{
    public static class QueryStringChecker
    {
        public static bool Match(string pattern, string queryString)
        {
            pattern = pattern.StartsWith('?') ? pattern.Substring(1) : pattern;
            queryString = "/" + (queryString.StartsWith('?') ? queryString.Substring(1) : queryString);
            var patternTemplate = TemplateParser.Parse(pattern);
            var patternMatcher = new TemplateMatcher(patternTemplate, null);
            var values = new RouteValueDictionary();

            return patternMatcher.TryMatch(queryString, values);
        }
    }
}
