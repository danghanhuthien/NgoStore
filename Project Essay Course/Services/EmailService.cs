using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Project_Essay_Course.Models;

namespace Project_Essay_Course.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSSL { get; set; } = true;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order);
        Task SendOrderStatusUpdateAsync(Order order);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(Order order)
        {
            var toEmail = order.Email ?? "";
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var subject = $"[NGỘ STORE] Xác nhận đơn hàng #{order.OrderCode}";
            var body = BuildEmailHtml(order, isConfirmation: true);
            await SendAsync(toEmail, order.FullName, subject, body);
        }

        public async Task SendOrderStatusUpdateAsync(Order order)
        {
            var toEmail = order.Email ?? "";
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var subject = $"[NGỘ STORE] Đơn hàng #{order.OrderCode} — {order.StatusDisplay}";
            var body = BuildEmailHtml(order, isConfirmation: false);
            await SendAsync(toEmail, order.FullName, subject, body);
        }

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {Email} — {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  SHARED HTML BUILDER — dùng cho cả 2 loại email
        //  isConfirmation = true  → "Xác nhận đặt hàng"
        //  isConfirmation = false → "Cập nhật trạng thái"
        // ══════════════════════════════════════════════════════════════
        private static string BuildEmailHtml(Order order, bool isConfirmation)
        {
            // ── Status info ──
            var (statusIcon, statusDesc, headerLabel) = isConfirmation
                ? ("✅", "Đơn hàng của bạn đã được tiếp nhận thành công. Chúng tôi sẽ xác nhận và xử lý sớm nhất!", "Xác Nhận Đơn Hàng")
                : order.Status switch
                {
                    OrderStatus.Confirmed => ("✅", "Đơn hàng của bạn đã được xác nhận. Chúng tôi đang chuẩn bị hàng!", "Cập Nhật Đơn Hàng"),
                    OrderStatus.Processing => ("📦", "Đơn hàng đang được đóng gói cẩn thận. Chúng tôi sẽ sớm giao cho đơn vị vận chuyển.", "Cập Nhật Đơn Hàng"),
                    OrderStatus.Shipping => ("🚚", "Đơn hàng đang trên đường đến với bạn. Vui lòng chú ý điện thoại để nhận hàng nhé!", "Đơn Hàng Đang Giao"),
                    OrderStatus.Delivered => ("🎉", "Đơn hàng đã được giao thành công! Cảm ơn bạn đã mua sắm tại NGỘ STORE. Hẹn gặp lại!", "Giao Hàng Thành Công"),
                    OrderStatus.Cancelled => ("❌", "Đơn hàng của bạn đã bị hủy. Nếu có thắc mắc, vui lòng liên hệ chúng tôi.", "Đơn Hàng Đã Hủy"),
                    _ => ("📋", "Trạng thái đơn hàng của bạn vừa được cập nhật.", "Cập Nhật Đơn Hàng")
                };

            // ── Status badge color ──
            var statusBg = isConfirmation ? "#f0fdf4" : GetStatusBg(order.Status);
            var statusFg = isConfirmation ? "#15803d" : order.StatusColor;
            var statusText = isConfirmation ? "Đã tiếp nhận" : order.StatusDisplay;

            // ── Items rows ──
            var itemRows = string.Join("", order.OrderItems.Select(item =>
            {
                var variantHtml = !string.IsNullOrEmpty(item.VariantDisplay)
                    ? $@"<div style='margin-top:5px;display:flex;gap:4px;flex-wrap:wrap'>
                           {string.Join("", item.VariantDisplay.Split(" / ").Select(p =>
                               $"<span style='font-size:11px;padding:2px 8px;border:1px solid #e5e5e5;background:#f5f0e8;color:#555'>{p.Trim()}</span>"
                           ))}
                         </div>"
                    : "";

                return $@"
                <tr>
                    <td style='padding:14px 16px;border-bottom:1px solid #f0f0f0;vertical-align:top'>
                        <div style='font-size:14px;font-weight:500;color:#0a0a0a;line-height:1.4'>{item.ProductName}</div>
                        {variantHtml}
                        {(string.IsNullOrEmpty(item.SKU) ? "" : $"<div style='font-size:11px;color:#bbb;margin-top:4px;font-family:monospace'>SKU: {item.SKU}</div>")}
                    </td>
                    <td style='padding:14px 16px;border-bottom:1px solid #f0f0f0;text-align:center;font-size:14px;color:#555;white-space:nowrap'>x{item.Quantity}</td>
                    <td style='padding:14px 16px;border-bottom:1px solid #f0f0f0;text-align:right;font-size:14px;font-weight:600;color:#0a0a0a;white-space:nowrap'>{item.SubTotal:N0}₫</td>
                </tr>";
            }));

            // ── Payment note ──
            var paymentNote = order.PaymentMethod == PaymentMethod.BankTransfer
                ? $@"<div style='background:#fffbeb;border:1px solid #fde68a;border-left:4px solid #f59e0b;padding:18px 20px;margin:24px 0'>
                        <div style='font-size:12px;font-weight:700;letter-spacing:0.1em;text-transform:uppercase;color:#92400e;margin-bottom:12px'>⚠ Thông Tin Chuyển Khoản</div>
                        <table style='width:100%;font-size:13px;border-collapse:collapse'>
                            <tr><td style='color:#92400e;padding:5px 0;width:140px;font-weight:500'>Ngân hàng</td>   <td style='color:#78350f;font-weight:700'>ACB</td></tr>
                            <tr><td style='color:#92400e;padding:5px 0;font-weight:500'>Số tài khoản</td>            <td style='color:#78350f;font-weight:700;font-family:monospace;font-size:14px'>20499761</td></tr>
                            <tr><td style='color:#92400e;padding:5px 0;font-weight:500'>Chủ tài khoản</td>           <td style='color:#78350f;font-weight:700'>DANG HA NHU THIEN</td></tr>
                            <tr><td style='color:#92400e;padding:5px 0;font-weight:500'>Số tiền</td>                 <td style='color:#78350f;font-weight:800;font-size:16px'>{order.TotalAmount:N0}₫</td></tr>
                            <tr><td style='color:#92400e;padding:5px 0;font-weight:500'>Nội dung CK</td>             <td style='color:#b45309;font-weight:800;font-family:monospace;font-size:16px;letter-spacing:0.05em'>{order.OrderCode}</td></tr>
                        </table>
                        <div style='font-size:12px;color:#92400e;margin-top:12px;padding-top:12px;border-top:1px solid #fde68a;line-height:1.7'>
                            ⚡ Đơn hàng sẽ được <strong>xác nhận tự động</strong> sau khi nhận được tiền.<br>
                            Vui lòng ghi <strong>đúng nội dung</strong> chuyển khoản để hệ thống xử lý nhanh nhất.
                        </div>
                     </div>"
                : $@"<div style='background:#f0fdf4;border:1px solid #bbf7d0;border-left:4px solid #16a34a;padding:14px 20px;margin:24px 0;font-size:13px;color:#15803d'>
                        ✓ <strong>Thanh toán khi nhận hàng (COD)</strong> — Bạn chỉ cần thanh toán khi nhận được hàng. Không cần chuẩn bị trước!
                     </div>";

            // ── Timeline steps ──
            var timelineSteps = new[]
            {
                (OrderStatus.Pending,    "Tiếp nhận"),
                (OrderStatus.Confirmed,  "Xác nhận"),
                (OrderStatus.Processing, "Chuẩn bị"),
                (OrderStatus.Shipping,   "Đang giao"),
                (OrderStatus.Delivered,  "Hoàn thành"),
            };

            var currentStatusInt = isConfirmation ? 0 : (int)order.Status;
            if (order.Status == OrderStatus.Cancelled) currentStatusInt = -1;

            var timelineCells = string.Join(
                "<td style='text-align:center;vertical-align:top;width:8px'><div style='height:2px;background:#e5e5e5;margin-top:12px'></div></td>",
                timelineSteps.Select((step, i) =>
                {
                    var stepInt = (int)step.Item1;
                    var isDone = currentStatusInt > stepInt;
                    var isActive = currentStatusInt == stepInt;
                    var dotBg = isDone ? "#0a0a0a" : (isActive ? "#b8860b" : "#e5e5e5");
                    var dotTxt = isDone ? "✓" : (isActive ? "●" : "○");
                    var labelCol = isDone ? "#0a0a0a" : (isActive ? "#b8860b" : "#bbb");
                    var fw = isActive ? "700" : "400";
                    return $@"<td style='text-align:center;vertical-align:top;min-width:60px'>
                                <div style='width:24px;height:24px;border-radius:50%;background:{dotBg};color:#fff;font-size:11px;line-height:24px;text-align:center;margin:0 auto'>{dotTxt}</div>
                                <div style='font-size:10px;color:{labelCol};font-weight:{fw};margin-top:5px;letter-spacing:0.04em'>{step.Item2}</div>
                              </td>";
                })
            );

            return $@"<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width,initial-scale=1'>
    <title>NGỘ STORE — {headerLabel}</title>
</head>
<body style='margin:0;padding:0;background:#f5f0e8;font-family:""DM Sans"",system-ui,sans-serif;-webkit-font-smoothing:antialiased'>
<div style='padding:24px 16px'>
<div style='max-width:620px;margin:0 auto;background:#ffffff;box-shadow:0 2px 12px rgba(0,0,0,0.08)'>

    <!-- HEADER -->
    <div style='background:#0a0a0a;padding:28px 36px;text-align:center'>
        <div style='font-family:Georgia,""Times New Roman"",serif;font-size:28px;font-weight:600;letter-spacing:0.08em;color:#ffffff'>
            NGỘ STORE<span style='color:#b8860b'>.</span>
        </div>
        <div style='font-size:10px;letter-spacing:0.28em;text-transform:uppercase;color:rgba(255,255,255,0.4);margin-top:6px'>{headerLabel}</div>
    </div>

    <!-- STATUS BANNER -->
    <div style='background:{statusBg};padding:20px 36px;border-bottom:1px solid #f0f0f0;display:flex;align-items:center;gap:14px'>
        <span style='font-size:28px'>{statusIcon}</span>
        <div>
            <div style='font-size:11px;letter-spacing:0.18em;text-transform:uppercase;color:#999;margin-bottom:3px'>Trạng thái đơn hàng</div>
            <div style='font-family:Georgia,serif;font-size:18px;font-weight:600;color:{statusFg}'>{statusText}</div>
        </div>
        <div style='margin-left:auto;text-align:right'>
            <div style='font-size:10px;letter-spacing:0.14em;text-transform:uppercase;color:#bbb;margin-bottom:3px'>Mã đơn</div>
            <div style='font-family:monospace;font-size:15px;font-weight:700;color:#0a0a0a;letter-spacing:0.06em'>{order.OrderCode}</div>
        </div>
    </div>

    <!-- BODY -->
    <div style='padding:32px 36px'>

        <p style='font-size:15px;color:#0a0a0a;margin:0 0 6px 0'>Xin chào <strong>{order.FullName}</strong>,</p>
        <p style='font-size:14px;color:#666;line-height:1.75;margin:0 0 28px 0'>{statusDesc}</p>

        <!-- STATUS TIMELINE -->
        {(order.Status != OrderStatus.Cancelled ? $@"
        <div style='background:#f9f9f8;border:1px solid #f0f0f0;padding:16px;margin-bottom:28px;overflow-x:auto'>
            <table style='width:100%;border-collapse:collapse'>
                <tr>{timelineCells}</tr>
            </table>
        </div>" : "")}

        <!-- PAYMENT NOTE (chỉ hiện khi confirmation hoặc còn unpaid) -->
        {((isConfirmation || order.PaymentStatus == PaymentStatus.Unpaid) && order.Status != OrderStatus.Cancelled ? paymentNote : "")}

        <!-- ORDER ITEMS -->
        <div style='font-size:10px;letter-spacing:0.22em;text-transform:uppercase;color:#b8860b;margin-bottom:10px;font-weight:600'>Sản Phẩm Đã Đặt</div>
        <table style='width:100%;border-collapse:collapse;border:1px solid #f0f0f0;margin-bottom:0'>
            <thead>
                <tr style='background:#f9f9f8'>
                    <th style='padding:10px 16px;text-align:left;font-size:10px;letter-spacing:0.16em;text-transform:uppercase;color:#bbb;font-weight:500;border-bottom:1px solid #f0f0f0'>Sản phẩm</th>
                    <th style='padding:10px 16px;text-align:center;font-size:10px;letter-spacing:0.16em;text-transform:uppercase;color:#bbb;font-weight:500;border-bottom:1px solid #f0f0f0'>SL</th>
                    <th style='padding:10px 16px;text-align:right;font-size:10px;letter-spacing:0.16em;text-transform:uppercase;color:#bbb;font-weight:500;border-bottom:1px solid #f0f0f0'>Thành tiền</th>
                </tr>
            </thead>
            <tbody>{itemRows}</tbody>
            <tfoot style='border-top:2px solid #0a0a0a'>
                <tr>
                    <td colspan='2' style='padding:10px 16px;font-size:13px;color:#888;border-top:1px solid #e5e5e5'>Tạm tính ({order.OrderItems.Sum(i => i.Quantity)} sản phẩm)</td>
                    <td style='padding:10px 16px;text-align:right;font-size:13px;color:#888;border-top:1px solid #e5e5e5'>{order.SubTotal:N0}₫</td>
                </tr>
                <tr>
                    <td colspan='2' style='padding:6px 16px;font-size:13px;color:#888'>Phí vận chuyển</td>
                    <td style='padding:6px 16px;text-align:right;font-size:13px;color:#16a34a;font-weight:500'>{(order.ShippingFee == 0 ? "Miễn phí" : order.ShippingFee.ToString("N0") + "₫")}</td>
                </tr>
                <tr style='background:#0a0a0a'>
                    <td colspan='2' style='padding:14px 16px;font-family:Georgia,serif;font-size:15px;font-weight:600;color:#ffffff'>Tổng cộng</td>
                    <td style='padding:14px 16px;text-align:right;font-family:Georgia,serif;font-size:18px;font-weight:700;color:#b8860b'>{order.TotalAmount:N0}₫</td>
                </tr>
            </tfoot>
        </table>

        <!-- INFO GRID -->
        <table style='width:100%;border-collapse:collapse;margin-top:20px'>
            <tr>
                <!-- Shipping info -->
                <td style='vertical-align:top;width:50%;padding-right:10px'>
                    <div style='background:#f9f9f8;border:1px solid #f0f0f0;padding:18px'>
                        <div style='font-size:10px;letter-spacing:0.22em;text-transform:uppercase;color:#b8860b;margin-bottom:12px;font-weight:600'>Giao Hàng</div>
                        <table style='width:100%;font-size:13px;border-collapse:collapse'>
                            <tr><td style='color:#999;padding:4px 0;width:80px;vertical-align:top'>Người nhận</td><td style='color:#0a0a0a;font-weight:600;padding:4px 0'>{order.FullName}</td></tr>
                            <tr><td style='color:#999;padding:4px 0;vertical-align:top'>Điện thoại</td><td style='color:#0a0a0a;font-weight:600;padding:4px 0'>{order.Phone}</td></tr>
                            <tr><td style='color:#999;padding:4px 0;vertical-align:top'>Địa chỉ</td><td style='color:#0a0a0a;padding:4px 0;line-height:1.5'>{order.Address},<br>{order.Ward},<br>{order.District},<br>{order.Province}</td></tr>
                            {(string.IsNullOrEmpty(order.Note) ? "" : $"<tr><td style='color:#999;padding:4px 0;vertical-align:top'>Ghi chú</td><td style='color:#666;padding:4px 0;font-style:italic'>{order.Note}</td></tr>")}
                        </table>
                    </div>
                </td>
                <!-- Payment info -->
                <td style='vertical-align:top;width:50%;padding-left:10px'>
                    <div style='background:#f9f9f8;border:1px solid #f0f0f0;padding:18px'>
                        <div style='font-size:10px;letter-spacing:0.22em;text-transform:uppercase;color:#b8860b;margin-bottom:12px;font-weight:600'>Thanh Toán</div>
                        <table style='width:100%;font-size:13px;border-collapse:collapse'>
                            <tr><td style='color:#999;padding:4px 0;width:80px'>Phương thức</td><td style='color:#0a0a0a;font-weight:600;padding:4px 0'>{order.PaymentMethodDisplay}</td></tr>
                            <tr>
                                <td style='color:#999;padding:4px 0'>Trạng thái</td>
                                <td style='padding:4px 0'>
                                    {(order.PaymentStatus == PaymentStatus.Paid
                                        ? "<span style='font-size:11px;padding:2px 8px;background:#f0fdf4;color:#15803d;border:1px solid #bbf7d0;font-weight:600'>✓ Đã thanh toán</span>"
                                        : "<span style='font-size:11px;padding:2px 8px;background:#fef2f2;color:#c0392b;border:1px solid #fecaca;font-weight:600'>Chưa thanh toán</span>")}
                                </td>
                            </tr>
                            <tr><td style='color:#999;padding:4px 0'>Ngày đặt</td><td style='color:#0a0a0a;padding:4px 0'>{order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}</td></tr>
                            {(order.PaidAt.HasValue ? $"<tr><td style='color:#999;padding:4px 0'>Ngày TT</td><td style='color:#0a0a0a;padding:4px 0'>{order.PaidAt.Value.ToLocalTime():dd/MM/yyyy HH:mm}</td></tr>" : "")}
                        </table>
                    </div>
                </td>
            </tr>
        </table>

    </div>

    <!-- FOOTER -->
    <div style='background:#f5f0e8;padding:24px 36px;border-top:1px solid #e5e5e5;text-align:center'>
        <p style='font-size:13px;color:#888;margin:0 0 8px'>
            Thắc mắc? Liên hệ <a href='mailto:hello@ngostore.vn' style='color:#b8860b;text-decoration:none;font-weight:600'>hello@ngostore.vn</a>
            hoặc hotline <strong style='color:#0a0a0a'>1800 9999</strong>
        </p>
        <p style='font-size:11px;color:#bbb;margin:0;letter-spacing:0.08em'>
            © 2025 NGỘ STORE · Cảm ơn bạn đã tin tưởng mua sắm!
        </p>
    </div>

</div>
</div>
</body>
</html>";
        }

        private static string GetStatusBg(OrderStatus status) => status switch
        {
            OrderStatus.Confirmed => "#eff6ff",
            OrderStatus.Processing => "#faf5ff",
            OrderStatus.Shipping => "#f0f9ff",
            OrderStatus.Delivered => "#f0fdf4",
            OrderStatus.Cancelled => "#fef2f2",
            _ => "#f9f9f8"
        };
    }
}