﻿using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Utils
{
    class Mobi
    {
        /// <summary>
        /// Parse backward-encoded Mobipocket variable-width int
        /// Retuns int with optional out param for number of bytes used to create int
        /// 
        /// https://wiki.mobileread.com/wiki/MOBI#Variable-width_integers
        /// </summary>
        /// <param name="buffer"> At least four bytes read from end of text record</param>
        /// <returns></returns>
        public static int VarLengthInt(byte[] buffer, out int c)
        {
            int varint = 0;
            c = 0;
            byte b;
            for (int i = 0; i < 4; i++)
            {
                b = buffer[i];
                c++;
                varint = (varint << 7) | (b & 0x7f);
                if ((b & 0x80) > 0)
                {
                    break;
                }
            }
            return varint;
        }

        public static int VarLengthInt(byte[] buffer)
        {
            return VarLengthInt(buffer, out int _);
        }

        /// <summary>
        /// Turns uint into big-endian VarLengthInt byte array
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte[] EncVarLengthInt(uint val)
        {
            List<byte> output = new List<byte>();

            while (output.Count < 4)
            {
                output.Add((byte)(val & 0x7f));
                val >>= 7;
                if (val == 0) break;
            }
            output[0] |= 0x80;
            output.Reverse();
            return output.ToArray();
        }

        public static int CountBits(byte b)
        {
            int count = 0;
            while (b > 0)
            {
                if ((b & 0x01) == 0x01)
                {
                    count++;
                }
                b >>= 1;
            }
            return count;
        }
    }

    class PalmDoc
    {
        /// <summary>
        /// 
        /// </summary>
        /// 

        public static byte[] decompress(byte[] buffer, int compressedLen)
        {
            byte[] output = new byte[decompressedLength(buffer, compressedLen)];
            int i = 0;
            int j = 0;
            while (i < compressedLen)
            {
                int c = buffer[i++];

                if (c >= 0xc0)
                {
                    output[j++] = (byte)' ';
                    output[j++] = (byte)(c & 0x7f);
                }
                else if (c >= 0x80)
                {
                    c = (c << 8) + buffer[i++];
                    int windowLen = (c & 0x0007) + 3;
                    int windowDist = (c >> 3) & 0x07FF;
                    int windowCopyFrom = j - windowDist;

                    windowLen = Math.Min(windowLen, output.Length - j);

                    while (windowLen-- > 0)
                    {
                        output[j++] = output[windowCopyFrom++];
                    }
                }
                else if (c >= 0x09)
                {
                    output[j++] = (byte)c;
                }
                else if (c >= 0x01)
                {
                    c = Math.Min(c, output.Length - j);
                    while (c-- > 0)
                    {
                        output[j++] = buffer[i++];
                    }
                }
                else
                {
                    output[j++] = (byte)c;
                }
            }
            return output;
        }

        private static int decompressedLength(byte[] buffer, int compressedLen)
        {
            int i = 0;
            int len = 0;

            while (i < compressedLen)
            {
                int c = buffer[i++] & 0x00ff;
                if (c >= 0x00c0)
                {
                    len += 2;
                }
                else if (c >= 0x0080)
                {
                    c = (c << 8) | (buffer[i++] & 0x00FF);
                    len += 3 + (c & 0x0007);
                }
                else if (c >= 0x0009)
                {
                    len++;
                }
                else if (c >= 0x0001)
                {
                    len += c;
                    i += c;
                }
                else
                {
                    len++;
                }
            }
            return len;
        }
    }

    class Metadata
    {
        private static Random RandomNum = new Random();
        public static int RandomNumber(int digits = 3)
        {
            int upper = digits >= 10 ? int.MaxValue : (int)(Math.Pow(10, digits) - 1);
            return RandomNum.Next(1, upper);
        }
        public static int TimeStamp()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1904, 1, 1);
            return (int)t.TotalSeconds;
        }

        public static int TimeStamp(int year, int month, int day)
        {
            TimeSpan t = new DateTime(year, month, day) - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        private static CultureInfo culture = new CultureInfo("en-US");

        private static string[] dateFormats = new string[] { "yyyy", "yyyy-MM", "yyyy-MM-dd", "MM/dd/yyyy" };

        /// <summary>
        /// Reorders author name for standard lastname-first sorting ie "Charles Dickens" becomes "Dickens, Charles"
        /// </summary>
        public static string SortAuthor(string author)
        {
            string[] splt = author.Split(' ');
            if (splt.Length == 1) return author;

            return splt[splt.Length - 1] + ", " + string.Join(" ", splt.SubArray(0, splt.Length - 1));
        }

        /// <summary>
        /// Converts date strings into epub standard yyyy-MM-dd (1950-01-01)
        /// </summary>
        public static string GetDate(string date)
        {
            if (date == "")
            {
                return DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
            return DateTime.ParseExact(date.Truncate(10), dateFormats, culture, DateTimeStyles.None).ToString("yyyy-MM-dd");
        }
    }

    /// <summary>
    /// Collection of methods to convert various types to/from big-endian byte[]
    /// </summary>
    class BigEndian
    {
        private static bool reverseBytes = BitConverter.IsLittleEndian;

        #region From Bytes
        public static short ToInt16(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return BitConverter.ToInt16(slice, 0x0);
        }

        public static ushort ToUInt16(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return BitConverter.ToUInt16(slice, 0x0);
        }

        public static uint ToUInt32(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt32(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x4);
            Array.Reverse(slice);
            return BitConverter.ToUInt32(slice, 0x0);
        }

        public static ulong ToUInt64(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt64(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x8);
            Array.Reverse(slice);
            return BitConverter.ToUInt64(slice, 0x0);
        }

        public static int ToInt32(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToInt32(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x4);
            Array.Reverse(slice);
            return BitConverter.ToInt32(slice, 0x0);
        }
        #endregion

        #region GetBytes
        // Why can't GetBytes be generic....
        public static byte[] GetBytes(short val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(ushort val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(int val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(uint val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(ulong val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }
        #endregion
    }

    class Files
    {
        /// <summary>
        /// Creates string[] of absolute paths to all files and folders in dir
        /// Ignores dirs that throw any errors (typically access denied)
        /// Pass 'true' for subdirsOnly to get array of subdirs without files
        /// </summary>
        public static string[] DirSearch(string dir, bool subdirsOnly = false)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string subdir in Directory.GetDirectories(dir))
                {
                    if (!subdirsOnly)
                    {
                        Console.WriteLine(string.Join(", ", Directory.GetFiles(subdir)));
                        files.AddRange(Directory.GetFiles(subdir));
                    }
                    files.AddRange(DirSearch(subdir));
                }
            }
            catch
            {
            }
            return files.ToArray();
        }
    }
}