using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class CreateDonorRequest
{
    [Required(ErrorMessage = "Vui lòng nhập số CCCD")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số")]
    public string CitizenId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giới tính")]
    [StringLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    public string? Email { get; set; }
}