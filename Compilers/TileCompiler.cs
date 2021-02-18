using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ResourceCompiler.Compilers
{
    class TileCompiler : Compiler
    {
        public static TextureCompiler.CColor[] GetTilePartPixels(Image<Rgba32> tex, int size, int px, int py)
        {
            var result = new TextureCompiler.CColor[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = tex[px * size + x, py * size + size - 1 - y];
                    result[y * size + x] = new TextureCompiler.CColor(p);
                }
            }

            return result;
        }
        public static TextureCompiler.CColor[] GetTilePartFrames(Image<Rgba32> tex, int size, int uc, int frames, int px, int py)
        {
            var result = new TextureCompiler.CColor[size * size * frames];
            for (int f = 0; f < frames; f++)
                GetTilePartPixels(tex, size, px, py + f * uc).CopyTo(result, f * size * size);
            return result;
        }
        public static TextureCompiler.CColor[] GetTilePartsCompound(Image<Rgba32> tex, int size, int uc, int frames, int px, int py)
        {
            var result = new TextureCompiler.CColor[size * size * frames * 4];
            GetTilePartFrames(tex, size, uc, frames, px, py).CopyTo(result, size * size * frames * 0);
            GetTilePartFrames(tex, size, uc, frames, px, uc - 1 - py).CopyTo(result, size * size * frames * 1);
            GetTilePartFrames(tex, size, uc, frames, uc - 1 - px, uc - 1 - py).CopyTo(result, size * size * frames * 2);
            GetTilePartFrames(tex, size, uc, frames, uc - 1 - px, py).CopyTo(result, size * size * frames * 3);
            return result;
        }
        public static TextureCompiler.CColor[] GetTilePixels(Image<Rgba32> tex, int size, int uc, int frames)
        {
            int unit_size = size * size * frames * 4;
            if (uc == 2)
            {
                return GetTilePartsCompound(tex, size, uc, frames, 0, 0);
            }                                                        
            else if (uc == 4)
            {
                var result = new TextureCompiler.CColor[unit_size * 4];
                GetTilePartsCompound(tex, size, uc, frames, 1, 1).CopyTo(result, unit_size * 0);
                GetTilePartsCompound(tex, size, uc, frames, 1, 0).CopyTo(result, unit_size * 1);
                GetTilePartsCompound(tex, size, uc, frames, 0, 1).CopyTo(result, unit_size * 2);
                GetTilePartsCompound(tex, size, uc, frames, 0, 0).CopyTo(result, unit_size * 3); 
                return result;
            }                                                        
            else if (uc == 6)
            {
                var result = new TextureCompiler.CColor[unit_size * 5];
                GetTilePartsCompound(tex, size, uc, frames, 2, 2).CopyTo(result, unit_size * 0);
                GetTilePartsCompound(tex, size, uc, frames, 2, 1).CopyTo(result, unit_size * 1);
                GetTilePartsCompound(tex, size, uc, frames, 1, 2).CopyTo(result, unit_size * 2);
                GetTilePartsCompound(tex, size, uc, frames, 1, 1).CopyTo(result, unit_size * 3);
                GetTilePartsCompound(tex, size, uc, frames, 0, 0).CopyTo(result, unit_size * 4);
                return result;
            }
            else throw new Exception("Unsupported texture format.");
        }
        private struct CTile
        {
            public int OffsetX;
            public int OffsetY;

            public uint Properties;
            public uint Form;
            public uint Anchors;
            public uint Reactions;
            public TextureCompiler.CColor Light;
            public int Solidity;

            public int TextureIndex;
            public int PartSize;
            public int FrameCount;
            public int FrameDelay;
            public int Layer;

            public int SetupEventID;
            public int ReformEventID;
            public int TouchEventID;
            public int ActivateEventID;
            public int RecieveEventID;
            public int RemoveEventID;
        }

        public override void Placeholder()
        {
            Writer.WriteStruct(new CTile());
        }
        public override void Compile(string path)
        {
            var res = new TileResource(Path.Combine(RootDirectory, path));

            var ctile = new CTile();
            ctile.OffsetX = res.OffsetX;
            ctile.OffsetY = res.OffsetY;

            ctile.Properties = (uint)res.Properties;
            ctile.Form = (uint)res.Form;
            ctile.Anchors = (uint)res.Anchors;
            ctile.Reactions = (uint)res.Reactions;
            ctile.Light = new TextureCompiler.CColor(res.Light);
            ctile.Solidity = res.Solidity;

            ctile.TextureIndex = Texture.FindLoad(
                RootDirectory, 
                ResolveLink(path, res.Texture),
                (Image<Rgba32> tex) =>
                {
                    int tw = tex.Width;
                    int th = tex.Height;

                    int ucw = tw / res.PartSize;
                    int uch = th / res.FrameCount / res.PartSize;

                    //if (tex.Width % res.PartSize != 0 || tex.Height % res.FrameCount != 0 || uch != ucw ||
                    //    tex.Height / res.FrameCount % res.PartSize != 0 || ucw % 2 != 0 || uch % 2 != 0)
                    //    LogQueue.Put("Warning: Bad tile texture proporions.");
                    int uc = Math.Min(ucw, uch);

                    return GetTilePixels(tex, res.PartSize, uc, res.FrameCount);
                });
            ctile.PartSize = res.PartSize;
            ctile.FrameCount = res.FrameCount;
            ctile.FrameDelay = res.FrameDelay;
            ctile.Layer = res.Layer;

            ctile.SetupEventID = Table[ResolveLink(path, res.SetupEvent)];
            ctile.ReformEventID = Table[ResolveLink(path, res.ReformEvent)];
            ctile.TouchEventID = Table[ResolveLink(path, res.TouchEvent)];
            ctile.ActivateEventID = Table[ResolveLink(path, res.ActivateEvent)];
            ctile.RecieveEventID = Table[ResolveLink(path, res.RecieveEvent)];
            ctile.RemoveEventID = Table[ResolveLink(path, res.RemoveEvent)];

            Writer.WriteStruct(ctile);
        }
    }
}
