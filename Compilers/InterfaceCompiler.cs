using Resources;
using Resources.Interface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceCompiler.Compilers
{
    class InterfaceCompiler : Compiler
    {
        private enum Type : int
        {
            Element,
            Panel,
            Label,
        }
        private enum Anchor : int
        {
            None,
            Lesser,
            Larger,
            Middle,
        }
        private struct CConstraint
        {
            public Anchor AnchorU;
            public Anchor AnchorL;
            public Anchor AnchorD;
            public Anchor AnchorR;
            public int OffsetU;
            public int OffsetL;
            public int OffsetD;
            public int OffsetR;
        }

        private struct CImage
        {
            public enum StretchType : int
            {
                Solid,
                Repeat,
                Scale,
                Glyph,
            };
            public TextureCompiler.CColor Color;
            public int Texture;
            public StretchType Stretch;
        }
        private CImage Compile(string path, InterfaceImage res)
        {
            var cimage = new CImage();

            cimage.Texture = Texture.FindLoad(
                RootDirectory,
                ResolveLink(path, res.Texture),
                (Image<Rgba32> img) =>
                {
                    int tw = img.Width;
                    int th = img.Height;

                    var pixels = new TextureCompiler.CColor[tw * th];
                    if (res.VerticalFrames)
                    {
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

            cimage.Color = new TextureCompiler.CColor(res.Color);
            cimage.Stretch = CImage.StretchType.Solid;
            if (cimage.Texture >= 0)
            {
                switch (res.Stretch)
                {
                    case InterfaceTextureStretchType.Repeat: cimage.Stretch = CImage.StretchType.Repeat; break;
                    case InterfaceTextureStretchType.Scale: cimage.Stretch = CImage.StretchType.Scale; break;
                }
            }
            return cimage;
        }

        private struct CElement
        {
            public int PositionX;
            public int PositionY;
            public int ExtentX;
            public int ExtentY;
            public CConstraint Constraints;
            public bool Resizable;
            public int Count;

            public CElement(InterfaceElement elem)
            {
                PositionX = elem.PositionX;
                PositionY = elem.PositionY;
                ExtentX = elem.ExtentX;
                ExtentY = elem.ExtentY;

                Constraints.AnchorU = (Anchor)elem.AnchorU;
                Constraints.AnchorL = (Anchor)elem.AnchorL;
                Constraints.AnchorD = (Anchor)elem.AnchorD;
                Constraints.AnchorR = (Anchor)elem.AnchorR;

                Constraints.OffsetU = elem.OffsetU;
                Constraints.OffsetL = elem.OffsetL;
                Constraints.OffsetD = elem.OffsetD;
                Constraints.OffsetR = elem.OffsetR;

                Resizable = elem.Resizable;

                Count = elem.Elements.Count;
            }

            public void Write(BinaryWriter w)
            {
                w.Write(PositionX);
                w.Write(PositionY);
                w.Write(ExtentX);
                w.Write(ExtentY);
                w.WriteStruct(Constraints);
                w.Write(Resizable);
                w.Write(Count);
            }
        }
        private void Compile(string path, InterfaceElement res)
        {
            Writer.Write((int)Type.Element);
            (new CElement(res)).Write(Writer);
        }

        private struct CPanel
        {
            public CElement Base;
            public CImage Image;

            public void Write(BinaryWriter w)
            {
                Base.Write(w);
                w.WriteStruct(Image);
            }
        }
        private void Compile(string path, InterfacePanel res)
        {
            Writer.Write((int)Type.Panel);
            var cpanel = new CPanel();
            cpanel.Base = new CElement(res);
            cpanel.Image = Compile(path, res.Image);
            cpanel.Write(Writer);
        }

        private struct CLabel
        {
            public CElement Base;
            public TextureCompiler.CColor Color;
            public string Text;

            public void Write(BinaryWriter w)
            {
                Base.Write(w);
                w.WriteStruct(Color);
                byte[] utf8 = Encoding.UTF8.GetBytes(Text);
                w.Write(utf8.Length);
                w.Write(utf8);
            }
        }
        private void Compile(string path, InterfaceLabel res)
        {
            Writer.Write((int)Type.Label);
            var clabel = new CLabel();
            clabel.Base = new CElement(res);
            clabel.Color = new TextureCompiler.CColor(res.Color);
            clabel.Text = res.Text;
            clabel.Write(Writer);
        }

        private struct CButton
        {

        }

        private void CompileBase(string path, InterfaceElement elem)
        {
            switch (elem.Type)
            {
                case InterfaceElementType.Element: Compile(path, elem); break;

                case InterfaceElementType.Panel: Compile(path, elem as InterfacePanel); break;

                case InterfaceElementType.Label: Compile(path, elem as InterfaceLabel); break;

                default: throw new Exception($"Unsopported element type [{elem.Type}].");
            }
            foreach (var sub_elem in elem.Elements)
                CompileBase(path, sub_elem);
        }

        public override void Placeholder()
        {
            (new CElement()).Write(Writer);
        }
        public override void Compile(string path)
        {
            var res = new InterfaceResource(Path.Combine(RootDirectory, path));

            CompileBase(path, res.BaseElement);
        }
    }
}
