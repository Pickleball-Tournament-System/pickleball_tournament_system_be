using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PickleballTournamentAPI.Services.VnPayHelper; 

namespace PickleballTournamentAPI.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VnPayService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        // SỬA 1: Chấp nhận 'orderId' có thể là null (string?)
        public string CreatePaymentUrl(string? orderId, decimal amount, string orderInfo)
        {
            // SỬA 2: Xử lý giá trị config có thể null
            // Thêm '?? throw new...' để báo lỗi nếu config bị thiếu
            var vnp_TmnCode = _config["VnPay:TmnCode"]
                ?? throw new ArgumentNullException("VnPay:TmnCode is not configured.");
            var vnp_HashSecret = _config["VnPay:HashSecret"]
                ?? throw new ArgumentNullException("VnPay:HashSecret is not configured.");
            var vnp_Url = _config["VnPay:BaseUrl"]
                ?? throw new ArgumentNullException("VnPay:BaseUrl is not configured.");
            var vnp_ReturnUrl = _config["VnPay:ReturnUrl"]
                ?? throw new ArgumentNullException("VnPay:ReturnUrl is not configured.");
            var vnp_IpnUrl = _config["VnPay:IpnUrl"]
                ?? throw new ArgumentNullException("VnPay:IpnUrl is not configured.");

            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);

            // SỬA 3: Logic bên trong AddRequestData đã kiểm tra null/empty
            // nên 'orderId' (kiểu string?) vẫn an toàn khi truyền vào
            vnpay.AddRequestData("vnp_TxnRef", orderId);

            vnpay.AddRequestData("vnp_IpnUrl", vnp_IpnUrl);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }
    }
}