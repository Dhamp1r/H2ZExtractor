using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H2ZExtractor
{
    class HizReader
    {
        public HizArchive HizArchive;
        private Decoder decoder;

        private int checksum;
        private int hiz_buffer_size;

        public HizReader(string str_key)
        {
            decoder = new Decoder(str_key);
        }

        public void ReadFromH2Z(string h2z)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(h2z));
            Console.WriteLine("Reading {0}", Path.GetFileName(h2z));
            decoder.Clear();
            if (!ReadH2ZHeader(reader)) return;
            byte[] hiz_buffer = reader.ReadBytes(hiz_buffer_size);
            ReadHizArchive(hiz_buffer);
            reader.Close();
        }

        private void ReadHizArchive(byte[] hiz_buffer)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(hiz_buffer));
            HizArchive = new HizArchive();
            if (!ReadHizHeader(reader)) return;

            HizArchive.hizFiles = new HizFile[HizArchive.file_count];
            for (int i = 0; i < HizArchive.file_count; i++)
            {
                HizFile hizFile = new HizFile();
                hizFile.type = decoder.DecodeInt16(reader);
                hizFile.compress_size = decoder.DecodeInt32(reader);
                hizFile.decompress_size = decoder.DecodeInt32(reader);
                int name_len = decoder.DecodeInt16(reader);
                byte[] name = decoder.DecodeData(reader.ReadBytes(name_len));
                hizFile.name = Encoding.UTF8.GetString(name);
                if (hizFile.isCompressed())
                    hizFile.data = Unzip.GetData(reader.ReadBytes(hizFile.compress_size), true);
                else
                    hizFile.data = reader.ReadBytes(hizFile.decompress_size);
                HizArchive.hizFiles[i] = hizFile;
            }
        }

        public void ReadFromDir(string dir)
        {
            HizArchive = new HizArchive();

            long maxFileSize = 0;
            HizFile[] hizFiles = GetFilesForPack(dir, out maxFileSize);

            if (hizFiles.Length == 0)
            {
                Console.WriteLine("Hiz files count = 0.");
                return;
            }

            HizArchive.header = "HIZ".ToCharArray();
            HizArchive.file_count = hizFiles.Length;
            HizArchive.max_compress_size = (uint)maxFileSize;
            HizArchive.hizFiles = hizFiles;
        }

        private HizFile[] GetFilesForPack(string rootDir, out long maxFileSize)
        {
            List<HizFile> fileList = new List<HizFile>();
            maxFileSize = 0;

            string[] allFiles = Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories);
            string[] allDirs = Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories);

            List<string> allPaths = new List<string>();
            allPaths.AddRange(allFiles);
            allPaths.AddRange(allDirs);

            Console.WriteLine("Preparing files and compressing...");

            foreach (string fullPath in allPaths)
            {
                HizFile hizFile = new HizFile();
                bool isDir = File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory);

                string relativePath = fullPath.Substring(rootDir.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()) || relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
                {
                    relativePath = relativePath.Substring(1);
                }
                relativePath = relativePath.Replace('\\', '/');

                if (isDir && !relativePath.EndsWith("/"))
                {
                    relativePath += "/";
                }

                hizFile.name = relativePath;

                if (isDir)
                {
                    hizFile.type = 0;
                    hizFile.compress_size = 0;
                    hizFile.decompress_size = 0;
                    hizFile.data = new byte[0];
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(fullPath);
                    byte[] originalData = File.ReadAllBytes(fullPath);

                    if (originalData.Length > maxFileSize)
                        maxFileSize = originalData.Length;

                    byte[] compressedData = Unzip.GetData(originalData, false);

                    if (compressedData.Length < originalData.Length)
                    {
                        hizFile.type = 8;
                        hizFile.decompress_size = originalData.Length;
                        hizFile.compress_size = compressedData.Length;
                        hizFile.data = compressedData;
                    }
                    else
                    {
                        hizFile.type = 0;
                        hizFile.decompress_size = originalData.Length;
                        hizFile.compress_size = originalData.Length;
                        hizFile.data = originalData;
                    }
                }
                fileList.Add(hizFile);
            }

            fileList.Sort((x, y) => String.Compare(x.name, y.name, StringComparison.Ordinal));

            return fileList.ToArray();
        }

        private bool ReadH2ZHeader(BinaryReader reader)
        {
            string header = new string(reader.ReadChars(4));
            if (string.Compare(header, "H2Z") != 0) return false;
            checksum = reader.ReadInt32();
            hiz_buffer_size = reader.ReadInt32();
            return true;
        }

        private bool ReadHizHeader(BinaryReader reader)
        {
            HizArchive.header = reader.ReadChars(4);
            if (string.Compare(new string(HizArchive.header), "HIZ") != 0) return false;
            HizArchive.file_count = reader.ReadInt32();
            HizArchive.max_compress_size = reader.ReadUInt32();
            return true;
        }
    }
}