using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResourceCompiler;
using Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace ResourceCompiler.Compilers
{
    class TextureCompiler : IDisposable
    {
        public struct CColor
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;

            public CColor(Rgba32 rgba)
            {
                R = rgba.R;
                G = rgba.G;
                B = rgba.B;
                A = rgba.A;
            }
            public CColor(System.Drawing.Color rgba)
            {
                R = rgba.R;
                G = rgba.G;
                B = rgba.B;
                A = rgba.A;
            }
            public CColor(uint rgba)
            {
                R = (byte)((rgba >> 24) & 0xFF);
                G = (byte)((rgba >> 16) & 0xFF);
                B = (byte)((rgba >> 8) & 0xFF);
                A = (byte)((rgba >> 0) & 0xFF);
            }
        }
        private static string ToHex(CColor[] data)
        {
            var builder = new StringBuilder(data.Length * 8);
            foreach (var v in data)
            {
                builder.Append(v.R.ToString("0X2"));
                builder.Append(v.G.ToString("0X2"));
                builder.Append(v.B.ToString("0X2"));
                builder.Append(v.A.ToString("0X2"));
            }
            return builder.ToString();
        }

        private BinaryWriter Writer;
        private int Index = 0;
        private Dictionary<string, int> Indexes = new Dictionary<string, int>();

        public TextureCompiler(string path)
        {
            Writer = new BinaryWriter(File.OpenWrite(path));
            Writer.Write(0);
        }

        public int FindLoad(string root, string link, Func<Image<Rgba32>, CColor[]> converter, Func<Image<Rgba32>, CColor[]> meta = null)
        {
            using (var img = Image.Load<Rgba32>(Path.Combine(root, link)))
            {
                var meta_data = meta != null ? meta(img) : null; 
                string name = meta_data != null ? "~:/[" + ToHex(meta_data) + "]\\:~" + link : link;

                if (Indexes.ContainsKey(name)) return Indexes[name];

                var data = converter(img);

                int index = Index;
                if (meta != null)
                {
                    foreach (var datum in meta_data)
                        Writer.WriteStruct(datum);
                    Index += meta_data.Length;
                }
                if (data != null)
                {
                    foreach (var datum in data)
                        Writer.WriteStruct(datum);
                    Index += data.Length;
                }
                Indexes.Add(name, index);
                return index;
            }
        }

        public void Dispose()
        {
            Writer.Seek(0, SeekOrigin.Begin);
            Writer.Write(Index);
            Writer.Dispose();
        }
    }
}
