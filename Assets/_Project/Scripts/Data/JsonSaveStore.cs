using System;
using System.IO;
using SubmarineVoyage.Core;
using UnityEngine;

namespace SubmarineVoyage.Data
{
    /// <summary>
    /// Reads and writes <see cref="SaveData"/> as JSON under Application.persistentDataPath.
    /// </summary>
    public class JsonSaveStore
    {
        private readonly string _path;

        public JsonSaveStore(string fileName = "save.json")
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string FilePath => _path;

        /// <summary>Returns null when there is no save or it cannot be read.</summary>
        public SaveData Load()
        {
            if (!File.Exists(_path)) return null;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
                if (data == null) throw new InvalidDataException("Save file is empty.");
                return data;
            }
            catch (Exception e)
            {
                // Keep the broken file for inspection instead of silently overwriting it.
                var backup = _path + ".bak";
                File.Copy(_path, backup, overwrite: true);
                Debug.LogWarning($"Could not read save file, starting fresh. Backup: {backup}\n{e}");
                return null;
            }
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Write to a temp file first so a crash mid-write cannot corrupt the existing save.
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, prettyPrint: true));
            if (File.Exists(_path)) File.Replace(temp, _path, null);
            else File.Move(temp, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
