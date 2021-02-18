using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceCompiler
{
    static class Utilities
    {
        public static string MakeDirectoryRelated(string dir, string sub)
        {
            dir = Path.GetFullPath(dir);
            sub = Path.GetFullPath(sub);
            if (dir == sub) return ".";

            var sep = Path.DirectorySeparatorChar;
            var len = 0;
            while (len < dir.Length && len < sub.Length && dir[len] == sub[len]) len++;
            dir = dir.Remove(0, len);
            sub = sub.Remove(0, len);

            if (sub.Length == 0) return "";
            if (sub[0] == sep) return sub.Remove(0, 1);

            string result = "";
            foreach (var c in dir)
                if (c == sep) result += ".." + sep;

            return result + sub;
        }
    }
    public static class StructStreamer
    {
        public static byte[] ToBytes<S>(S str)
        {
            var size = Marshal.SizeOf(str);
            var arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public static S FromBytes<S>(byte[] arr)
        {
            var size = Marshal.SizeOf(typeof(S));
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            var str = (S)Marshal.PtrToStructure(ptr, typeof(S));
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static S ReadStruct<S>(this BinaryReader r)
        {
            var size = Marshal.SizeOf<S>();
            var arr = r.ReadBytes(size);

            var ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            var str = Marshal.PtrToStructure<S>(ptr);
            Marshal.FreeHGlobal(ptr);
            return str;
        }
        public static void WriteStruct<S>(this BinaryWriter w, S str)
        {
            var size = Marshal.SizeOf(str);
            var arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            w.Write(arr);
        }
    }
}
