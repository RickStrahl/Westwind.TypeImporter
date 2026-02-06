using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Westwind.Utilities;

namespace Westwind.TypeImporter
{
    /// <summary>
    /// Class that handles importing of XML documentation to an existing
    /// DotnetObject instance
    /// </summary>
    public class XmlDocumentationParser
    {
        private readonly string XmlDocFile;

        public bool WordWrap { get; set; }
        public string ErrorMessage { get; set; }

        public XmlDocumentationParser(string xmlDocFile)
        {
            XmlDocFile = xmlDocFile;
        }

        /// <summary>
        /// This method parses comments from C# XML documentation into the appropriate
        /// cHelpText properties.
        /// </summary>
        /// <returns></returns>
        public bool ParseXmlProperties(DotnetObject dotnetObject)
        {
            string xPath;
            XmlDocument oDom = new XmlDocument();

            try
            {
                oDom.Load("file://" + XmlDocFile);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Unable to load XML Documentation file." + ex.Message;
                return false;
            }


            XmlNode Match = null;
            XmlNode SubMatch = null;

            try
            {
                Match = oDom.SelectSingleNode("/doc/members/member[@name='" +
                    "T:" + dotnetObject.Signature + "']");
                
                dotnetObject.HelpText = GetNodeValue(Match, "summary") ?? string.Empty;

                dotnetObject.Remarks = GetNodeValue(Match, "remarks");
                dotnetObject.Example = GetExampleCode(Match);

                var dotnetObjectRemarks = dotnetObject.Remarks.Trim();
                var dotnetObjectHelpText = dotnetObject.HelpText.Trim();


                dotnetObjectHelpText = dotnetObjectHelpText.Replace("\r", "");
                dotnetObjectHelpText = dotnetObjectHelpText.Replace("\n\n", "#$#$#$");
                dotnetObjectHelpText = dotnetObjectHelpText.Replace("\n", " ");
                dotnetObjectHelpText = dotnetObjectHelpText.Replace("#$#$#$", "\r\n\r\n");


                dotnetObject.SeeAlso = FixupSeeAlsoLinks(ref dotnetObjectHelpText) +
                                       FixupSeeAlsoLinks(ref dotnetObjectRemarks);
                dotnetObject.HelpText = dotnetObjectHelpText;
                dotnetObject.Remarks = dotnetObjectRemarks;

                dotnetObject.Contract = GetContracts(Match);

                Match = null;
            }
            catch (Exception ex)
            {
                string lcError = ex.Message;
            }

            // *** Loop through the methods
            foreach (var method in dotnetObject.AllMethods)
            {
                if (method.Name == "FlattenTree")
                {
                    Debugger.Break();
                }

                // *** Strip off empty method parens
                string methodSignature = method.Signature.Replace("()", "");

                // *** Replace Reference of Reflection for the one use by XML docs
                methodSignature = methodSignature.Replace("&", "@");

                if (method.RawParameters == "")
                    xPath = "/doc/members/member[@name='" +
                        "M:" + methodSignature + "']";  // .cNamespace + "." + loObject.cName + "." + loMethod.cName + "']";
                else
                    xPath = "/doc/members/member[@name='" +
                        "M:" + methodSignature + "']";

                //							"M:" + loObject.cNamespace + "." + loObject.cName + "." + loMethod.cName +
                //							"(" + loMethod.cRawParameters + ")']";


                try
                {
                    // M:Westwind.Utilities.Encryption.EncryptBytes(System.Byte[],System.String)
                    Match = oDom.SelectSingleNode(xPath);
                    if (Match == null)
                        continue;

                    method.HelpText = GetNodeValue(Match, "summary");

                   

                    method.Remarks = GetNodeValue(Match, "remarks");
                    method.Example = GetExampleCode(Match);
                    method.Contract = GetContracts(Match);

                    var methodHelpText = method.HelpText;
                    var methodRemarks = method.Remarks;
                    method.SeeAlso = FixupSeeAlsoLinks(ref methodHelpText) +
                                     FixupSeeAlsoLinks(ref methodRemarks);

                    method.HelpText = methodHelpText;
                    method.Remarks = methodRemarks;
                    //this.GetNodeValue(Match, "example");

                    // *** Only update returns if the return comment is actually set
                    // *** Otherwise leave the type intact
                    method.ReturnDescription = GetNodeValue(Match, "returns");

                    // *** Parse the parameters
                    string parmDescriptions = "";
                    foreach (var parm in method.ParameterList)
                    {
                        parmDescriptions = parmDescriptions +
                            "**" + parm.Name + "**  \r\n";

                        SubMatch = Match.SelectSingleNode("param[@name='" + parm.Name + "']");
                        if (SubMatch != null)
                        {
                            string value = SubMatch.InnerText;
                            if (value.Length > 0)
                            {
                                value = value.Trim(' ', '\t', '\r', '\n');
                                parmDescriptions = parmDescriptions + value + "\r\n\r\n";
                            }
                            else
                                // empty line
                                parmDescriptions = parmDescriptions + "\r\n";
                        }

                    }
                    method.DescriptiveParameters = parmDescriptions;

                    string Exceptions = "";
                    XmlNodeList ExceptionMatches = null;
                    try
                    {
                        ExceptionMatches = Match.SelectNodes("exception");
                    }
                    catch { }

                    if (ExceptionMatches != null)
                    {
                        foreach (XmlNode Node in ExceptionMatches)
                        {
                            XmlAttribute Ref = Node.Attributes["cref"];
                            if (Ref != null)
                                Exceptions += "**" + Ref.Value.Replace("T:", "").Replace("!:", "") + "**  \r\n";
                            if (!string.IsNullOrEmpty(Node.InnerText))
                                Exceptions += GetNodeValue(Node, "") + "\r\n";
                            Exceptions += "\r\n";
                        }
                        method.Exceptions = Exceptions.TrimEnd('\r', '\n');
                    }

                    Match = null;
                }
                catch (Exception ex)
                {
                    string lcError = ex.Message;
                }
            }  // for methods

            // Combine properties and fields into a single list
            var propAndFieldList = new List<ObjectProperty>();
            propAndFieldList.AddRange(dotnetObject.Properties);
            propAndFieldList.AddRange(dotnetObject.Fields);

            // *** Loop through the properties and fields in one pass so we can use the same logic
            foreach (var property in propAndFieldList)
            {
                string fieldPrefix = "P:";
                if (property.PropertyMode == PropertyModes.Field)
                    fieldPrefix = "F:";

                xPath = "/doc/members/member[@name='" + fieldPrefix +
                    property.Signature.Replace("()", "") + "']";

                Match = oDom.SelectSingleNode(xPath);

                property.HelpText = GetNodeValue(Match, "summary");
                property.Remarks = GetNodeValue(Match, "remarks");
                property.Example = GetExampleCode(Match);
                property.Contract = GetContracts(Match);

                var propertyHelpText = property.HelpText;
                var propertyRemarks = property.Remarks;
                property.SeeAlso = FixupSeeAlsoLinks(ref propertyHelpText) +
                                   FixupSeeAlsoLinks(ref propertyRemarks);
                property.HelpText = propertyHelpText;
                property.Remarks = propertyRemarks;

                string value = GetNodeValue(Match, "value");
                if (!string.IsNullOrEmpty(value))
                    property.DefaultValue = value;

                Match = null;

            }  // for properties

            // *** Loop through the fields
            foreach (var evt in dotnetObject.Events)
            {
                string lcFieldPrefix = "E:";

                xPath = "/doc/members/member[@name='" + lcFieldPrefix +
                    evt.Signature + "']";

                Match = oDom.SelectSingleNode(xPath);
                evt.HelpText = GetNodeValue(Match, "summary");
                evt.Remarks = GetNodeValue(Match, "remarks");
                evt.Example = GetExampleCode(Match);

                var evtHelpText = evt.HelpText;
                var evtRemarks = evt.Remarks;
                evt.SeeAlso = this.FixupSeeAlsoLinks(ref evtHelpText) +
                                   this.FixupSeeAlsoLinks(ref evtRemarks);
                evt.HelpText = evtHelpText;
                evt.Remarks = evtRemarks;

                Match = null;
            }  // for events

            return true;
        } // ParseXMLProperties (method)



