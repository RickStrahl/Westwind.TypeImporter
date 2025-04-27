# Westwind.TypeImporter

**a internal small library to import .NET Assemblies and classes for creating References documentation**

This is a small helper library that parses .NET assemblies or individual .NET types into an easily traversible structure containing human readable content, that can be easily used for producing class reference documentation, for support features that need to integrate type information at runtime, or that want to utilize Xml documentation inside of running applications.

This library:

* Parses Assembly and Class Structures
* Extracts classes and members
* Cleans up type signatures
* Produces readable Syntax and Signature information
* Extracts XML documentation for help content

The obvious use case is to create class documentation for libraries and this library provides an easy way to get:

* A list of all types in an assembly
* Every type
* Every method with Parameters and Return Values
* Every property
* Every delegate/event

While you can do all this with Reflection the structure of this library makes this much easier to iterate and parse. Additionally the class information returned is parsed and cleaned up so it suitable for documentation.

Class and member information has:

* Cleaned up Syntax and Signature fields
* Generic types have been properly expanded
* Xml Documentation is retrieved (if available) during parsing
* Documentation text is properly word wrapped

## Use Cases

* Documentation Creation
* Runtime access to human readable type information
* Displaying help or tooltip information about settings from Xml Documentation

## Example

```cs
var typeParser = new Westwind.TypeImporter.TypeParser()
{
    ParseXmlDocumentation = true
};
var dotnetObject = typeParser.ParseObject(appConfigType);
if (dotnetObject == null)
{
    return null;
}

foreach(var method in dotnetObject.Methods.Where(m=> !m.IsConstructor))
{
    Console.WriteLine(method.Name);
    Console.WriteLine(method.Syntax);
    Console.WriteLine(method.HelpText);
    Console.WriteLine(method.DescriptiveParameters)
}
foreach(var prop in dotnetObject.Properties)
{
    Console.WriteLine(prop.Name);
    Console.WriteLine(prop.Syntax);
    Console.WriteLine(prop.Syntax);
}
```