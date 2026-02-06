namespace Westwind.TypeImporter.Test
{
    [TestClass]
    public sealed class ImportLibraryTests
    {
        [TestInitialize]
        public void TypeParserTests()
        {
            // This method is called before each test method.
        }


        [TestMethod]
        public void ParseUtilitiesLibraryTest()
        {
            string assemblyFile = @"D:\projects\Westwind.Utilities\Westwind.Utilities\bin\Release\net10.0\Westwind.Utilities.dll";
            //string assemblyFile = @"D:\projects\Westwind.Utilities\Westwind.Utilities\bin\Release\net9.0\Westwind.Utilities.dll";
            //string assemblyFile = @"D:\projects\Westwind.Data.EfCore\Westwind.Data.EfCore\bin\Release\net9.0\Westwind.Data.EfCore.dll";
            var importer = new Westwind.TypeImporter.TypeParser();

            importer.AssemblyFilename = assemblyFile;
            importer.NoInheritedMembers = true;
            importer.ParseXmlDocumentation = true;

            var types = importer.GetAllTypes();

            foreach (var type in types)
            {
                Console.WriteLine(type.RawTypeName + " -  " + type.Signature + " - " + type.Syntax);
                foreach (var meth in type.Methods)
                {
                    Console.WriteLine(meth.Signature + "\n   " + meth.Syntax + "\n");
                }
            }
        }

        [TestMethod]
        public void ParseLibraryTest()
        {
            string assemblyFile = @"D:\projects\Libraries\Westwind.AI\Westwind.AI\bin\Release\.net10.0\Westwind.AI.dll";
            //string assemblyFile = @"D:\projects\Westwind.Utilities\Westwind.Utilities\bin\Release\net9.0\Westwind.Utilities.dll";
            //string assemblyFile = @"D:\projects\Westwind.Data.EfCore\Westwind.Data.EfCore\bin\Release\net9.0\Westwind.Data.EfCore.dll";
            var importer = new Westwind.TypeImporter.TypeParser();

            importer.AssemblyFilename = assemblyFile;

            importer.NoInheritedMembers = true;
            importer.ParseXmlDocumentation = true;

            var types = importer.GetAllTypes();
            
            foreach(var type in types)
            {
                Console.WriteLine(type.RawTypeName + " -  " + type.Signature + " - " + type.Syntax);
                foreach(var meth in type.Methods)
                {
                    Console.WriteLine(meth.Signature + "\n   " + meth.Syntax + "\n");
                }
            }
        }

        [TestMethod]
        public void ParseLibraryWithLotsOfGenericsTest()
        {
            string assemblyFile = @"D:\projects\Westwind.WebStore\Westwind.Webstore.Web\bin\Release\net9.0\Westwind.AspNetCore.dll";            
            var importer = new Westwind.TypeImporter.TypeParser();
            importer.AssemblyFilename = assemblyFile;
            importer.NoInheritedMembers = true;
            importer.ParseXmlDocumentation = true;

            var types = importer.GetAllTypes();

            foreach (var type in types)
            {
                Console.WriteLine(type.Name); // + " -  " + type.Signature + " - " + type.Syntax);

                ///if(type.Name.Contains("<"))
                Console.WriteLine(type.InheritanceTree);

                Console.WriteLine("---");
                //foreach (var meth in type.Methods)
                //{
                //    Console.WriteLine(meth.Signature + "\n   " + meth.Syntax + "\n");
                //}
            }
        }

        [TestMethod]
        public void ParseLibraryEnumsTest()
        {
            string assemblyFile = @"D:\projects\Libraries\Westwind.AI\Westwind.AI\bin\Release\.net9.0\Westwind.AI.dll";
            //string assemblyFile = @"D:\projects\Westwind.Utilities\Westwind.Utilities\bin\Release\net9.0\Westwind.Utilities.dll";
            //string assemblyFile = @"D:\projects\Westwind.Data.EfCore\Westwind.Data.EfCore\bin\Release\net9.0\Westwind.Data.EfCore.dll";
            var importer = new Westwind.TypeImporter.TypeParser();

            importer.AssemblyFilename = assemblyFile;

            importer.NoInheritedMembers = true;
            importer.ParseXmlDocumentation = true;

            var types = importer.GetAllTypes();

            foreach (var type in types.Where(t=> t.IsEnum ))
            {
                Console.WriteLine(type.RawTypeName + " -  " + type.Signature + " - " + type.Syntax);
                foreach (var prop in type.Properties)
                {
                    Console.WriteLine(" -- " + prop.Name);
                }
            }
        }
    }
}