        /// <summary>
        /// Retrieves the text from a child XML node
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="keyword">subnode XPATH expression. Pass empty string for current node</param>
        /// <returns></returns>
        protected string GetNodeValue(XmlNode node, string keyword)
        {
            return GetNodeValue(node, keyword, this.WordWrap);
        }



        /// <summary>
        /// Retrieves the text from a child XML node
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="keyword">subnode XPATH expression. Pass empty string for current node</param>
        /// <returns></returns>
        protected string GetNodeValue(XmlNode node, string keyword, bool wordWrap)
        {
            XmlNode subMatch = null;
            string result = "";

            if (node != null)
            {
                if (string.IsNullOrEmpty(keyword))
                    subMatch = node;
                else
                    subMatch = node.SelectSingleNode(keyword);

                if (subMatch != null)
                {
                    // *** Need to pull inner XML so we can get the sub XML strings ilke <seealso> etc.
                    var nodeContent = subMatch.InnerXml.Replace("\r\n", "\r");

                    // expand <see> and <seealso> tags
                    nodeContent = ParseSeeAndSeeAlso(nodeContent);

                    if (keyword == "example")
                        result = FixHelpString(nodeContent, false);
                    else
                        result = FixHelpString(nodeContent, wordWrap);


                    //result = FixupCrefInlineCode(result);
                    //result = FixupSeeLinks(result);

                    subMatch = null;

                    // *** Deal with embedded list types - suboptimal at best but...
                    if (result.IndexOf("<list type=\"table\"") > -1 && result.IndexOf("<description>") > -1)
                    {
                        // <list type="table">
                        // <item>
                        //		<term>test item</term>
                        //		<description>aDescription</description>
                        // </item>
                        // </list>
                        string list = StringUtils.ExtractString(result, "<list type=\"table\">", "</list>");

                        XmlDocument loDom = new XmlDocument();
                        try
                        {
                            loDom.LoadXml("<list>" + list + "</list>");
                            StringBuilder sbHtml = new StringBuilder();

                            sbHtml.Append("<RAWHTML>\r\n<table border=\"1\" cellpadding=\"3\" width=\"400\">\r\n");

                            XmlNode Header = loDom.DocumentElement.SelectSingleNode("listheader");
                            if (Header != null)
                            {
                                sbHtml.Append("<tr><th>" + Header.SelectSingleNode("term").InnerText);
                                sbHtml.Append("</td><th>" + Header.SelectSingleNode("description").InnerText);
                                sbHtml.Append("</th></tr>\r\n");
                            }
                            XmlNodeList nodeItems = loDom.DocumentElement.SelectNodes("item");
                            foreach (XmlNode Item in nodeItems)
                            {
                                sbHtml.Append("<tr><td>" + Item.SelectSingleNode("term").InnerText);
                                sbHtml.Append("</td><td>" + Item.SelectSingleNode("description").InnerText);
                                sbHtml.Append("</td></tr>\r\n");
                            }
                            sbHtml.Append("</table>\r\n</RAWHTML>\r\n");

                            string output = sbHtml.ToString();
                            int lnAt = result.IndexOf(list);

                            result = StringUtils.ExtractString(result, "<list type=\"table\">" + list + "</list>", output, true);
                        }
                        catch (Exception e) { string lcError = e.Message; }  // ignore errors - leave as is
                    }
                }
            }

            // *** Now un-markup what's left of the XML
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");

            result = result.TrimEnd([' ', '\t', '\r', '\n']);

            return result;
        }

