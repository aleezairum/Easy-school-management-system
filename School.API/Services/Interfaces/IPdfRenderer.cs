namespace School.API.Services.Interfaces
{
    // Renders an HTML string into a PDF document (A4). One shared headless
    // browser is reused across calls; implementations must be thread-safe.
    public interface IPdfRenderer
    {
        Task<byte[]> RenderHtmlToPdfAsync(string html);
    }
}
