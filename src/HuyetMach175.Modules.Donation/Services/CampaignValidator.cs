using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace HuyetMach175.Modules.Donation.Services;

public class CampaignValidator : ICampaignValidator
{
    public void ValidateDates(DateOnly startDate, DateOnly endDate, bool isNew = false)
    {
        if (endDate < startDate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        if (isNew)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (startDate < today)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Ngày bắt đầu đợt hiến máu không thể trong quá khứ.");
            }
        }
    }

    public void ValidateTargetDonations(int targetDonations)
    {
        if (targetDonations <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Chỉ tiêu thu gom phải lớn hơn 0 túi máu.");
        }
    }

    public void ValidateEditable(DonationCampaign campaign)
    {
        if (campaign.Status != CampaignStatus.DRAFT && campaign.Status != CampaignStatus.REJECTED)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Không thể chỉnh sửa đợt hiến máu ở trạng thái '{campaign.Status}'. Chỉ cho phép chỉnh sửa khi ở trạng thái Bản nháp (DRAFT) hoặc Bị từ chối (REJECTED).");
        }
    }

    public void ValidateSubmittable(DonationCampaign campaign)
    {
        if (campaign.Status != CampaignStatus.DRAFT && campaign.Status != CampaignStatus.REJECTED)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Không thể gửi duyệt đợt hiến máu ở trạng thái '{campaign.Status}'. Chỉ áp dụng cho trạng thái Bản nháp (DRAFT) hoặc Bị từ chối (REJECTED).");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (campaign.EndDate < today)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Không thể gửi duyệt đợt hiến máu đã quá hạn (ngày kết thúc nằm trong quá khứ).");
        }
    }

    public void ValidateApprovable(DonationCampaign campaign, bool isApproved, string? rejectionReason)
    {
        if (campaign.Status != CampaignStatus.PENDING_APPROVAL)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Đợt hiến máu không ở trạng thái Chờ duyệt (PENDING_APPROVAL). Trạng thái hiện tại: '{campaign.Status}'.");
        }

        if (!isApproved && string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng cung cấp lý do từ chối đợt hiến máu.");
        }
    }

    public void ValidateCancellable(DonationCampaign campaign, string reason)
    {
        if (campaign.Status != CampaignStatus.APPROVED && campaign.Status != CampaignStatus.IN_PROGRESS)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Chỉ có thể hủy bỏ đợt hiến máu khi đang ở trạng thái Đã duyệt (APPROVED) hoặc Đang diễn ra (IN_PROGRESS). Trạng thái hiện tại: '{campaign.Status}'.");
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Lý do hủy đợt hiến máu là bắt buộc và phải có ít nhất 5 ký tự.");
        }

        if (reason.Trim().Length > 255)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Lý do hủy đợt hiến máu không được vượt quá 255 ký tự.");
        }
    }

    public void ValidateStatusTransition(DonationCampaign campaign, CampaignStatus targetStatus)
    {
        switch (targetStatus)
        {
            case CampaignStatus.IN_PROGRESS:
                if (campaign.Status != CampaignStatus.APPROVED)
                {
                    throw new HttpException(
                        StatusCodes.Status400BadRequest,
                        $"Chỉ có thể chuyển sang trạng thái Đang diễn ra (IN_PROGRESS) từ trạng thái Đã duyệt (APPROVED). Trạng thái hiện tại: '{campaign.Status}'.");
                }
                break;

            case CampaignStatus.COMPLETED:
                if (campaign.Status != CampaignStatus.IN_PROGRESS)
                {
                    throw new HttpException(
                        StatusCodes.Status400BadRequest,
                        $"Chỉ có thể kết thúc đợt hiến máu (COMPLETED) khi đợt hiến đang diễn ra (IN_PROGRESS). Trạng thái hiện tại: '{campaign.Status}'.");
                }
                break;

            case CampaignStatus.CANCELLED:
                throw new HttpException(
                    StatusCodes.Status400BadRequest,
                    "Không thể hủy đợt hiến máu qua thao tác vận hành này. Thao tác hủy yêu cầu sự phê duyệt của Lãnh đạo qua chức năng Hủy chuyên biệt.");

            default:
                throw new HttpException(
                    StatusCodes.Status400BadRequest,
                    $"Không thể chuyển sang trạng thái '{targetStatus}' bằng thao tác này. Các trạng thái hợp lệ là: IN_PROGRESS, COMPLETED.");
        }
    }

    public void ValidateDeletable(DonationCampaign campaign)
    {
        if (campaign.Status != CampaignStatus.DRAFT)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Chỉ có thể xóa đợt hiến máu ở trạng thái Bản nháp (DRAFT). Trạng thái hiện tại: '{campaign.Status}'. Vui lòng sử dụng tính năng Hủy (CANCELLED) nếu muốn dừng đợt hiến.");
        }

        if (campaign.Appointments.Any())
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Không thể xóa đợt hiến máu đã có người hiến đăng ký lịch hẹn.");
        }
    }
}