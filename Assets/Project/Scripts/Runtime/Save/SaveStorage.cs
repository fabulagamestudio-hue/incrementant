using System.IO;
using UnityEngine;

namespace Fabula.Incrementant.Save
{
    /// <summary>Lê e grava o save rápido em <c>Application.persistentDataPath/Saves</c>.</summary>
    public static class SaveStorage
    {
        public const string QuickSaveName = "quicksave.sav";

        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Saves");

        public static string QuickSavePath => Path.Combine(DirectoryPath, QuickSaveName);

        /// <summary>Grava num arquivo temporário e troca no fim, para uma falha no meio não destruir o save anterior.</summary>
        public static void Write(string path, GameSnapshot snapshot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (FileStream stream = File.Create(temp))
                snapshot.Write(stream);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static GameSnapshot Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
                return GameSnapshot.Read(stream);
        }
    }
}
