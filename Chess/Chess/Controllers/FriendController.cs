using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Chess.Controllers
{
    /* CONTROLLER KẾT BẠN - TÌM BẠN - HẸN PHÒNG */
    public class FriendController : Controller
    {
        private readonly string _connStr = string.Empty;

        public FriendController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        }

        /* KIỂM TRA ĐĂNG NHẬP */
        private int? GetCurrentUserId()
        {
            var userIdText = HttpContext.Session.GetString("UserID");

            if (int.TryParse(userIdText, out int userId))
            {
                return userId;
            }

            return null;
        }

        /* ĐỌC 1 DÒNG DATA THÀNH DICTIONARY */
        private Dictionary<string, object> ReadRow(SqlDataReader reader)
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
            }

            return row;
        }

        /* TRANG DANH SÁCH BẠN BÈ */
        [HttpGet]
        public IActionResult Index()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            var friends = new List<Dictionary<string, object>>();
            var incomingRequests = new List<Dictionary<string, object>>();
            var outgoingRequests = new List<Dictionary<string, object>>();
            var schedules = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            /* LẤY DANH SÁCH BẠN BÈ */
            var friendCmd = new SqlCommand(
                @"SELECT 
                    b.BanBeID,
                    b.ThoiGianKetBan,
                    u.UserID,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Avatar,
                    u.Gmail,
                    ISNULL(x.Diem, 1200) AS Diem
                  FROM BanBe b
                  INNER JOIN ThongTinUser u
                    ON u.UserID = CASE 
                        WHEN b.UserID1 = @userId THEN b.UserID2
                        ELSE b.UserID1
                    END
                  OUTER APPLY
                  (
                      SELECT TOP 1 Diem
                      FROM XepHang x
                      WHERE x.UserID = u.UserID
                      ORDER BY x.Diem DESC
                  ) x
                  WHERE (b.UserID1 = @userId OR b.UserID2 = @userId)
                    AND b.TrangThai = 1
                  ORDER BY b.ThoiGianKetBan DESC",
                conn);

            friendCmd.Parameters.AddWithValue("@userId", currentUserId.Value);

            using (var reader = friendCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    friends.Add(ReadRow(reader));
                }
            }

            /* LẤY LỜI MỜI KẾT BẠN ĐẾN */
            var incomingCmd = new SqlCommand(
                @"SELECT 
                    l.LoiMoiID,
                    l.ThoiGianGui,
                    u.UserID,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Avatar,
                    ISNULL(x.Diem, 1200) AS Diem
                  FROM LoiMoiKetBan l
                  INNER JOIN ThongTinUser u ON l.NguoiGuiID = u.UserID
                  OUTER APPLY
                  (
                      SELECT TOP 1 Diem
                      FROM XepHang x
                      WHERE x.UserID = u.UserID
                      ORDER BY x.Diem DESC
                  ) x
                  WHERE l.NguoiNhanID = @userId
                    AND l.TrangThai = N'PENDING'
                  ORDER BY l.ThoiGianGui DESC",
                conn);

            incomingCmd.Parameters.AddWithValue("@userId", currentUserId.Value);

            using (var reader = incomingCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    incomingRequests.Add(ReadRow(reader));
                }
            }

            /* LẤY LỜI MỜI ĐÃ GỬI */
            var outgoingCmd = new SqlCommand(
                @"SELECT 
                    l.LoiMoiID,
                    l.ThoiGianGui,
                    u.UserID,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Avatar,
                    ISNULL(x.Diem, 1200) AS Diem
                  FROM LoiMoiKetBan l
                  INNER JOIN ThongTinUser u ON l.NguoiNhanID = u.UserID
                  OUTER APPLY
                  (
                      SELECT TOP 1 Diem
                      FROM XepHang x
                      WHERE x.UserID = u.UserID
                      ORDER BY x.Diem DESC
                  ) x
                  WHERE l.NguoiGuiID = @userId
                    AND l.TrangThai = N'PENDING'
                  ORDER BY l.ThoiGianGui DESC",
                conn);

            outgoingCmd.Parameters.AddWithValue("@userId", currentUserId.Value);

            using (var reader = outgoingCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    outgoingRequests.Add(ReadRow(reader));
                }
            }

            /* LẤY LỊCH HẸN LIÊN QUAN */
            var scheduleCmd = new SqlCommand(
                @"SELECT 
                    lh.LichHenID,
                    lh.NguoiTaoID,
                    lh.NguoiDuocMoiID,
                    lh.ThoiGianHen,
                    lh.GhiChu,
                    lh.TrangThai,
                    lh.PhongID,
                    cd.TenCheDo,
                    cd.LoaiCheDo,
                    nguoiTao.TenDangNhap AS TenNguoiTao,
                    nguoiTao.HoTen AS HoTenNguoiTao,
                    nguoiTao.Avatar AS AvatarNguoiTao,
                    nguoiMoi.TenDangNhap AS TenNguoiDuocMoi,
                    nguoiMoi.HoTen AS HoTenNguoiDuocMoi,
                    nguoiMoi.Avatar AS AvatarNguoiDuocMoi
                  FROM LichHenPhong lh
                  INNER JOIN CheDoChoi cd ON lh.CheDoID = cd.CheDoID
                  INNER JOIN ThongTinUser nguoiTao ON lh.NguoiTaoID = nguoiTao.UserID
                  INNER JOIN ThongTinUser nguoiMoi ON lh.NguoiDuocMoiID = nguoiMoi.UserID
                  WHERE lh.NguoiTaoID = @userId
                     OR lh.NguoiDuocMoiID = @userId
                  ORDER BY lh.ThoiGianHen DESC",
                conn);

            scheduleCmd.Parameters.AddWithValue("@userId", currentUserId.Value);

            using (var reader = scheduleCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    schedules.Add(ReadRow(reader));
                }
            }

            ViewBag.Friends = friends;
            ViewBag.IncomingRequests = incomingRequests;
            ViewBag.OutgoingRequests = outgoingRequests;
            ViewBag.Schedules = schedules;
            ViewBag.CurrentUserId = currentUserId.Value;

            return View();
        }

        /* TRANG TÌM KIẾM BẠN */
        [HttpGet]
        public IActionResult Search()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        /* API TÌM KIẾM BẠN THEO TỪ KHÓA */
        [HttpGet]
        public IActionResult SearchUsers(string keyword = "")
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn chưa đăng nhập."
                });
            }

            keyword = keyword?.Trim() ?? "";

            if (keyword.Length < 2)
            {
                return Json(new
                {
                    success = true,
                    users = new List<object>()
                });
            }

            var users = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT TOP 20
            u.UserID,
            u.TenDangNhap,
            u.HoTen,
            u.Avatar,
            u.Gmail,
            ISNULL(x.Diem, 1200) AS Diem,

            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM BanBe b
                    WHERE b.TrangThai = 1
                      AND
                      (
                          (b.UserID1 = @currentUserId AND b.UserID2 = u.UserID)
                          OR
                          (b.UserID2 = @currentUserId AND b.UserID1 = u.UserID)
                      )
                )
                THEN N'FRIEND'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM LoiMoiKetBan l
                    WHERE l.NguoiGuiID = @currentUserId
                      AND l.NguoiNhanID = u.UserID
                      AND l.TrangThai = N'PENDING'
                )
                THEN N'SENT'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM LoiMoiKetBan l
                    WHERE l.NguoiGuiID = u.UserID
                      AND l.NguoiNhanID = @currentUserId
                      AND l.TrangThai = N'PENDING'
                )
                THEN N'RECEIVED'

                ELSE N'NONE'
            END AS QuanHe
          FROM ThongTinUser u
          OUTER APPLY
          (
              SELECT TOP 1 Diem
              FROM XepHang x
              WHERE x.UserID = u.UserID
              ORDER BY x.Diem DESC
          ) x
          WHERE u.UserID <> @currentUserId
            AND ISNULL(u.TrangThai, 1) = 1
            AND
            (
                u.TenDangNhap LIKE N'%' + @keyword + N'%'
                OR ISNULL(u.HoTen, N'') LIKE N'%' + @keyword + N'%'
                OR ISNULL(u.Gmail, N'') LIKE N'%' + @keyword + N'%'
            )
          ORDER BY 
            CASE 
                WHEN u.TenDangNhap LIKE @keyword + N'%' THEN 1
                WHEN ISNULL(u.HoTen, N'') LIKE @keyword + N'%' THEN 2
                ELSE 3
            END,
            u.TenDangNhap",
                conn);

            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);
            cmd.Parameters.AddWithValue("@keyword", keyword);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(ReadRow(reader));
            }

            return Json(new
            {
                success = true,
                users = users
            });
        }

        /* GỬI LỜI MỜI KẾT BẠN */
        [HttpPost]
        public IActionResult SendRequest(int receiverId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (currentUserId.Value == receiverId)
            {
                TempData["Error"] = "Bạn không thể tự kết bạn với chính mình.";
                return RedirectToAction("Search");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var checkCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM BanBe
                  WHERE TrangThai = 1
                    AND
                    (
                        (UserID1 = @u1 AND UserID2 = @u2)
                        OR
                        (UserID1 = @u2 AND UserID2 = @u1)
                    )",
                conn);

            checkCmd.Parameters.AddWithValue("@u1", currentUserId.Value);
            checkCmd.Parameters.AddWithValue("@u2", receiverId);

            int isFriend = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (isFriend > 0)
            {
                TempData["Error"] = "Hai người đã là bạn bè.";
                return RedirectToAction("Search");
            }

            var pendingCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM LoiMoiKetBan
                  WHERE TrangThai = N'PENDING'
                    AND
                    (
                        (NguoiGuiID = @u1 AND NguoiNhanID = @u2)
                        OR
                        (NguoiGuiID = @u2 AND NguoiNhanID = @u1)
                    )",
                conn);

            pendingCmd.Parameters.AddWithValue("@u1", currentUserId.Value);
            pendingCmd.Parameters.AddWithValue("@u2", receiverId);

            int hasPending = Convert.ToInt32(pendingCmd.ExecuteScalar());

            if (hasPending > 0)
            {
                TempData["Error"] = "Đã có lời mời kết bạn đang chờ.";
                return RedirectToAction("Search");
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO LoiMoiKetBan(NguoiGuiID, NguoiNhanID)
                  VALUES(@senderId, @receiverId)",
                conn);

            insertCmd.Parameters.AddWithValue("@senderId", currentUserId.Value);
            insertCmd.Parameters.AddWithValue("@receiverId", receiverId);
            insertCmd.ExecuteNonQuery();

            TempData["Success"] = "Đã gửi lời mời kết bạn.";

            return RedirectToAction("Search");
        }

        /* CHẤP NHẬN LỜI MỜI KẾT BẠN */
        [HttpPost]
        public IActionResult AcceptRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                var getCmd = new SqlCommand(
                    @"SELECT NguoiGuiID, NguoiNhanID
                      FROM LoiMoiKetBan
                      WHERE LoiMoiID = @requestId
                        AND NguoiNhanID = @currentUserId
                        AND TrangThai = N'PENDING'",
                    conn,
                    transaction);

                getCmd.Parameters.AddWithValue("@requestId", requestId);
                getCmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

                int senderId = 0;
                int receiverId = 0;

                using (var reader = getCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        senderId = Convert.ToInt32(reader["NguoiGuiID"]);
                        receiverId = Convert.ToInt32(reader["NguoiNhanID"]);
                    }
                }

                if (senderId == 0 || receiverId == 0)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Lời mời không hợp lệ.";
                    return RedirectToAction("Index");
                }

                int userId1 = Math.Min(senderId, receiverId);
                int userId2 = Math.Max(senderId, receiverId);

                var updateCmd = new SqlCommand(
                    @"UPDATE LoiMoiKetBan
                      SET TrangThai = N'ACCEPTED',
                          ThoiGianPhanHoi = SYSDATETIME()
                      WHERE LoiMoiID = @requestId",
                    conn,
                    transaction);

                updateCmd.Parameters.AddWithValue("@requestId", requestId);
                updateCmd.ExecuteNonQuery();

                var insertFriendCmd = new SqlCommand(
                    @"IF NOT EXISTS
                      (
                          SELECT 1
                          FROM BanBe
                          WHERE UserID1 = @userId1 AND UserID2 = @userId2
                      )
                      BEGIN
                          INSERT INTO BanBe(UserID1, UserID2)
                          VALUES(@userId1, @userId2)
                      END",
                    conn,
                    transaction);

                insertFriendCmd.Parameters.AddWithValue("@userId1", userId1);
                insertFriendCmd.Parameters.AddWithValue("@userId2", userId2);
                insertFriendCmd.ExecuteNonQuery();

                transaction.Commit();

                TempData["Success"] = "Đã chấp nhận lời mời kết bạn.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Error"] = "Lỗi chấp nhận lời mời: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        /* TỪ CHỐI LỜI MỜI KẾT BẠN */
        [HttpPost]
        public IActionResult RejectRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"UPDATE LoiMoiKetBan
                  SET TrangThai = N'REJECTED',
                      ThoiGianPhanHoi = SYSDATETIME()
                  WHERE LoiMoiID = @requestId
                    AND NguoiNhanID = @currentUserId
                    AND TrangThai = N'PENDING'",
                conn);

            cmd.Parameters.AddWithValue("@requestId", requestId);
            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData[rows > 0 ? "Success" : "Error"] =
                rows > 0 ? "Đã từ chối lời mời." : "Không tìm thấy lời mời hợp lệ.";

            return RedirectToAction("Index");
        }

        /* HỦY LỜI MỜI ĐÃ GỬI */
        [HttpPost]
        public IActionResult CancelRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"UPDATE LoiMoiKetBan
                  SET TrangThai = N'CANCELLED',
                      ThoiGianPhanHoi = SYSDATETIME()
                  WHERE LoiMoiID = @requestId
                    AND NguoiGuiID = @currentUserId
                    AND TrangThai = N'PENDING'",
                conn);

            cmd.Parameters.AddWithValue("@requestId", requestId);
            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData[rows > 0 ? "Success" : "Error"] =
                rows > 0 ? "Đã hủy lời mời." : "Không tìm thấy lời mời hợp lệ.";

            return RedirectToAction("Index");
        }

        /* HỦY BẠN BÈ */
        [HttpPost]
        public IActionResult RemoveFriend(int friendId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"UPDATE BanBe
                  SET TrangThai = 0
                  WHERE TrangThai = 1
                    AND
                    (
                        (UserID1 = @currentUserId AND UserID2 = @friendId)
                        OR
                        (UserID2 = @currentUserId AND UserID1 = @friendId)
                    )",
                conn);

            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);
            cmd.Parameters.AddWithValue("@friendId", friendId);

            int rows = cmd.ExecuteNonQuery();

            TempData[rows > 0 ? "Success" : "Error"] =
                rows > 0 ? "Đã hủy kết bạn." : "Không tìm thấy bạn bè hợp lệ.";

            return RedirectToAction("Index");
        }

        /* TRANG TẠO LỊCH HẸN */
        [HttpGet]
        public IActionResult Schedule(int friendId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var friendCmd = new SqlCommand(
                @"SELECT TOP 1 UserID, TenDangNhap, HoTen, Avatar
                  FROM ThongTinUser
                  WHERE UserID = @friendId",
                conn);

            friendCmd.Parameters.AddWithValue("@friendId", friendId);

            using (var reader = friendCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    ViewBag.Friend = ReadRow(reader);
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy người chơi.";
                    return RedirectToAction("Index");
                }
            }

            var modes = new List<Dictionary<string, object>>();

            var modeCmd = new SqlCommand(
                @"SELECT CheDoID, TenCheDo, LoaiCheDo, ThoiGian
                  FROM CheDoChoi
                  ORDER BY CheDoID",
                conn);

            using (var reader = modeCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    modes.Add(ReadRow(reader));
                }
            }

            ViewBag.Modes = modes;
            ViewBag.FriendId = friendId;

            return View();
        }

        /* GỬI LỊCH HẸN */
        [HttpPost]
        public IActionResult Schedule(int friendId, int cheDoId, DateTime thoiGianHen, string ghiChu)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (friendId == currentUserId.Value)
            {
                TempData["Error"] = "Bạn không thể tự hẹn chính mình.";
                return RedirectToAction("Index");
            }

            if (thoiGianHen <= DateTime.Now)
            {
                TempData["Error"] = "Thời gian hẹn phải lớn hơn hiện tại.";
                return RedirectToAction("Schedule", new { friendId });
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var checkFriendCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM BanBe
                  WHERE TrangThai = 1
                    AND
                    (
                        (UserID1 = @u1 AND UserID2 = @u2)
                        OR
                        (UserID2 = @u1 AND UserID1 = @u2)
                    )",
                conn);

            checkFriendCmd.Parameters.AddWithValue("@u1", currentUserId.Value);
            checkFriendCmd.Parameters.AddWithValue("@u2", friendId);

            int isFriend = Convert.ToInt32(checkFriendCmd.ExecuteScalar());

            if (isFriend <= 0)
            {
                TempData["Error"] = "Chỉ có thể hẹn phòng với bạn bè.";
                return RedirectToAction("Index");
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO LichHenPhong
                  (
                      NguoiTaoID,
                      NguoiDuocMoiID,
                      CheDoID,
                      ThoiGianHen,
                      GhiChu
                  )
                  VALUES
                  (
                      @nguoiTaoId,
                      @nguoiDuocMoiId,
                      @cheDoId,
                      @thoiGianHen,
                      @ghiChu
                  )",
                conn);

            insertCmd.Parameters.AddWithValue("@nguoiTaoId", currentUserId.Value);
            insertCmd.Parameters.AddWithValue("@nguoiDuocMoiId", friendId);
            insertCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            insertCmd.Parameters.AddWithValue("@thoiGianHen", thoiGianHen);
            insertCmd.Parameters.AddWithValue("@ghiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu.Trim());

            insertCmd.ExecuteNonQuery();

            TempData["Success"] = "Đã gửi lịch hẹn phòng.";

            return RedirectToAction("Index");
        }

        /* CHẤP NHẬN LỊCH HẸN */
        [HttpPost]
        public IActionResult AcceptSchedule(int scheduleId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"UPDATE LichHenPhong
                  SET TrangThai = N'ACCEPTED',
                      ThoiGianCapNhat = SYSDATETIME()
                  WHERE LichHenID = @scheduleId
                    AND NguoiDuocMoiID = @currentUserId
                    AND TrangThai = N'PENDING'",
                conn);

            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData[rows > 0 ? "Success" : "Error"] =
                rows > 0 ? "Đã chấp nhận lịch hẹn." : "Không tìm thấy lịch hẹn hợp lệ.";

            return RedirectToAction("Index");
        }

        /* TỪ CHỐI LỊCH HẸN */
        [HttpPost]
        public IActionResult RejectSchedule(int scheduleId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"UPDATE LichHenPhong
                  SET TrangThai = N'REJECTED',
                      ThoiGianCapNhat = SYSDATETIME()
                  WHERE LichHenID = @scheduleId
                    AND NguoiDuocMoiID = @currentUserId
                    AND TrangThai = N'PENDING'",
                conn);

            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
            cmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData[rows > 0 ? "Success" : "Error"] =
                rows > 0 ? "Đã từ chối lịch hẹn." : "Không tìm thấy lịch hẹn hợp lệ.";

            return RedirectToAction("Index");
        }

        /* TẠO PHÒNG TỪ LỊCH HẸN */
        [HttpPost]
        public IActionResult CreateRoomFromSchedule(int scheduleId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                var getCmd = new SqlCommand(
                    @"SELECT 
                        LichHenID,
                        NguoiTaoID,
                        NguoiDuocMoiID,
                        CheDoID,
                        TrangThai,
                        PhongID
                      FROM LichHenPhong
                      WHERE LichHenID = @scheduleId
                        AND TrangThai = N'ACCEPTED'
                        AND PhongID IS NULL
                        AND
                        (
                            NguoiTaoID = @currentUserId
                            OR NguoiDuocMoiID = @currentUserId
                        )",
                    conn,
                    transaction);

                getCmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                getCmd.Parameters.AddWithValue("@currentUserId", currentUserId.Value);

                int nguoiTaoId = 0;
                int nguoiDuocMoiId = 0;
                int cheDoId = 0;

                using (var reader = getCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nguoiTaoId = Convert.ToInt32(reader["NguoiTaoID"]);
                        nguoiDuocMoiId = Convert.ToInt32(reader["NguoiDuocMoiID"]);
                        cheDoId = Convert.ToInt32(reader["CheDoID"]);
                    }
                }

                if (nguoiTaoId == 0 || nguoiDuocMoiId == 0 || cheDoId == 0)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Lịch hẹn không hợp lệ hoặc đã tạo phòng.";
                    return RedirectToAction("Index");
                }

                var insertRoomCmd = new SqlCommand(
                    @"INSERT INTO Phong
                      (
                          ChuPhongID,
                          KhachID,
                          CheDoID,
                          TrangThai
                      )
                      VALUES
                      (
                          @chuPhongId,
                          @khachId,
                          @cheDoId,
                          N'WAITING'
                      );
                      SELECT SCOPE_IDENTITY();",
                    conn,
                    transaction);

                insertRoomCmd.Parameters.AddWithValue("@chuPhongId", nguoiTaoId);
                insertRoomCmd.Parameters.AddWithValue("@khachId", nguoiDuocMoiId);
                insertRoomCmd.Parameters.AddWithValue("@cheDoId", cheDoId);

                int phongId = Convert.ToInt32(insertRoomCmd.ExecuteScalar());

                var updateScheduleCmd = new SqlCommand(
                    @"UPDATE LichHenPhong
                      SET TrangThai = N'CREATED',
                          PhongID = @phongId,
                          ThoiGianCapNhat = SYSDATETIME()
                      WHERE LichHenID = @scheduleId",
                    conn,
                    transaction);

                updateScheduleCmd.Parameters.AddWithValue("@phongId", phongId);
                updateScheduleCmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                updateScheduleCmd.ExecuteNonQuery();

                transaction.Commit();

                TempData["Success"] = "Đã tạo phòng từ lịch hẹn.";

                return RedirectToAction("Room", "Play", new { id = phongId });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Error"] = "Lỗi tạo phòng: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}