namespace Game.Core;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Game.Entities;

public static class VerificationEngine
{
    // Danh sách các mã phê duyệt tị nạn hợp pháp trong Sách Luật
    private static readonly HashSet<string> ValidAsylumCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALPHA-992-SEC",
        "SACRED-771-VEX",
        "REBEL-041-BOOT",
        "BIO-442-QUAR",
        "CYBER-009-RAW",
        "SHIELD-553-SAFE"
    };

    /// <summary>
    /// Hàm đối chiếu chéo toàn bộ giấy tờ của NPC
    /// Trả về true nếu hợp lệ hoàn toàn, false nếu phát hiện bất kỳ mâu thuẫn nào
    /// </summary>
    public static bool CrossCheckNPC(
        PassportJsonData passport,
        JsonDocument trainTicket,
        JsonDocument asylumPaper,
        JsonDocument diplomatAuth,
        DateTime currentGameDate,
        out List<string> discrepancies)
    {
        discrepancies = new List<string>();

        // 1. Kiểm tra sự tồn tại của Hộ chiếu
        if (passport == null)
        {
            discrepancies.Add("NPC không xuất trình Hộ chiếu.");
            return false;
        }

        // 2. Kiểm tra hạn sử dụng Hộ chiếu
        if (DateTime.TryParse(passport.ExpiryDate, out DateTime passportExpiry))
        {
            if (passportExpiry < currentGameDate)
            {
                discrepancies.Add($"Hộ chiếu đã hết hạn ({passport.ExpiryDate}) so với ngày {currentGameDate:yyyy-MM-dd}.");
            }
        }

        // 3. Đối chiếu chéo với Vé tàu hỏa (Train Ticket)
        if (trainTicket != null)
        {
            var root = trainTicket.RootElement;

            // So khớp tên hành khách
            if (root.TryGetProperty("passenger_name", out var ticketNameProp))
            {
                string ticketName = ticketNameProp.GetString();
                if (!string.Equals(passport.Name, ticketName, StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"Lệch tên: Hộ chiếu ghi '{passport.Name}' nhưng Vé tàu ghi '{ticketName}'.");
                }
            }

            // So khớp ngày đi tàu
            if (root.TryGetProperty("travel_date", out var travelDateProp))
            {
                if (DateTime.TryParse(travelDateProp.GetString(), out DateTime travelDate))
                {
                    if (travelDate.Date != currentGameDate.Date)
                    {
                        discrepancies.Add($"Vé tàu sai ngày di chuyển ({travelDate:yyyy-MM-dd}).");
                    }
                }
            }
        }

        // 4. Đối chiếu chéo với Đơn xin tị nạn (Political Asylum Paper)
        if (asylumPaper != null)
        {
            var root = asylumPaper.RootElement;

            // So khớp tên người xin tị nạn
            if (root.TryGetProperty("applicant_name", out var appNameProp))
            {
                string appName = appNameProp.GetString();
                if (!string.Equals(passport.Name, appName, StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"Lệch tên: Hộ chiếu ghi '{passport.Name}' nhưng Đơn tị nạn ghi '{appName}'.");
                }
            }

            // Kiểm tra mã phê duyệt
            if (root.TryGetProperty("approval_code", out var codeProp))
            {
                string code = codeProp.GetString() ?? "";
                if (!ValidAsylumCodes.Contains(code))
                {
                    discrepancies.Add($"Mã phê duyệt tị nạn '{code}' không hợp lệ hoặc giả mạo.");
                }
            }
        }

        // 5. Đối chiếu chéo với Thẻ ngoại giao (Diplomat Authorization)
        if (diplomatAuth != null)
        {
            var root = diplomatAuth.RootElement;

            if (root.TryGetProperty("diplomat_name", out var dipNameProp))
            {
                string dipName = dipNameProp.GetString();
                if (!string.Equals(passport.Name, dipName, StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"Lệch tên: Hộ chiếu ghi '{passport.Name}' nhưng Thẻ ngoại giao ghi '{dipName}'.");
                }
            }

            if (root.TryGetProperty("issuing_authority", out var authProp))
            {
                string authority = authProp.GetString() ?? "";
                if (!string.IsNullOrEmpty(passport.IssuingCountry) &&
                    !authority.Contains(passport.IssuingCountry, StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"Cơ quan cấp ngoại giao '{authority}' không khớp với quốc gia '{passport.IssuingCountry}'.");
                }
            }
        }

        return discrepancies.Count == 0;
    }
}
