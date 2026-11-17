using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace DriveFinder.Services;

public interface IGoogleDriveService
{
    Task<string> GetStartPageTokenAsync(CancellationToken cancellationToken = default);

    Task<List<GoogleFile>> ListAllFilesAsync(CancellationToken cancellationToken = default);

    Task<List<GoogleFile>> ListAllFoldersAsync(CancellationToken cancellationToken = default);

    bool IsSupported(string mimeType);

    Task<string> ReadFileTextAsync(GoogleFile file, CancellationToken cancellationToken = default);
}

public class GoogleDriveService : IGoogleDriveService
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private const string GoogleDocMimeType = "application/vnd.google-apps.document";
    private const string GoogleSlidesMimeType = "application/vnd.google-apps.presentation";
    private const string GoogleSheetMimeType = "application/vnd.google-apps.spreadsheet";
    private const string PdfMimeType = "application/pdf";
    private const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private const string FileFields = "nextPageToken, files(id, name, mimeType, parents, webViewLink, modifiedTime)";

    private readonly DriveService _driveService;
    // When empty, the app reads every folder shared with the service account (used for testing with a personal account)
    private readonly string? _sharedDriveId;

    public GoogleDriveService(DriveService driveService, IConfiguration configuration)
    {
        _driveService = driveService;

        var sharedDriveId = configuration.GetValue<string>("GoogleDrive:SharedDriveId");
        _sharedDriveId = string.IsNullOrWhiteSpace(sharedDriveId) ? null : sharedDriveId;
    }

    public async Task<string> GetStartPageTokenAsync(CancellationToken cancellationToken = default)
    {
        var request = _driveService.Changes.GetStartPageToken();

        if (_sharedDriveId != null)
        {
            request.DriveId = _sharedDriveId;
            request.SupportsAllDrives = true;
        }

        var response = await request.ExecuteAsync(cancellationToken);
        return response.StartPageTokenValue;
    }

    public Task<List<GoogleFile>> ListAllFilesAsync(CancellationToken cancellationToken = default)
    {
        return ListFilesAsync($"trashed = false and mimeType != '{FolderMimeType}'", cancellationToken);
    }

    public Task<List<GoogleFile>> ListAllFoldersAsync(CancellationToken cancellationToken = default)
    {
        return ListFilesAsync($"trashed = false and mimeType = '{FolderMimeType}'", cancellationToken);
    }

    public bool IsSupported(string mimeType)
    {
        return mimeType is GoogleDocMimeType
            or GoogleSlidesMimeType
            or GoogleSheetMimeType
            or PdfMimeType
            or DocxMimeType
            or "application/json"
            or "application/xml"
            || mimeType.StartsWith("text/");
    }

    public async Task<string> ReadFileTextAsync(GoogleFile file, CancellationToken cancellationToken = default)
    {
        if (file.MimeType is GoogleDocMimeType or GoogleSlidesMimeType)
        {
            return await ExportAsTextAsync(file.Id, "text/plain", cancellationToken);
        }

        if (file.MimeType == GoogleSheetMimeType)
        {
            // Google only exports the first sheet of a spreadsheet as CSV
            return await ExportAsTextAsync(file.Id, "text/csv", cancellationToken);
        }

        using var stream = await DownloadFileAsync(file.Id, cancellationToken);

        if (file.MimeType == PdfMimeType)
        {
            return ExtractPdfText(stream);
        }

        if (file.MimeType == DocxMimeType)
        {
            return ExtractDocxText(stream);
        }

        // Plain text formats (text/*, JSON, XML) are read as UTF-8
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task<List<GoogleFile>> ListFilesAsync(string query, CancellationToken cancellationToken)
    {
        var files = new List<GoogleFile>();
        string? pageToken = null;

        do
        {
            var request = _driveService.Files.List();

            if (_sharedDriveId != null)
            {
                // Only list files from the configured shared drive
                request.Corpora = "drive";
                request.DriveId = _sharedDriveId;
                request.SupportsAllDrives = true;
                request.IncludeItemsFromAllDrives = true;
            }

            request.Q = query;
            request.Fields = FileFields;
            request.PageSize = 1000;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(cancellationToken);

            files.AddRange(response.Files);
            pageToken = response.NextPageToken;
        }
        while (pageToken != null);

        return files;
    }

    private async Task<string> ExportAsTextAsync(string fileId, string exportMimeType, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();

        var progress = await _driveService.Files.Export(fileId, exportMimeType).DownloadAsync(stream, cancellationToken);
        ThrowIfDownloadFailed(progress, fileId);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task<MemoryStream> DownloadFileAsync(string fileId, CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();

        var request = _driveService.Files.Get(fileId);
        request.SupportsAllDrives = true;

        var progress = await request.DownloadAsync(stream, cancellationToken);
        ThrowIfDownloadFailed(progress, fileId);

        stream.Position = 0;
        return stream;
    }

    private static void ThrowIfDownloadFailed(IDownloadProgress progress, string fileId)
    {
        if (progress.Status == DownloadStatus.Failed)
        {
            throw new InvalidOperationException($"Download of Drive file {fileId} failed.", progress.Exception);
        }
    }

    private static string ExtractPdfText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);

        var pageTexts = document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page));

        return string.Join("\n", pageTexts);
    }

    private static string ExtractDocxText(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);

        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
        {
            return string.Empty;
        }

        var paragraphTexts = body.Descendants<Paragraph>().Select(paragraph => paragraph.InnerText);

        return string.Join("\n", paragraphTexts);
    }
}
