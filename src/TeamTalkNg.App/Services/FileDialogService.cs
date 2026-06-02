using System.IO;
using Microsoft.Win32;

namespace TeamTalkNg.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? ShowUploadFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Upload File",
            CheckFileExists = true,
            Multiselect = false,
            Filter = "All files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowDownloadFileDialog(string fileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Downloaded File",
            FileName = SanitizeFileName(fileName),
            Filter = "All files (*.*)|*.*",
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string SanitizeFileName(string fileName)
    {
        string safeName = string.IsNullOrWhiteSpace(fileName) ? "download" : fileName;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar, '_');
        }

        return safeName;
    }
}
