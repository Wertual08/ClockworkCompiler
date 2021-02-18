using Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ResourceCompiler.Compilers
{
    static class SpriteCompiler
    {
        public struct CSprite
        {
            public int TextureIndex;
            public int FrameCount;
            public int FrameDelay;
            public float ImgboxW;
            public float ImgboxH;
            public float AxisX;
            public float AxisY;
            public float Angle;
        }
        public static CSprite Compile(TextureCompiler texture, Sprite res, string root, string path)
        {
            var csprite = new CSprite();
            string link = Compiler.ResolveLink(root, path, res.Texture);
            csprite.TextureIndex = texture.FindLoad(
                root, link, 
                (Image<Rgba32> img) =>
                {
                    int tw = img.Width;
                    int th = img.Height;

                    var pixels = new TextureCompiler.CColor[tw * th];
                    if (res.VerticalFrames)
                    {
                        //if (th % res.FrameCount != 0) LogQueue.Put("Warning: Bad texture proporions.");
                        th /= res.FrameCount;

                        for (int f = 0; f < res.FrameCount; f++)
                        {
                            for (int y = 0; y < th; y++)
                            {
                                for (int x = 0; x < tw; x++)
                                {
                                    var p = img[x, f * th + th - 1 - y];
                                    pixels[f * tw * th + y * tw + x] = new TextureCompiler.CColor(p);
                                }
                            }
                        }
                    }
                    else
                    {
                        //if (tw % res.FrameCount != 0) LogQueue.Put("Warning: Bad texture proporions.");
                        tw /= res.FrameCount;

                        for (int f = 0; f < res.FrameCount; f++)
                        {
                            for (int y = 0; y < th; y++)
                            {
                                for (int x = 0; x < tw; x++)
                                {
                                    var p = img[f * tw + x, th - 1 - y];
                                    pixels[f * tw * th + y * tw + x] = new TextureCompiler.CColor(p);
                                }
                            }
                        }
                    }
                    return pixels;
                },
                (Image<Rgba32> img) =>
                {
                    int w = img.Width;
                    int h = img.Height;
                    if (res.VerticalFrames) h /= res.FrameCount;
                    else w /= res.FrameCount;
                    return new TextureCompiler.CColor[] { new TextureCompiler.CColor((uint)w), new TextureCompiler.CColor((uint)h) };
                }
            );
            csprite.FrameCount = res.FrameCount;
            csprite.FrameDelay = res.FrameDelay;
            csprite.ImgboxW = res.ImgboxW;
            csprite.ImgboxH = res.ImgboxH;
            csprite.AxisX = res.AxisX;
            csprite.AxisY = res.AxisY;
            csprite.Angle = res.Angle;

            return csprite;
        }
    }
}
