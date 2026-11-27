using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class CreateBloodRequestItemDto
{
    [Required(ErrorMessage = "Mã loại chế phẩm máu là bắt buộc.")]
    public Guid ComponentTypeId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng đơn vị yêu cầu phải từ 1 đến 100.")]
    public int RequestedUnits { get; set; }
}
