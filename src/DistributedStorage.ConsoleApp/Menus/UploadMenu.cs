using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.ConsoleApp.Menus;

public static class UploadMenu
{
    public static async Task DrawAsync(IFileUploadService uploadService, ILogger logger)
    {
        Console.WriteLine("Yüklemek istediğiniz dosyanın yolunu yazınız. (1' den fazla dosya yüklemek için yollar arasında virgül kullanınız. Örn: c:\\test1.pdf,c:\\test2.pdf ):");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            logger.LogWarning("{@LogCategory} | Boş input girildi, yükleme iptal edildi.", LogCategory.Upload);
            return;
        }

        var allPaths = input.Split(',').Select(f => f.Trim()).ToArray();
        var files = allPaths.Where(File.Exists).ToArray();
        var notFound = allPaths.Except(files).ToArray();

        if (notFound.Length > 0)
            logger.LogWarning("{@LogCategory} | Bulunamayan dosyalar: {NotFound}", LogCategory.Upload, string.Join(", ", notFound));

        if (!files.Any())
        {
            logger.LogWarning("{@LogCategory} | Hiçbir geçerli dosya bulunamadı, yükleme iptal edildi.", LogCategory.Upload);
            Console.WriteLine("Dosya bulunamadı.");
            return;
        }

        logger.LogInformation("{@LogCategory} | {Count} dosya yüklenecek: {Files}", LogCategory.Upload, files.Length, string.Join(", ", files));

        var fileIds = await uploadService.UploadAsync(files);

        logger.LogInformation("{@LogCategory} | Yükleme tamamlandı. {Count} dosya yüklendi.", LogCategory.Upload, fileIds.Count);

        Console.WriteLine("Dosyalar yüklendi. Oluşturulan dosya ID'leri:");
        foreach (var id in fileIds)
            Console.WriteLine(id);
    }
}
