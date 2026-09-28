using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// Replaces the legacy MultipartFormDataStreamProvider with IFormFile-based validation.
/// Validates file extensions and provides a sanitized local filename.
/// </summary>
public static class FileUploadHelper
{
    private static readonly string[] AllowedExtensions = { "png", "gif", "jpg", "zip", "xml" };

    /// <summary>
    /// Checks if the uploaded file has an allowed extension.
    /// </summary>
    public static bool IsAllowedExtension(IFormFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileName))
            return false;

        var filename = file.FileName.Replace("\"", string.Empty);

        if (filename.IndexOf('.') < 0)
            return false;

        var extension = filename.Split('.').Last();
        return AllowedExtensions.Any(i => i.Equals(extension, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Returns a sanitized local filename from the uploaded file.
    /// </summary>
    public static string GetLocalFileName(IFormFile file)
    {
        return file.FileName.Replace("\"", string.Empty);
    }

    /// <summary>
    /// Saves an IFormFile to the specified directory path, validating extension first.
    /// Returns the full path of the saved file, or null if extension not allowed.
    /// </summary>
    public static string SaveToDirectory(IFormFile file, string directoryPath)
    {
        if (!IsAllowedExtension(file))
            return null;

        var fileName = GetLocalFileName(file);
        var fullPath = Path.Combine(directoryPath, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return fullPath;
    }
}