        //protected string FixupCrefInlineCode(string result)
        //{
        //    if (string.IsNullOrEmpty(result))
        //        return result;

        //    while (result.IndexOf("<see ") > -1)
        //    {
        //        string lcSee = StringUtils.ExtractString(result, "<see ", "/>", true);
        //        if (lcSee != string.Empty)
        //        {
        //            string lcRef = StringUtils.ExtractString(lcSee, "cref=\"", "\"");
        //            string lcCaption = GetCrefCaption(lcRef);
        //            result = StringUtils.ExtractString(result, "<see " + lcSee + "/>", "`" + lcCaption + "`");
        //            continue;
        //        }

        //        lcSee = StringUtils.ExtractString(result, "<see ", "</see>", true);
        //        if (lcSee == string.Empty)
        //            break;

        //        string caption = lcSee.Substring(lcSee.IndexOf(">") + 1);
        //        if (string.IsNullOrWhiteSpace(caption))
        //        {
        //            string lcRef = StringUtils.ExtractString(lcSee, "cref=\"", "\"");
        //            caption = GetCrefCaption(lcRef);
        //        }

        //        result = StringUtils.ExtractString(result, "<see " + lcSee + "</see>", "`" + caption + "`");
        //    }

        //    return result;
        //}

        //private string GetCrefCaption(string cref)
        //{
        //    if (string.IsNullOrEmpty(cref))
        //        return string.Empty;

