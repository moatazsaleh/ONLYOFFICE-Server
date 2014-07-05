/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

#region Copyright � 2001-2003 Jean-Claude Manoli [jc@manoli.net]
/*
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the author(s) be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *   1. The origin of this software must not be misrepresented; you must not
 *      claim that you wrote the original software. If you use this software
 *      in a product, an acknowledgment in the product documentation would be
 *      appreciated but is not required.
 * 
 *   2. Altered source versions must be plainly marked as such, and must not
 *      be misrepresented as being the original software.
 * 
 *   3. This notice may not be removed or altered from any source distribution.
 */
#endregion

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ASC.Web.Studio.Utility.HtmlUtility.CodeFormat
{
    /// <summary>
    ///	Provides a base implementation for all code formatters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To display the formatted code on your web site, the web page must 
    /// refer to a stylesheet that defines the formatting for the different 
    /// CSS classes generated by CSharpFormat:
    /// .codestyle, pre, .rem, .kwrd, .str, .op, .preproc, .alt, .lnum.
    /// </para>
    /// <para>
    /// Note that if you have multi-line comments in your source code
    /// (like /* ... */), the "line numbers" or "alternate line background" 
    /// options will generate code that is not strictly HTML 4.01 compliant. 
    /// The code will still look good with IE5+ or Mozilla 0.8+. 
    /// </para>
    /// </remarks>
    internal abstract class SourceFormat
    {
        /// <summary/>
        protected SourceFormat()
        {
            _tabSpaces = 4;
            LineNumbers = true;
            Alternate = false;
            _customProtectedTags = false;
        }

        private byte _tabSpaces;

        /// <summary>
        /// Gets or sets the tabs width.
        /// </summary>
        /// <value>The number of space characters to substitute for tab 
        /// characters. The default is <b>4</b>, unless overridden is a 
        /// derived class.</value>
        public byte TabSpaces
        {
            get { return _tabSpaces; }
            set { _tabSpaces = value; }
        }

        private bool _customProtectedTags;

        /// <summary>
        /// Enables or disables line numbers in output.
        /// </summary>
        /// <value>When <b>true</b>, line numbers are generated. 
        /// The default is <b>false</b>.</value>
        public bool CustomProtectedTags
        {
            get { return _customProtectedTags; }
            set { _customProtectedTags = value; }
        }

        /// <summary>
        /// Enables or disables line numbers in output.
        /// </summary>
        /// <value>When <b>true</b>, line numbers are generated. 
        /// The default is <b>false</b>.</value>
        public bool LineNumbers { get; set; }

        /// <summary>
        /// Enables or disables alternating line background.
        /// </summary>
        /// <value>When <b>true</b>, lines background is alternated. 
        /// The default is <b>false</b>.</value>
        public bool Alternate { get; set; }

        /// <overloads>Transform source code to HTML 4.01.</overloads>
        /// 
        /// <summary>
        /// Transforms a source code stream to HTML 4.01.
        /// </summary>
        /// <param name="source">Source code stream.</param>
        /// <returns>A string containing the HTML formatted code.</returns>
        public string FormatCode(Stream source)
        {
            var reader = new StreamReader(source);
            var s = reader.ReadToEnd();
            reader.Close();
            return FormatCode(s, false, _customProtectedTags);
        }

        /// <summary>
        /// Transforms a source code string to HTML 4.01.
        /// </summary>
        /// <returns>A string containing the HTML formatted code.</returns>
        public string FormatCode(string source)
        {
            return FormatCode(source, false, _customProtectedTags);
        }

        /// <summary>
        /// Allows formatting a part of the code in a different language,
        /// for example a JavaScript block inside an HTML file.
        /// </summary>
        public string FormatSubCode(string source)
        {
            return FormatCode(source, true, _customProtectedTags);
        }

        private Regex _codeRegex;

        /// <summary>
        /// The regular expression used to capture language tokens.
        /// </summary>
        protected Regex CodeRegex
        {
            get { return _codeRegex; }
            set { _codeRegex = value; }
        }

        private static readonly Regex SpaceReg = new Regex(@"(^\s*)|(\s*$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Called to evaluate the HTML fragment corresponding to each 
        /// matching token in the code.
        /// </summary>
        /// <param name="match">The <see cref="Match"/> resulting from a 
        /// single regular expression match.</param>
        /// <returns>A string containing the HTML code fragment.</returns>
        protected abstract string MatchEval(Match match);

        private static string GetLines(string source)
        {
            var result = string.Empty;
            var i = 1;
            foreach (var ss in source.Split('\n'))
            {
                result += string.Format("{0}:\n", i++);
            }

            result = string.Format("<pre>{0}</pre>", result);

            return result;
        }

        //does the formatting job
        private string FormatCode(string source, bool subCode, bool customProtect)
        {
            //replace special characters
            var sb = new StringBuilder(source);

            if (!subCode)
            {
                if (!customProtect)
                {
                    sb.Replace("&", "&amp;");
                    sb.Replace("<", "&lt;");
                    sb.Replace(">", "&gt;");
                }
                sb.Replace("\t", string.Empty.PadRight(_tabSpaces));
            }

            //color the code
            source = _codeRegex.Replace(sb.ToString(), MatchEval);

            sb = new StringBuilder();

            source = SpaceReg.Replace(source, "");

            if (!subCode)
            {
                sb.Append("<div class=\"codestyle\"><table cellspacing=\"0\"  cellpadding=\"0\" ><tr>\n");
                if (LineNumbers)
                {
                    sb.AppendFormat("<td class=\"lines\"  valign=\"top\">{0}</td>", GetLines(source));
                }
            }

            var reader = new StringReader(source);
            string line;

            if (!subCode)
            {
                sb.Append("<td class=\"tdcode\" valign=\"top\"><pre>");
            }
            while ((line = reader.ReadLine()) != null)
            {
                sb.AppendLine(line.Length == 0 ? "&nbsp;" : line);
            }
            if (!subCode)
            {
                sb.Append("\n&nbsp;</pre></td>");
            }
            reader.Close();

            if (!subCode)
            {
                sb.Append("</tr></table></div>");
            }

            return sb.ToString();
        }
    }
}