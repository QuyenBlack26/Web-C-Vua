using System.Diagnostics;
using Chess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;

namespace Chess.Controllers
{
    /* CONTROLLER QUẢN LÝ TRANG CHỦ, ĐĂNG NHẬP, ĐĂNG KÝ VÀ THÔNG TIN CÁ NHÂN */
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _connStr = string.Empty;

        /* LẤY LOGGER VÀ CHUỖI KẾT NỐI DATABASE */
        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _connStr = config.GetConnectionString("DefaultConnection");
        }

        /* HIỂN THỊ TRANG CHỦ VÀ KIỂM TRA TÀI KHOẢN CÓ BỊ KHÓA KHÔNG */
        public IActionResult Index()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var userId = HttpContext.Session.GetString("UserID");

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var checkUserCmd = new SqlCommand(
                        @"SELECT TrangThai
                  FROM ThongTinUser
                  WHERE UserID = @userId",
                        conn);

                    checkUserCmd.Parameters.AddWithValue("@userId", userId);

                    var result = checkUserCmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        bool trangThai = Convert.ToBoolean(result);

                        if (trangThai == false)
                        {
                            HttpContext.Session.Clear();
                            TempData["Error"] = "Tài khoản của bạn đã bị khóa!";
                            return RedirectToAction("DangNhap");
                        }
                    }
                }

                ViewBag.TotalPlayers = GetIntValue(
                    conn,
                    @"SELECT COUNT(*)
              FROM ThongTinUser
              WHERE ISNULL(TrangThai, 1) = 1"
                );

                ViewBag.TodayMatches = GetIntValue(
                    conn,
                    @"SELECT COUNT(*)
              FROM VanCo
              WHERE CAST(ISNULL(ThoiGianKetThuc, ThoiGianBatDau) AS DATE) = CAST(GETDATE() AS DATE)"
                );

                ViewBag.TotalModes = GetIntValue(
                    conn,
                    @"SELECT COUNT(*)
              FROM CheDoChoi"
                );
            }
            catch
            {
                ViewBag.TotalPlayers = 0;
                ViewBag.TodayMatches = 0;
                ViewBag.TotalModes = 0;
            }

            return View();
        }

        /* HIỂN THỊ TRANG PRIVACY */
        public IActionResult Privacy()
        {
            return View();
        }

        /* HIỂN THỊ TRANG CHƠI VỚI AI */
        public IActionResult ChoiVoiAI()
        {
            return View();
        }

        /* HIỂN THỊ TRANG CHƠI VỚI AI 3D */
        public IActionResult ChoiVoiAI3D()
        {
            return View();
        }

        // GET - Hiện form đăng nhập
        /* HIỂN THỊ FORM ĐĂNG NHẬP */
        public IActionResult DangNhap()
        {
            return View();
        }

        // POST - Xử lý đăng nhập
        /* XỬ LÝ ĐĂNG NHẬP */
        [HttpPost]
        public IActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            tenDangNhap = tenDangNhap?.Trim();
            matKhau = matKhau?.Trim();

            if (string.IsNullOrWhiteSpace(tenDangNhap) ||
                string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return View("DangNhap");
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var cmd = new SqlCommand(
                @"SELECT TOP 1
                      u.UserID,
                      u.TenDangNhap,
                      u.HoTen,
                      u.Avatar,
                      u.TrangThai,
                      ISNULL(v.TenVaiTro, N'USER') AS TenVaiTro
                  FROM ThongTinUser u
                  LEFT JOIN NguoiDungVaiTro n ON u.UserID = n.UserID
                  LEFT JOIN VaiTro v ON n.RoleID = v.RoleID
                  WHERE u.TenDangNhap = @ten AND u.MatKhau = @mk",
                conn);

                cmd.Parameters.AddWithValue("@ten", tenDangNhap);
                cmd.Parameters.AddWithValue("@mk", matKhau);

                using var reader = cmd.ExecuteReader();

                // Phải reader.Read() trước rồi mới được lấy dữ liệu
                if (reader.Read())
                {
                    string userId = reader["UserID"]?.ToString() ?? "";
                    string ten = reader["TenDangNhap"]?.ToString() ?? "";
                    string hoTen = reader["HoTen"] == DBNull.Value ? "" : reader["HoTen"]?.ToString() ?? "";
                    string avatar = reader["Avatar"] == DBNull.Value ? "" : reader["Avatar"]?.ToString() ?? "";
                    string role = reader["TenVaiTro"] == DBNull.Value ? "USER" : reader["TenVaiTro"]?.ToString() ?? "USER";

                    bool trangThai = reader["TrangThai"] == DBNull.Value
                        ? true
                        : Convert.ToBoolean(reader["TrangThai"]);

                    if (trangThai == false)
                    {
                        ViewBag.Error = "Tài khoản của bạn đã bị khóa!";
                        return View("DangNhap");
                    }

                    HttpContext.Session.SetString("Role", role);
                    HttpContext.Session.SetString("UserID", userId);
                    HttpContext.Session.SetString("TenDangNhap", ten);

                    if (!string.IsNullOrWhiteSpace(hoTen))
                    {
                        HttpContext.Session.SetString("HoTen", hoTen);
                    }
                    else
                    {
                        HttpContext.Session.Remove("HoTen");
                    }

                    if (!string.IsNullOrWhiteSpace(avatar))
                    {
                        HttpContext.Session.SetString("Avatar", avatar);
                    }
                    else
                    {
                        HttpContext.Session.Remove("Avatar");
                    }

                    if (role == "ADMIN")
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Index");
                }

                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View("DangNhap");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi đăng nhập: " + ex.Message;
                return View("DangNhap");
            }
        }

        /* HIỂN THỊ TRANG LỖI */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // GET - Hiện trang quên mật khẩu
        /* HIỂN THỊ TRANG QUÊN MẬT KHẨU */
        public IActionResult QuenMatKhau()
        {
            ViewBag.Step = 1;
            return View();
        }

        // POST - Bước 1: Kiểm tra tài khoản + Gmail, sau đó tạo mã xác minh
        /* TẠO MÃ XÁC MINH QUÊN MẬT KHẨU */
        [HttpPost]
        public IActionResult GuiMaXacMinh(string tenDangNhap, string gmail)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) ||
                string.IsNullOrWhiteSpace(gmail))
            {
                ViewBag.Step = 1;
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và Gmail!";
                return View("QuenMatKhau");
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var checkCmd = new SqlCommand(
                    @"SELECT UserID 
              FROM ThongTinUser
              WHERE TenDangNhap = @ten AND Gmail = @gmail",
                    conn);

                checkCmd.Parameters.AddWithValue("@ten", tenDangNhap.Trim());
                checkCmd.Parameters.AddWithValue("@gmail", gmail.Trim());

                var userId = checkCmd.ExecuteScalar();

                if (userId == null)
                {
                    ViewBag.Step = 1;
                    ViewBag.Error = "Tên đăng nhập hoặc Gmail không đúng!";
                    return View("QuenMatKhau");
                }

                // Tạo mã xác minh 6 số
                var random = new Random();
                string maXacMinh = random.Next(100000, 999999).ToString();

                // Lưu tạm mã vào Session
                HttpContext.Session.SetString("ResetUserID", userId.ToString() ?? "");
                HttpContext.Session.SetString("ResetCode", maXacMinh);
                HttpContext.Session.SetString("ResetExpire", DateTime.Now.AddMinutes(5).ToString());

                ViewBag.Step = 2;

                // Tạm thời hiện mã ra màn hình để test
                // Sau này mình sẽ đổi thành gửi qua Gmail thật
                ViewBag.DevCode = maXacMinh;

                ViewBag.Success = "Đã tạo mã xác minh. Mã có hiệu lực trong 5 phút.";
                return View("QuenMatKhau");
            }
            catch (Exception ex)
            {
                ViewBag.Step = 1;
                ViewBag.Error = "Lỗi tạo mã xác minh: " + ex.Message;
                return View("QuenMatKhau");
            }
        }

        // POST - Bước 2: Xác minh mã và đổi mật khẩu
        /* XÁC MINH MÃ VÀ ĐỔI MẬT KHẨU */
        [HttpPost]
        public IActionResult XacMinhDoiMatKhau(
            string maXacMinh,
            string matKhauMoi,
            string xacNhanMatKhauMoi)
        {
            ViewBag.Step = 2;

            if (string.IsNullOrWhiteSpace(maXacMinh) ||
                string.IsNullOrWhiteSpace(matKhauMoi) ||
                string.IsNullOrWhiteSpace(xacNhanMatKhauMoi))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View("QuenMatKhau");
            }

            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp!";
                return View("QuenMatKhau");
            }

            if (matKhauMoi.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View("QuenMatKhau");
            }

            string? resetUserId = HttpContext.Session.GetString("ResetUserID");
            string? resetCode = HttpContext.Session.GetString("ResetCode");
            string? resetExpire = HttpContext.Session.GetString("ResetExpire");

            if (string.IsNullOrWhiteSpace(resetUserId) ||
                string.IsNullOrWhiteSpace(resetCode) ||
                string.IsNullOrWhiteSpace(resetExpire))
            {
                ViewBag.Step = 1;
                ViewBag.Error = "Phiên xác minh đã hết hạn. Vui lòng tạo mã mới!";
                return View("QuenMatKhau");
            }

            if (!DateTime.TryParse(resetExpire, out DateTime expireTime) ||
                DateTime.Now > expireTime)
            {
                HttpContext.Session.Remove("ResetUserID");
                HttpContext.Session.Remove("ResetCode");
                HttpContext.Session.Remove("ResetExpire");

                ViewBag.Step = 1;
                ViewBag.Error = "Mã xác minh đã hết hạn. Vui lòng tạo mã mới!";
                return View("QuenMatKhau");
            }

            if (maXacMinh.Trim() != resetCode)
            {
                ViewBag.Error = "Mã xác minh không đúng!";
                return View("QuenMatKhau");
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var updateCmd = new SqlCommand(
                    @"UPDATE ThongTinUser
              SET MatKhau = @matKhauMoi
              WHERE UserID = @userId",
                    conn);

                updateCmd.Parameters.AddWithValue("@matKhauMoi", matKhauMoi.Trim());
                updateCmd.Parameters.AddWithValue("@userId", resetUserId);

                updateCmd.ExecuteNonQuery();

                HttpContext.Session.Remove("ResetUserID");
                HttpContext.Session.Remove("ResetCode");
                HttpContext.Session.Remove("ResetExpire");

                ViewBag.Step = 1;
                ViewBag.Success = "Đổi mật khẩu thành công! Bạn có thể đăng nhập lại.";
                return View("QuenMatKhau");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi đổi mật khẩu: " + ex.Message;
                return View("QuenMatKhau");
            }
        }

        //Dang ki
        /* XỬ LÝ ĐĂNG KÝ TÀI KHOẢN */
        [HttpPost]
        public IActionResult DangKy(
            string tenDangNhap,
            string gmail,
            string matKhau,
            string xacNhanMatKhau)
                {
                    if (string.IsNullOrWhiteSpace(tenDangNhap) ||
                        string.IsNullOrWhiteSpace(gmail) ||
                        string.IsNullOrWhiteSpace(matKhau) ||
                        string.IsNullOrWhiteSpace(xacNhanMatKhau))
                    {
                        ViewBag.RegisterError = "Vui lòng nhập đầy đủ thông tin!";
                        return View("DangNhap");
                    }

                    if (matKhau != xacNhanMatKhau)
                    {
                        ViewBag.RegisterError = "Mật khẩu nhập lại không khớp!";
                        return View("DangNhap");
                    }

                    if (matKhau.Length < 6)
                    {
                        ViewBag.RegisterError = "Mật khẩu phải có ít nhất 6 ký tự!";
                        return View("DangNhap");
                    }

                    try
                    {
                        using var conn = new SqlConnection(_connStr);
                        conn.Open();

                        var checkCmd = new SqlCommand(
                            @"SELECT COUNT(*) 
                      FROM ThongTinUser 
                      WHERE TenDangNhap = @ten OR Gmail = @gmail",
                            conn);

                        checkCmd.Parameters.AddWithValue("@ten", tenDangNhap);
                        checkCmd.Parameters.AddWithValue("@gmail", gmail);

                        int exists = (int)checkCmd.ExecuteScalar();

                        if (exists > 0)
                        {
                            ViewBag.RegisterError = "Tên đăng nhập hoặc Gmail đã tồn tại!";
                            return View("DangNhap");
                        }

                        var insertCmd = new SqlCommand(
                            @"INSERT INTO ThongTinUser 
                        (TenDangNhap, MatKhau, Gmail)
                      VALUES 
                        (@ten, @mk, @gmail);
                      SELECT SCOPE_IDENTITY();",
                            conn);

                        insertCmd.Parameters.AddWithValue("@ten", tenDangNhap);
                        insertCmd.Parameters.AddWithValue("@mk", matKhau);
                        insertCmd.Parameters.AddWithValue("@gmail", gmail);

                        int newUserId = Convert.ToInt32(insertCmd.ExecuteScalar());

                        var roleCmd = new SqlCommand(
                            @"INSERT INTO NguoiDungVaiTro (UserID, RoleID)
                      SELECT @userId, RoleID
                      FROM VaiTro
                      WHERE TenVaiTro = N'USER'",
                            conn);

                roleCmd.Parameters.AddWithValue("@userId", newUserId);
                roleCmd.ExecuteNonQuery();

                /* TẠO XẾP HẠNG MẶC ĐỊNH CHO TÀI KHOẢN VỪA ĐĂNG KÝ */
                var rankCmd = new SqlCommand(
                    @"INSERT INTO XepHang
                      (
                          UserID,
                          CheDoID,
                          Diem,
                          SoVan,
                          Thang,
                          Thua,
                          Hoa
                      )
                      SELECT
                          @userId,
                          CheDoID,
                          1200,
                          0,
                          0,
                          0,
                          0
                      FROM CheDoChoi
                      WHERE NOT EXISTS
                      (
                          SELECT 1
                          FROM XepHang
                          WHERE UserID = @userId
                            AND CheDoID = CheDoChoi.CheDoID
                      )",
                                    conn);

                                rankCmd.Parameters.AddWithValue("@userId", newUserId);
                                rankCmd.ExecuteNonQuery();

                                ViewBag.RegisterSuccess = "Đăng ký thành công! Bạn có thể đăng nhập.";
                                return View("DangNhap");
                            }
                                    catch (Exception ex)
                                    {
                                        ViewBag.RegisterError = "Lỗi đăng ký: " + ex.Message;
                                        return View("DangNhap");
                                    }
                                }

        // GET - Trang thông tin cá nhân
        /* HIỂN THỊ TRANG THÔNG TIN CÁ NHÂN */
        [HttpGet]
        public IActionResult ThongTinCaNhan()
        {
            var userId = HttpContext.Session.GetString("UserID");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap");
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var cmd = new SqlCommand(
                    @"SELECT UserID, TenDangNhap, HoTen, SoDienThoai, Gmail, NgaySinh, GioiTinh, Avatar
              FROM ThongTinUser
              WHERE UserID = @userId",
                    conn);

                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    ViewBag.UserID = reader["UserID"]?.ToString();
                    ViewBag.TenDangNhap = reader["TenDangNhap"]?.ToString();
                    ViewBag.HoTen = reader["HoTen"] == DBNull.Value ? "" : reader["HoTen"]?.ToString();
                    ViewBag.SoDienThoai = reader["SoDienThoai"] == DBNull.Value ? "" : reader["SoDienThoai"]?.ToString();
                    ViewBag.Gmail = reader["Gmail"] == DBNull.Value ? "" : reader["Gmail"]?.ToString();
                    ViewBag.GioiTinh = reader["GioiTinh"] == DBNull.Value ? "" : reader["GioiTinh"]?.ToString();
                    ViewBag.Avatar = reader["Avatar"] == DBNull.Value ? "/images/default-avatar.png" : reader["Avatar"]?.ToString();

                    if (reader["NgaySinh"] != DBNull.Value)
                    {
                        ViewBag.NgaySinh = Convert.ToDateTime(reader["NgaySinh"]).ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        ViewBag.NgaySinh = "";
                    }

                    return View();
                }

                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi tải thông tin cá nhân: " + ex.Message;
                return View();
            }
        }


        // POST - Cập nhật thông tin cá nhân
        /* XỬ LÝ CẬP NHẬT THÔNG TIN CÁ NHÂN */
        [HttpPost]
        public IActionResult ThongTinCaNhan(
            string hoTen,
            string soDienThoai,
            string gmail,
            DateTime? ngaySinh,
            string gioiTinh,
            IFormFile avatarFile)
        {
            var userId = HttpContext.Session.GetString("UserID");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap");
            }

            if (string.IsNullOrWhiteSpace(gmail))
            {
                ViewBag.Error = "Gmail không được để trống!";
                return ThongTinCaNhan();
            }

            if (!string.IsNullOrWhiteSpace(gioiTinh))
            {
                gioiTinh = gioiTinh.ToUpper();

                if (gioiTinh != "NAM" && gioiTinh != "NU" && gioiTinh != "KHAC")
                {
                    ViewBag.Error = "Giới tính không hợp lệ!";
                    return ThongTinCaNhan();
                }
            }
            else
            {
                gioiTinh = null;
            }

            if (!string.IsNullOrWhiteSpace(soDienThoai))
            {
                soDienThoai = soDienThoai.Trim();

                if (!soDienThoai.StartsWith("+") ||
                    soDienThoai.Length < 9 ||
                    soDienThoai.Length > 16 ||
                    soDienThoai.StartsWith("+0") ||
                    soDienThoai.Count(c => c == '+') != 1 ||
                    soDienThoai.Any(c => !char.IsDigit(c) && c != '+'))
                {
                    ViewBag.Error = "Số điện thoại không đúng định dạng quốc tế. Ví dụ đúng: +84901234567";
                    return ThongTinCaNhan();
                }
            }

            if (ngaySinh.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - ngaySinh.Value.Year;

                if (ngaySinh.Value.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age < 6 || age > 100)
                {
                    ViewBag.Error = "Tuổi phải từ 6 đến 100!";
                    return ThongTinCaNhan();
                }
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var checkEmailCmd = new SqlCommand(
                    @"SELECT COUNT(*) 
              FROM ThongTinUser
              WHERE Gmail = @gmail AND UserID <> @userId",
                    conn);

                checkEmailCmd.Parameters.AddWithValue("@gmail", gmail.Trim());
                checkEmailCmd.Parameters.AddWithValue("@userId", userId);

                int emailExists = (int)checkEmailCmd.ExecuteScalar();

                if (emailExists > 0)
                {
                    ViewBag.Error = "Gmail này đã được tài khoản khác sử dụng!";
                    return ThongTinCaNhan();
                }

                string avatarPath = null;

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string extension = Path.GetExtension(avatarFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ViewBag.Error = "Chỉ cho phép ảnh JPG, PNG, GIF hoặc WEBP!";
                        return ThongTinCaNhan();
                    }

                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        ViewBag.Error = "Ảnh không được vượt quá 2MB!";
                        return ThongTinCaNhan();
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName = $"avatar_{userId}_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        avatarFile.CopyTo(stream);
                    }

                    avatarPath = "/uploads/avatars/" + fileName;
                }

                SqlCommand updateCmd;

                if (avatarPath != null)
                {
                    updateCmd = new SqlCommand(
                        @"UPDATE ThongTinUser
                  SET HoTen = @hoTen,
                      SoDienThoai = @soDienThoai,
                      Gmail = @gmail,
                      NgaySinh = @ngaySinh,
                      GioiTinh = @gioiTinh,
                      Avatar = @avatar
                  WHERE UserID = @userId",
                        conn);

                    updateCmd.Parameters.AddWithValue("@avatar", avatarPath);
                }
                else
                {
                    updateCmd = new SqlCommand(
                        @"UPDATE ThongTinUser
                  SET HoTen = @hoTen,
                      SoDienThoai = @soDienThoai,
                      Gmail = @gmail,
                      NgaySinh = @ngaySinh,
                      GioiTinh = @gioiTinh
                  WHERE UserID = @userId",
                        conn);
                }

                updateCmd.Parameters.AddWithValue("@hoTen", string.IsNullOrWhiteSpace(hoTen) ? DBNull.Value : hoTen.Trim());
                updateCmd.Parameters.AddWithValue("@soDienThoai", string.IsNullOrWhiteSpace(soDienThoai) ? DBNull.Value : soDienThoai.Trim());
                updateCmd.Parameters.AddWithValue("@gmail", gmail.Trim());
                updateCmd.Parameters.AddWithValue("@ngaySinh", ngaySinh.HasValue ? ngaySinh.Value : DBNull.Value);
                updateCmd.Parameters.AddWithValue("@gioiTinh", string.IsNullOrWhiteSpace(gioiTinh) ? DBNull.Value : gioiTinh);
                updateCmd.Parameters.AddWithValue("@userId", userId);

                updateCmd.ExecuteNonQuery();

                if (avatarPath != null)
                {
                    HttpContext.Session.SetString("Avatar", avatarPath);
                }

                if (!string.IsNullOrWhiteSpace(hoTen))
                {
                    HttpContext.Session.SetString("HoTen", hoTen.Trim());
                }

                ViewBag.Success = "Cập nhật thông tin thành công!";
                return ThongTinCaNhan();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi cập nhật thông tin: " + ex.Message;
                return ThongTinCaNhan();
            }
        }

        /* ĐĂNG XUẤT TÀI KHOẢN */
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Cookies()
        {
            return View();
        }

        private int GetIntValue(SqlConnection conn, string sql)
        {
            using var cmd = new SqlCommand(sql, conn);
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }
    }
}