        //    string caption = cref;
        //    if (caption.IndexOf(":") == 1)
        //        caption = caption.Substring(2);

        //    int at = caption.IndexOf("(");
        //    if (at > -1)
        //        caption = caption.Substring(0, at);

        //    return caption;
        //}

        /// <summary>
        /// Returns the Example code snippet either as a single
        /// string or as a collection of code snippets from multiple
        /// code sub items.
        /// </summary>
        /// <param name="match">Pass in the member root node</param>
        /// <returns></returns>
        string GetExampleCode(XmlNode match)
        {
            string code = string.Empty;
            if (match == null)
                return code;

            // Check for <code lang="c#"> tags
            try
            {
                XmlNodeList codeNodes = match.SelectNodes("example/code");
                if (codeNodes.Count > 0)
                {
                    StringBuilder codeSb = new StringBuilder();

                    foreach (XmlNode codeNode in codeNodes)
                    {
                        string lang = codeNode.Attributes["lang"].Value as string;
                        if (lang != null)
                        {
                            codeSb.AppendLine("```" + lang);
                        }
                        string plainCode = FixHelpString(codeNode.InnerXml.Replace("\r\n", "\r"), false);
                        //this.GetNodeValue(codeNode, "", false) ?? string.Empty;

                        codeSb.AppendLine(plainCode.TrimEnd());

                        if (lang != null)
                            codeSb.AppendLine("```");
                    }
                    code = codeSb.ToString();
                    Debug.WriteLine(code);
                }
                else
                    code = GetNodeValue(match, "example");
            }
            catch
            {
                code = GetNodeValue(match, "example");
            }

            // *** Now un-markup what's left of the XML
            code = code.Replace("&lt;", "<").Replace("&gt;", ">");

            return code;
        }

