using School.API.DTOs.Academic;

namespace School.API.Services.Implementations.Academic
{
    /// <summary>
    /// An image sent up from the browser, checked before it is stored: it must
    /// really be a JPEG or a PNG (the bytes, not the name), and no larger than
    /// WhatsApp accepts, since every one of these is sent on WhatsApp.
    /// </summary>
    public static class UploadedImage
    {
        /// <summary>WhatsApp's own limit for an image is 5 MB.</summary>
        public const int MaxBytes = 5 * 1024 * 1024;

        public static (byte[] Data, string ContentType, string FileName) Decode(UploadImageDto img)
        {
            byte[] data;
            try
            {
                var b64 = img.Base64 ?? string.Empty;
                var comma = b64.IndexOf(',');
                if (b64.StartsWith("data:") && comma > 0) b64 = b64[(comma + 1)..];   // a data: URL is fine too
                data = Convert.FromBase64String(b64);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"\"{img.FileName}\" could not be read as an image.");
            }

            if (data.Length == 0)
                throw new InvalidOperationException($"\"{img.FileName}\" is empty.");
            if (data.Length > MaxBytes)
                throw new InvalidOperationException($"\"{img.FileName}\" is {data.Length / 1024 / 1024.0:0.0} MB; the limit is 5 MB.");

            // Judged by the bytes. WhatsApp sends JPEG and PNG as pictures;
            // anything else would arrive as a file nobody can open on a phone.
            string type, ext;
            if (data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) { type = "image/jpeg"; ext = ".jpg"; }
            else if (data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) { type = "image/png"; ext = ".png"; }
            else throw new InvalidOperationException($"\"{img.FileName}\" is not a JPEG or PNG picture.");

            var name = Path.GetFileNameWithoutExtension(img.FileName ?? "");
            name = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ' ')).Trim();
            if (name.Length == 0) name = "image";
            if (name.Length > 150) name = name[..150];

            return (data, type, name + ext);
        }
    }
}
