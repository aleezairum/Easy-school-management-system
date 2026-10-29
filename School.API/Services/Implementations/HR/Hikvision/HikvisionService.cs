using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Implementations.Academic;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR.Hikvision
{
    public class HikvisionService : IHikvisionService
    {
        private readonly IHikvisionRepository _repo;
        private readonly HikvisionEventProcessor _processor;
        private readonly ILogger<HikvisionService> _logger;

        public HikvisionService(IHikvisionRepository repo, HikvisionEventProcessor processor, ILogger<HikvisionService> logger)
        {
            _repo = repo;
            _processor = processor;
            _logger = logger;
        }

        private static HikvisionIsapiClient ClientFor(HrAttendanceDevice d) =>
            new(d.IpAddress, d.Port, d.UseHttps, d.Username, d.Password);

        // ── Machines ───────────────────────────────────────────────────

        public Task<List<AttendanceDeviceDto>> GetDevicesAsync() => _repo.GetDevicesAsync();

        public async Task<AttendanceDeviceDto> SaveDeviceAsync(int? id, SaveAttendanceDeviceDto dto)
        {
            var name = dto.Name?.Trim() ?? "";
            var ip = dto.IpAddress?.Trim() ?? "";
            if (name.Length == 0) throw new InvalidOperationException("Give the machine a name, e.g. Boys Campus Gate.");
            if (ip.Length == 0) throw new InvalidOperationException("Enter the machine's IP address, e.g. 192.168.1.64.");
            if (ip.Contains("://")) ip = new Uri(ip).Host;
            if (dto.Port is < 1 or > 65535) throw new InvalidOperationException("The port must be between 1 and 65535 (usually 80).");

            HrAttendanceDevice d;
            if (id is > 0)
            {
                d = await _repo.FindDeviceAsync(id.Value) ?? throw new KeyNotFoundException();
                d.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                d = new HrAttendanceDevice { PushKey = NewKey(), CreatedAt = DateTime.UtcNow };
            }

            d.Name = name;
            d.CampusId = dto.CampusId is > 0 ? dto.CampusId : null;
            d.IpAddress = ip;
            d.Port = dto.Port;
            d.UseHttps = dto.UseHttps;
            d.Username = string.IsNullOrWhiteSpace(dto.Username) ? "admin" : dto.Username.Trim();
            if (!string.IsNullOrEmpty(dto.Password)) d.Password = dto.Password;
            d.IsActive = dto.IsActive;
            d.PullEnabled = dto.PullEnabled;

            if (d.Id == 0) await _repo.AddDeviceAsync(d);
            else await _repo.SaveChangesAsync();

            return (await _repo.GetDevicesAsync()).First(x => x.Id == d.Id);
        }

        private static string NewKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        public Task<bool> DeleteDeviceAsync(int id) => _repo.DeleteDeviceAsync(id);

        public async Task<DeviceTestResultDto> TestDeviceAsync(int id)
        {
            var d = await _repo.FindDeviceAsync(id) ?? throw new KeyNotFoundException();
            var result = new DeviceTestResultDto { ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            using var client = ClientFor(d);
            try
            {
                var info = await client.GetDeviceInfoAsync();
                result.DeviceName = info.Name;
                result.Model = info.Model;
                result.SerialNumber = info.Serial;
                result.Firmware = info.Firmware;
                try { result.DeviceTime = (await client.GetDeviceTimeAsync())?.Replace("T", " "); } catch (Exception) { }
                try { result.UsersOnDevice = (await client.ListUsersAsync(5)).Count; } catch (Exception) { }

                d.DeviceModel = info.Model;
                d.SerialNumber = info.Serial;
                d.Firmware = info.Firmware;
                d.LastSeenAt = DateTime.UtcNow;
                d.LastError = null;
                result.Ok = true;
                result.Message = "Connected.";
            }
            catch (HikvisionIsapiClient.IsapiException ex)
            {
                d.LastError = ex.Message;
                result.Message = ex.Message;
            }
            await _repo.SaveChangesAsync();
            return result;
        }

        // ── Clocks ─────────────────────────────────────────────────────
        // A punch carries the machine's own time, so a machine running fast or
        // slow gives staff the wrong time in and out. The server's clock is the
        // reference: Windows keeps it right from the internet.

        public async Task<List<DeviceClockDto>> GetClocksAsync()
        {
            var list = new List<DeviceClockDto>();
            foreach (var row in (await _repo.GetDevicesAsync()).Where(x => x.IsActive))
            {
                var d = await _repo.FindDeviceAsync(row.Id);
                if (d != null) list.Add(await ReadClockAsync(d, row.CampusName));
            }
            return list;
        }

        public async Task<DeviceClockDto> CorrectClockAsync(int id)
        {
            var d = await _repo.FindDeviceAsync(id) ?? throw new KeyNotFoundException();
            var campus = (await _repo.GetDevicesAsync()).FirstOrDefault(x => x.Id == id)?.CampusName;
            return await SetClockAsync(d, campus);
        }

        public async Task<List<DeviceClockDto>> CorrectClocksAsync(int maxDriftSeconds)
        {
            var result = new List<DeviceClockDto>();
            foreach (var row in (await _repo.GetDevicesAsync()).Where(x => x.IsActive))
            {
                var d = await _repo.FindDeviceAsync(row.Id);
                if (d == null) continue;
                var clock = await ReadClockAsync(d, row.CampusName);
                if (clock.DiffSeconds is int diff && Math.Abs(diff) > maxDriftSeconds)
                {
                    clock = await SetClockAsync(d, row.CampusName);
                    _logger.LogInformation("[Hikvision] {Device}: clock was {Diff}s off the server, corrected ({Error}).",
                        d.Name, diff, clock.Error ?? "ok");
                }
                result.Add(clock);
            }
            return result;
        }

        private async Task<DeviceClockDto> ReadClockAsync(HrAttendanceDevice d, string? campus)
        {
            var c = new DeviceClockDto { DeviceId = d.Id, Name = d.Name, Campus = campus };
            using var client = ClientFor(d);
            try
            {
                // The server's time taken half way through the round trip.
                var before = DateTime.Now;
                var text = await client.GetDeviceTimeAsync();
                var server = before + (DateTime.Now - before) / 2;
                c.ServerTime = server.ToString("yyyy-MM-dd HH:mm:ss");
                var device = HikvisionIsapiClient.ParseTime(text);
                if (device == null) { c.Error = "The machine did not say its time."; return c; }
                c.DeviceTime = device.Value.ToString("yyyy-MM-dd HH:mm:ss");
                c.DiffSeconds = (int)Math.Round((device.Value - server).TotalSeconds);
            }
            catch (HikvisionIsapiClient.IsapiException ex)
            {
                c.ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                c.Error = ex.Message;
            }
            return c;
        }

        private async Task<DeviceClockDto> SetClockAsync(HrAttendanceDevice d, string? campus)
        {
            using (var client = ClientFor(d))
            {
                try { await client.SetDeviceTimeAsync(DateTime.Now); }
                catch (HikvisionIsapiClient.IsapiException ex)
                {
                    var failed = await ReadClockAsync(d, campus);
                    failed.Error = "Could not set the clock: " + ex.Message;
                    return failed;
                }
            }
            var after = await ReadClockAsync(d, campus);
            after.Corrected = after.Error == null;
            return after;
        }

        // ── Punches ────────────────────────────────────────────────────

        public async Task<DevicePullResultDto> PullAsync(int id, DateTime? from = null)
        {
            var d = await _repo.FindDeviceAsync(id) ?? throw new KeyNotFoundException();
            return await PullDeviceAsync(d, from);
        }

        public async Task PullAllAsync()
        {
            foreach (var d in await _repo.GetPullableDevicesAsync())
            {
                try { await PullDeviceAsync(d, null); }
                catch (Exception ex) { _logger.LogWarning(ex, "[Hikvision] Pull from {Device} failed", d.Name); }
            }
        }

        private async Task<DevicePullResultDto> PullDeviceAsync(HrAttendanceDevice d, DateTime? from)
        {
            // From a little before the newest punch already read, so one that
            // reached the machine late is not skipped; repeats are dropped.
            // The first read of a machine starts at the beginning of today.
            var start = from ?? (d.LastEventTime?.AddMinutes(-10) ?? DateTime.Today);
            var end = DateTime.Now.AddMinutes(1);
            var result = new DevicePullResultDto();

            using var client = ClientFor(d);
            try
            {
                var events = await client.SearchEventsAsync(start, end);
                result.Read = events.Count;
                foreach (var ev in events.OrderBy(e => e.Time).ThenBy(e => e.SerialNo))
                {
                    if (await _processor.ProcessAsync(d.Id, ev, "pull"))
                    {
                        result.New++;
                        if (!string.IsNullOrEmpty(ev.EmployeeNo)) result.Accepted++;
                    }
                    if (d.LastEventTime == null || ev.Time > d.LastEventTime) d.LastEventTime = ev.Time;
                }
                d.LastSeenAt = DateTime.UtcNow;
                d.LastError = null;
                result.Ok = true;
                result.Message = $"Read {result.Read} punch(es) from {start:dd-MMM HH:mm}; {result.New} new.";
            }
            catch (HikvisionIsapiClient.IsapiException ex)
            {
                d.LastError = ex.Message;
                result.Message = ex.Message;
            }
            d.LastPullAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync();
            return result;
        }

        /// <summary>
        /// The machine posts its event to the address in its HTTP Listening
        /// setting - JSON, or multipart with the JSON in one part (and a
        /// photo in another, ignored). Heartbeats and anything without an
        /// AccessControllerEvent only mark the machine as seen.
        /// </summary>
        public async Task<bool> ReceivePushAsync(string pushKey, string body, string? contentType)
        {
            var d = await _repo.FindDeviceByKeyAsync(pushKey);
            if (d == null) return false;

            d.LastSeenAt = DateTime.UtcNow;
            foreach (var json in JsonParts(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("AccessControllerEvent", out var ace)) continue;
                    var dateTime = root.TryGetProperty("dateTime", out var dt) ? dt.GetString() : null;
                    var ev = HikvisionIsapiClient.ParseEvent(ace, dateTime);
                    await _processor.ProcessAsync(d.Id, ev, "push");
                    if (d.LastEventTime == null || ev.Time > d.LastEventTime) d.LastEventTime = ev.Time;
                }
                catch (JsonException) { }
            }
            await _repo.SaveChangesAsync();
            return true;
        }

        // Each top-level JSON object in a body: a plain JSON body, or the
        // JSON parts of a multipart one.
        private static IEnumerable<string> JsonParts(string body)
        {
            var t = body.Trim();
            var start = 0;
            while (start < t.Length && (start = t.IndexOf('{', start)) >= 0)
            {
                var depth = 0; var inStr = false; var esc = false; var end = -1;
                for (var i = start; i < t.Length && end < 0; i++)
                {
                    var c = t[i];
                    if (inStr) { if (esc) esc = false; else if (c == '\\') esc = true; else if (c == '"') inStr = false; continue; }
                    if (c == '"') inStr = true;
                    else if (c == '{') depth++;
                    else if (c == '}' && --depth == 0) end = i;
                }
                if (end < 0) yield break;           // an unfinished object: nothing more to read
                yield return t[start..(end + 1)];
                start = end + 1;
            }
        }

        public Task<List<DeviceEventRowDto>> GetEventsAsync(int? deviceId, DateTime? date, int take) =>
            _repo.GetEventsAsync(deviceId, date, Math.Clamp(take, 1, 1000));

        // ── People ─────────────────────────────────────────────────────

        public async Task<List<DevicePersonRowDto>> GetPeopleAsync(int deviceId, byte personType, int? classId, int? sectionId)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var type = (DevicePersonType)personType;
            var people = type == DevicePersonType.Staff
                ? await _repo.GetStaffAsync(d.CampusId)
                : await _repo.GetStudentsAsync(d.CampusId, classId, sectionId);
            var onDevice = await _repo.GetDevicePeopleAsync(deviceId);
            var faces = await _repo.GetFacePhotoKeysAsync();
            var fingers = await _repo.GetFingerCountsAsync();

            return people.Select(p =>
            {
                onDevice.TryGetValue((type, p.Id), out var dp);
                return new DevicePersonRowDto
                {
                    PersonType = personType,
                    PersonId = p.Id,
                    EmployeeNo = HikvisionEventProcessor.EmployeeNoFor(type, p.Id),
                    Code = p.Code,
                    Name = p.Name,
                    Detail = p.Detail,
                    OnDevice = dp?.UserSyncedAt != null,
                    FaceOnDevice = dp?.FaceSyncedAt != null,
                    HasFacePhoto = faces.Contains((type, p.Id)),
                    FingersOnDevice = dp?.FingersOnDevice,
                    FingersSaved = fingers.TryGetValue((type, p.Id), out var fc) ? fc : 0,
                    CardNo = dp?.CardNo,
                    SyncedAt = dp?.UserSyncedAt,
                    LastError = dp?.LastError
                };
            }).ToList();
        }

        public async Task<DeviceSyncResultDto> RefreshFromDeviceAsync(int deviceId)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var result = new DeviceSyncResultDto();
            using var client = ClientFor(d);
            List<HikvisionIsapiClient.DeviceUser> users;
            try { users = await client.ListUsersAsync(); }
            catch (HikvisionIsapiClient.IsapiException ex) { result.Failed = 1; result.Errors.Add(ex.Message); return result; }

            var known = await _repo.GetDevicePeopleAsync(deviceId);
            var seen = new HashSet<(DevicePersonType, int)>();
            foreach (var u in users)
            {
                var (type, id) = HikvisionEventProcessor.Identify(u.EmployeeNo);
                if (type == null) continue;
                seen.Add((type.Value, id!.Value));
                var p = await _repo.GetOrAddDevicePersonAsync(deviceId, type.Value, id.Value, u.EmployeeNo);
                p.UserSyncedAt ??= DateTime.UtcNow;
                p.FaceSyncedAt = u.Faces > 0 ? p.FaceSyncedAt ?? DateTime.UtcNow : null;
                p.FingersOnDevice = (byte)Math.Clamp(u.Fingerprints, 0, 255);
                p.UpdatedAt = DateTime.UtcNow;
                result.Done++;
            }
            // Removed on the machine itself: no longer on it.
            foreach (var k in known.Keys.Where(k => !seen.Contains(k)))
                await _repo.RemoveDevicePersonAsync(deviceId, k.Item1, k.Item2);

            d.LastSeenAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync();
            return result;
        }

        public async Task<DeviceSyncResultDto> SyncPeopleAsync(int deviceId, DeviceSyncRequestDto dto)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var result = new DeviceSyncResultDto();
            var names = await NamesAsync(d, dto.People);
            using var client = ClientFor(d);

            foreach (var r in dto.People.DistinctBy(p => (p.PersonType, p.PersonId)))
            {
                var type = (DevicePersonType)r.PersonType;
                var no = HikvisionEventProcessor.EmployeeNoFor(type, r.PersonId);
                if (!names.TryGetValue((type, r.PersonId), out var name))
                {
                    result.Failed++; result.Errors.Add($"{no}: not found on this machine's campus.");
                    continue;
                }

                var p = await _repo.GetOrAddDevicePersonAsync(deviceId, type, r.PersonId, no);
                try
                {
                    await client.UpsertUserAsync(no, name);
                    p.UserSyncedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(p.CardNo)) await client.SetCardAsync(no, p.CardNo);
                    if (dto.IncludeFace && await _repo.GetFacePhotoAsync(type, r.PersonId) is { } face)
                    {
                        await client.UploadFaceAsync(no, face);
                        p.FaceSyncedAt = DateTime.UtcNow;
                    }
                    p.LastError = null;
                    if (dto.IncludeFingers)
                    {
                        // One finger the machine refuses does not hold back the others.
                        var stored = 0;
                        foreach (var f in await _repo.GetFingersAsync(type, r.PersonId))
                        {
                            try { await client.SetFingerprintAsync(no, f.FingerNo, f.FingerType, f.TemplateData); stored++; }
                            catch (HikvisionIsapiClient.IsapiException ex) when (!Unreachable(ex))
                            {
                                p.LastError = Clip(ex.Message, 500);
                                result.Errors.Add($"{name}: {ex.Message}");
                            }
                        }
                        if (stored > 0) { p.FingersOnDevice = (byte)Math.Max(stored, p.FingersOnDevice ?? 0); result.Fingers += stored; }
                    }
                    result.Done++;
                }
                catch (HikvisionIsapiClient.IsapiException ex)
                {
                    p.LastError = Clip(ex.Message, 500);
                    result.Failed++; result.Errors.Add($"{name}: {ex.Message}");
                    // The machine cannot be reached at all: stop, rather than time out on every person.
                    if (Unreachable(ex))
                    {
                        p.UpdatedAt = DateTime.UtcNow;
                        break;
                    }
                }
                p.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync();
            }
            d.LastSeenAt = result.Done > 0 ? DateTime.UtcNow : d.LastSeenAt;
            await _repo.SaveChangesAsync();
            return result;
        }

        private static bool Unreachable(HikvisionIsapiClient.IsapiException ex) =>
            ex.Message.Contains("cannot reach") || ex.Message.Contains("did not answer") || ex.Message.Contains("refused the user");

        public async Task<DeviceSyncResultDto> FetchBiometricsAsync(int deviceId, DeviceSyncRequestDto dto, int? userId) =>
            (await FetchAsync(deviceId, dto, userId)).Result;

        /// <summary>
        /// Reads each person's fingerprint templates and face from the machine and
        /// keeps them in EasySchool: the face as their face photo, the fingerprints
        /// to enrol on any other machine. A member of staff with no picture on the
        /// employee form gets the face as that picture too.
        /// </summary>
        /// <remarks>
        /// With nobody chosen, everyone on the machine who has a face or a
        /// fingerprint is read. A person with neither is reported, not an error.
        /// Fingerprints are replaced only when the machine has some, so templates
        /// read from another machine are not wiped by one that never had them.
        /// </remarks>
        private async Task<(DeviceSyncResultDto Result, List<DevicePersonRefDto> People)> FetchAsync(int deviceId, DeviceSyncRequestDto dto, int? userId)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var result = new DeviceSyncResultDto();
            var read = new List<DevicePersonRefDto>();
            using var client = ClientFor(d);

            List<(DevicePersonType Type, int Id)> people;
            if (dto.People.Count > 0)
                people = dto.People.Select(p => ((DevicePersonType)p.PersonType, p.PersonId)).Distinct().ToList();
            else
            {
                try
                {
                    people = (await client.ListUsersAsync())
                        .Where(u => u.Faces > 0 || u.Fingerprints > 0)
                        .Select(u => HikvisionEventProcessor.Identify(u.EmployeeNo))
                        .Where(x => x.Item1 != null && x.Item2 != null)
                        .Select(x => (x.Item1!.Value, x.Item2!.Value)).Distinct().ToList();
                }
                catch (HikvisionIsapiClient.IsapiException ex) { result.Failed = 1; result.Errors.Add(ex.Message); return (result, read); }
            }

            foreach (var (type, id) in people)
            {
                var no = HikvisionEventProcessor.EmployeeNoFor(type, id);
                try
                {
                    var fingers = await client.GetFingerprintsAsync(no);
                    byte[]? face = null;
                    try { face = await client.DownloadFaceAsync(no); }
                    catch (HikvisionIsapiClient.IsapiException ex) when (!Unreachable(ex)) { result.Errors.Add($"{no}: face not read - {ex.Message}"); }

                    if (fingers.Count == 0 && face == null)
                    {
                        result.Errors.Add($"{no}: no face or fingerprint on this machine.");
                        continue;
                    }

                    if (fingers.Count > 0)
                    {
                        await _repo.ReplaceFingersAsync(type, id, fingers.Select(f => new HrBiometricFinger
                        {
                            PersonType = type, PersonId = id, FingerNo = (byte)f.FingerNo,
                            FingerType = Clip(f.FingerType, 20) ?? "normalFP",
                            TemplateData = f.Data, SourceDeviceId = d.Id, UpdatedAt = DateTime.Now
                        }));
                        result.Fingers += fingers.Count;
                    }
                    if (face != null)
                    {
                        await _repo.SaveFacePhotoAsync(type, id, face, userId, d.Id);
                        result.Faces++;
                        if (type == DevicePersonType.Staff
                            && await _repo.SetEmployeePhotoIfEmptyAsync(id, "data:image/jpeg;base64," + Convert.ToBase64String(face)))
                            result.PhotosSet++;
                    }

                    var p = await _repo.GetOrAddDevicePersonAsync(deviceId, type, id, no);
                    p.UserSyncedAt ??= DateTime.UtcNow;
                    if (face != null) p.FaceSyncedAt ??= DateTime.UtcNow;
                    p.FingersOnDevice = (byte)Math.Clamp(fingers.Count, 0, 255);
                    p.UpdatedAt = DateTime.UtcNow;
                    await _repo.SaveChangesAsync();

                    read.Add(new DevicePersonRefDto { PersonType = (byte)type, PersonId = id });
                    result.Done++;
                }
                catch (HikvisionIsapiClient.IsapiException ex)
                {
                    result.Failed++; result.Errors.Add($"{no}: {ex.Message}");
                    if (Unreachable(ex)) break;
                }
            }

            d.LastSeenAt = result.Done > 0 ? DateTime.UtcNow : d.LastSeenAt;
            await _repo.SaveChangesAsync();
            return (result, read);
        }

        public async Task<DeviceSyncResultDto> CopyToDeviceAsync(int sourceDeviceId, int targetDeviceId, DeviceSyncRequestDto dto, int? userId)
        {
            if (sourceDeviceId == targetDeviceId) throw new InvalidOperationException("Choose a different machine to copy to.");
            _ = await _repo.FindDeviceAsync(targetDeviceId) ?? throw new KeyNotFoundException();

            var (fetched, people) = await FetchAsync(sourceDeviceId, dto, userId);
            var result = new DeviceSyncResultDto
            {
                Faces = fetched.Faces, Fingers = fetched.Fingers, PhotosSet = fetched.PhotosSet,
                Failed = fetched.Failed, Errors = fetched.Errors
            };
            if (people.Count == 0) return result;

            var sent = await SyncPeopleAsync(targetDeviceId, new DeviceSyncRequestDto { People = people, IncludeFace = true, IncludeFingers = true });
            result.Done = sent.Done;
            result.Sent = sent.Done;
            result.Failed += sent.Failed;
            result.Errors.AddRange(sent.Errors);
            return result;
        }

        public async Task<DeviceSyncResultDto> RemovePeopleAsync(int deviceId, DeviceSyncRequestDto dto)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var result = new DeviceSyncResultDto();
            using var client = ClientFor(d);
            foreach (var r in dto.People)
            {
                var type = (DevicePersonType)r.PersonType;
                var no = HikvisionEventProcessor.EmployeeNoFor(type, r.PersonId);
                try
                {
                    await client.DeleteUserAsync(no);
                    await _repo.RemoveDevicePersonAsync(deviceId, type, r.PersonId);
                    result.Done++;
                }
                catch (HikvisionIsapiClient.IsapiException ex)
                {
                    result.Failed++; result.Errors.Add($"{no}: {ex.Message}");
                }
            }
            return result;
        }

        public async Task<DeviceSyncResultDto> SaveFaceAsync(SaveFacePhotoDto dto, int? userId)
        {
            var (data, type, _) = UploadedImage.Decode(dto.Image);
            if (type != "image/jpeg")
                throw new InvalidOperationException("The machine takes a JPEG photo. Choose the picture again so it is converted.");
            if (data.Length > 200 * 1024)
                throw new InvalidOperationException("The photo is over 200 KB; the machine refuses larger ones. Choose it again so it is made smaller.");

            var personType = (DevicePersonType)dto.PersonType;
            await _repo.SaveFacePhotoAsync(personType, dto.PersonId, data, userId);

            var result = new DeviceSyncResultDto { Done = 1 };
            if (!dto.SendToMachines) return result;

            // To every machine the person is already on.
            foreach (var dp in await _repo.GetPersonOnDevicesAsync(personType, dto.PersonId))
            {
                var d = await _repo.FindDeviceAsync(dp.DeviceId);
                if (d == null || !d.IsActive) continue;
                using var client = ClientFor(d);
                try
                {
                    await client.UploadFaceAsync(dp.EmployeeNo, data);
                    dp.FaceSyncedAt = DateTime.UtcNow;
                    dp.LastError = null;
                }
                catch (HikvisionIsapiClient.IsapiException ex)
                {
                    dp.LastError = Clip(ex.Message, 500);
                    result.Failed++; result.Errors.Add($"{d.Name}: {ex.Message}");
                }
                dp.UpdatedAt = DateTime.UtcNow;
            }
            await _repo.SaveChangesAsync();
            return result;
        }

        public Task<byte[]?> GetFaceAsync(byte personType, int personId) =>
            _repo.GetFacePhotoAsync((DevicePersonType)personType, personId);

        public async Task<DeviceSyncResultDto> SaveCardAsync(int deviceId, SaveDeviceCardDto dto)
        {
            var d = await _repo.FindDeviceAsync(deviceId) ?? throw new KeyNotFoundException();
            var card = (dto.CardNo ?? "").Trim();
            if (card.Length > 32 || card.Any(c => !char.IsLetterOrDigit(c)))
                throw new InvalidOperationException("A card number is letters and digits only, at most 32.");

            var type = (DevicePersonType)dto.PersonType;
            var no = HikvisionEventProcessor.EmployeeNoFor(type, dto.PersonId);
            var p = await _repo.GetOrAddDevicePersonAsync(deviceId, type, dto.PersonId, no);
            p.CardNo = card.Length == 0 ? null : card;
            p.UpdatedAt = DateTime.UtcNow;

            var result = new DeviceSyncResultDto();
            if (p.UserSyncedAt != null && p.CardNo != null)
            {
                using var client = ClientFor(d);
                try { await client.SetCardAsync(no, p.CardNo); result.Done = 1; p.LastError = null; }
                catch (HikvisionIsapiClient.IsapiException ex) { result.Failed = 1; result.Errors.Add(ex.Message); p.LastError = Clip(ex.Message, 500); }
            }
            else result.Done = 1;   // saved; goes to the machine with the person
            await _repo.SaveChangesAsync();
            return result;
        }

        // Names of the requested people, from this machine's campus.
        private async Task<Dictionary<(DevicePersonType, int), string>> NamesAsync(HrAttendanceDevice d, List<DevicePersonRefDto> people)
        {
            var map = new Dictionary<(DevicePersonType, int), string>();
            if (people.Any(p => p.PersonType == (byte)DevicePersonType.Staff))
                foreach (var s in await _repo.GetStaffAsync(d.CampusId)) map[(DevicePersonType.Staff, s.Id)] = s.Name;
            if (people.Any(p => p.PersonType == (byte)DevicePersonType.Student))
                foreach (var s in await _repo.GetStudentsAsync(d.CampusId, null, null)) map[(DevicePersonType.Student, s.Id)] = s.Name;
            return map;
        }

        private static string? Clip(string? s, int max) => s == null ? null : s.Length <= max ? s : s[..max];
    }
}
