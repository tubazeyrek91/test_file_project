using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.ConsoleApp.Menus;

public static class DownloadMenu
{
    public static async Task DrawAsync(
        IFileDownloadService downloadService,
        IMetadataRepository metadataRepo,
        ILogger logger)
    {
        var files = await metadataRepo.GetAllAsync();

        if (!files.Any())
        {
            logger.LogWarning("{@LogCategory} | İndirilecek dosya bulunamadı.", LogCategory.Download);
            Console.WriteLine("İndirilecek dosya bulunamadı.");
            return;
        }

        logger.LogInformation("{@LogCategory} | {Count} dosya listelendi.", LogCategory.Download, files.Count);

        Console.WriteLine("İndirmek için dosya seçimi yapınız:");

        for (int i = 0; i < files.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {files[i].OriginalFileName}");
        }

        Console.Write("Seçim: ");
        if (!int.TryParse(Console.ReadLine(), out var selection))
        {
            logger.LogWarning("{@LogCategory} | Geçersiz input girildi.", LogCategory.Download);
            return;
        }

        var file = files.ElementAtOrDefault(selection - 1);
        if (file == null)
        {
            logger.LogWarning("{@LogCategory} | Geçersiz seçim: {Selection}", LogCategory.Download, selection);
            Console.WriteLine("Geçersiz seçim.");
            return;
        }

        var targetPath = $"reconstructed_{file.OriginalFileName}";

        logger.LogInformation("{@LogCategory} | Dosya indirme başlatılıyor. Dosya: {FileName}, FileId: {FileId}",
            LogCategory.Download, file.OriginalFileName, file.FileId);

        await downloadService.DownloadAsync(file.FileId, targetPath);

        logger.LogInformation("{@LogCategory} | Dosya indirildi. Hedef: {TargetPath}", LogCategory.Download, targetPath);

        Console.WriteLine($"İndirilen dosya: {targetPath}");
    }
}
