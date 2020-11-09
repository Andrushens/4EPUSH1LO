using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace lab2_service
{
    class FileWatcher
    {
        DirectoryInfo sourceDirectory;
        DirectoryInfo targetDirectory;
        bool isWorking;

        byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        byte[] iv = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        public FileWatcher(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory)
        {
            this.sourceDirectory = sourceDirectory;
            this.targetDirectory = targetDirectory;
            isWorking = true;
        }

        public void Process()
        {
            ClearDirectory(targetDirectory);
            Directory.CreateDirectory(targetDirectory.FullName);
            List<FileInfo> archivedFiles = new List<FileInfo>();

            while (isWorking)
            {
                DirectoryInfo[] yearDirectories = sourceDirectory.GetDirectories();

                foreach (DirectoryInfo yearDir in yearDirectories)
                {
                    DirectoryInfo[] monthDirectories = yearDir.GetDirectories();

                    foreach (DirectoryInfo monthDir in monthDirectories)
                    {
                        DirectoryInfo[] DayDirectories = monthDir.GetDirectories();

                        foreach (DirectoryInfo dayDir in DayDirectories)
                        {
                            FileInfo[] files = dayDir.GetFiles();

                            foreach (FileInfo f in files)
                            {
                                if (!Regex.IsMatch(f.Name, @"[^_]*_[0-9]{4}_(0[1-9]|1[0-2])_(0[1-9]|[1-2][\d]|3[0-1])_(0[1-9]|1[\d]|2[0-4])_(0[1-9]|[1-5][\d])_(0[1-9]|[1-5][\d])\.txt")) continue;
                                bool isArchived = false;

                                foreach (FileInfo archFile in archivedFiles)
                                {
                                    if (f.Name == archFile.Name)
                                    {
                                        isArchived = true;
                                        break;
                                    }
                                }

                                if (!isArchived && f.Length != 0)
                                {
                                    string zipsDirectory = targetDirectory.FullName + @"\zips";
                                    
                                    if (!Directory.Exists(zipsDirectory))
                                    {
                                        Directory.CreateDirectory(zipsDirectory);
                                    }
                                    Compress(f, new DirectoryInfo(zipsDirectory));
                                    Decompress(new FileInfo(f.FullName.Substring(0, f.FullName.Length - 3) + "gz"), new DirectoryInfo(zipsDirectory));
                                    archivedFiles.Add(f);
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(500);
            }
        }

        public void Stop()
        {
            isWorking = false;
            Thread.Sleep(500);
        }

        private void Compress(FileInfo sourceFile, DirectoryInfo targetDir)
        {
            try
            {
                string arcFileName = targetDir.FullName + @"\" + sourceFile.Name;
                Encrypt(sourceFile, new FileInfo(arcFileName));

                using (FileStream sourceStream = new FileStream(arcFileName, FileMode.Open))
                {
                    using (FileStream targetStream = File.Create(arcFileName.Substring(0, arcFileName.Length - 3) + "gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                        {

                            sourceStream.CopyTo(compressionStream);
                        }
                    }
                }
                Console.WriteLine(sourceFile.Name + " comressed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Decompress(FileInfo sourceFile, DirectoryInfo targetDir)
        {
            try
            {
                string arcFileName = targetDir.FullName + @"\" + sourceFile.Name;

                using (FileStream sourceStream = new FileStream(arcFileName, FileMode.Open))
                {
                    using (FileStream targetStream = File.Create(arcFileName.Substring(0, arcFileName.Length - 2) + "txt"))
                    {
                        using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(targetStream);
                        }
                    }
                }
                Decrypt(new FileInfo(arcFileName.Substring(0, arcFileName.Length - 2) + "txt"), targetDir);
                Console.WriteLine(sourceFile.Name + " decompressed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Encrypt(FileInfo sourceFile, FileInfo targetFile)
        {
            try
            {
                byte[] sourceData = null;
                byte[] encryptedData = null;

                using (FileStream fs = File.OpenRead(sourceFile.FullName))
                {
                    sourceData = new byte[fs.Length];
                    fs.Read(sourceData, 0, sourceData.Length);
                }
                Aes Aes = Aes.Create();

                using (MemoryStream memStream = new MemoryStream())
                {
                    using (CryptoStream encrypStream = new CryptoStream(memStream, Aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                    {
                        encrypStream.Write(sourceData, 0, sourceData.Length);
                        encrypStream.FlushFinalBlock();
                        encryptedData = memStream.ToArray();
                        string text = Convert.ToBase64String(encryptedData);
                        
                        using (StreamWriter sWrite = new StreamWriter(targetFile.FullName))
                        {
                            sWrite.Write(text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Decrypt(FileInfo file, DirectoryInfo targetDir)
        {
            try
            {
                byte[] encryptedData = null;
                byte[] decryptedData = null;

                using (StreamReader readS = new StreamReader(file.FullName))
                {
                    encryptedData = Convert.FromBase64String(readS.ReadToEnd());
                }
                Aes Aes = Aes.Create();

                using (MemoryStream memStream = new MemoryStream(encryptedData))
                {
                    using (CryptoStream decrypStream = new CryptoStream(memStream, Aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
                    {
                        using (MemoryStream tempMem = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024];

                            while ((decrypStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                tempMem.Write(buffer, 0, buffer.Length);
                            }
                            decryptedData = tempMem.ToArray();
                            string text = System.Text.Encoding.UTF8.GetString(decryptedData);
                            
                            using (StreamWriter writeS = new StreamWriter(targetDir + @"\" + file.Name))
                            {
                                writeS.Write(text);
                            }
                        }
                    }
                }
                PutInArchive(new FileInfo(targetDir.FullName + @"\" + file.Name), new DirectoryInfo(targetDir.Parent.FullName + @"\txts"));
                File.Delete(targetDir + @"\" + file.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PutInArchive(FileInfo file, DirectoryInfo targetDir)
        {
            string dirName = targetDir.FullName;
            string fileName;
            string[] nameParts = file.Name.Split('_');

            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            for (int i = 1; i <= 3; ++i)
            {
                dirName += @"\" + nameParts[i];

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
            }
            fileName = dirName + @"\" + file.Name;

            if (!File.Exists(fileName))
            {
                File.Copy(file.FullName, fileName);
            }
        }

        private void ClearDirectory(DirectoryInfo directory)
        {
            foreach(DirectoryInfo dir in directory.GetDirectories())
            {
                ClearDirectory(dir);
                dir.Delete();
            }

            foreach(FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}