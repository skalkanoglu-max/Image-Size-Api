using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using static ImageOptimizeApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Big request body limit (örneðin 10 MB)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

var app = builder.Build();

// Basit saðlýk kontrolü
app.MapGet("/", () => "OK");

// HTTPS yönlendirmesi (isteðe baðlý ama kalsýn)
app.UseHttpsRedirection();

// Authorization kullanmýyoruz, bu yüzden UseAuthorization da yok!
// app.UseAuthorization();

// Optimize endpoint
app.MapPost("/api/optimize", async (OptimizeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.ImageBase64))
        return Results.BadRequest("ImageBase64 boþ.");

    // "data:image/..;base64,AA..." ise prefix’i ayýkla
    var idx = req.ImageBase64.IndexOf("base64,");
    var pureBase64 = idx >= 0 ? req.ImageBase64[(idx + 7)..] : req.ImageBase64;

    byte[] bytes;
    try { bytes = Convert.FromBase64String(pureBase64); }
    catch { return Results.BadRequest("Geçersiz base64."); }

    // Güvenlik: üst boyut sýnýrý
    if (bytes.Length > 8 * 1024 * 1024)
        return Results.BadRequest("Dosya çok büyük (limit 8MB).");

    using var inStream = new MemoryStream(bytes);
    using var image = await Image.LoadAsync(inStream);

    // Oraný koruyarak MaxWidth x MaxHeight kutusuna sýðdýr
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Mode = ResizeMode.Max,
        Size = new Size(req.MaxWidth, req.MaxHeight),
        Sampler = KnownResamplers.Lanczos3
    }));

    using var outStream = new MemoryStream();

    string mime;
    if (req.Format.Equals("png", StringComparison.OrdinalIgnoreCase))
    {
        await image.SaveAsync(outStream, new PngEncoder { CompressionLevel = PngCompressionLevel.Level6 });
        mime = "image/png";
    }
    else
    {
        var q = Math.Clamp(req.Quality, 1, 100);
        await image.SaveAsync(outStream, new JpegEncoder { Quality = q });
        mime = "image/jpeg";
    }

    var b64 = Convert.ToBase64String(outStream.ToArray());
    var dataUrl = $"data:{mime};base64,{b64}";

    return Results.Ok(new OptimizeResponse(dataUrl));
})
.WithName("OptimizeImage")
.Produces<OptimizeResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.Run();
