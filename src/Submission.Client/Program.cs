using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine("=== CHƯƠNG TRÌNH MÁY KHÁCH SUBMISSION.CLIENT ===");

// 1. Lấy danh sách bài tập
Console.WriteLine("\n1. Lay danh sach bai tap:");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? [])
{
    Console.WriteLine($"  - [{a.Id}] {a.Title} | Trang thai mo: {a.IsOpen}");
}

// 2. Nộp bài hợp lệ cho bài tập đầu tiên đang mở
var selectedId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
Console.WriteLine($"\n2. Nop bai cho bai tap ID {selectedId}:");
var createReq = new CreateSubmissionRequest("SV001", "https://github.com/sv001/rest-api-lab");
var createRes = await http.PostAsJsonAsync($"/api/assignments/{selectedId}/submissions", createReq);

if (createRes.StatusCode == HttpStatusCode.Created)
{
    var created = await createRes.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"  -> Da tao luot nop: {created?.Id}");
    Console.WriteLine($"  -> Trang thai: {created?.Status}");
    Console.WriteLine($"  -> Location header: {createRes.Headers.Location}");

    // 3. Đọc lại chi tiết lượt nộp qua Location header
    if (createRes.Headers.Location != null)
    {
        Console.WriteLine($"\n3. Doc lai chi tiet luot nop tu location '{createRes.Headers.Location}':");
        var subDetail = await http.GetFromJsonAsync<SubmissionDto>(createRes.Headers.Location.ToString());
        Console.WriteLine($"  -> Ket qua: Sinh vien {subDetail?.StudentCode} - Repo: {subDetail?.RepositoryUrl}");
    }
}
else
{
    await PrintApiErrorAsync(createRes);
}

// 4. Thử gửi dữ liệu sai để kiểm tra xử lý lỗi (Mã 400)
Console.WriteLine("\n4. Thu gui du lieu sai (thieu repo, ma SV):");
var invalidRes = await http.PostAsJsonAsync($"/api/assignments/{selectedId}/submissions", new CreateSubmissionRequest("", ""));
Console.WriteLine($"  -> Response Status: {(int)invalidRes.StatusCode} {invalidRes.StatusCode}");
await PrintApiErrorAsync(invalidRes);

// Hàm dùng chung đọc và in lỗi ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"  [LỖI API] {error?.Code}: {error?.Message}");
}

// Khai báo DTOs ở máy khách
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);