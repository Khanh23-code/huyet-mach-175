using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class AllocateBloodBagsRequest
{
    [Required(ErrorMessage = "Danh sách ID túi máu cần giữ chỗ không được để trống.")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 túi máu để giữ chỗ.")]
    public List<int> BagIds { get; set; } = new List<int>();
}
