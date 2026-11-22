using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Donors;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class DonorImportService : IDonorImportService
{
    private readonly ApplicationDbContext _context;

    public DonorImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(byte[] Content, string FileName)> GenerateTemplateAsync(int campaignId)
    {
        if (campaignId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .AsNoTracking()
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{campaignId}.");
        }

        using var workbook = new XLWorkbook();

        var ws1 = workbook.Worksheets.Add("Danh sách đăng ký");

        ws1.Cell(1, 1).Value = $"DANH SÁCH ĐĂNG KÝ HIẾN MÁU - ĐỢT #{campaign.CampaignId}: {campaign.CampaignName}";
        ws1.Range(1, 1, 1, 20).Merge();
        ws1.Cell(1, 1).Style.Font.Bold = true;
        ws1.Cell(1, 1).Style.Font.FontSize = 13;
        ws1.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws1.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a7534");
        ws1.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws1.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws1.Row(1).Height = 32;

        ws1.Cell(2, 1).Value = $"MÃ ĐỢT: #{campaign.CampaignId} | ĐỊA ĐIỂM: {campaign.LocationName} | THỜI GIAN: {campaign.StartDate:dd/MM/yyyy} - {campaign.EndDate:dd/MM/yyyy}";
        ws1.Range(2, 1, 2, 20).Merge();
        ws1.Cell(2, 1).Style.Font.Italic = true;
        ws1.Cell(2, 1).Style.Font.FontSize = 10;
        ws1.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#e9f5ed");
        ws1.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws1.Row(2).Height = 22;

        ws1.Cell(3, 1).Value = "* Lưu ý: Không sửa đổi ô Mã chiến dịch ở trên và cấu trúc các cột. Các cột có dấu (*) là bắt buộc. 9 câu hỏi khảo sát chọn Có / Không (hoặc để trống nếu chưa trả lời).";
        ws1.Range(3, 1, 3, 20).Merge();
        ws1.Cell(3, 1).Style.Font.Italic = true;
        ws1.Cell(3, 1).Style.Font.FontSize = 9;
        ws1.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#c4261b");
        ws1.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        ws1.Row(3).Height = 20;

        string[] headers =
        [
            "STT",
            "Họ và tên (*)",
            "Số CCCD (*)",
            "Ngày sinh (*)",
            "Giới tính (*)",
            "Số điện thoại (*)",
            "Email",
            "Nhóm máu ABO",
            "Yếu tố Rh",
            "Khung giờ đăng ký (*)",
            "Ngày hẹn (*)",
            "1. Đã từng hiến máu?",
            "2. Bệnh mãn tính?",
            "3. Xăm/xỏ/phẫu thuật 12 tháng qua?",
            "4. Sụt cân/sốt kéo dài 6 tháng qua?",
            "5. Cảm cúm/kháng sinh 14 ngày qua?",
            "6. Vaccine/thuốc 7 ngày qua?",
            "7. Nguy cơ lây nhiễm HIV/Viêm gan?",
            "8. Triệu chứng bất thường?",
            "9. Nữ: Mang thai/kinh nguyệt?"
        ];

        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws1.Cell(4, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d6a4f");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        ws1.Row(4).Height = 40;

        var orderedSlots = campaign.Slots.OrderBy(s => s.StartTime).ToList();
        var slotStrings = orderedSlots
            .Select(s => $"{s.StartTime:HH\\:mm} - {s.EndTime:HH\\:mm}")
            .ToList();
        var firstSlotTime = slotStrings.FirstOrDefault() ?? "07:30 - 09:00";

        object[] sampleRow =
        [
            1,
            "Nguyễn Văn An",
            "079012345678",
            "15/05/2003",
            "Nam",
            "0901234567",
            "an.nguyen@example.com",
            "O",
            "Rh+",
            firstSlotTime,
            campaign.StartDate.ToString("dd/MM/yyyy"),
            "Có", "Không", "Không", "Không", "Không", "Không", "Không", "Không", "Không"
        ];

        for (int col = 1; col <= sampleRow.Length; col++)
        {
            var cell = ws1.Cell(5, col);
            cell.Value = sampleRow[col - 1].ToString();
            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#5f7367");
        }

        ws1.Range("E5:E500").CreateDataValidation().List("Nam,Nữ");
        ws1.Range("H5:H500").CreateDataValidation().List("A,B,AB,O");
        ws1.Range("I5:I500").CreateDataValidation().List("Rh+,Rh-");

        if (slotStrings.Any())
        {
            var slotListJoined = string.Join(",", slotStrings);
            if (slotListJoined.Length <= 255)
            {
                ws1.Range("J5:J500").CreateDataValidation().List(slotListJoined);
            }
        }

        for (int c = 12; c <= 20; c++)
        {
            var colLetter = XLHelper.GetColumnLetterFromNumber(c);
            ws1.Range($"{colLetter}5:{colLetter}500").CreateDataValidation().List("Có,Không");
        }

        ws1.Columns().AdjustToContents();
        ws1.Column(1).Width = 6;
        ws1.Column(2).Width = 24;
        ws1.Column(3).Width = 16;
        ws1.Column(4).Width = 14;
        ws1.Column(5).Width = 10;
        ws1.Column(6).Width = 16;
        ws1.Column(7).Width = 24;
        ws1.Column(8).Width = 14;
        ws1.Column(9).Width = 12;
        ws1.Column(10).Width = 22;
        ws1.Column(11).Width = 14;

        var ws2 = workbook.Worksheets.Add("Khung giờ & Hướng dẫn");

        ws2.Cell(1, 1).Value = $"DANH SÁCH KHUNG GIỜ CỦA ĐỢT HIẾN MÁU #{campaign.CampaignId}";
        ws2.Range(1, 1, 1, 5).Merge();
        ws2.Cell(1, 1).Style.Font.Bold = true;
        ws2.Cell(1, 1).Style.Font.FontSize = 12;
        ws2.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws2.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a7534");
        ws2.Row(1).Height = 28;

        string[] slotHeaders = ["STT", "Mã Slot", "Giờ bắt đầu", "Giờ kết thúc", "Chuỗi khung giờ", "Sức chứa"];
        for (int c = 1; c <= slotHeaders.Length; c++)
        {
            var cell = ws2.Cell(2, c);
            cell.Value = slotHeaders[c - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e9f5ed");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int slotRow = 3;
        int slotIndex = 1;
        foreach (var slot in orderedSlots)
        {
            ws2.Cell(slotRow, 1).Value = slotIndex++;
            ws2.Cell(slotRow, 2).Value = slot.SlotId;
            ws2.Cell(slotRow, 3).Value = slot.StartTime.ToString("HH:mm");
            ws2.Cell(slotRow, 4).Value = slot.EndTime.ToString("HH:mm");
            ws2.Cell(slotRow, 5).Value = $"{slot.StartTime:HH\\:mm} - {slot.EndTime:HH\\:mm}";
            ws2.Cell(slotRow, 6).Value = slot.Capacity;
            slotRow++;
        }

        slotRow += 2;
        ws2.Cell(slotRow, 1).Value = "HƯỚNG DẪN ĐIỀN THÔNG TIN:";
        ws2.Cell(slotRow, 1).Style.Font.Bold = true;
        slotRow++;
        ws2.Cell(slotRow, 1).Value = "1. Số CCCD: Phải là 12 chữ số theo Căn cước công dân gắn chip.";
        slotRow++;
        ws2.Cell(slotRow, 1).Value = "2. Ngày sinh: Định dạng Ngày/Tháng/Năm (ví dụ: 15/05/2003). Người hiến phải đủ từ 18 đến 60 tuổi.";
        slotRow++;
        ws2.Cell(slotRow, 1).Value = "3. Khung giờ: Chọn chính xác chuỗi khung giờ theo cột E của bảng trên.";
        slotRow++;
        ws2.Cell(slotRow, 1).Value = "4. Bảng hỏi khảo sát (9 câu hỏi): Trả lời Có hoặc Không. Nếu người hiến chưa khai báo thì có thể để trống.";

        ws2.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        var fileName = $"Mau_Danh_Sach_Hien_Mau_Campaign_{campaign.CampaignId}.xlsx";

        return (bytes, fileName);
    }

    public async Task<DonorImportResultDto> ImportDonorsAsync(int campaignId, IFormFile file)
    {
        if (campaignId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        if (file == null || file.Length == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng chọn file Excel danh sách người hiến để tải lên.");
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Hệ thống chỉ chấp nhận định dạng file Excel (.xlsx).");
        }

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{campaignId}.");
        }

        if (campaign.Status == CampaignStatus.COMPLETED ||
            campaign.Status == CampaignStatus.CANCELLED ||
            campaign.Status == CampaignStatus.REJECTED)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Đợt hiến máu đang ở trạng thái '{campaign.Status}', không thể nhập thêm danh sách người hiến.");
        }

        var slotLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in campaign.Slots)
        {
            var strStandard = $"{s.StartTime:HH\\:mm} - {s.EndTime:HH\\:mm}".Replace(" ", "");
            slotLookup[strStandard] = s.SlotId;
            slotLookup[s.StartTime.ToString("HH:mm")] = s.SlotId;
            slotLookup[s.SlotId.ToString()] = s.SlotId;
        }

        var defaultSlotId = campaign.Slots.OrderBy(s => s.StartTime).FirstOrDefault()?.SlotId;

        var existingAppointments = await _context.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
            .Where(a => a.CampaignSlot != null && a.CampaignSlot.CampaignId == campaignId && a.Donor != null)
            .Select(a => a.Donor!.CitizenId.Trim())
            .ToListAsync();

        var existingCitizenIdsInCampaign = new HashSet<string>(existingAppointments, StringComparer.OrdinalIgnoreCase);
        var processedInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new DonorImportResultDto
        {
            CampaignId = campaign.CampaignId,
            CampaignName = campaign.CampaignName
        };

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var workbook = new XLWorkbook(memoryStream);
        var ws = workbook.Worksheet("Danh sách đăng ký") ?? workbook.Worksheet(1);

        var trackedDonors = new List<Donor>();
        int rowNumber = 5;

        while (true)
        {
            var row = ws.Row(rowNumber);
            if (row == null || row.IsEmpty())
            {
                break;
            }

            var stt = ParseStt(row.Cell(1), rowNumber - 4);
            var fullName = row.Cell(2).GetString()?.Trim();
            var citizenIdRaw = row.Cell(3).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(citizenIdRaw))
            {
                break;
            }

            if (string.Equals(citizenIdRaw, "079012345678", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(fullName, "Nguyễn Văn An", StringComparison.OrdinalIgnoreCase))
            {
                rowNumber++;
                continue;
            }

            result.TotalRows++;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenIdRaw,
                    FullName = fullName,
                    ErrorMessage = "Họ và tên không được để trống."
                });
                rowNumber++;
                continue;
            }

            var citizenId = CleanDigits(citizenIdRaw);
            if (string.IsNullOrWhiteSpace(citizenId) || !Regex.IsMatch(citizenId, @"^[0-9]{12}$"))
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenIdRaw,
                    FullName = fullName,
                    ErrorMessage = "Số CCCD không hợp lệ. Số CCCD phải gồm đúng 12 chữ số."
                });
                rowNumber++;
                continue;
            }

            if (!processedInFile.Add(citizenId))
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenId,
                    FullName = fullName,
                    ErrorMessage = "Số CCCD bị trùng lặp nhiều lần trong file Excel này."
                });
                rowNumber++;
                continue;
            }

            if (existingCitizenIdsInCampaign.Contains(citizenId))
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenId,
                    FullName = fullName,
                    ErrorMessage = "Người hiến này đã đăng ký lịch hẹn trong đợt hiến máu này rồi."
                });
                rowNumber++;
                continue;
            }

            var dobParsed = TryParseDate(row.Cell(4));
            if (!dobParsed.HasValue)
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenId,
                    FullName = fullName,
                    ErrorMessage = "Ngày sinh không hợp lệ. Vui lòng nhập theo định dạng DD/MM/YYYY."
                });
                rowNumber++;
                continue;
            }
            var dateOfBirth = dobParsed.Value;

            var phoneRaw = row.Cell(6).GetString()?.Trim();
            var phone = CleanPhone(phoneRaw);
            if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^[0-9]{10,11}$"))
            {
                result.ErrorsCount++;
                result.Errors.Add(new DonorImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Stt = stt,
                    CitizenId = citizenId,
                    FullName = fullName,
                    ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại gồm 10-11 chữ số."
                });
                rowNumber++;
                continue;
            }

            var genderRaw = row.Cell(5).GetString()?.Trim();
            var gender = string.Equals(genderRaw, "Nữ", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(genderRaw, "Female", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(genderRaw, "Nu", StringComparison.OrdinalIgnoreCase)
                ? "Nữ"
                : "Nam";

            var email = row.Cell(7).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            {
                email = null;
            }

            var bloodType = ParseBloodType(row.Cell(8).GetString()?.Trim());
            var rhFactor = ParseRhFactor(row.Cell(9).GetString()?.Trim());

            var slotRaw = row.Cell(10).GetString()?.Trim() ?? string.Empty;
            var normalizedSlot = slotRaw.Replace(" ", "");
            int? slotId = null;

            if (!string.IsNullOrWhiteSpace(normalizedSlot) && slotLookup.TryGetValue(normalizedSlot, out var matchedSlotId))
            {
                slotId = matchedSlotId;
            }
            else
            {
                slotId = defaultSlotId;
            }

            var apptDateParsed = TryParseDate(row.Cell(11));
            var apptDate = apptDateParsed ?? campaign.StartDate;

            var donor = trackedDonors.FirstOrDefault(d => d.CitizenId == citizenId) ??
                        await _context.Donors.FirstOrDefaultAsync(d => d.CitizenId == citizenId);

            if (donor == null)
            {
                donor = new Donor
                {
                    FullName = fullName,
                    CitizenId = citizenId,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    PhoneNumber = phone,
                    Email = email,
                    BloodType = bloodType,
                    RhFactor = rhFactor,
                    TotalDonations = 0
                };
                _context.Donors.Add(donor);
                trackedDonors.Add(donor);
                result.CreatedDonorsCount++;
            }
            else
            {
                result.ReusedDonorsCount++;
                if (string.IsNullOrWhiteSpace(donor.PhoneNumber))
                {
                    donor.PhoneNumber = phone;
                }
                if (string.IsNullOrWhiteSpace(donor.Email) && !string.IsNullOrWhiteSpace(email))
                {
                    donor.Email = email;
                }
                if (!donor.BloodType.HasValue && bloodType.HasValue)
                {
                    donor.BloodType = bloodType;
                }
                if (!donor.RhFactor.HasValue && rhFactor.HasValue)
                {
                    donor.RhFactor = rhFactor;
                }
            }

            var appointment = new DonationAppointment
            {
                Donor = donor,
                SlotId = slotId,
                AppointmentDate = apptDate,
                QrCodeToken = $"HM175-{Guid.NewGuid():N}",
                Status = AppointmentStatus.BOOKED,
                IsWalkIn = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.DonationAppointments.Add(appointment);
            result.CreatedAppointmentsCount++;

            var surveyAnswers = ExtractSurveyAnswers(row);
            if (surveyAnswers != null)
            {
                var (riskScore, hasRisk) = CalculateSurveyRisk(surveyAnswers);
                var survey = new PreScreeningSurvey
                {
                    Appointment = appointment,
                    SurveyAnswers = JsonSerializer.Serialize(surveyAnswers),
                    RiskScore = riskScore,
                    HasRisk = hasRisk,
                    SurveySubmittedAt = DateTime.UtcNow,
                    ScreeningStatus = ScreeningStatus.PENDING
                };
                _context.PreScreeningSurveys.Add(survey);
                result.SurveysIncludedCount++;
            }

            result.SuccessCount++;
            existingCitizenIdsInCampaign.Add(citizenId);
            rowNumber++;
        }

        if (result.CreatedAppointmentsCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    private static string? CleanDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        return Regex.Replace(input, @"[^\d]", "");
    }

    private static string? CleanPhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = Regex.Replace(input, @"[^\d]", "");
        if (digits.StartsWith("84") && digits.Length >= 11)
        {
            digits = "0" + digits.Substring(2);
        }
        return digits;
    }

    private static DateOnly? TryParseDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }

        var str = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(str)) return null;

        string[] formats = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "yyyy/MM/dd"];
        if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return DateOnly.FromDateTime(parsed);
        }

        if (DateTime.TryParse(str, out var generalParsed))
        {
            return DateOnly.FromDateTime(generalParsed);
        }

        return null;
    }

    private static BloodType? ParseBloodType(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return null;
        var upper = str.Trim().ToUpperInvariant();
        return upper switch
        {
            "A" => BloodType.A,
            "B" => BloodType.B,
            "AB" => BloodType.AB,
            "O" => BloodType.O,
            _ => null
        };
    }

    private static RhFactor? ParseRhFactor(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return null;
        var upper = str.Trim().ToUpperInvariant().Replace(" ", "");
        if (upper == "RH+" || upper == "POSITIVE" || upper == "DUONG" || upper == "+") return RhFactor.POSITIVE;
        if (upper == "RH-" || upper == "NEGATIVE" || upper == "AM" || upper == "-") return RhFactor.NEGATIVE;
        return null;
    }

    private static Dictionary<string, bool>? ExtractSurveyAnswers(IXLRow row)
    {
        bool hasAnyAnswer = false;
        var answers = new Dictionary<string, bool>();

        string[] keys =
        [
            "q1_previous_donation",
            "q2_chronic_diseases",
            "q3_last_12_months",
            "q4_last_6_months",
            "q5_last_14_days",
            "q6_last_7_days",
            "q7_infectious_risk",
            "q8_unexplained_symptoms",
            "q9_female_specific"
        ];

        for (int i = 0; i < keys.Length; i++)
        {
            var colIndex = 12 + i;
            var valStr = row.Cell(colIndex).GetString()?.Trim();

            if (!string.IsNullOrWhiteSpace(valStr))
            {
                hasAnyAnswer = true;
                bool isTrue = string.Equals(valStr, "Có", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(valStr, "Co", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(valStr, "Yes", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(valStr, "1", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(valStr, "X", StringComparison.OrdinalIgnoreCase);
                answers[keys[i]] = isTrue;
            }
            else
            {
                answers[keys[i]] = false;
            }
        }

        return hasAnyAnswer ? answers : null;
    }

    private static (int RiskScore, bool HasRisk) CalculateSurveyRisk(Dictionary<string, bool> answers)
    {
        int riskScore = 0;
        foreach (var kvp in answers)
        {
            if (string.Equals(kvp.Key, "q1_previous_donation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kvp.Value)
            {
                riskScore++;
            }
        }

        return (riskScore, riskScore > 0);
    }

    private static int ParseStt(IXLCell cell, int defaultStt)
    {
        try
        {
            if (cell.TryGetValue(out int intVal) && intVal > 0)
            {
                return intVal;
            }
            if (cell.TryGetValue(out double dblVal) && dblVal > 0)
            {
                return (int)Math.Round(dblVal);
            }
            var text = cell.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text, out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }
        catch
        {
        }
        return defaultStt;
    }
}