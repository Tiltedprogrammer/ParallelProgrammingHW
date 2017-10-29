using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace ConsoleApplication
{
    internal class MD5FolderProgram
    {
        static void Main(string[] args)
        {
            
            var res = MD5Folder.HashFolder(@"C:\Users\Holden Caulfield\.IntelliJIdea2016.3");
            
            Console.WriteLine(res);
        }
    }


    static class MD5Folder
    {

        public static string HashFolder(string path)
        {
            StringBuilder tmpHashedFolder = new StringBuilder();
            string[] files = Directory.GetFiles(path);
            Task<string>[] _fileTasks= new Task<string>[files.Length];
            string[] directories = Directory.GetDirectories(path);
            Task<string>[] _hashFolderTasks = new Task<string>[directories.Length];
            /* Hashing folder's path*/
            tmpHashedFolder.Append(HashString(path));
            /* Hashing all files in the folder*/
            if (files.Length != 0)
            {
                for(int i = 0; i < files.Length; i++)
                {
                    
                    var unmodified_i = i;
                    _fileTasks[i] = Task.Run(() => HashFile(files[unmodified_i]));
                    
                }
            }
            /* Hashing all nested folders in the folder*/
            if (directories.Length != 0)
            {

                for (int i = 0; i < directories.Length; i++)
                {
                    var unmodified_i = i;
                    _hashFolderTasks[i] = Task.Run(() => HashFolder(directories[unmodified_i]));

                }
            }
            Task.WaitAll(_fileTasks);
                foreach (var _filetask in _fileTasks)
                {
                    tmpHashedFolder.Append(_filetask.Result);

                }
            Task.WaitAll(_hashFolderTasks);
            foreach (var _hashFolderTask in _hashFolderTasks)
            {
                tmpHashedFolder.Append(_hashFolderTask.Result);

            }
            
            return HashString(tmpHashedFolder.ToString());

        }

        private static string HashString(string str)
        {
            StringBuilder _stringBuilderhash = new StringBuilder();
            using (MD5 md5 = MD5.Create())
            {

                var bytearray = md5.ComputeHash(Encoding.UTF8.GetBytes(str));

                foreach (var _byte in bytearray)
                {
                    _stringBuilderhash.Append(_byte.ToString("x2"));
                }
            }
            return _stringBuilderhash.ToString();
            
        }

        private static string HashFile(string path)
        {
            StringBuilder result = new StringBuilder();
            using (MD5 md5 = MD5.Create())
            {
                result.Append(HashString(path));
                using (var filesream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(filesream);
                    foreach (var _byte in hash)
                    {
                        result.Append(_byte.ToString("X2"));
                    }
                }
                
            }
            return result.ToString();
        }

    }
}