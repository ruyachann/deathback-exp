using System;
using System.IO;
using System.Security;
using UnityEngine;

namespace LoopRoom
{
    public sealed partial class PlayAreaSettings
    {
        public static PlayAreaSettings LoadOrCreate(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<PlayAreaSettings>(File.ReadAllText(path));
                    if (loaded == null)
                        throw new ArgumentException("The play-area settings file is empty or invalid.", nameof(path));
                    loaded.Validate();
                    return loaded;
                }

                var settings = new PlayAreaSettings();
                settings.Validate();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                try
                {
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(JsonUtility.ToJson(settings, true));
                    }
                }
                catch (IOException) when (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<PlayAreaSettings>(File.ReadAllText(path));
                    if (loaded == null)
                        throw new ArgumentException("The play-area settings file is empty or invalid.", nameof(path));
                    loaded.Validate();
                    return loaded;
                }
                return settings;
            }
            catch (Exception error) when (
                error is ArgumentException ||
                error is IOException ||
                error is NotSupportedException ||
                error is SecurityException ||
                error is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not load or create play-area settings at '" + path +
                    "'. Using defaults without overwriting the file. " + error.Message);
                return new PlayAreaSettings();
            }
        }
    }
}
