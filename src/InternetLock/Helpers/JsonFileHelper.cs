using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InternetLock.Helpers
{
    /// <summary>
    /// Provides atomic and safe JSON serialization/deserialization helper methods.
    /// </summary>
    public static class JsonFileHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Reads and deserializes JSON from file. Returns default(T) if file doesn't exist or is corrupted.
        /// </summary>
        public static async Task<T?> LoadAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
            }
            catch (Exception)
            {
                // File corrupted or read error
                return default;
            }
        }

        /// <summary>
        /// Writes data to a temporary file first, then atomically replaces the target file.
        /// </summary>
        public static async Task SaveAtomicAsync<T>(string filePath, T data)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFilePath = filePath + ".tmp";

            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
                    await stream.FlushAsync();
                }

                if (File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
            }
            catch
            {
                // Cleanup temp file if error occurred
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
                throw;
            }
        }
    }
}
