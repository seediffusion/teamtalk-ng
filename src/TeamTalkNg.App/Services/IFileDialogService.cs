namespace TeamTalkNg.App.Services;

public interface IFileDialogService
{
    string? ShowUploadFileDialog();

    string? ShowDownloadFileDialog(string fileName);
}
