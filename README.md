# Westwind.TypeImporter

This is a small library that uses Mono.Cecil and a custom XML Documentation parser to pull class documentation from .NET assemblies and types. It uses Mono.Cecil to allow consistent load of assemblies for various version of .NET as it's pulling only the metadata without loading types. The XML doc parser then post parses the XML documentation and retrieves Xml docs for objects and members.

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

foreach(var method in dotnetObject.Methods)
{
    Console.WriteLine(method.Syntax);
    Console.WriteLine(method.HelpText);
}
foreach9var prop in dotnetObject.Properties)
{
    Console.WriteLine(prop.Syntax);
    Console.WriteLine(prop.Syntax);
}
```