        /// <summary>
        /// Return code contract information
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private string GetContracts(XmlNode match, bool isProperty = false)
        {
            if (match == null)
                return string.Empty;

            XmlNodeList nodes;

            StringBuilder sb = new StringBuilder(256);

            // 0 - not a property, 1 get, 2 set
            int getSet;

            var tokens = new string[] { "invariant", "ensures", "requires", "getter/requires", "setter/requires", "getter/ensures", "setter/ensures" };
            foreach (string token in tokens)
            {
                nodes = match.SelectNodes(token);
                if (nodes == null || nodes.Count < 1)
                    continue;

                if (token.StartsWith("getter"))
                    getSet = 1;
                else if (token.StartsWith("setter"))
                    getSet = 2;
                else
                    getSet = 0;

                string headerText = StringUtils.ProperCase(nodes[0].Name);
                if (headerText == "Invariant")
                    headerText = "Invariants";

                if (getSet == 1)
                    sb.AppendLine("<div class=\"contractgetset\">Get</div>");
                if (getSet == 2)
                    sb.AppendLine("<div class=\"contractgetset\">Set</div>");

                sb.AppendLine("<div class=\"contractitemheader\">" + headerText + "</div>");
                foreach (XmlNode node in nodes)
                {
                    string val = node.InnerText;
                    string desc = string.Empty;
                    string exception = string.Empty;
                    XmlAttribute att = node.Attributes["description"];
                    if (att != null)
                        desc = att.InnerText;

                    att = node.Attributes["exception"];
                    if (att != null)
                        exception = att.InnerText;

                    sb.AppendLine(val);
                    if (!string.IsNullOrEmpty(exception))
                        sb.AppendLine("*Exception: " + exception + "*");
                    if (!string.IsNullOrEmpty(desc))
                        sb.AppendLine("*Description: " + desc + "*");

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }




        //        /// <summary>
        //        /// Fixes up <see></see> links in various formats.
        //        /// </summary>
        //        /// <param name="result"></param>
        //        /// <returns></returns>
        //        protected string FixupSeeLinks(string result)
        //        {
        //            if (string.IsNullOrEmpty(result)) return result;

        //            int Mode = 0;
        //            while (result.IndexOf("<see") > -1)
        //            {
        //                string lcCaption = "";
        //                string lcRef = "";

        //                // *** <see>Class Test</see>
        //                string lcSee = StringUtils.ExtractString(result, "<seealso>", "</seealso>", true);
        //                if (lcSee != string.Empty)
        //                {
        //                    // plain tag
        //                    lcRef = lcSee;
        //                    lcCaption = lcSee;
        //                    Mode = 0;
        //                }
        //                else
        //                {
        //                    // *** Full CRef syntax:
        //                    lcSee = StringUtils.ExtractString(result, "<seealso ", "/>", true);
        //                    if (lcSee != string.Empty)
        //                    {
        //                        // <see cref="..." />
        //                        Mode = 2;
        //                        lcRef = StringUtils.ExtractString(lcSee, "cref=\"", "\"");
        //                        if (lcRef != string.Empty)
        //                        {
        //                            if (lcRef.IndexOf(":") == 1)
        //                                lcCaption = lcRef.Substring(2);
        //                            else
        //                                lcCaption = lcRef;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        lcSee = StringUtils.ExtractString(result, "<see ", "</see>", true);
        //                        if (lcSee == string.Empty)
        //                            break;

        //                        Mode = 1;
        //                        lcRef =StringUtils.ExtractString(lcSee, "cref=\"", "\"");
        //                        lcCaption = lcSee.Substring(lcSee.IndexOf(">") + 1);
        //                    }

        //                    if (lcCaption == "")
        //                    {
        //                        lcCaption = lcRef;
        //                        if (lcCaption.IndexOf(":") == 1)
        //                            lcCaption = lcCaption.Substring(2);

        //                        int At = lcCaption.IndexOf("(");
        //                        if (At > -1)
        //                            lcCaption = lcCaption.Substring(0, At);
        //                    }
        //                    if (lcRef == "")
        //                        lcRef = lcCaption; // try to find in Help File by name

        //#if VSNet_Addin
        //							// *** Check for .NET References -  point at MSDN
        //							if ( lcRef.StartsWith("System.") || lcRef.StartsWith("Microsoft.") )
        //								lcRef = "msdn:T:" + lcRef;
        //							else
        //							{
        //								lcRef = "sig:" + lcRef;
        //							}

        //#else
        //                    // *** Check for .NET References -  point at MSDN
        //                    if (lcRef.IndexOf("System.") == 2 || lcRef.IndexOf("Microsoft.") == 2)
        //                        lcRef = "msdn:" + lcRef;
        //                    else
        //                    {
        //                        // *** Nope must be internal links - look up in project.
        //                        // *** Strip of prefix and add sig: prefix
        //                        if (lcRef.IndexOf(":") > -1)
        //                        {
        //                            // *** Add method brackets on empty methods because that's
        //                            // *** that's how we are importing them
        //                            if (lcRef.StartsWith("M:") && lcRef.IndexOf("(") == -1)
        //                                lcRef += "()";

        //                            // *** Mark as a Signature link skipping over prefix
        //                            lcRef = "sig:" + lcRef.Substring(2);
        //                        }
        //                        else
        //                          lcRef = "sig:" + lcRef;
        //                    }
        //#endif
        //                }

        //                if (Mode == 0)
        //                    result = StringUtils.ReplaceString(result, "<seealso>" + lcSee + "</seealso>",
        //                        "[" + lcCaption + "](dm-title://" + UrlEncodeSimple(lcRef) + ")",true);
        //                else if (Mode == 1)
        //                    result = StringUtils.ReplaceString(result, "<seealso " + lcSee + "</seealso>",
        //                        "[" + lcCaption + "](dm-title://" + UrlEncodeSimple(lcRef) + ")", true);
        //                else if (Mode == 2)
        //                    result = StringUtils.ReplaceString(result, "<seealso " + lcSee + "/>",
        //                        "[" + lcCaption + "](dm-title://" + UrlEncodeSimple(lcRef) + ")",true);

        //            }

        //            return result;
        //        }

        /// <summary>
        /// Extracts seealso sections out of the XML into a CR delimited strings
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        protected string FixupSeeAlsoLinks(ref string body)
        {
            if (body.IndexOf("<seealso") < 0)
                return "";

            if (body.Contains("Remarks duplicated from summary"))
            {
                string x2 = "";
                x2 += body;
            }

            string result = "";
            string extract = "XX";

            while (extract != "")
            {
                extract = body.ExtractString("<seealso>", "</seealso>");
                if (extract != "")
                {
                    var insert = extract;
                    if (insert is not null && insert.Length > 2 && insert[1] == ':')
                        insert = insert.Substring(2);

                    result += insert + "\r\n";
                    body = body.Replace("<seealso>" + extract + "</seealso>", "").Trim();
                }
                else
                {
                    // *** retrieve  CREF
                    extract = body.ExtractString("<seealso cref=\"", ">", false, false, true);
                    if (extract != "")
                    {
                        // Extract just the class string
                        string cref = extract.ExtractString("cref=\"", "\"");
                        cref = FixupCRefUrl(cref);

                        // strip out type prefix (T:System.String)
                        string insertText = cref.ExtractString(":", "xx", false, true);

                        body = body.Replace(extract, "").Trim();
                        result += cref + "\r\n";
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cref">.NET Signature like T:NameSpace.Class or M:NameSpace.Class.Method(string,string)</param>
        /// <returns></returns>
        string FixupCRefUrl(string cref)
        {

            // *** Check for .NET References -  point at MSDN
            if (cref.IndexOf("System.") == 2 || cref.IndexOf("Microsoft.") == 2)
                cref = "msdn:" + cref;
            else
            {
                // *** Nope must be internal links - look up in project.
                // *** Strip of prefix and add sig: prefix
                if (cref.IndexOf(":") == 1)
                {
                    // *** Add method brackets on empty methods because that's
                    // *** that's how we are importing them
                    if (cref.StartsWith("M:") && cref.IndexOf("(") == -1)
                        cref += "()";

                    cref= cref.Substring(2);                    
                }
            }
            return cref;
        }

        static string UrlEncodeSimple(string url, bool encodeSlashes = false)
        {
            if (encodeSlashes)
                return System.Net.WebUtility.UrlEncode(url);

            return url
                .Replace("%", "%25")
                .Replace(" ", "%20")
                .Replace("&", "%26")
                .Replace("?", "%3F")
                .Replace("#", "%23")
                .Replace("\n", "%0A")
                .Replace("\r", "%0D");
        }

        protected string GetAttributeValue(XmlNode Node, string Keyword, string Attribute)
        {
            XmlNode loSubMatch = null;
            string lcResult = "";

            if (Node != null)
            {
                loSubMatch = Node.SelectSingleNode(Keyword);
                if (loSubMatch != null)
                    lcResult = FixHelpString(loSubMatch.InnerText, WordWrap);
            }

            return lcResult;
        }


        protected string GetHelpDetails()
        {
            return "";
        }


        /// <summary>
        /// Fixes up a description string by splitting it
        /// into separate lines on Char(13)'s.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        protected string FixHelpString(string inputString, bool wordWrap)
        {
            string[] strings = inputString.Split(new char[2] { '\r', '\n' });
            string output = "";

            //*** We need to strip padding that XML Help topics add
            //*** so we look at the first line of non-empty text and figure
            //*** out the padding applied, then skip over that many leading chars
            //*** for every line.
            int padding = -1;

            for (int x = 0; x < strings.Length; x++)
            {
                // *** Strip off leading spaces and the \n
                string currentString = strings[x].TrimStart(new char[2] { '\r', '\n' }).TrimEnd();

                // *** Padding is unset until we hit the first line
                if (padding == -1)
                {
                    int len = currentString.TrimStart().Length;
                    if (len != currentString.Length)
                        padding = currentString.Length - len;

                    if (padding == -1 && len > 0)
                        padding = 0;
                }

                // *** Don't do anything if padding is -1
                // *** line is empty and no content's been hit yet
                // *** IOW, skip over it
                if (padding == -1)
                {
                    // *** In some situations there's no padding on the first line
                    // *** Example: Starts with <para> tag. So just add it.
                    //     If empty don't append.
                    if (!string.IsNullOrEmpty(currentString))
                        output += currentString + "\r\n";

                    // if string is still -1 check for for non-empty
                    // in that case: no padding
                    if (padding == -1 && !string.IsNullOrEmpty(currentString))
                        padding = 0;
                }
                else
                {

                    if (currentString.Length > padding)
                        output += currentString.Substring(padding) + "\r\n";
                    else
                        // *** Empty line
                        output += "\r\n";
                }
            }

            output = output.Replace("<pre>", "<pre>");
            output = output.Replace("</pre>", "</pre>");

            output = output.Replace("<c>", "<span class=\"code\">");
            output = output.Replace("</c>", "</span>");

            output = output.Replace("<code>", "<pre>");
            output = output.Replace("</code>", "</pre>");

            // *** Pick out <pre> blocks and combine lines to wrap
            if (wordWrap)
            {
                ArrayList al = new ArrayList();

                // remove code blocks and insert back after formatting
                // to avoid encoding of code
                while (true)
                {
                    string Text = StringUtils.ExtractString(output, "<pre>", "</pre>");
                    if (Text == null || Text == "")
                        break;

                    output = output.Replace("<pre>" + Text + "</pre>", "@@!@@");
                    al.Add(Text);
                }

                // *** Try to fix up line feeds - keep only double breaks
                output = output.Replace("\r\n\r\n", "^^^^");
                output = output.Replace("\r\n", " ");
                output = output.Replace("^^^^", "\r\n\r\n");

                // *** Replace paragraph tags after the fact
                output = output.Replace("<para>", "\r\n");
                output = output.Replace("</para>", "\r\n");
                output = output.Replace("<para />", "\r\n");
                output = output.Replace("<para/>", "\r\n");

                foreach (string Text in al)
                {
                    output = StringUtils.ReplaceStringInstance(output, "@@!@@", "<pre>" + Text + "</pre>", 1, true);
                }
            }

            return output;
        }




        #region See and SeeAlso Parsing

        /// <summary>
        /// Parses the raw text of an XML documentation element (e.g., content from <summary> or <remarks>) 
        /// and expands <see> and <seealso> tags into Markdown links.
        /// 
        /// - If the tag has a cref/href and inner content, uses the inner content as link text.
        /// - If no inner content, uses a friendly version of the cref (stripping prefix like 'T:').
        /// - Links use 'dm-title://' prefix for non-HTTP cref values.
        /// - Other XML tags are stripped, preserving their inner text.
        /// - Falls back to original input on parse errors.
        /// </summary>
        /// <param name="input">The raw XML fragment text to parse.</param>
        /// <returns>The parsed text with expanded links in Markdown format.</returns>
        public static string ParseSeeAndSeeAlso(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                // Wrap in a dummy element to parse as XML fragment
                var dummy = XElement.Parse("<dummy>" + input + "</dummy>");
                return string.Concat(dummy.Nodes().Select(ProcessNode)).Trim();
            }
            catch (Exception)
            {
                // Fallback to original on parse failure (e.g., malformed XML)
                return input;
            }
        }



        private static string ProcessNode(XNode node)
        {
            if (node is XText text)
            {
                return text.Value;
            }

            if (node is XElement element)
            {
                if(element.Name.LocalName == "see")
                {
                    string name = element.Name.LocalName;
                    string cref = element.Attribute("cref")?.Value;
                    string href = element.Attribute("href")?.Value;
                    string attrib = cref ?? href ?? string.Empty;

                    // Get inner content recursively
                    string inner = string.Concat(element.Nodes().Select(ProcessNode)).Trim();

                    // Use inner as link text if present; else friendly cref
                    string linkText = string.IsNullOrEmpty(inner) ? GetFriendlyName(attrib) : inner;

                    if (string.IsNullOrEmpty(attrib))
                        attrib = linkText;

                    if (!string.IsNullOrEmpty(linkText) && linkText.StartsWithAny("M:", "T:", "P:", "E:", "N:"))
                        linkText = linkText.Substring(2);

                    // Determine link scheme
                    string link = attrib.StartsWith("http")
                        ? attrib
                        : $"dm-title://{UrlEncodeSimple(attrib)}";

                    return $"[{linkText}]({link})";
                }                
            }

            // unknown element            
            var res  = node.ToString();
            return res;
        }

        private static string GetFriendlyName(string cref)
        {

            if (string.IsNullOrEmpty(cref))
                return string.Empty;
            if (cref.StartsWith("http"))
                return cref;

            // Strip prefix like 'T:' or 'M:' if present
            int colonIndex = cref.IndexOf(':');
            if (colonIndex == 1)
            {
                return cref.Substring(colonIndex + 1);
            }

            return cref;
        }

        #endregion

    }

}


