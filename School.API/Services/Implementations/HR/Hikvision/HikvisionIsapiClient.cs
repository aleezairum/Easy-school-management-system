using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace School.API.Services.Implementations.HR.Hikvision
{
    /// <summary>
    /// Talks to one Hikvision face terminal over ISAPI, the machine's own HTTP
    /// interface, with the machine's admin login (HTTP digest).
    /// </summary>
    /// <remarks>
    /// Only the calls attendance needs: who the machine is, its clock, the
    /// punches it holds, and adding, changing and removing a person, their card,
    /// their face and their fingerprints - and reading the face and fingerprints
    /// back, to keep in EasySchool and put on another machine. The JSON shapes are the MinMoe terminals' (DS-K1T3xx /
    /// DS-K1T6xx, firmware 3.x and later). Every failure comes back as text
    /// that names the call and quotes the machine, for the screen to show.
    /// </remarks>
    public sealed class HikvisionIsapiClient : IDisposable
    {
        private readonly HttpClient _http;

        public HikvisionIsapiClient(string ip, int port, bool https, string username, string? password)
        {
            var baseUri = new Uri($"{(https ? "https" : "http")}://{ip}:{port}/");
            var handler = new HttpClientHandler
            {
                // Digest is answered automatically from these credentials.
                Credentials = new NetworkCredential(username, password ?? string.Empty),
                PreAuthenticate = false,
                // A machine on the school network carries its own self-signed certificate.
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            _http = new HttpClient(handler) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(20) };
        }

        public void Dispose() => _http.Dispose();

        public sealed class IsapiException : Exception
        {
            public string? SubStatus { get; }
            public IsapiException(string message, string? subStatus = null) : base(message) => SubStatus = subStatus;
        }

        // ── Device ─────────────────────────────────────────────────────

        public async Task<(string? Name, string? Model, string? Serial, string? Firmware)> GetDeviceInfoAsync()
        {
            var xml = await SendXmlAsync(HttpMethod.Get, "ISAPI/System/deviceInfo", null);
            string? V(string n) => xml.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value;
            return (V("deviceName"), V("model"), V("serialNumber"), V("firmwareVersion"));
        }

        public async Task<string?> GetDeviceTimeAsync()
        {
            var xml = await SendXmlAsync(HttpMethod.Get, "ISAPI/System/time", null);
            return xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "localTime")?.Value;
        }

        /// <summary>
        /// Sets the machine's clock to <paramref name="local"/> (the server's wall time).
        /// The machine's own time settings are read and sent back with only the mode
        /// (manual) and the time changed, so its time zone and the rest stay as they were.
        /// A machine left on NTP that kept drifting is switched to manual by this.
        /// </summary>
        public async Task SetDeviceTimeAsync(DateTime local)
        {
            var xml = await SendXmlAsync(HttpMethod.Get, "ISAPI/System/time", null);
            XElement Field(string name)
            {
                var e = xml.Elements().FirstOrDefault(x => x.Name.LocalName == name);
                if (e == null) { e = new XElement(xml.Name.Namespace + name); xml.Add(e); }
                return e;
            }
            Field("timeMode").Value = "manual";
            Field("localTime").Value = IsapiTime(local);
            await SendXmlAsync(HttpMethod.Put, "ISAPI/System/time", xml.ToString(SaveOptions.DisableFormatting));
        }

        // ── Punches ────────────────────────────────────────────────────

        public sealed class AcsEvent
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public DateTime Time { get; set; }
            public string? EmployeeNo { get; set; }
            public string? Name { get; set; }
            public long? SerialNo { get; set; }
            public string? VerifyMode { get; set; }
        }

        /// <summary>
        /// Every access event (major 5) between two local times, page by page.
        /// The machine answers at most a few dozen per call and says MORE while
        /// there are others.
        /// </summary>
        public async Task<List<AcsEvent>> SearchEventsAsync(DateTime from, DateTime to, int maxPages = 200)
        {
            var all = new List<AcsEvent>();
            var searchId = Guid.NewGuid().ToString("N")[..16];
            var position = 0;

            for (var page = 0; page < maxPages; page++)
            {
                var body = new
                {
                    AcsEventCond = new
                    {
                        searchID = searchId,
                        searchResultPosition = position,
                        maxResults = 30,
                        major = 5,
                        minor = 0,
                        startTime = IsapiTime(from),
                        endTime = IsapiTime(to)
                    }
                };
                using var doc = await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/AcsEvent?format=json", body);
                if (!doc.RootElement.TryGetProperty("AcsEvent", out var acs)) break;

                var count = 0;
                if (acs.TryGetProperty("InfoList", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in list.EnumerateArray())
                    {
                        all.Add(ParseEvent(e));
                        count++;
                    }
                }

                var status = acs.TryGetProperty("responseStatusStrg", out var s) ? s.GetString() : null;
                position += count;
                if (count == 0 || !string.Equals(status, "MORE", StringComparison.OrdinalIgnoreCase)) break;
            }
            return all;
        }

        /// <summary>One event, from a search result or a pushed AccessControllerEvent.</summary>
        public static AcsEvent ParseEvent(JsonElement e, string? fallbackTime = null)
        {
            int Int(params string[] names)
            {
                foreach (var n in names)
                    if (e.TryGetProperty(n, out var v))
                    {
                        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
                        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var j)) return j;
                    }
                return 0;
            }
            string? Str(params string[] names)
            {
                foreach (var n in names)
                    if (e.TryGetProperty(n, out var v))
                    {
                        var s = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Number ? v.GetRawText() : null;
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                return null;
            }

            long? serial = null;
            if (long.TryParse(Str("serialNo"), out var sn)) serial = sn;

            return new AcsEvent
            {
                Major = Int("major", "majorEventType"),
                Minor = Int("minor", "subEventType"),
                Time = ParseTime(Str("time", "dateTime") ?? fallbackTime) ?? DateTime.Now,
                EmployeeNo = Str("employeeNoString", "employeeNo"),
                Name = Str("name"),
                SerialNo = serial,
                VerifyMode = Str("currentVerifyMode")
            };
        }

        /// <summary>
        /// The machine's own wall-clock time. Its offset is dropped on purpose:
        /// the machine and the school keep the same local time, and attendance
        /// is judged on the clock on the wall, not converted.
        /// </summary>
        public static DateTime? ParseTime(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dto))
                return dto.DateTime;
            return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;
        }

        private static string IsapiTime(DateTime local)
        {
            var off = TimeZoneInfo.Local.GetUtcOffset(local);
            return local.ToString("yyyy-MM-dd'T'HH:mm:ss") + (off < TimeSpan.Zero ? "-" : "+") + off.ToString(@"hh\:mm");
        }

        // ── People ─────────────────────────────────────────────────────

        /// <summary>Adds the person, or changes their name when they are already there.</summary>
        public async Task UpsertUserAsync(string employeeNo, string name)
        {
            var body = new
            {
                UserInfo = new
                {
                    employeeNo,
                    name = Clip(name, 32),
                    userType = "normal",
                    Valid = new { enable = true, beginTime = "2020-01-01T00:00:00", endTime = "2037-12-31T23:59:59", timeType = "local" },
                    doorRight = "1",
                    RightPlan = new[] { new { doorNo = 1, planTemplateNo = "1" } }
                }
            };
            try
            {
                using var _ = await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/UserInfo/Record?format=json", body);
            }
            catch (IsapiException ex) when (ex.SubStatus?.Contains("AlreadyExist", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var _ = await SendJsonAsync(HttpMethod.Put, "ISAPI/AccessControl/UserInfo/Modify?format=json", body);
            }
        }

        public async Task DeleteUserAsync(string employeeNo)
        {
            var body = new { UserInfoDelCond = new { EmployeeNoList = new[] { new { employeeNo } } } };
            using var _ = await SendJsonAsync(HttpMethod.Put, "ISAPI/AccessControl/UserInfo/Delete?format=json", body);
        }

        public async Task SetCardAsync(string employeeNo, string cardNo)
        {
            var body = new { CardInfo = new { employeeNo, cardNo, cardType = "normalCard" } };
            try
            {
                using var _ = await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/CardInfo/Record?format=json", body);
            }
            catch (IsapiException ex) when (ex.SubStatus?.Contains("AlreadyExist", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var _ = await SendJsonAsync(HttpMethod.Put, "ISAPI/AccessControl/CardInfo/Modify?format=json", body);
            }
        }

        /// <summary>
        /// Puts a face photo (JPEG) on the person. FDSetUp adds or replaces in
        /// one call; older firmware lacks it, so FaceDataRecord, then FDModify
        /// when the person already has a face.
        /// </summary>
        public async Task UploadFaceAsync(string employeeNo, byte[] jpeg)
        {
            var meta = JsonSerializer.Serialize(new { faceLibType = "blackFD", FDID = "1", FPID = employeeNo });
            try
            {
                await SendFaceAsync(HttpMethod.Put, "ISAPI/Intelligent/FDLib/FDSetUp?format=json", meta, jpeg);
                return;
            }
            catch (IsapiException ex) when (IsNotSupported(ex)) { }

            try
            {
                await SendFaceAsync(HttpMethod.Post, "ISAPI/Intelligent/FDLib/FaceDataRecord?format=json", meta, jpeg);
            }
            catch (IsapiException ex) when (ex.SubStatus?.Contains("exist", StringComparison.OrdinalIgnoreCase) == true)
            {
                await SendFaceAsync(HttpMethod.Put, "ISAPI/Intelligent/FDLib/FDModify?format=json", meta, jpeg);
            }
        }

        private static bool IsNotSupported(IsapiException ex) =>
            ex.Message.Contains("HTTP 404") || ex.Message.Contains("HTTP 405")
            || (ex.SubStatus?.Contains("notSupport", StringComparison.OrdinalIgnoreCase) ?? false)
            || ex.Message.Contains("notSupport", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Invalid Operation", StringComparison.OrdinalIgnoreCase);

        private async Task SendFaceAsync(HttpMethod method, string path, string metaJson, byte[] jpeg)
        {
            using var form = new MultipartFormDataContent();
            var meta = new StringContent(metaJson, Encoding.UTF8, "application/json");
            form.Add(meta, "FaceDataRecord");
            var img = new ByteArrayContent(jpeg);
            img.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(img, "img", "face.jpg");
            await SendAsync(method, path, form);
        }

        // ── Faces and fingerprints, read back from the machine ─────────

        /// <summary>
        /// The person's face as the machine holds it (JPEG), or null when they
        /// have none. FDSearch gives the picture's address on the machine; the
        /// picture is then fetched from there with the same login.
        /// </summary>
        public async Task<byte[]?> DownloadFaceAsync(string employeeNo)
        {
            var body = new { searchResultPosition = 0, maxResults = 5, faceLibType = "blackFD", FDID = "1", FPID = employeeNo };
            using var doc = await SendJsonAsync(HttpMethod.Post, "ISAPI/Intelligent/FDLib/FDSearch?format=json", body);
            if (!doc.RootElement.TryGetProperty("MatchList", out var list) || list.ValueKind != JsonValueKind.Array) return null;

            foreach (var m in list.EnumerateArray())
            {
                // Without the FPID filter some firmware returns other people as well.
                if (m.TryGetProperty("FPID", out var id) && !string.Equals(id.GetString(), employeeNo, StringComparison.Ordinal)) continue;
                var url = m.TryGetProperty("faceURL", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(url)) continue;

                // The address names the machine as it knows itself, which may not be
                // how this server reaches it (a different IP, a forwarded port):
                // only its path is used, on the connection already open.
                var path = Uri.TryCreate(url, UriKind.Absolute, out var abs) ? abs.PathAndQuery : url;
                var jpeg = await GetBytesAsync(path.TrimStart('/'));
                return jpeg.Length > 3 && jpeg[0] == 0xFF && jpeg[1] == 0xD8 ? jpeg : null;
            }
            return null;
        }

        public sealed class Fingerprint
        {
            public int FingerNo { get; set; }
            public string FingerType { get; set; } = "normalFP";
            public string Data { get; set; } = string.Empty;
        }

        /// <summary>
        /// Every fingerprint template the machine holds for the person (base64).
        /// The machine may answer "processing" first; it is asked again until done.
        /// </summary>
        public async Task<List<Fingerprint>> GetFingerprintsAsync(string employeeNo)
        {
            var found = new List<Fingerprint>();
            var searchId = Guid.NewGuid().ToString("N")[..16];
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var body = new { FingerPrintCond = new { searchID = searchId, employeeNo } };
                using var doc = await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/FingerPrintUpload?format=json", body);
                if (!doc.RootElement.TryGetProperty("FingerPrintInfo", out var info)) break;

                if (info.TryGetProperty("FingerPrintList", out var list) && list.ValueKind == JsonValueKind.Array)
                    foreach (var f in list.EnumerateArray())
                    {
                        var data = f.TryGetProperty("fingerData", out var d) ? d.GetString() : null;
                        var no = f.TryGetProperty("fingerPrintID", out var n) && n.TryGetInt32(out var i) ? i : 0;
                        if (string.IsNullOrWhiteSpace(data) || no < 1) continue;
                        found.Add(new Fingerprint
                        {
                            FingerNo = no,
                            FingerType = f.TryGetProperty("fingerType", out var t) && !string.IsNullOrWhiteSpace(t.GetString()) ? t.GetString()! : "normalFP",
                            Data = data
                        });
                    }

                var status = info.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (!string.Equals(status, "processing", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(status, "NoFinish", StringComparison.OrdinalIgnoreCase)) break;
                await Task.Delay(300);
            }
            return found.GroupBy(f => f.FingerNo).Select(g => g.Last()).OrderBy(f => f.FingerNo).ToList();
        }

        /// <summary>
        /// Enrols one fingerprint template on the person (added, or replaced if
        /// that finger number is already there). FingerPrint/SetUp does it in one
        /// call; older firmware takes FingerPrintDownload and reports through
        /// FingerPrintProgress.
        /// </summary>
        public async Task SetFingerprintAsync(string employeeNo, int fingerNo, string fingerType, string data)
        {
            var cfg = new
            {
                FingerPrintCfg = new
                {
                    employeeNo,
                    enableCardReader = new[] { 1 },
                    fingerPrintID = fingerNo,
                    deleteFingerPrint = false,
                    fingerType = string.IsNullOrWhiteSpace(fingerType) ? "normalFP" : fingerType,
                    fingerData = data
                }
            };
            try
            {
                using var doc = await SendJsonAsync(HttpMethod.Put, "ISAPI/AccessControl/FingerPrint/SetUp?format=json", cfg);
                CheckFingerStatus(doc.RootElement, fingerNo);
                return;
            }
            catch (IsapiException ex) when (IsNotSupported(ex)) { }

            using (await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/FingerPrintDownload?format=json", cfg)) { }
            for (var i = 0; i < 20; i++)
            {
                await Task.Delay(300);
                using var doc = await SendJsonAsync(HttpMethod.Get, "ISAPI/AccessControl/FingerPrintProgress?format=json", null);
                if (doc.RootElement.TryGetProperty("FingerPrintStatus", out var st)
                    && st.TryGetProperty("totalStatus", out var ts) && ts.TryGetInt32(out var total) && total == 1)
                {
                    CheckFingerStatus(doc.RootElement, fingerNo);
                    return;
                }
            }
            throw new IsapiException($"FingerPrintDownload: finger {fingerNo} was not stored - the machine did not finish in time.");
        }

        // The machine answers per fingerprint reader; anything but 1 (stored) or
        // 5 (that very fingerprint is already there) is a failure worth reading.
        private static void CheckFingerStatus(JsonElement root, int fingerNo)
        {
            if (!root.TryGetProperty("FingerPrintStatus", out var st) || !st.TryGetProperty("StatusList", out var list)
                || list.ValueKind != JsonValueKind.Array) return;
            foreach (var r in list.EnumerateArray())
            {
                var code = r.TryGetProperty("cardReaderRecvStatus", out var c) && c.TryGetInt32(out var v) ? v : 1;
                if (code is 1 or 5) continue;
                var why = code switch
                {
                    0 => "could not reach its fingerprint reader",
                    2 => "its fingerprint reader is offline",
                    3 => "the template was not accepted (poor quality) - enrol this finger on the machine",
                    4 => "its fingerprint memory is full",
                    6 => "this fingerprint is already enrolled under another person",
                    7 => "the finger number is not valid",
                    10 => "its fingerprint reader firmware is too old",
                    _ => $"error {code}"
                };
                var msg = r.TryGetProperty("errorMsg", out var em) ? em.GetString() : null;
                throw new IsapiException($"Finger {fingerNo}: the machine says {why}{(string.IsNullOrWhiteSpace(msg) ? "" : $" ({msg})")}.");
            }
        }

        public sealed class DeviceUser
        {
            public string EmployeeNo { get; set; } = string.Empty;
            public string? Name { get; set; }
            public int Faces { get; set; }
            public int Cards { get; set; }
            public int Fingerprints { get; set; }
        }

        /// <summary>Everybody on the machine, page by page.</summary>
        public async Task<List<DeviceUser>> ListUsersAsync(int maxPages = 500)
        {
            var users = new List<DeviceUser>();
            var searchId = Guid.NewGuid().ToString("N")[..16];
            for (var page = 0; page < maxPages; page++)
            {
                var body = new { UserInfoSearchCond = new { searchID = searchId, searchResultPosition = users.Count, maxResults = 30 } };
                using var doc = await SendJsonAsync(HttpMethod.Post, "ISAPI/AccessControl/UserInfo/Search?format=json", body);
                if (!doc.RootElement.TryGetProperty("UserInfoSearch", out var r)) break;

                var count = 0;
                if (r.TryGetProperty("UserInfo", out var list) && list.ValueKind == JsonValueKind.Array)
                    foreach (var u in list.EnumerateArray())
                    {
                        int N(string p) => u.TryGetProperty(p, out var v) && v.TryGetInt32(out var i) ? i : 0;
                        users.Add(new DeviceUser
                        {
                            EmployeeNo = u.TryGetProperty("employeeNo", out var no) ? no.GetString() ?? "" : "",
                            Name = u.TryGetProperty("name", out var nm) ? nm.GetString() : null,
                            Faces = N("numOfFace"),
                            Cards = N("numOfCard"),
                            Fingerprints = N("numOfFP")
                        });
                        count++;
                    }

                var status = r.TryGetProperty("responseStatusStrg", out var s) ? s.GetString() : null;
                if (count == 0 || !string.Equals(status, "MORE", StringComparison.OrdinalIgnoreCase)) break;
            }
            return users;
        }

        // ── Transport ──────────────────────────────────────────────────

        private async Task<JsonDocument> SendJsonAsync(HttpMethod method, string path, object? body)
        {
            var content = body == null ? null : new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var text = await SendAsync(method, path, content);
            if (string.IsNullOrWhiteSpace(text)) return JsonDocument.Parse("{}");
            try { return JsonDocument.Parse(text); }
            catch (JsonException) { return JsonDocument.Parse("{}"); }
        }

        private async Task<XElement> SendXmlAsync(HttpMethod method, string path, string? body)
        {
            var text = await SendAsync(method, path, body == null ? null : new StringContent(body, Encoding.UTF8, "application/xml"));
            try { return XElement.Parse(text); }
            catch (Exception) { throw new IsapiException($"{path}: the machine did not answer with XML."); }
        }

        private async Task<string> SendAsync(HttpMethod method, string path, HttpContent? content)
        {
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(method, path) { Content = content };
                response = await _http.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new IsapiException($"{Short(path)}: the machine did not answer in 20 seconds. Check it is on and the IP address is right.");
            }
            catch (HttpRequestException ex)
            {
                throw new IsapiException($"{Short(path)}: cannot reach the machine ({ex.Message}).");
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new IsapiException($"{Short(path)}: the machine refused the user name or password.");

                // ISAPI reports an error in the body as well as the status:
                // JSON statusCode/subStatusCode, or XML ResponseStatus.
                var (failed, sub, msg) = ReadStatus(text);
                if (!response.IsSuccessStatusCode || failed)
                    throw new IsapiException(
                        $"{Short(path)}: HTTP {(int)response.StatusCode} {msg ?? Clip(text, 200)}".Trim(), sub);

                return text;
            }
        }

        /// <summary>A file the machine serves (a face picture), as bytes.</summary>
        private async Task<byte[]> GetBytesAsync(string path)
        {
            try
            {
                using var response = await _http.GetAsync(path);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new IsapiException("Face picture: the machine refused the user name or password.");
                if (!response.IsSuccessStatusCode)
                    throw new IsapiException($"Face picture: HTTP {(int)response.StatusCode} from the machine.");
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (TaskCanceledException) { throw new IsapiException("Face picture: the machine did not answer in 20 seconds."); }
            catch (HttpRequestException ex) { throw new IsapiException($"Face picture: cannot reach the machine ({ex.Message})."); }
        }

        private static (bool Failed, string? Sub, string? Message) ReadStatus(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return (false, null, null);
            var t = text.TrimStart();
            try
            {
                if (t.StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(t);
                    var r = doc.RootElement;
                    if (!r.TryGetProperty("statusCode", out var sc)) return (false, null, null);
                    var code = sc.ValueKind == JsonValueKind.Number ? sc.GetInt32() : 1;
                    var sub = r.TryGetProperty("subStatusCode", out var ss) ? ss.GetString() : null;
                    var msg = r.TryGetProperty("errorMsg", out var em) ? em.GetString()
                            : r.TryGetProperty("statusString", out var st) ? st.GetString() : null;
                    return (code != 1, sub, $"{msg} {sub}".Trim());
                }
                if (t.StartsWith("<") && t.Contains("ResponseStatus"))
                {
                    var x = XElement.Parse(t);
                    string? V(string n) => x.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value
                                          ?? (x.Name.LocalName == n ? x.Value : null);
                    var code = V("statusCode");
                    var sub = V("subStatusCode");
                    return (code != null && code != "1", sub, $"{V("statusString")} {sub}".Trim());
                }
            }
            catch (Exception) { }
            return (false, null, null);
        }

        private static string Short(string path) => path.Split('?')[0].Replace("ISAPI/", "");
        private static string Clip(string? s, int max) => string.IsNullOrEmpty(s) ? string.Empty : s.Length <= max ? s : s[..max];
    }
}
