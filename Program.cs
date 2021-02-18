using System;
using System.IO;
using System.Linq;
using ResourceCompiler.Compilers;
using Resources;

namespace ResourceCompiler
{
    class Program
    {
        private static readonly string Version = "0.2.0.0";
        private string InputDirectory = Directory.GetCurrentDirectory();
        private string OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../Compilation");
        private string TablePath = Path.Combine(Directory.GetCurrentDirectory(), "../IDTable.txt");

        private void ShowHelp()
        {
            Console.WriteLine($"Resource compiler V{Version}.");
            Console.WriteLine("    -i <path>     (optional) specify input directory.");
            Console.WriteLine("    -o <path>     (optional) specify output directory.");
            Console.WriteLine("    -t <path>     (optional) specify id table file.");
        }
        public Program(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-help":
                        ShowHelp();
                        break;

                    case "-i":
                        if (i >= args.Length - 1)
                            throw new Exception($"-i requires a directory to be specified.");
                        InputDirectory = args[++i];
                        if (!Directory.Exists(InputDirectory))
                            throw new Exception($"Specified directory \"{InputDirectory}\" does not exists.");
                        break;

                    case "-o":
                        if (i >= args.Length - 1)
                            throw new Exception($"-o requires a directory to be specified.");
                        OutputDirectory = args[++i];
                        Directory.CreateDirectory(OutputDirectory);
                        break;

                    case "-t":
                        if (i >= args.Length - 1)
                            throw new Exception($"-t requires a file to be specified.");
                        TablePath = args[++i];
                        break;

                    default: throw new Exception($"Invalid flag \"{args[i]}\".");
                }
            }
        }

        private IDTable Table = null;
        private void Init()
        {
            Table = new IDTable();

            if (File.Exists(TablePath))
            {
                Console.WriteLine("Reading ID table...");
                using (var file = File.OpenText(TablePath))
                    Table.Read(file);

                foreach (var items in Table.Categories)
                {
                    string name = items.Key;
                    int loaded = items.Value.Count;
                    Console.WriteLine($"{name} loaded: {loaded}");
                }
                Console.WriteLine("Reading ID table done.");
            }
            else Console.WriteLine("ID table not found.");
            Console.WriteLine();

            Console.WriteLine("Indexing files...");
            string[] paths = Directory.GetFiles(InputDirectory, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++) paths[i] = 
                    Utilities.MakeDirectoryRelated(InputDirectory, paths[i]).Replace('\\', '/');
            Console.WriteLine("Files count: " + paths.Length);
            Console.WriteLine("Indexing files done.");
            Console.WriteLine("");
            
            Console.WriteLine("Searching resources...");
            int errors = 0;
            foreach (var path in paths)
            {
                try
                {
                    var type = Resource.GetType(Path.Combine(InputDirectory, path));
                    Table.Add(type.ToString(), path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing resource \"{path}\":");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine();
                    errors++;
                }
            }
            if (errors > 0) Console.WriteLine($"Errors: {errors}");
            foreach (var items in Table.Categories)
            {
                string name = items.Key;
                int found = items.Value.Count;
                Console.WriteLine($"{name} count: {found}");
            }
            Console.WriteLine($"Total resources count: {Table.Count}");
            Console.WriteLine("Searching resources done.");
            Console.WriteLine("");
        }
        private void Compile()
        {
            Console.WriteLine("Compiling resources...");
            using (var sprites_texture = new TextureCompiler(Path.Combine(OutputDirectory, "SpritesTexture")))
            using (var tiles_texture = new TextureCompiler(Path.Combine(OutputDirectory, "TilesTexture")))
            using (var interfaces_texture = new TextureCompiler(Path.Combine(OutputDirectory, "InterfacesTexture")))
            {
                foreach (var cat in Table.Categories)
                {
                    var type = Enum.Parse<ResourceType>(cat.Key);
                    string output = "";
                    Compiler compiler = null;
                    switch (type)
                    {
                        case ResourceType.Entity:
                            compiler = new EntityCompiler();
                            compiler.Texture = sprites_texture;
                            output = Path.Combine(OutputDirectory, "Entities");
                            break;
                        case ResourceType.Event:
                            compiler = new EventCompiler();
                            compiler.Texture = sprites_texture;
                            output = Path.Combine(OutputDirectory, "Events");
                            break;
                        case ResourceType.Interface:
                            compiler = new InterfaceCompiler();
                            compiler.Texture = interfaces_texture;
                            output = Path.Combine(OutputDirectory, "Interfaces");
                            break;
                        //case ResourceType.Item: 
                        //    compiler = new ItemCompiler(); 
                        //    break;
                        case ResourceType.Tile:
                            compiler = new TileCompiler();
                            compiler.Texture = tiles_texture;
                            output = Path.Combine(OutputDirectory, "Tiles");
                            break;
                    }

                    if (compiler != null)
                    {
                        using (compiler.Writer = new BinaryWriter(File.OpenWrite(output)))
                        {
                            compiler.RootDirectory = InputDirectory;
                            compiler.Table = Table;

                            compiler.Writer.Write(Table.GetLastID(cat.Key) + 1);

                            int id = 0;
                            foreach (var item in cat.Value)
                            {
                                while (id < item.ID)
                                {
                                    compiler.Placeholder();
                                    id++;
                                }
                                compiler.Compile(item.Path);
                                id++;
                            }
                        }
                    }
                    else Console.WriteLine($"Warning: Unsupported resource type [{type}].");
                    //foreach (var v in cat.Value)
                    //    Console.WriteLine(v.Path);
                }
            }
            Console.WriteLine("Compilation done.");
            Console.WriteLine();
        }
        private void Finish()
        {
            Console.WriteLine("Updating ID table...");
            using (var file = File.CreateText(TablePath))
                Table.Write(file, new string[] { "Tile", "Event", "Entity", "Outfit", "Item" });
            Console.WriteLine("Update done.");
        }
        public void Run()
        {
            Init();
            Compile();
            Finish();
        }

        static void Main(string[] args)
        {
            Program program = null;

            try { program = new Program(args); }
            catch (Exception ex)
            {
                Console.WriteLine("Arguments error (use -help to list optiions):");
                Console.WriteLine(ex.Message);
            }

            try { program?.Run(); }
            catch (Exception ex)
            {
                Console.WriteLine("Runtime error:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
