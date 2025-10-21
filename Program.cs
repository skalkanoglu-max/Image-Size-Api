using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using static ImageOptimizeApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Big request body limit (�rne�in 10 MB)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

var app = builder.Build();

// Basit sa�l�k kontrol�
app.MapGet("/", () => "OK");

// HTTPS y�nlendirmesi (iste�e ba�l� ama kals�n)
app.UseHttpsRedirection();

// Authorization kullanm�yoruz, bu y�zden UseAuthorization da yok!
// app.UseAuthorization();

// Optimize endpoint
app.MapPost("/api/optimize", async (OptimizeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.ImageBase64))
        return Results.BadRequest("ImageBase64 bo�.");

    // "data:image/..;base64,AA..." ise prefix�i ay�kla
    var idx = req.ImageBase64.IndexOf("base64,");
    var pureBase64 = idx >= 0 ? req.ImageBase64[(idx + 7)..] : req.ImageBase64;

    byte[] bytes;
    try { bytes = Convert.FromBase64String(pureBase64); }
    catch { return Results.BadRequest("Ge�ersiz base64."); }

    // G�venlik: �st boyut s�n�r�
    if (bytes.Length > 8 * 1024 * 1024)
        return Results.BadRequest("Dosya �ok b�y�k (limit 8MB).");

    using var inStream = new MemoryStream(bytes);
    using var image = await Image.LoadAsync(inStream);

    // Oran� koruyarak MaxWidth x MaxHeight kutusuna s��d�r
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
