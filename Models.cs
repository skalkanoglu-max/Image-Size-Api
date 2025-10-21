namespace ImageOptimizeApi
{
    public class Models
    {
        public record OptimizeRequest(
        string ImageBase64,   // "data:image/...;base64,..." veya çıplak base64
        int MaxWidth = 1024,
        int MaxHeight = 1024,
        int Quality = 80,     // 1-100 (JPEG için)
        string Format = "jpeg" // "jpeg" | "png"
        );

        public record OptimizeResponse(string OptimizedBase64);
    }
}
