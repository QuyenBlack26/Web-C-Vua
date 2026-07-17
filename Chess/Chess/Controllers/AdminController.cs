using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Chess.Controllers
{
    /* CONTROLLER QUẢN LÝ TRANG ADMIN */
    public class AdminController : Controller
    {
        private readonly string _connStr = string.Empty;
        private readonly IWebHostEnvironment _env;

        /* LẤY CHUỖI KẾT NỐI DATABASE + MÔI TRƯỜNG WWWROOT */
        public AdminController(IConfiguration config, IWebHostEnvironment env)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "";
            _env = env;
        }

        /* KIỂM TRA TÀI KHOẢN CÓ PHẢI ADMIN VÀ CÒN HOẠT ĐỘNG KHÔNG */
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetString("UserID");

            if (role != "ADMIN" || string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var cmd = new SqlCommand(
                    @"SELECT TrangThai
                      FROM ThongTinUser
                      WHERE UserID = @userId",
                    conn);

                cmd.Parameters.AddWithValue("@userId", userId);

                var result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    HttpContext.Session.Clear();
                    return false;
                }

                bool trangThai = Convert.ToBoolean(result);

                if (trangThai == false)
                {
                    HttpContext.Session.Clear();
                    return false;
                }

                return true;
            }
            catch
            {
                HttpContext.Session.Clear();
                return false;
            }
        }

        /* HIỂN THỊ TRANG DASHBOARD ADMIN */
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var userCmd = new SqlCommand("SELECT COUNT(*) FROM ThongTinUser", conn);
            var botCmd = new SqlCommand("SELECT COUNT(*) FROM Bot", conn);
            var puzzleCmd = new SqlCommand("SELECT COUNT(*) FROM Puzzle", conn);
            var gameCmd = new SqlCommand("SELECT COUNT(*) FROM VanCo", conn);

            ViewBag.TotalUsers = Convert.ToInt32(userCmd.ExecuteScalar());
            ViewBag.TotalBots = Convert.ToInt32(botCmd.ExecuteScalar());
            ViewBag.TotalPuzzles = Convert.ToInt32(puzzleCmd.ExecuteScalar());
            ViewBag.TotalGames = Convert.ToInt32(gameCmd.ExecuteScalar());

            return View();
        }

        /* HIỂN THỊ FORM ĐĂNG NHẬP ADMIN */
        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }

        /* XỬ LÝ ĐĂNG NHẬP ADMIN */
        [HttpPost]
        public IActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            tenDangNhap = tenDangNhap?.Trim();
            matKhau = matKhau?.Trim();

            if (string.IsNullOrWhiteSpace(tenDangNhap) ||
                string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập tài khoản và mật khẩu admin!";
                return View();
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
                        v.TenVaiTro
                      FROM ThongTinUser u
                      INNER JOIN NguoiDungVaiTro n ON u.UserID = n.UserID
                      INNER JOIN VaiTro v ON n.RoleID = v.RoleID
                      WHERE u.TenDangNhap = @ten
                        AND u.MatKhau = @mk
                        AND v.TenVaiTro = N'ADMIN'",
                    conn);

                cmd.Parameters.AddWithValue("@ten", tenDangNhap);
                cmd.Parameters.AddWithValue("@mk", matKhau);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    bool trangThai = reader["TrangThai"] == DBNull.Value
                        ? true
                        : Convert.ToBoolean(reader["TrangThai"]);

                    if (trangThai == false)
                    {
                        ViewBag.Error = "Tài khoản admin này đã bị khóa!";
                        return View();
                    }

                    HttpContext.Session.SetString("UserID", reader["UserID"]?.ToString() ?? "");
                    HttpContext.Session.SetString("TenDangNhap", reader["TenDangNhap"]?.ToString() ?? "");
                    HttpContext.Session.SetString("Role", "ADMIN");

                    if (reader["HoTen"] != DBNull.Value)
                    {
                        HttpContext.Session.SetString("HoTen", reader["HoTen"]?.ToString() ?? "");
                    }

                    if (reader["Avatar"] != DBNull.Value)
                    {
                        HttpContext.Session.SetString("Avatar", reader["Avatar"]?.ToString() ?? "");
                    }

                    return RedirectToAction("Index", "Admin");
                }

                ViewBag.Error = "Tài khoản admin hoặc mật khẩu không đúng!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi đăng nhập admin: " + ex.Message;
                return View();
            }
        }

        /* HIỂN THỊ DANH SÁCH TÀI KHOẢN */
        public IActionResult Users()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            var users = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT 
                    u.UserID,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Gmail,
                    u.SoDienThoai,
                    u.NgaySinh,
                    u.GioiTinh,
                    u.Avatar,
                    u.NgayTao,
                    u.NgayCapNhat,
                    u.TrangThai,
                    ISNULL(
                        (
                            SELECT TOP 1 v.TenVaiTro
                            FROM NguoiDungVaiTro n
                            INNER JOIN VaiTro v ON n.RoleID = v.RoleID
                            WHERE n.UserID = u.UserID
                            ORDER BY 
                                CASE 
                                    WHEN v.TenVaiTro = N'ADMIN' THEN 1
                                    WHEN v.TenVaiTro = N'USER' THEN 2
                                    ELSE 3
                                END
                        ),
                        N'USER'
                    ) AS TenVaiTro
                  FROM ThongTinUser u
                  ORDER BY u.UserID DESC",
                conn);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(ReadRow(reader));
            }

            ViewBag.Users = users;
            return View();
        }

        /* HIỂN THỊ FORM SỬA TÀI KHOẢN */
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT 
                    UserID,
                    TenDangNhap,
                    HoTen,
                    Gmail,
                    SoDienThoai,
                    NgaySinh,
                    GioiTinh,
                    Avatar,
                    TrangThai
                  FROM ThongTinUser
                  WHERE UserID = @id",
                conn);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                ViewBag.UserID = reader["UserID"];
                ViewBag.TenDangNhap = reader["TenDangNhap"]?.ToString();
                ViewBag.HoTen = reader["HoTen"] == DBNull.Value ? "" : reader["HoTen"]?.ToString();
                ViewBag.Gmail = reader["Gmail"] == DBNull.Value ? "" : reader["Gmail"]?.ToString();
                ViewBag.SoDienThoai = reader["SoDienThoai"] == DBNull.Value ? "" : reader["SoDienThoai"]?.ToString();
                ViewBag.GioiTinh = reader["GioiTinh"] == DBNull.Value ? "" : reader["GioiTinh"]?.ToString();
                ViewBag.Avatar = reader["Avatar"] == DBNull.Value ? "/images/default-avatar.png" : reader["Avatar"]?.ToString();
                ViewBag.TrangThai = reader["TrangThai"] == DBNull.Value ? "1" : reader["TrangThai"]?.ToString();

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

            return RedirectToAction("Users");
        }

        /* HIỂN THỊ FORM THÊM TÀI KHOẢN */
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            return View();
        }

        /* XỬ LÝ THÊM TÀI KHOẢN */
        [HttpPost]
        public IActionResult CreateUser(
            string tenDangNhap,
            string gmail,
            string matKhau,
            string xacNhanMatKhau,
            string hoTen,
            string soDienThoai,
            string gioiTinh,
            string trangThai,
            string role)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            if (string.IsNullOrWhiteSpace(tenDangNhap) ||
                string.IsNullOrWhiteSpace(gmail) ||
                string.IsNullOrWhiteSpace(matKhau) ||
                string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ tên đăng nhập, Gmail và mật khẩu!";
                return RedirectToAction("CreateUser");
            }

            if (matKhau != xacNhanMatKhau)
            {
                TempData["Error"] = "Mật khẩu nhập lại không khớp!";
                return RedirectToAction("CreateUser");
            }

            if (matKhau.Length < 6)
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                return RedirectToAction("CreateUser");
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
                    TempData["Error"] = "Số điện thoại phải đúng dạng quốc tế. Ví dụ: +84901234567";
                    return RedirectToAction("CreateUser");
                }
            }

            if (!string.IsNullOrWhiteSpace(gioiTinh))
            {
                gioiTinh = gioiTinh.ToUpper();

                if (gioiTinh != "NAM" && gioiTinh != "NU" && gioiTinh != "KHAC")
                {
                    TempData["Error"] = "Giới tính không hợp lệ!";
                    return RedirectToAction("CreateUser");
                }
            }
            else
            {
                gioiTinh = null;
            }

            bool trangThaiBit = trangThai == "1";

            if (string.IsNullOrWhiteSpace(role))
            {
                role = "USER";
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

                checkCmd.Parameters.AddWithValue("@ten", tenDangNhap.Trim());
                checkCmd.Parameters.AddWithValue("@gmail", gmail.Trim());

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["Error"] = "Tên đăng nhập hoặc Gmail đã tồn tại!";
                    return RedirectToAction("CreateUser");
                }

                var insertCmd = new SqlCommand(
                    @"INSERT INTO ThongTinUser
                        (TenDangNhap, MatKhau, HoTen, Gmail, SoDienThoai, GioiTinh, TrangThai)
                      VALUES
                        (@ten, @mk, @hoTen, @gmail, @sdt, @gioiTinh, @trangThai);
                      SELECT SCOPE_IDENTITY();",
                    conn);

                insertCmd.Parameters.AddWithValue("@ten", tenDangNhap.Trim());
                insertCmd.Parameters.AddWithValue("@mk", matKhau.Trim());
                insertCmd.Parameters.AddWithValue("@hoTen", string.IsNullOrWhiteSpace(hoTen) ? (object)DBNull.Value : hoTen.Trim());
                insertCmd.Parameters.AddWithValue("@gmail", gmail.Trim());
                insertCmd.Parameters.AddWithValue("@sdt", string.IsNullOrWhiteSpace(soDienThoai) ? (object)DBNull.Value : soDienThoai.Trim());
                insertCmd.Parameters.AddWithValue("@gioiTinh", string.IsNullOrWhiteSpace(gioiTinh) ? (object)DBNull.Value : gioiTinh);
                insertCmd.Parameters.AddWithValue("@trangThai", trangThaiBit);

                int newUserId = Convert.ToInt32(insertCmd.ExecuteScalar());

                var roleCmd = new SqlCommand(
                    @"INSERT INTO NguoiDungVaiTro (UserID, RoleID)
                      SELECT @userId, RoleID
                      FROM VaiTro
                      WHERE TenVaiTro = @role",
                    conn);

                roleCmd.Parameters.AddWithValue("@userId", newUserId);
                roleCmd.Parameters.AddWithValue("@role", role);
                roleCmd.ExecuteNonQuery();
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

                TempData["Success"] = "Thêm tài khoản thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi thêm tài khoản: " + ex.Message;
                return RedirectToAction("CreateUser");
            }
        }

        /* XỬ LÝ CẬP NHẬT TÀI KHOẢN */
        [HttpPost]
        public IActionResult EditUser(
            int userId,
            string hoTen,
            string gmail,
            string soDienThoai,
            DateTime? ngaySinh,
            string gioiTinh,
            string trangThai)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            if (string.IsNullOrWhiteSpace(gmail))
            {
                TempData["Error"] = "Gmail không được để trống!";
                return RedirectToAction("EditUser", new { id = userId });
            }

            if (!string.IsNullOrWhiteSpace(gioiTinh))
            {
                gioiTinh = gioiTinh.ToUpper();

                if (gioiTinh != "NAM" && gioiTinh != "NU" && gioiTinh != "KHAC")
                {
                    TempData["Error"] = "Giới tính không hợp lệ!";
                    return RedirectToAction("EditUser", new { id = userId });
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
                    TempData["Error"] = "Số điện thoại phải đúng dạng quốc tế. Ví dụ: +84901234567";
                    return RedirectToAction("EditUser", new { id = userId });
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

                int exists = Convert.ToInt32(checkEmailCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["Error"] = "Gmail này đã được tài khoản khác sử dụng!";
                    return RedirectToAction("EditUser", new { id = userId });
                }

                var updateCmd = new SqlCommand(
                    @"UPDATE ThongTinUser
                      SET HoTen = @hoTen,
                          Gmail = @gmail,
                          SoDienThoai = @soDienThoai,
                          NgaySinh = @ngaySinh,
                          GioiTinh = @gioiTinh,
                          TrangThai = @trangThai,
                          NgayCapNhat = GETDATE()
                      WHERE UserID = @userId",
                    conn);

                updateCmd.Parameters.AddWithValue("@hoTen", string.IsNullOrWhiteSpace(hoTen) ? (object)DBNull.Value : hoTen.Trim());
                updateCmd.Parameters.AddWithValue("@gmail", gmail.Trim());
                updateCmd.Parameters.AddWithValue("@soDienThoai", string.IsNullOrWhiteSpace(soDienThoai) ? (object)DBNull.Value : soDienThoai.Trim());
                updateCmd.Parameters.AddWithValue("@ngaySinh", ngaySinh.HasValue ? ngaySinh.Value : (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@gioiTinh", string.IsNullOrWhiteSpace(gioiTinh) ? (object)DBNull.Value : gioiTinh);

                bool trangThaiBit = trangThai == "1";
                updateCmd.Parameters.AddWithValue("@trangThai", trangThaiBit);
                updateCmd.Parameters.AddWithValue("@userId", userId);

                updateCmd.ExecuteNonQuery();

                TempData["Success"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi cập nhật tài khoản: " + ex.Message;
                return RedirectToAction("EditUser", new { id = userId });
            }
        }

        /* KHÓA HOẶC MỞ KHÓA TÀI KHOẢN USER */
        [HttpPost]
        public IActionResult ToggleUserStatus(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            var currentUserId = HttpContext.Session.GetString("UserID");

            if (currentUserId == id.ToString())
            {
                TempData["Error"] = "Bạn không thể tự khóa tài khoản admin đang đăng nhập!";
                return RedirectToAction("Users");
            }

            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var roleCheckCmd = new SqlCommand(
                    @"SELECT COUNT(*)
                      FROM NguoiDungVaiTro n
                      INNER JOIN VaiTro v ON n.RoleID = v.RoleID
                      WHERE n.UserID = @id
                        AND v.TenVaiTro = N'ADMIN'",
                    conn);

                roleCheckCmd.Parameters.AddWithValue("@id", id);

                int isAdminAccount = Convert.ToInt32(roleCheckCmd.ExecuteScalar());

                if (isAdminAccount > 0)
                {
                    TempData["Error"] = "Không được khóa tài khoản ADMIN!";
                    return RedirectToAction("Users");
                }

                var getCmd = new SqlCommand(
                    @"SELECT ISNULL(TrangThai, 1)
                      FROM ThongTinUser
                      WHERE UserID = @id",
                    conn);

                getCmd.Parameters.AddWithValue("@id", id);

                bool currentStatus = Convert.ToBoolean(getCmd.ExecuteScalar());
                bool newStatus = !currentStatus;

                var updateCmd = new SqlCommand(
                    @"UPDATE ThongTinUser
                      SET TrangThai = @newStatus,
                          NgayCapNhat = GETDATE()
                      WHERE UserID = @id",
                    conn);

                updateCmd.Parameters.AddWithValue("@newStatus", newStatus);
                updateCmd.Parameters.AddWithValue("@id", id);

                updateCmd.ExecuteNonQuery();

                TempData["Success"] = "Cập nhật trạng thái tài khoản thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi đổi trạng thái: " + ex.Message;
                return RedirectToAction("Users");
            }
        }

        /* HIỂN THỊ TRANG QUẢN LÝ SKIN CỜ */
        [HttpGet]
        public IActionResult Skins()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            var boardSkins = new List<Dictionary<string, object>>();
            var pieceSkins = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var boardCmd = new SqlCommand(
                @"SELECT 
                      sb.SkinBanCoID,
                      sb.TenSkin,
                      sb.MaSkin,
                      sb.MauOTrang,
                      sb.MauODen,
                      sb.AnhNenBanCo,
                      sb.AnhOSang,
                      sb.AnhODen,
                      sb.TrangThai,
                      ISNULL(lc.TenLoai, N'Dùng chung') AS TenLoai
                  FROM SkinBanCo sb
                  LEFT JOIN LoaiCo lc ON sb.LoaiCoID = lc.LoaiCoID
                  ORDER BY sb.SkinBanCoID DESC",
                conn);

            using (var reader = boardCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    boardSkins.Add(ReadRow(reader));
                }
            }

            var pieceCmd = new SqlCommand(
                @"SELECT
                      sq.SkinQuanCoID,
                      sq.TenSkin,
                      sq.MaSkin,
                      sq.KieuHienThi,
                      sq.DuongDanThuMuc,
                      sq.CssClass,
                      sq.TrangThai,
                      ISNULL(lc.TenLoai, N'Dùng chung') AS TenLoai,
                      (
                          SELECT COUNT(*)
                          FROM ChiTietSkinQuanCo ct
                          WHERE ct.SkinQuanCoID = sq.SkinQuanCoID
                      ) AS SoQuan
                  FROM SkinQuanCo sq
                  LEFT JOIN LoaiCo lc ON sq.LoaiCoID = lc.LoaiCoID
                  ORDER BY sq.SkinQuanCoID DESC",
                conn);

            using (var reader = pieceCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    pieceSkins.Add(ReadRow(reader));
                }
            }

            ViewBag.BoardSkins = boardSkins;
            ViewBag.PieceSkins = pieceSkins;

            return View();
        }

        /* THÊM SKIN BÀN CỜ */
        [HttpPost]
        public IActionResult AddBoardSkin(
            string tenSkin,
            string maSkin,
            string mauOTrang,
            string mauODen,
            IFormFile? anhBanCo,
            IFormFile? anhOSang,
            IFormFile? anhODen)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            try
            {
                tenSkin = tenSkin?.Trim() ?? "";
                maSkin = NormalizeSkinCode(maSkin);
                mauOTrang = mauOTrang?.Trim() ?? "#f0d9b5";
                mauODen = mauODen?.Trim() ?? "#b58863";

                if (string.IsNullOrWhiteSpace(tenSkin) || string.IsNullOrWhiteSpace(maSkin))
                {
                    TempData["Error"] = "Tên skin và mã skin không được để trống!";
                    return RedirectToAction("Skins");
                }

                string? previewImagePath = null;
                string? lightImagePath = null;
                string? darkImagePath = null;

                string boardFolder = Path.Combine("IMG", "SkinUpload", "BanCo", maSkin);

                if (anhBanCo != null && anhBanCo.Length > 0)
                {
                    previewImagePath = SaveUploadedFile(
                        anhBanCo,
                        boardFolder,
                        "preview.png"
                    );
                }

                if (anhOSang != null && anhOSang.Length > 0)
                {
                    lightImagePath = SaveUploadedFile(
                        anhOSang,
                        boardFolder,
                        "light.png"
                    );
                }

                if (anhODen != null && anhODen.Length > 0)
                {
                    darkImagePath = SaveUploadedFile(
                        anhODen,
                        boardFolder,
                        "dark.png"
                    );
                }

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                int loaiCoId = GetDefaultLoaiCoId(conn);

                var cmd = new SqlCommand(
                    @"INSERT INTO SkinBanCo
                      (
                          LoaiCoID,
                          TenSkin,
                          MaSkin,
                          MauOTrang,
                          MauODen,
                          AnhNenBanCo,
                          AnhOSang,
                          AnhODen,
                          MoTa,
                          TrangThai
                      )
                      VALUES
                      (
                          @loaiCoId,
                          @tenSkin,
                          @maSkin,
                          @mauOTrang,
                          @mauODen,
                          @anhNenBanCo,
                          @anhOSang,
                          @anhODen,
                          N'Skin bàn cờ do admin thêm',
                          1
                      )",
                    conn);

                cmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);
                cmd.Parameters.AddWithValue("@tenSkin", tenSkin);
                cmd.Parameters.AddWithValue("@maSkin", maSkin);
                cmd.Parameters.AddWithValue("@mauOTrang", mauOTrang);
                cmd.Parameters.AddWithValue("@mauODen", mauODen);
                cmd.Parameters.AddWithValue("@anhNenBanCo", string.IsNullOrWhiteSpace(previewImagePath) ? (object)DBNull.Value : previewImagePath);
                cmd.Parameters.AddWithValue("@anhOSang", string.IsNullOrWhiteSpace(lightImagePath) ? (object)DBNull.Value : lightImagePath);
                cmd.Parameters.AddWithValue("@anhODen", string.IsNullOrWhiteSpace(darkImagePath) ? (object)DBNull.Value : darkImagePath);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Đã thêm skin bàn cờ!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi thêm skin bàn cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* THÊM SKIN QUÂN CỜ PNG */
        [HttpPost]
        public IActionResult AddPieceSkin(
            string tenSkin,
            string maSkin,
            IFormFile wK,
            IFormFile wQ,
            IFormFile wR,
            IFormFile wB,
            IFormFile wN,
            IFormFile wP,
            IFormFile bK,
            IFormFile bQ,
            IFormFile bR,
            IFormFile bB,
            IFormFile bN,
            IFormFile bP)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                tenSkin = tenSkin?.Trim() ?? "";
                maSkin = NormalizeSkinCode(maSkin);

                if (string.IsNullOrWhiteSpace(tenSkin) || string.IsNullOrWhiteSpace(maSkin))
                {
                    throw new Exception("Tên skin và mã skin không được để trống!");
                }

                int loaiCoId = GetDefaultLoaiCoId(conn, tran);

                string folderRelative = Path.Combine("IMG", "SkinUpload", "QuanCo", maSkin);
                string webFolder = "/" + folderRelative.Replace("\\", "/") + "/";

                SaveUploadedFile(wK, folderRelative, "wK.png");
                SaveUploadedFile(wQ, folderRelative, "wQ.png");
                SaveUploadedFile(wR, folderRelative, "wR.png");
                SaveUploadedFile(wB, folderRelative, "wB.png");
                SaveUploadedFile(wN, folderRelative, "wN.png");
                SaveUploadedFile(wP, folderRelative, "wP.png");

                SaveUploadedFile(bK, folderRelative, "bK.png");
                SaveUploadedFile(bQ, folderRelative, "bQ.png");
                SaveUploadedFile(bR, folderRelative, "bR.png");
                SaveUploadedFile(bB, folderRelative, "bB.png");
                SaveUploadedFile(bN, folderRelative, "bN.png");
                SaveUploadedFile(bP, folderRelative, "bP.png");

                var skinCmd = new SqlCommand(
                    @"INSERT INTO SkinQuanCo
                      (
                          LoaiCoID,
                          TenSkin,
                          MaSkin,
                          KieuHienThi,
                          DuongDanThuMuc,
                          CssClass,
                          MoTa,
                          TrangThai
                      )
                      OUTPUT INSERTED.SkinQuanCoID
                      VALUES
                      (
                          @loaiCoId,
                          @tenSkin,
                          @maSkin,
                          N'IMAGE',
                          @duongDanThuMuc,
                          @cssClass,
                          N'Bộ quân cờ PNG do admin thêm',
                          1
                      )",
                    conn,
                    tran);

                skinCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);
                skinCmd.Parameters.AddWithValue("@tenSkin", tenSkin);
                skinCmd.Parameters.AddWithValue("@maSkin", maSkin);
                skinCmd.Parameters.AddWithValue("@duongDanThuMuc", webFolder);
                skinCmd.Parameters.AddWithValue("@cssClass", "piece-" + maSkin);

                int skinQuanCoId = Convert.ToInt32(skinCmd.ExecuteScalar());

                var detailCmd = new SqlCommand(
                    @"INSERT INTO ChiTietSkinQuanCo
                      (
                          SkinQuanCoID,
                          MaQuan,
                          KyTuUnicode,
                          FileAnh
                      )
                      VALUES
                      (@skinQuanCoId, 'wK', NULL, 'wK.png'),
                      (@skinQuanCoId, 'wQ', NULL, 'wQ.png'),
                      (@skinQuanCoId, 'wR', NULL, 'wR.png'),
                      (@skinQuanCoId, 'wB', NULL, 'wB.png'),
                      (@skinQuanCoId, 'wN', NULL, 'wN.png'),
                      (@skinQuanCoId, 'wP', NULL, 'wP.png'),
                      (@skinQuanCoId, 'bK', NULL, 'bK.png'),
                      (@skinQuanCoId, 'bQ', NULL, 'bQ.png'),
                      (@skinQuanCoId, 'bR', NULL, 'bR.png'),
                      (@skinQuanCoId, 'bB', NULL, 'bB.png'),
                      (@skinQuanCoId, 'bN', NULL, 'bN.png'),
                      (@skinQuanCoId, 'bP', NULL, 'bP.png')",
                    conn,
                    tran);

                detailCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                detailCmd.ExecuteNonQuery();

                tran.Commit();

                TempData["Success"] = "Đã thêm skin quân cờ!";
            }
            catch (Exception ex)
            {
                tran.Rollback();
                TempData["Error"] = "Lỗi thêm skin quân cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* SỬA SKIN BÀN CỜ */
        [HttpPost]
        public IActionResult EditBoardSkin(
            int skinBanCoId,
            string tenSkin,
            string maSkin,
            string mauOTrang,
            string mauODen,
            IFormFile? anhBanCo,
            IFormFile? anhOSang,
            IFormFile? anhODen)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            try
            {
                tenSkin = tenSkin?.Trim() ?? "";
                maSkin = NormalizeSkinCode(maSkin);
                mauOTrang = mauOTrang?.Trim() ?? "#f0d9b5";
                mauODen = mauODen?.Trim() ?? "#b58863";

                if (skinBanCoId <= 0)
                {
                    TempData["Error"] = "Skin bàn cờ không hợp lệ!";
                    return RedirectToAction("Skins");
                }

                if (string.IsNullOrWhiteSpace(tenSkin) || string.IsNullOrWhiteSpace(maSkin))
                {
                    TempData["Error"] = "Tên skin và mã skin không được để trống!";
                    return RedirectToAction("Skins");
                }

                string boardFolder = Path.Combine("IMG", "SkinUpload", "BanCo", maSkin);

                string? previewImagePath = null;
                string? lightImagePath = null;
                string? darkImagePath = null;

                if (anhBanCo != null && anhBanCo.Length > 0)
                {
                    previewImagePath = SaveUploadedFile(anhBanCo, boardFolder, "preview.png");
                }

                if (anhOSang != null && anhOSang.Length > 0)
                {
                    lightImagePath = SaveUploadedFile(anhOSang, boardFolder, "light.png");
                }

                if (anhODen != null && anhODen.Length > 0)
                {
                    darkImagePath = SaveUploadedFile(anhODen, boardFolder, "dark.png");
                }

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var cmd = new SqlCommand(
                    @"UPDATE SkinBanCo
                      SET TenSkin = @tenSkin,
                          MaSkin = @maSkin,
                          MauOTrang = @mauOTrang,
                          MauODen = @mauODen,
                          AnhNenBanCo = CASE WHEN @anhNenBanCo IS NULL THEN AnhNenBanCo ELSE @anhNenBanCo END,
                          AnhOSang = CASE WHEN @anhOSang IS NULL THEN AnhOSang ELSE @anhOSang END,
                          AnhODen = CASE WHEN @anhODen IS NULL THEN AnhODen ELSE @anhODen END
                      WHERE SkinBanCoID = @skinBanCoId",
                    conn);

                cmd.Parameters.AddWithValue("@tenSkin", tenSkin);
                cmd.Parameters.AddWithValue("@maSkin", maSkin);
                cmd.Parameters.AddWithValue("@mauOTrang", mauOTrang);
                cmd.Parameters.AddWithValue("@mauODen", mauODen);
                cmd.Parameters.AddWithValue("@anhNenBanCo", string.IsNullOrWhiteSpace(previewImagePath) ? (object)DBNull.Value : previewImagePath);
                cmd.Parameters.AddWithValue("@anhOSang", string.IsNullOrWhiteSpace(lightImagePath) ? (object)DBNull.Value : lightImagePath);
                cmd.Parameters.AddWithValue("@anhODen", string.IsNullOrWhiteSpace(darkImagePath) ? (object)DBNull.Value : darkImagePath);
                cmd.Parameters.AddWithValue("@skinBanCoId", skinBanCoId);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Đã sửa skin bàn cờ!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi sửa skin bàn cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* XÓA SKIN BÀN CỜ */
        [HttpPost]
        public IActionResult DeleteBoardSkin(int skinBanCoId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            if (skinBanCoId <= 0)
            {
                TempData["Error"] = "Skin bàn cờ không hợp lệ!";
                return RedirectToAction("Skins");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                // Xóa lựa chọn skin của user đang dùng skin bàn cờ này
                var deleteUserSkinCmd = new SqlCommand(
                    @"DELETE FROM CaiDatSkinNguoiDung
              WHERE SkinBanCoID = @skinBanCoId",
                    conn,
                    tran);

                deleteUserSkinCmd.Parameters.AddWithValue("@skinBanCoId", skinBanCoId);
                deleteUserSkinCmd.ExecuteNonQuery();

                // Xóa thật skin bàn cờ
                var deleteBoardCmd = new SqlCommand(
                    @"DELETE FROM SkinBanCo
              WHERE SkinBanCoID = @skinBanCoId",
                    conn,
                    tran);

                deleteBoardCmd.Parameters.AddWithValue("@skinBanCoId", skinBanCoId);
                int rows = deleteBoardCmd.ExecuteNonQuery();

                if (rows <= 0)
                {
                    throw new Exception("Không tìm thấy skin bàn cờ cần xóa.");
                }

                tran.Commit();

                TempData["Success"] = "Đã xóa vĩnh viễn skin bàn cờ!";
            }
            catch (Exception ex)
            {
                tran.Rollback();
                TempData["Error"] = "Lỗi xóa skin bàn cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* SỬA SKIN QUÂN CỜ */
        [HttpPost]
        public IActionResult EditPieceSkin(
            int skinQuanCoId,
            string tenSkin,
            string maSkin,
            IFormFile? wK,
            IFormFile? wQ,
            IFormFile? wR,
            IFormFile? wB,
            IFormFile? wN,
            IFormFile? wP,
            IFormFile? bK,
            IFormFile? bQ,
            IFormFile? bR,
            IFormFile? bB,
            IFormFile? bN,
            IFormFile? bP)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            try
            {
                tenSkin = tenSkin?.Trim() ?? "";
                maSkin = NormalizeSkinCode(maSkin);

                if (skinQuanCoId <= 0)
                {
                    TempData["Error"] = "Skin quân cờ không hợp lệ!";
                    return RedirectToAction("Skins");
                }

                if (string.IsNullOrWhiteSpace(tenSkin) || string.IsNullOrWhiteSpace(maSkin))
                {
                    TempData["Error"] = "Tên skin và mã skin không được để trống!";
                    return RedirectToAction("Skins");
                }

                string folderRelative = Path.Combine("IMG", "SkinUpload", "QuanCo", maSkin);
                string webFolder = "/" + folderRelative.Replace("\\", "/") + "/";

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                using var tran = conn.BeginTransaction();

                try
                {
                    var updateCmd = new SqlCommand(
                        @"UPDATE SkinQuanCo
                          SET TenSkin = @tenSkin,
                              MaSkin = @maSkin,
                              DuongDanThuMuc = @duongDanThuMuc,
                              CssClass = @cssClass
                          WHERE SkinQuanCoID = @skinQuanCoId",
                        conn,
                        tran);

                    updateCmd.Parameters.AddWithValue("@tenSkin", tenSkin);
                    updateCmd.Parameters.AddWithValue("@maSkin", maSkin);
                    updateCmd.Parameters.AddWithValue("@duongDanThuMuc", webFolder);
                    updateCmd.Parameters.AddWithValue("@cssClass", "piece-" + maSkin);
                    updateCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);

                    updateCmd.ExecuteNonQuery();

                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wK", wK, folderRelative, "wK.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wQ", wQ, folderRelative, "wQ.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wR", wR, folderRelative, "wR.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wB", wB, folderRelative, "wB.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wN", wN, folderRelative, "wN.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "wP", wP, folderRelative, "wP.png");

                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bK", bK, folderRelative, "bK.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bQ", bQ, folderRelative, "bQ.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bR", bR, folderRelative, "bR.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bB", bB, folderRelative, "bB.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bN", bN, folderRelative, "bN.png");
                    UpdatePieceImageIfProvided(conn, tran, skinQuanCoId, "bP", bP, folderRelative, "bP.png");

                    tran.Commit();

                    TempData["Success"] = "Đã sửa skin quân cờ!";
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi sửa skin quân cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* XÓA SKIN QUÂN CỜ */
        [HttpPost]
        public IActionResult DeletePieceSkin(int skinQuanCoId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            if (skinQuanCoId <= 0)
            {
                TempData["Error"] = "Skin quân cờ không hợp lệ!";
                return RedirectToAction("Skins");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                // Xóa lựa chọn skin của user đang dùng skin quân này
                var deleteUserSkinCmd = new SqlCommand(
                    @"DELETE FROM CaiDatSkinNguoiDung
              WHERE SkinQuanCoID = @skinQuanCoId",
                    conn,
                    tran);

                deleteUserSkinCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                deleteUserSkinCmd.ExecuteNonQuery();

                // Xóa chi tiết 12 quân trước
                var deleteDetailCmd = new SqlCommand(
                    @"DELETE FROM ChiTietSkinQuanCo
              WHERE SkinQuanCoID = @skinQuanCoId",
                    conn,
                    tran);

                deleteDetailCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                deleteDetailCmd.ExecuteNonQuery();

                // Xóa thật skin quân cờ
                var deletePieceCmd = new SqlCommand(
                    @"DELETE FROM SkinQuanCo
              WHERE SkinQuanCoID = @skinQuanCoId",
                    conn,
                    tran);

                deletePieceCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                int rows = deletePieceCmd.ExecuteNonQuery();

                if (rows <= 0)
                {
                    throw new Exception("Không tìm thấy skin quân cờ cần xóa.");
                }

                tran.Commit();

                TempData["Success"] = "Đã xóa vĩnh viễn skin quân cờ!";
            }
            catch (Exception ex)
            {
                tran.Rollback();
                TempData["Error"] = "Lỗi xóa skin quân cờ: " + ex.Message;
            }

            return RedirectToAction("Skins");
        }

        /* ĐỌC 1 DÒNG SQL THÀNH DICTIONARY */
        private Dictionary<string, object> ReadRow(SqlDataReader reader)
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader[i] == DBNull.Value ? "" : reader[i];
            }

            return row;
        }

        /* LẤY LOẠI CỜ MẶC ĐỊNH */
        private int GetDefaultLoaiCoId(SqlConnection conn, SqlTransaction? tran = null)
        {
            var cmd = new SqlCommand(
                @"SELECT TOP 1 LoaiCoID
                  FROM LoaiCo
                  WHERE TenLoai IN (N'Cờ vua', N'Chess')
                  ORDER BY LoaiCoID",
                conn,
                tran);

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
            {
                throw new Exception("Không tìm thấy loại cờ mặc định.");
            }

            return Convert.ToInt32(result);
        }

        /* LƯU FILE PNG VÀO WWWROOT */
        private string SaveUploadedFile(IFormFile file, string relativeFolder, string? fixedFileName = null)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception("File upload không hợp lệ.");
            }

            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".png")
            {
                throw new Exception("Chỉ hỗ trợ file PNG.");
            }

            string folderPath = Path.Combine(_env.WebRootPath, relativeFolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = fixedFileName ?? $"{Guid.NewGuid():N}.png";
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return "/" + Path.Combine(relativeFolder, fileName).Replace("\\", "/");
        }

        /* CẬP NHẬT ẢNH QUÂN CỜ NẾU ADMIN CÓ UPLOAD FILE MỚI */
        private void UpdatePieceImageIfProvided(
            SqlConnection conn,
            SqlTransaction tran,
            int skinQuanCoId,
            string maQuan,
            IFormFile? file,
            string folderRelative,
            string fixedFileName)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            SaveUploadedFile(file, folderRelative, fixedFileName);

            var checkCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM ChiTietSkinQuanCo
                  WHERE SkinQuanCoID = @skinQuanCoId
                    AND MaQuan = @maQuan",
                conn,
                tran);

            checkCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
            checkCmd.Parameters.AddWithValue("@maQuan", maQuan);

            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (exists > 0)
            {
                var updateCmd = new SqlCommand(
                    @"UPDATE ChiTietSkinQuanCo
                      SET FileAnh = @fileAnh
                      WHERE SkinQuanCoID = @skinQuanCoId
                        AND MaQuan = @maQuan",
                    conn,
                    tran);

                updateCmd.Parameters.AddWithValue("@fileAnh", fixedFileName);
                updateCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                updateCmd.Parameters.AddWithValue("@maQuan", maQuan);

                updateCmd.ExecuteNonQuery();
            }
            else
            {
                var insertCmd = new SqlCommand(
                    @"INSERT INTO ChiTietSkinQuanCo
                      (
                          SkinQuanCoID,
                          MaQuan,
                          KyTuUnicode,
                          FileAnh
                      )
                      VALUES
                      (
                          @skinQuanCoId,
                          @maQuan,
                          NULL,
                          @fileAnh
                      )",
                    conn,
                    tran);

                insertCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                insertCmd.Parameters.AddWithValue("@maQuan", maQuan);
                insertCmd.Parameters.AddWithValue("@fileAnh", fixedFileName);

                insertCmd.ExecuteNonQuery();
            }
        }

        /* CHUẨN HÓA MÃ SKIN ĐỂ LÀM TÊN THƯ MỤC */
        private string NormalizeSkinCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            value = value.Trim().ToLower();

            var chars = value
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
                .ToArray();

            return new string(chars);
        }

        /* HIỂN THỊ TRANG QUẢN LÝ CÂU ĐỐ */
        [HttpGet]
        public IActionResult Puzzles()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            var levels = new List<Dictionary<string, object>>();
            var puzzles = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var levelCmd = new SqlCommand(
                @"SELECT 
              CapDoID,
              TenCapDo,
              MaCapDo,
              DiemCong,
              MoTa,
              TrangThai
          FROM CapDoCauDo
          ORDER BY CapDoID",
                conn);

            using (var reader = levelCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    levels.Add(ReadRow(reader));
                }
            }

            var puzzleCmd = new SqlCommand(
                @"SELECT
              p.PuzzleID,
              p.TieuDe,
              p.FEN,
              p.LoiGiai,
              p.DoKho,
              p.MoTa,
              p.LoaiCauDo,
              p.DiemThuong,
              p.TrangThai,
              p.CapDoID,
              cd.TenCapDo
          FROM Puzzle p
          LEFT JOIN CapDoCauDo cd ON p.CapDoID = cd.CapDoID
          ORDER BY p.PuzzleID DESC",
                conn);

            using (var reader = puzzleCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    puzzles.Add(ReadRow(reader));
                }
            }

            ViewBag.Levels = levels;
            ViewBag.Puzzles = puzzles;

            return View();
        }


        /* THÊM CÂU ĐỐ */
        [HttpPost]
        public IActionResult AddPuzzle(
            string tieuDe,
            string fen,
            string loiGiai,
            int capDoId,
            int doKho,
            string loaiCauDo,
            int diemThuong,
            string moTa)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            try
            {
                tieuDe = tieuDe?.Trim() ?? "";
                fen = fen?.Trim() ?? "";
                loiGiai = loiGiai?.Trim() ?? "";
                loaiCauDo = loaiCauDo?.Trim().ToUpper() ?? "MATE";
                moTa = moTa?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(tieuDe) ||
                    string.IsNullOrWhiteSpace(fen) ||
                    string.IsNullOrWhiteSpace(loiGiai))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ tiêu đề, FEN và lời giải!";
                    return RedirectToAction("Puzzles");
                }

                if (loaiCauDo != "MATE" && loaiCauDo != "SAVE")
                {
                    loaiCauDo = "MATE";
                }

                if (doKho <= 0)
                {
                    doKho = 1;
                }

                if (diemThuong <= 0)
                {
                    diemThuong = 10;
                }

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var checkLevelCmd = new SqlCommand(
                    @"SELECT COUNT(*)
              FROM CapDoCauDo
              WHERE CapDoID = @capDoId",
                    conn);

                checkLevelCmd.Parameters.AddWithValue("@capDoId", capDoId);

                int levelExists = Convert.ToInt32(checkLevelCmd.ExecuteScalar());

                if (levelExists == 0)
                {
                    TempData["Error"] = "Cấp độ câu đố không hợp lệ!";
                    return RedirectToAction("Puzzles");
                }

                var cmd = new SqlCommand(
                    @"INSERT INTO Puzzle
              (
                  FEN,
                  LoiGiai,
                  DoKho,
                  MoTa,
                  CapDoID,
                  TieuDe,
                  LoaiCauDo,
                  DiemThuong,
                  TrangThai
              )
              VALUES
              (
                  @fen,
                  @loiGiai,
                  @doKho,
                  @moTa,
                  @capDoId,
                  @tieuDe,
                  @loaiCauDo,
                  @diemThuong,
                  1
              )",
                    conn);

                cmd.Parameters.AddWithValue("@fen", fen);
                cmd.Parameters.AddWithValue("@loiGiai", loiGiai);
                cmd.Parameters.AddWithValue("@doKho", doKho);
                cmd.Parameters.AddWithValue("@moTa", string.IsNullOrWhiteSpace(moTa) ? (object)DBNull.Value : moTa);
                cmd.Parameters.AddWithValue("@capDoId", capDoId);
                cmd.Parameters.AddWithValue("@tieuDe", tieuDe);
                cmd.Parameters.AddWithValue("@loaiCauDo", loaiCauDo);
                cmd.Parameters.AddWithValue("@diemThuong", diemThuong);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Đã thêm câu đố!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi thêm câu đố: " + ex.Message;
            }

            return RedirectToAction("Puzzles");
        }


        /* SỬA CÂU ĐỐ */
        [HttpPost]
        public IActionResult EditPuzzle(
            int puzzleId,
            string tieuDe,
            string fen,
            string loiGiai,
            int capDoId,
            int doKho,
            string loaiCauDo,
            int diemThuong,
            string moTa,
            string trangThai)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            try
            {
                tieuDe = tieuDe?.Trim() ?? "";
                fen = fen?.Trim() ?? "";
                loiGiai = loiGiai?.Trim() ?? "";
                loaiCauDo = loaiCauDo?.Trim().ToUpper() ?? "MATE";
                moTa = moTa?.Trim() ?? "";

                if (puzzleId <= 0)
                {
                    TempData["Error"] = "Câu đố không hợp lệ!";
                    return RedirectToAction("Puzzles");
                }

                if (string.IsNullOrWhiteSpace(tieuDe) ||
                    string.IsNullOrWhiteSpace(fen) ||
                    string.IsNullOrWhiteSpace(loiGiai))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ tiêu đề, FEN và lời giải!";
                    return RedirectToAction("Puzzles");
                }

                if (loaiCauDo != "MATE" && loaiCauDo != "SAVE")
                {
                    loaiCauDo = "MATE";
                }

                if (doKho <= 0)
                {
                    doKho = 1;
                }

                if (diemThuong <= 0)
                {
                    diemThuong = 10;
                }

                bool trangThaiBit = trangThai == "1";

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                var cmd = new SqlCommand(
                    @"UPDATE Puzzle
              SET TieuDe = @tieuDe,
                  FEN = @fen,
                  LoiGiai = @loiGiai,
                  CapDoID = @capDoId,
                  DoKho = @doKho,
                  LoaiCauDo = @loaiCauDo,
                  DiemThuong = @diemThuong,
                  MoTa = @moTa,
                  TrangThai = @trangThai
              WHERE PuzzleID = @puzzleId",
                    conn);

                cmd.Parameters.AddWithValue("@tieuDe", tieuDe);
                cmd.Parameters.AddWithValue("@fen", fen);
                cmd.Parameters.AddWithValue("@loiGiai", loiGiai);
                cmd.Parameters.AddWithValue("@capDoId", capDoId);
                cmd.Parameters.AddWithValue("@doKho", doKho);
                cmd.Parameters.AddWithValue("@loaiCauDo", loaiCauDo);
                cmd.Parameters.AddWithValue("@diemThuong", diemThuong);
                cmd.Parameters.AddWithValue("@moTa", string.IsNullOrWhiteSpace(moTa) ? (object)DBNull.Value : moTa);
                cmd.Parameters.AddWithValue("@trangThai", trangThaiBit);
                cmd.Parameters.AddWithValue("@puzzleId", puzzleId);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Đã sửa câu đố!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi sửa câu đố: " + ex.Message;
            }

            return RedirectToAction("Puzzles");
        }


        /* XÓA THẬT CÂU ĐỐ */
        [HttpPost]
        public IActionResult DeletePuzzle(int puzzleId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            if (puzzleId <= 0)
            {
                TempData["Error"] = "Câu đố không hợp lệ!";
                return RedirectToAction("Puzzles");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var deleteHistoryCmd = new SqlCommand(
                    @"DELETE FROM LichSuLamCauDo
              WHERE PuzzleID = @puzzleId",
                    conn,
                    tran);

                deleteHistoryCmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                deleteHistoryCmd.ExecuteNonQuery();

                var deletePuzzleCmd = new SqlCommand(
                    @"DELETE FROM Puzzle
              WHERE PuzzleID = @puzzleId",
                    conn,
                    tran);

                deletePuzzleCmd.Parameters.AddWithValue("@puzzleId", puzzleId);

                int rows = deletePuzzleCmd.ExecuteNonQuery();

                if (rows <= 0)
                {
                    throw new Exception("Không tìm thấy câu đố cần xóa.");
                }

                tran.Commit();

                TempData["Success"] = "Đã xóa vĩnh viễn câu đố!";
            }
            catch (Exception ex)
            {
                tran.Rollback();
                TempData["Error"] = "Lỗi xóa câu đố: " + ex.Message;
            }

            return RedirectToAction("Puzzles");
        }
    }
}