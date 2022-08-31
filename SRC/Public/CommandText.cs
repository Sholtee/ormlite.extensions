/********************************************************************************
* CommandText.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions
{
    /// <summary>
    /// Command text related stuffs.
    /// </summary>
    public static class CommandText
    {
        private static readonly Regex FFormatter = new Regex(@"\?|@\w+|{\d+}", RegexOptions.Compiled);

        /// <summary>
        /// Formats the given SQL template. Templates may contain positional (?), named (@Name), or indexed ({0}) placeholders. 
        /// </summary>
        [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
        public static string Format(string sql, params IDataParameter[] paramz)
        {
            if (sql is null) 
                throw new ArgumentNullException(nameof(sql));

            if (paramz is null)
                throw new ArgumentNullException(nameof(paramz));

            int index = 0;

            return FFormatter.Replace(sql, match =>
            {
                string placeholder = match.Value;

                IDataParameter matchingParameter;

                switch (placeholder[0])
                {
                    case '?':
                        if (index == paramz.Length) throw new IndexOutOfRangeException();
                        matchingParameter = paramz[index++];
                        break;
                    case '@':
                        matchingParameter = paramz.SingleOrDefault(p => 
                        {
                            string name = p.ParameterName;
                            if (!name.StartsWith("@", StringComparison.Ordinal)) name = $"@{name}";
                            return placeholder == name;
                        });
                        if (matchingParameter is null)
                            throw new KeyNotFoundException();
                        break;
                    case '{':
                        uint i = uint.Parse(placeholder.Trim('{', '}'), null);
                        if (i >= paramz.Length)
                            throw new IndexOutOfRangeException();
                        matchingParameter = paramz[i];
                        break;
                    default:
                        throw new NotSupportedException();
                }

                object value = matchingParameter.Value;

                return value is not null
                    ? OrmLiteConfig.DialectProvider.GetQuotedValue(value, value.GetType())
                    : "NULL";
            });
        }
    }
}
