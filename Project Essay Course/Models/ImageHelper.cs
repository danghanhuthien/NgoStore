namespace Project_Essay_Course.Helpers
{
    /// <summary>
    /// Helper dùng chung cho upload/xóa ảnh.
    /// Tất cả ảnh đều lưu vào wwwroot/uploads/{folder}/
    /// Trả về relative path để lưu vào DB.
    /// </summary>
    public static class ImageHelper
    {
        // Các extension được phép upload
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        // Giới hạn 5MB mỗi file
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Upload 1 file ảnh.
        /// </summary>
        /// <param name="file">IFormFile từ form</param>
        /// <param name="webRootPath">IWebHostEnvironment.WebRootPath</param>
        /// <param name="folder">Thư mục con trong uploads, VD: "products", "categories"</param>
        /// <returns>Relative path VD: "/uploads/products/guid.jpg"</returns>
        public static async Task<(bool Success, string? Path, string? Error)> UploadAsync(
            IFormFile file,
            string webRootPath,
            string folder = "products")
        {
            // Validate
            if (file == null || file.Length == 0)
                return (false, null, "File không hợp lệ.");

            if (file.Length > MaxFileSizeBytes)
                return (false, null, "File vượt quá 5MB.");

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return (false, null, "Chỉ chấp nhận file .jpg, .jpeg, .png, .webp.");

            // Tạo thư mục nếu chưa có
            var uploadDir = Path.Combine(webRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadDir);

            // Tên file = GUID + extension, tránh trùng và tránh path traversal
            var fileName  = $"{Guid.NewGuid()}{ext.ToLower()}";
            var fullPath  = Path.Combine(uploadDir, fileName);
            var relativePath = $"/uploads/{folder}/{fileName}";

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, relativePath, null);
        }

        /// <summary>
        /// Upload nhiều file cùng lúc.
        /// </summary>
        public static async Task<List<(bool Success, string? Path, string? Error)>> UploadManyAsync(
            IEnumerable<IFormFile> files,
            string webRootPath,
            string folder = "products")
        {
            var results = new List<(bool, string?, string?)>();
            foreach (var file in files)
            {
                var result = await UploadAsync(file, webRootPath, folder);
                results.Add(result);
            }
            return results;
        }

        /// <summary>
        /// Xóa file ảnh khỏi wwwroot.
        /// Bỏ qua nếu file không tồn tại.
        /// </summary>
        /// <param name="relativePath">VD: "/uploads/products/guid.jpg"</param>
        /// <param name="webRootPath">IWebHostEnvironment.WebRootPath</param>
        public static void Delete(string? relativePath, string webRootPath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            // Chặn path traversal
            var safePath = relativePath.TrimStart('/').Replace("..", "");
            var fullPath = Path.Combine(webRootPath, safePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// Xóa nhiều file cùng lúc.
        /// </summary>
        public static void DeleteMany(IEnumerable<string?> relativePaths, string webRootPath)
        {
            foreach (var path in relativePaths)
                Delete(path, webRootPath);
        }
    }
}
