using ResourceCompiler;
using Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceCompiler.Compilers
{
    class EventCompiler : Compiler
    {
        private struct CEvent
        {
            public ulong MinDelay;
            public ulong MaxDelay;
        }
        private struct CAction
        {
            public int Type;
            public int LinkID;
            public int OffsetX;
            public int OffsetY;
        }

        public override void Placeholder()
        {
            var cevent = new CEvent();
            Writer.WriteStruct(cevent);
            Writer.Write(0);
        }
        public override void Compile(string path)
        {
            var res = new EventResource(Path.Combine(RootDirectory, path));

            var cevent = new CEvent();
            cevent.MinDelay = res.MinDelay;
            cevent.MaxDelay = res.MaxDelay;
            Writer.WriteStruct(cevent);

            Writer.Write(res.Actions.Count);
            foreach (var a in res.Actions)
            {
                var caction = new CAction();
                string link = ResolveLink(path, a.Tile);
                caction.LinkID = Table[link];
                caction.Type = (int)a.Type;
                caction.OffsetX = a.OffsetX;
                caction.OffsetY = a.OffsetY;
                Writer.WriteStruct(caction);
            }
        }
    }
}
