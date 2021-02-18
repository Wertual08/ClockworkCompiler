using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceCompiler.Compilers
{
    abstract class Compiler
    {
        public static string ResolveLink(string root, string path, string link)
        {
            if (link.StartsWith("./") || link.StartsWith(".\\"))
            {
                root = Path.GetFullPath(root);
                path = Path.GetFullPath(Path.Combine(root, path));
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                path = Path.GetFullPath(Path.Combine(path, link.Substring(2)));
                return path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            }
            else return link;
        }

        public string RootDirectory { get; set; }
        public IDTable Table { get; set; }
        public BinaryWriter Writer { get; set; }
        public TextureCompiler Texture { get; set; }

        public string ResolveLink(string path, string link)
        {
            if (link.StartsWith("./") || link.StartsWith(".\\"))
            {
                string root = Path.GetFullPath(RootDirectory);
                path = Path.GetFullPath(Path.Combine(root, path));
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                path = Path.GetFullPath(Path.Combine(path, link.Substring(2)));
                return path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            }
            else return link;
        }

        public abstract void Placeholder();
        public abstract void Compile(string path);
    }
}
