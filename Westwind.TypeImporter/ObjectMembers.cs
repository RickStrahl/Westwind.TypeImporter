using System;
using System.Collections.Generic;

namespace Westwind.TypeImporter
{
    public class ObjectMethod
    {
        public string Name { get; set; }

        public string Classname { get; set; }

        public string Parameters { get; set; }
        public string ReturnType { get; set; }
        public string ReturnDescription { get; set; }
        public string Scope { get; set; }

        public bool Static { get; set; }
        public bool Literal { get; set; }
        public bool Internal { get; set; }
        public bool IsConstructor { get; set; }
        public string Other { get; set; }

        //public string[] ParameterList = null;
        public List<MethodParameter> ParameterList { get; set; } = new List<MethodParameter>();

        // *** Parsed from XMLDocs
        public string HelpText { get; set; }

        public string HelpCategory { get; set; }

        public string Remarks { get; set; }

        public string Example { get; set; }
        public string Exceptions { get; set; }
        public string Contract { get; set; }
        public string SeeAlso { get; set; }

        public string Syntax;
        public string Signature { get; set; }

        public string DeclaringType { get; set; }

        public string GenericParameters { get; set; }
        public string RawParameters { get; set; }
        public string DescriptiveParameters { get; set; }

        public bool IsInherited { get; set; }
        


        public override string ToString()
        {
            if (string.IsNullOrEmpty(Syntax))
                return base.ToString();

            return Syntax ?? Name;
        }
    }

    public class ObjectProperty
    {
        public string Name { get; set; }

        public string Classname { get; set; }

        public string Type { get; set; }

        public string Scope { get; set; }
        public string AccessType { get; set; }
        public string DefaultValue { get; set; }

        public bool Static { get; set; }
        public bool ReadOnly { get; set; }
        public bool WriteOnly { get; set; }

        public bool Internal { get; set; }
        public bool Literal { get; set; }

        public string Other { get; set; }

        public string Syntax { get; set; }

        /// <summary>
        /// Property or Field
        /// </summary>
        public PropertyModes PropertyMode;

        /// <summary>
        /// Help Text parsed from XML docs or - if set via ParseDescriptionAttributes
        /// Important: Requires that you ship the XML doc file
        /// at runtime in order to parse XML docs
        /// </summary>
        public string HelpText { get; set; }
        
        /// <summary>
        /// Retrieved only if ParseDescriptionAttributes is set
        /// </summary>
        public string HelpCategory { get; set; }

        public string Remarks { get; set; }
        public string Example { get; set; }
        public string Contract { get; set; }
        public string SeeAlso { get; set; }

        public string Signature { get; set; }
        public string DeclaringType { get; set; }

        public bool IsInherited { get; set; }
        

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Syntax))
                return Syntax;

            return Name;
        }
    }


    public class ObjectEvent
    {
        public string Name;

        public string Classname { get; set; }

        public string Type { get; set; }

        public string Scope { get; set; }
        public bool Static { get; set; }
        public bool ReadOnly { get; set; }

        public string Other { get; set; }

        public string HelpText { get; set; }
        public string Remarks { get; set; }
        public string Example { get; set; }
        public string SeeAlso { get; set; }

        public string Signature { get; set; }
        public string RawSignature { get; set; }

        public string DeclaringType { get; set; }

        public string Syntax { get; set; }

        public bool IsInherited { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return base.ToString();

            return Syntax ?? Name;
        }
    }

    public class MethodParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ShortTypeName { get; set; }
        public string Other { get; set; }
    }

    public enum PropertyModes
    {
        Property,
        Field
    }
}
