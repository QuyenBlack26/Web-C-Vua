using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace Chess.Controllers
{
    public class SaveGameRequest
    {
        public string? ModeName { get; set; }
        public string? ModeType { get; set; }
        public string? BotName { get; set; }
        public string? Result { get; set; }
        public string? Fen { get; set; }
        public List<string>? Moves { get; set; }
    }

    public class KetThucVanCoRequest
    {
        public int VanCoId { get; set; }
        public string? KetQua { get; set; }
    }

    public class AnalyzeGameRequest
    {
        public int VanCoId { get; set; }
    }

    public class PlayController : Controller
    {
        private readonly string _connStr = string.Empty;

        public PlayController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserID"));
        }

        public IActionResult Index()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi!";
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        public IActionResult AI()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi với AI!";
                return RedirectToAction("DangNhap", "Home");
            }

            return RedirectToAction("ChoiVoiAI", "Home");
        }

        public IActionResult Local()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi 2 người!";
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        public IActionResult LocalGame(int minutes = 30)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi 2 người!";
                return RedirectToAction("DangNhap", "Home");
            }

            if (minutes != 10 && minutes != 15 && minutes != 30 && minutes != 45 && minutes != 60)
            {
                minutes = 30;
            }

            ViewBag.Minutes = minutes;
            return View();
        }

        public IActionResult Online()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi PvP online!";
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        public IActionResult Puzzle()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi chơi câu đố!";
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        public IActionResult Room()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập trước khi vào phòng PvP!";
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }

        public IActionResult HocCoBan()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Analysis(int cheDoId = 0, int page = 1)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để xem phân tích chi tiết!";
                return RedirectToAction("DangNhap", "Home");
            }

            var userIdText = HttpContext.Session.GetString("UserID");

            if (!int.TryParse(userIdText, out int userId))
            {
                TempData["Error"] = "Không đọc được UserID từ Session!";
                return RedirectToAction("DangNhap", "Home");
            }

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            var modes = new List<Dictionary<string, object>>();
            var games = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var modeCmd = new SqlCommand(
                @"SELECT 
                      c.CheDoID,
                      c.TenCheDo,
                      c.LoaiCheDo,
                      COUNT(v.VanCoID) AS SoVan
                  FROM CheDoChoi c
                  LEFT JOIN VanCo v 
                      ON c.CheDoID = v.CheDoID
                     AND (v.NguoiTrangID = @userId OR v.NguoiDenID = @userId)
                  GROUP BY c.CheDoID, c.TenCheDo, c.LoaiCheDo
                  ORDER BY c.CheDoID",
                conn);

            modeCmd.Parameters.AddWithValue("@userId", userId);

            using (var modeReader = modeCmd.ExecuteReader())
            {
                while (modeReader.Read())
                {
                    var mode = new Dictionary<string, object>();

                    for (int i = 0; i < modeReader.FieldCount; i++)
                    {
                        mode[modeReader.GetName(i)] = modeReader[i] == DBNull.Value ? "" : modeReader[i];
                    }

                    modes.Add(mode);
                }
            }

            if (cheDoId == 0)
            {
                var firstModeWithGame = modes.FirstOrDefault(m => Convert.ToInt32(m["SoVan"]) > 0);

                if (firstModeWithGame != null)
                {
                    cheDoId = Convert.ToInt32(firstModeWithGame["CheDoID"]);
                }
                else if (modes.Count > 0)
                {
                    cheDoId = Convert.ToInt32(modes[0]["CheDoID"]);
                }
            }

            var countCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM VanCo v
                  WHERE v.CheDoID = @cheDoId
                    AND (v.NguoiTrangID = @userId OR v.NguoiDenID = @userId)",
                conn);

            countCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            countCmd.Parameters.AddWithValue("@userId", userId);

            int totalItems = Convert.ToInt32(countCmd.ExecuteScalar());
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages < 1)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            int skip = (page - 1) * pageSize;

            var gameCmd = new SqlCommand(
                @"SELECT
                      v.VanCoID,
                      v.CheDoID,
                      c.TenCheDo,
                      c.LoaiCheDo,
                      v.KetQua,
                      v.TrangThai,
                      v.LuotDi,
                      v.FEN,
                      v.ThoiGianBatDau,
                      v.ThoiGianKetThuc,

                      ISNULL(t.TenDangNhap, N'Người chơi') AS TenNguoiTrang,
                      ISNULL(d.TenDangNhap, N'') AS TenNguoiDen,
                      ISNULL(b.TenBot, N'') AS TenBot,

                      (
                          SELECT COUNT(*) 
                          FROM NuocDi n 
                          WHERE n.VanCoID = v.VanCoID
                      ) AS SoNuoc
                  FROM VanCo v
                  INNER JOIN CheDoChoi c ON v.CheDoID = c.CheDoID
                  LEFT JOIN ThongTinUser t ON v.NguoiTrangID = t.UserID
                  LEFT JOIN ThongTinUser d ON v.NguoiDenID = d.UserID
                  LEFT JOIN Bot b ON v.BotID = b.BotID
                  WHERE v.CheDoID = @cheDoId
                    AND (v.NguoiTrangID = @userId OR v.NguoiDenID = @userId)
                  ORDER BY ISNULL(v.ThoiGianKetThuc, v.ThoiGianBatDau) DESC, v.VanCoID DESC
                  OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY",
                conn);

            gameCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            gameCmd.Parameters.AddWithValue("@userId", userId);
            gameCmd.Parameters.AddWithValue("@skip", skip);
            gameCmd.Parameters.AddWithValue("@pageSize", pageSize);

            using (var gameReader = gameCmd.ExecuteReader())
            {
                while (gameReader.Read())
                {
                    var game = new Dictionary<string, object>();

                    for (int i = 0; i < gameReader.FieldCount; i++)
                    {
                        game[gameReader.GetName(i)] = gameReader[i] == DBNull.Value ? "" : gameReader[i];
                    }

                    games.Add(game);
                }
            }

            ViewBag.Modes = modes;
            ViewBag.Games = games;
            ViewBag.CurrentCheDoId = cheDoId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View();
        }

        [HttpGet]
        public IActionResult AnalysisMoves(int vanCoId)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để xem lịch sử nước đi!"
                });
            }

            var userIdText = HttpContext.Session.GetString("UserID");

            if (!int.TryParse(userIdText, out int userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Không đọc được UserID từ Session!"
                });
            }

            var moves = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var checkCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM VanCo
                  WHERE VanCoID = @vanCoId
                    AND (NguoiTrangID = @userId OR NguoiDenID = @userId)",
                conn);

            checkCmd.Parameters.AddWithValue("@vanCoId", vanCoId);
            checkCmd.Parameters.AddWithValue("@userId", userId);

            int canView = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (canView == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền xem ván cờ này!"
                });
            }

            var moveCmd = new SqlCommand(
                @"SELECT 
                      NuocDiID,
                      VanCoID,
                      SoThuTu,
                      Nuoc,
                      ThoiGian
                  FROM NuocDi
                  WHERE VanCoID = @vanCoId
                  ORDER BY SoThuTu ASC, NuocDiID ASC",
                conn);

            moveCmd.Parameters.AddWithValue("@vanCoId", vanCoId);

            using var reader = moveCmd.ExecuteReader();

            while (reader.Read())
            {
                var move = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    move[reader.GetName(i)] = reader[i] == DBNull.Value ? "" : reader[i];
                }

                moves.Add(move);
            }

            return Json(new
            {
                success = true,
                vanCoId = vanCoId,
                moves = moves
            });
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeGame([FromBody] AnalyzeGameRequest request)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để phân tích ván cờ!"
                });
            }

            if (request == null || request.VanCoId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Ván cờ không hợp lệ!"
                });
            }

            var userIdText = HttpContext.Session.GetString("UserID");

            if (!int.TryParse(userIdText, out int userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Không đọc được UserID từ Session!"
                });
            }

            var moves = new List<string>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var checkCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM VanCo
                  WHERE VanCoID = @vanCoId
                    AND (NguoiTrangID = @userId OR NguoiDenID = @userId)",
                conn);

            checkCmd.Parameters.AddWithValue("@vanCoId", request.VanCoId);
            checkCmd.Parameters.AddWithValue("@userId", userId);

            int canView = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (canView == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền phân tích ván cờ này!"
                });
            }

            var moveCmd = new SqlCommand(
                @"SELECT Nuoc
                  FROM NuocDi
                  WHERE VanCoID = @vanCoId
                  ORDER BY SoThuTu ASC, NuocDiID ASC",
                conn);

            moveCmd.Parameters.AddWithValue("@vanCoId", request.VanCoId);

            using (var reader = moveCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var nuoc = reader["Nuoc"] == DBNull.Value ? "" : reader["Nuoc"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(nuoc))
                    {
                        moves.Add(nuoc.Trim());
                    }
                }
            }

            try
            {
                using var http = new HttpClient();

                var body = new
                {
                    moves = moves
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await http.PostAsync("http://localhost:5000/api/analyze-game", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = "API Python phân tích bị lỗi: " + responseText
                    });
                }

                return Content(responseText, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Không gọi được AI phân tích. Kiểm tra Python Flask đã chạy chưa. Chi tiết: " + ex.Message
                });
            }
        }

        public IActionResult Master()
        {
            return View();
        }

        public IActionResult Ranking(int cheDoId = 0, int page = 1)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để xem bảng xếp hạng!";
                return RedirectToAction("DangNhap", "Home");
            }

            const int pageSize = 20;

            if (page < 1)
            {
                page = 1;
            }

            var rankings = new List<Dictionary<string, object>>();
            var topRankings = new List<Dictionary<string, object>>();
            var modes = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var modeCmd = new SqlCommand(
                @"SELECT CheDoID, TenCheDo, LoaiCheDo
                  FROM CheDoChoi
                  ORDER BY CheDoID",
                conn);

            using (var modeReader = modeCmd.ExecuteReader())
            {
                while (modeReader.Read())
                {
                    var mode = new Dictionary<string, object>();

                    for (int i = 0; i < modeReader.FieldCount; i++)
                    {
                        mode[modeReader.GetName(i)] = modeReader[i] == DBNull.Value ? "" : modeReader[i];
                    }

                    modes.Add(mode);
                }
            }

            if (cheDoId == 0 && modes.Count > 0)
            {
                cheDoId = Convert.ToInt32(modes[0]["CheDoID"]);
            }

            var countCmd = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM XepHang xh
                  INNER JOIN ThongTinUser u ON xh.UserID = u.UserID
                  WHERE ISNULL(u.TrangThai, 1) = 1
                    AND xh.CheDoID = @cheDoId",
                conn);

            countCmd.Parameters.AddWithValue("@cheDoId", cheDoId);

            int totalItems = Convert.ToInt32(countCmd.ExecuteScalar());
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages < 1)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            int skip = (page - 1) * pageSize;

            var topCmd = new SqlCommand(
                @"WITH RankingData AS
                  (
                      SELECT 
                          ROW_NUMBER() OVER 
                          (
                              ORDER BY xh.Diem DESC, xh.Thang DESC, xh.SoVan DESC
                          ) AS Hang,

                          u.UserID,
                          u.TenDangNhap,
                          ISNULL(u.HoTen, '') AS HoTen,
                          ISNULL(u.Avatar, '/images/default-avatar.png') AS Avatar,

                          c.CheDoID,
                          c.TenCheDo,
                          c.LoaiCheDo,

                          xh.Diem,
                          xh.SoVan,
                          xh.Thang,
                          xh.Thua,
                          xh.Hoa
                      FROM XepHang xh
                      INNER JOIN ThongTinUser u ON xh.UserID = u.UserID
                      INNER JOIN CheDoChoi c ON xh.CheDoID = c.CheDoID
                      WHERE ISNULL(u.TrangThai, 1) = 1
                        AND xh.CheDoID = @cheDoId
                  )
                  SELECT TOP 3 *
                  FROM RankingData
                  ORDER BY Hang",
                conn);

            topCmd.Parameters.AddWithValue("@cheDoId", cheDoId);

            using (var topReader = topCmd.ExecuteReader())
            {
                while (topReader.Read())
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < topReader.FieldCount; i++)
                    {
                        row[topReader.GetName(i)] = topReader[i] == DBNull.Value ? "" : topReader[i];
                    }

                    topRankings.Add(row);
                }
            }

            var cmd = new SqlCommand(
                @"WITH RankingData AS
                  (
                      SELECT 
                          ROW_NUMBER() OVER 
                          (
                              ORDER BY xh.Diem DESC, xh.Thang DESC, xh.SoVan DESC
                          ) AS Hang,

                          u.UserID,
                          u.TenDangNhap,
                          ISNULL(u.HoTen, '') AS HoTen,
                          ISNULL(u.Avatar, '/images/default-avatar.png') AS Avatar,

                          c.CheDoID,
                          c.TenCheDo,
                          c.LoaiCheDo,

                          xh.Diem,
                          xh.SoVan,
                          xh.Thang,
                          xh.Thua,
                          xh.Hoa
                      FROM XepHang xh
                      INNER JOIN ThongTinUser u ON xh.UserID = u.UserID
                      INNER JOIN CheDoChoi c ON xh.CheDoID = c.CheDoID
                      WHERE ISNULL(u.TrangThai, 1) = 1
                        AND xh.CheDoID = @cheDoId
                  )
                  SELECT *
                  FROM RankingData
                  ORDER BY Hang
                  OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY",
                conn);

            cmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            cmd.Parameters.AddWithValue("@skip", skip);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader[i] == DBNull.Value ? "" : reader[i];
                }

                rankings.Add(row);
            }

            ViewBag.Rankings = rankings;
            ViewBag.TopRankings = topRankings;
            ViewBag.Modes = modes;
            ViewBag.CurrentCheDoId = cheDoId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View();
        }

        [HttpGet]
        public IActionResult Tournament(int giaiDauId = 0)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để tham gia giải đấu!";
                return RedirectToAction("DangNhap", "Home");
            }

            if (!TryGetCurrentUserId(out int userId))
            {
                TempData["Error"] = "Không đọc được UserID từ Session!";
                return RedirectToAction("DangNhap", "Home");
            }

            var tournaments = new List<Dictionary<string, object>>();
            var players = new List<Dictionary<string, object>>();
            var matches = new List<Dictionary<string, object>>();

            Dictionary<string, object>? selectedTournament = null;

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var listCmd = new SqlCommand(
                @"SELECT 
              gd.GiaiDauID,
              gd.TenGiaiDau,
              gd.TrangThai,
              gd.SoBang,
              gd.SoNguoiToiDa,
              gd.NguoiTaoID,
              gd.NguoiVoDichID,
              gd.ThoiGianTao,
              gd.ThoiGianBatDau,
              gd.ThoiGianKetThuc,
              ISNULL(creator.TenDangNhap, N'') AS TenNguoiTao,
              ISNULL(champion.TenDangNhap, N'') AS TenVoDich,
              (
                  SELECT COUNT(*) 
                  FROM NguoiChoiGiaiDau nc 
                  WHERE nc.GiaiDauID = gd.GiaiDauID
              ) AS SoNguoiDangKy
          FROM GiaiDau gd
          LEFT JOIN ThongTinUser creator ON gd.NguoiTaoID = creator.UserID
          LEFT JOIN ThongTinUser champion ON gd.NguoiVoDichID = champion.UserID
          ORDER BY gd.GiaiDauID DESC",
                conn);

            using (var reader = listCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = ReadRow(reader);
                    tournaments.Add(row);
                }
            }

            if (giaiDauId == 0 && tournaments.Count > 0)
            {
                giaiDauId = Convert.ToInt32(tournaments[0]["GiaiDauID"]);
            }

            if (giaiDauId > 0)
            {
                var detailCmd = new SqlCommand(
                    @"SELECT 
                  gd.GiaiDauID,
                  gd.TenGiaiDau,
                  gd.TrangThai,
                  gd.SoBang,
                  gd.SoNguoiToiDa,
                  gd.NguoiTaoID,
                  gd.NguoiVoDichID,
                  gd.ThoiGianTao,
                  gd.ThoiGianBatDau,
                  gd.ThoiGianKetThuc,
                  ISNULL(creator.TenDangNhap, N'') AS TenNguoiTao,
                  ISNULL(champion.TenDangNhap, N'') AS TenVoDich
              FROM GiaiDau gd
              LEFT JOIN ThongTinUser creator ON gd.NguoiTaoID = creator.UserID
              LEFT JOIN ThongTinUser champion ON gd.NguoiVoDichID = champion.UserID
              WHERE gd.GiaiDauID = @giaiDauId",
                    conn);

                detailCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                using (var reader = detailCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        selectedTournament = ReadRow(reader);
                    }
                }

                var playerCmd = new SqlCommand(
                    @"SELECT 
                  nc.NguoiChoiGiaiDauID,
                  nc.GiaiDauID,
                  nc.UserID,
                  nc.SoThuTu,
                  nc.TrangThai,
                  nc.NgayThamGia,
                  u.TenDangNhap,
                  ISNULL(u.HoTen, '') AS HoTen,
                  ISNULL(u.Avatar, '/images/default-avatar.png') AS Avatar
              FROM NguoiChoiGiaiDau nc
              INNER JOIN ThongTinUser u ON nc.UserID = u.UserID
              WHERE nc.GiaiDauID = @giaiDauId
              ORDER BY nc.SoThuTu",
                    conn);

                playerCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                using (var reader = playerCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(ReadRow(reader));
                    }
                }

                var matchCmd = new SqlCommand(
                    @"SELECT
                  td.TranDauGiaiDauID,
                  td.GiaiDauID,
                  td.BangDauID,
                  td.VongDau,
                  td.ThuTuTran,
                  td.NguoiChoi1ID,
                  td.NguoiChoi2ID,
                  td.NguoiThangID,
                  td.VanCoID,
                  td.TrangThai,
                  ISNULL(u1.TenDangNhap, N'Đang chờ') AS TenNguoiChoi1,
                  ISNULL(u2.TenDangNhap, N'Đang chờ') AS TenNguoiChoi2,
                  ISNULL(w.TenDangNhap, N'') AS TenNguoiThang,
                  ISNULL(b.TenBang, N'') AS TenBang
              FROM TranDauGiaiDau td
              LEFT JOIN ThongTinUser u1 ON td.NguoiChoi1ID = u1.UserID
              LEFT JOIN ThongTinUser u2 ON td.NguoiChoi2ID = u2.UserID
              LEFT JOIN ThongTinUser w ON td.NguoiThangID = w.UserID
              LEFT JOIN BangDau b ON td.BangDauID = b.BangDauID
              WHERE td.GiaiDauID = @giaiDauId
              ORDER BY 
                  CASE td.VongDau
                      WHEN N'VONG_BANG' THEN 1
                      WHEN N'VONG_1_16' THEN 2
                      WHEN N'TU_KET' THEN 3
                      WHEN N'BAN_KET' THEN 4
                      WHEN N'CHUNG_KET' THEN 5
                      ELSE 99
                  END,
                  td.ThuTuTran",
                    conn);

                matchCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                using (var reader = matchCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        matches.Add(ReadRow(reader));
                    }
                }
            }

            ViewBag.Tournaments = tournaments;
            ViewBag.SelectedTournament = selectedTournament;
            ViewBag.Players = players;
            ViewBag.Matches = matches;
            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentGiaiDauId = giaiDauId;

            return View();
        }

        [HttpPost]
        public IActionResult TaoGiaiDau()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (!TryGetCurrentUserId(out int userId))
            {
                TempData["Error"] = "Không đọc được UserID từ Session!";
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                int cheDoId = GetOrCreateCheDoId(conn, tran, "Giải Đấu", "PVP");
                int loaiCoId = GetDefaultLoaiCoId(conn, tran);

                var cmd = new SqlCommand(
                    @"INSERT INTO GiaiDau
              (
                  TenGiaiDau,
                  CheDoID,
                  LoaiCoID,
                  SoBang,
                  SoNguoiToiDa,
                  TrangThai,
                  NguoiTaoID
              )
              OUTPUT INSERTED.GiaiDauID
              VALUES
              (
                  @tenGiaiDau,
                  @cheDoId,
                  @loaiCoId,
                  16,
                  32,
                  N'CHO_DANG_KY',
                  @nguoiTaoId
              )",
                    conn,
                    tran);

                cmd.Parameters.AddWithValue("@tenGiaiDau", "Giải đấu " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                cmd.Parameters.AddWithValue("@cheDoId", cheDoId);
                cmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);
                cmd.Parameters.AddWithValue("@nguoiTaoId", userId);

                int giaiDauId = Convert.ToInt32(cmd.ExecuteScalar());

                tran.Commit();

                TempData["Success"] = "Đã tạo giải đấu mới!";
                return RedirectToAction("Tournament", new { giaiDauId });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                TempData["Error"] = "Lỗi tạo giải đấu: " + ex.Message;
                return RedirectToAction("Tournament");
            }
        }


        [HttpPost]
        public IActionResult ThamGiaGiaiDau(int giaiDauId)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (!TryGetCurrentUserId(out int userId))
            {
                TempData["Error"] = "Không đọc được UserID từ Session!";
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var statusCmd = new SqlCommand(
                    @"SELECT TrangThai
              FROM GiaiDau
              WHERE GiaiDauID = @giaiDauId",
                    conn,
                    tran);

                statusCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                var statusObj = statusCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Không tìm thấy giải đấu!";
                    tran.Rollback();
                    return RedirectToAction("Tournament");
                }

                string status = statusObj.ToString() ?? "";

                if (status != "CHO_DANG_KY")
                {
                    TempData["Error"] = "Giải đấu này đã bắt đầu hoặc đã kết thúc!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                var checkCmd = new SqlCommand(
                    @"SELECT COUNT(*)
              FROM NguoiChoiGiaiDau
              WHERE GiaiDauID = @giaiDauId
                AND UserID = @userId",
                    conn,
                    tran);

                checkCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                checkCmd.Parameters.AddWithValue("@userId", userId);

                int alreadyJoined = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (alreadyJoined > 0)
                {
                    TempData["Error"] = "Bạn đã tham gia giải đấu này rồi!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                var countCmd = new SqlCommand(
                    @"SELECT COUNT(*)
              FROM NguoiChoiGiaiDau
              WHERE GiaiDauID = @giaiDauId",
                    conn,
                    tran);

                countCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                int currentPlayers = Convert.ToInt32(countCmd.ExecuteScalar());

                if (currentPlayers >= 32)
                {
                    TempData["Error"] = "Giải đấu đã đủ 32 người!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                int soThuTu = currentPlayers + 1;

                var insertCmd = new SqlCommand(
                    @"INSERT INTO NguoiChoiGiaiDau
              (
                  GiaiDauID,
                  UserID,
                  SoThuTu,
                  TrangThai
              )
              VALUES
              (
                  @giaiDauId,
                  @userId,
                  @soThuTu,
                  N'DANG_THAM_GIA'
              )",
                    conn,
                    tran);

                insertCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@soThuTu", soThuTu);

                insertCmd.ExecuteNonQuery();

                tran.Commit();

                TempData["Success"] = "Bạn đã tham gia giải đấu!";
                return RedirectToAction("Tournament", new { giaiDauId });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                TempData["Error"] = "Lỗi tham gia giải đấu: " + ex.Message;
                return RedirectToAction("Tournament", new { giaiDauId });
            }
        }




        [HttpPost]
        public IActionResult BatDauGiaiDau(int giaiDauId)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (!TryGetCurrentUserId(out int userId))
            {
                TempData["Error"] = "Không đọc được UserID từ Session!";
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var infoCmd = new SqlCommand(
                    @"SELECT TrangThai, NguoiTaoID
              FROM GiaiDau
              WHERE GiaiDauID = @giaiDauId",
                    conn,
                    tran);

                infoCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                string trangThai = "";
                int nguoiTaoId = 0;

                using (var reader = infoCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        TempData["Error"] = "Không tìm thấy giải đấu!";
                        tran.Rollback();
                        return RedirectToAction("Tournament");
                    }

                    trangThai = reader["TrangThai"].ToString() ?? "";
                    nguoiTaoId = reader["NguoiTaoID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguoiTaoID"]);
                }

                if (nguoiTaoId != userId)
                {
                    TempData["Error"] = "Chỉ người tạo giải đấu mới được bắt đầu!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                if (trangThai != "CHO_DANG_KY")
                {
                    TempData["Error"] = "Giải đấu không ở trạng thái chờ đăng ký!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                var players = new List<int>();

                var playerCmd = new SqlCommand(
                    @"SELECT UserID
              FROM NguoiChoiGiaiDau
              WHERE GiaiDauID = @giaiDauId
              ORDER BY SoThuTu",
                    conn,
                    tran);

                playerCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                using (var reader = playerCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(Convert.ToInt32(reader["UserID"]));
                    }
                }

                if (players.Count != 32)
                {
                    TempData["Error"] = "Cần đủ 32 người mới bắt đầu giải đấu!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                var existCmd = new SqlCommand(
                    @"SELECT COUNT(*)
              FROM TranDauGiaiDau
              WHERE GiaiDauID = @giaiDauId",
                    conn,
                    tran);

                existCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

                int existingMatches = Convert.ToInt32(existCmd.ExecuteScalar());

                if (existingMatches > 0)
                {
                    TempData["Error"] = "Giải đấu này đã có trận đấu!";
                    tran.Rollback();
                    return RedirectToAction("Tournament", new { giaiDauId });
                }

                for (int i = 0; i < 16; i++)
                {
                    string tenBang = "Bảng " + Convert.ToChar('A' + i);
                    int nguoiChoi1 = players[i * 2];
                    int nguoiChoi2 = players[i * 2 + 1];

                    var bangCmd = new SqlCommand(
                        @"INSERT INTO BangDau
                  (
                      GiaiDauID,
                      TenBang,
                      ThuTuBang,
                      NguoiChoi1ID,
                      NguoiChoi2ID,
                      TrangThai
                  )
                  OUTPUT INSERTED.BangDauID
                  VALUES
                  (
                      @giaiDauId,
                      @tenBang,
                      @thuTuBang,
                      @nguoiChoi1,
                      @nguoiChoi2,
                      N'CHO_DAU'
                  )",
                        conn,
                        tran);

                    bangCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                    bangCmd.Parameters.AddWithValue("@tenBang", tenBang);
                    bangCmd.Parameters.AddWithValue("@thuTuBang", i + 1);
                    bangCmd.Parameters.AddWithValue("@nguoiChoi1", nguoiChoi1);
                    bangCmd.Parameters.AddWithValue("@nguoiChoi2", nguoiChoi2);

                    int bangDauId = Convert.ToInt32(bangCmd.ExecuteScalar());

                    var tranCmd = new SqlCommand(
                        @"INSERT INTO TranDauGiaiDau
                  (
                      GiaiDauID,
                      BangDauID,
                      VongDau,
                      ThuTuTran,
                      NguoiChoi1ID,
                      NguoiChoi2ID,
                      TrangThai
                  )
                  VALUES
                  (
                      @giaiDauId,
                      @bangDauId,
                      N'VONG_BANG',
                      @thuTuTran,
                      @nguoiChoi1,
                      @nguoiChoi2,
                      N'CHO_DAU'
                  )",
                        conn,
                        tran);

                    tranCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                    tranCmd.Parameters.AddWithValue("@bangDauId", bangDauId);
                    tranCmd.Parameters.AddWithValue("@thuTuTran", i + 1);
                    tranCmd.Parameters.AddWithValue("@nguoiChoi1", nguoiChoi1);
                    tranCmd.Parameters.AddWithValue("@nguoiChoi2", nguoiChoi2);

                    tranCmd.ExecuteNonQuery();
                }

                var updateCmd = new SqlCommand(
                    @"UPDATE GiaiDau
              SET TrangThai = N'DANG_DIEN_RA',
                  ThoiGianBatDau = SYSDATETIME()
              WHERE GiaiDauID = @giaiDauId",
                    conn,
                    tran);

                updateCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                updateCmd.ExecuteNonQuery();

                tran.Commit();

                TempData["Success"] = "Giải đấu đã bắt đầu!";
                return RedirectToAction("Tournament", new { giaiDauId });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                TempData["Error"] = "Lỗi bắt đầu giải đấu: " + ex.Message;
                return RedirectToAction("Tournament", new { giaiDauId });
            }
        }


        [HttpPost]
        public IActionResult ChonNguoiThangGiaiDau(int tranDauGiaiDauId, int winnerId)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var matchCmd = new SqlCommand(
                    @"SELECT 
                  TranDauGiaiDauID,
                  GiaiDauID,
                  BangDauID,
                  VongDau,
                  ThuTuTran,
                  NguoiChoi1ID,
                  NguoiChoi2ID,
                  NguoiThangID,
                  TrangThai
              FROM TranDauGiaiDau
              WHERE TranDauGiaiDauID = @tranDauGiaiDauId",
                    conn,
                    tran);

                matchCmd.Parameters.AddWithValue("@tranDauGiaiDauId", tranDauGiaiDauId);

                int giaiDauId;
                int? bangDauId;
                string vongDau;
                int nguoiChoi1Id;
                int nguoiChoi2Id;

                using (var reader = matchCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        TempData["Error"] = "Không tìm thấy trận đấu!";
                        tran.Rollback();
                        return RedirectToAction("Tournament");
                    }

                    giaiDauId = Convert.ToInt32(reader["GiaiDauID"]);
                    bangDauId = reader["BangDauID"] == DBNull.Value ? null : Convert.ToInt32(reader["BangDauID"]);
                    vongDau = reader["VongDau"].ToString() ?? "";
                    nguoiChoi1Id = reader["NguoiChoi1ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguoiChoi1ID"]);
                    nguoiChoi2Id = reader["NguoiChoi2ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguoiChoi2ID"]);
                }

                if (winnerId != nguoiChoi1Id && winnerId != nguoiChoi2Id)
                {
                    TempData["Error"] = "Người thắng không thuộc trận đấu này!";
                    tran.Rollback();
                    return RedirectToAction("Tournament");
                }

                int loserId = winnerId == nguoiChoi1Id ? nguoiChoi2Id : nguoiChoi1Id;

                var updateMatchCmd = new SqlCommand(
                    @"UPDATE TranDauGiaiDau
              SET NguoiThangID = @winnerId,
                  TrangThai = N'DA_KET_THUC',
                  ThoiGianKetThuc = SYSDATETIME()
              WHERE TranDauGiaiDauID = @tranDauGiaiDauId",
                    conn,
                    tran);

                updateMatchCmd.Parameters.AddWithValue("@winnerId", winnerId);
                updateMatchCmd.Parameters.AddWithValue("@tranDauGiaiDauId", tranDauGiaiDauId);
                updateMatchCmd.ExecuteNonQuery();

                if (bangDauId.HasValue)
                {
                    var updateBangCmd = new SqlCommand(
                        @"UPDATE BangDau
                  SET NguoiThangID = @winnerId,
                      TrangThai = N'DA_KET_THUC'
                  WHERE BangDauID = @bangDauId",
                        conn,
                        tran);

                    updateBangCmd.Parameters.AddWithValue("@winnerId", winnerId);
                    updateBangCmd.Parameters.AddWithValue("@bangDauId", bangDauId.Value);
                    updateBangCmd.ExecuteNonQuery();
                }

                var updateLoserCmd = new SqlCommand(
                    @"UPDATE NguoiChoiGiaiDau
              SET TrangThai = N'DA_BI_LOAI'
              WHERE GiaiDauID = @giaiDauId
                AND UserID = @loserId",
                    conn,
                    tran);

                updateLoserCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                updateLoserCmd.Parameters.AddWithValue("@loserId", loserId);
                updateLoserCmd.ExecuteNonQuery();

                TryCreateNextTournamentRound(conn, tran, giaiDauId, vongDau);

                tran.Commit();

                TempData["Success"] = "Đã cập nhật người thắng!";
                return RedirectToAction("Tournament", new { giaiDauId });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                TempData["Error"] = "Lỗi cập nhật người thắng: " + ex.Message;
                return RedirectToAction("Tournament");
            }
        }

        [HttpPost]
        public IActionResult SaveGameHistory([FromBody] SaveGameRequest request)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để lưu lịch sử ván cờ!"
                });
            }

            if (request == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu gửi lên không hợp lệ!"
                });
            }

            var userIdText = HttpContext.Session.GetString("UserID");

            if (!int.TryParse(userIdText, out int userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Không đọc được UserID từ Session!"
                });
            }

            var modeName = string.IsNullOrWhiteSpace(request.ModeName)
                ? "Hai Người Một Máy"
                : request.ModeName.Trim();

            if (modeName == "2 Người 1 Máy")
            {
                modeName = "Hai Người Một Máy";
            }

            var modeType = string.IsNullOrWhiteSpace(request.ModeType)
                ? ""
                : request.ModeType.Trim().ToUpper();

            if (modeName == "Chơi Với AI")
            {
                modeType = "BOT";
            }

            var result = string.IsNullOrWhiteSpace(request.Result)
                ? "DRAW"
                : request.Result.Trim().ToUpper();

            if (result != "WHITE_WIN" && result != "BLACK_WIN" && result != "DRAW")
            {
                result = "DRAW";
            }

            var fen = string.IsNullOrWhiteSpace(request.Fen)
                ? "8/8/8/8/8/8/8/8 w - - 0 1"
                : request.Fen.Trim();

            var moves = request.Moves ?? new List<string>();

            if (moves.Count > 500)
            {
                moves = moves.Take(500).ToList();
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                int cheDoId = GetOrCreateCheDoId(conn, tran, modeName, modeType);
                int loaiCoId = GetDefaultLoaiCoId(conn, tran);

                int? botId = null;

                if (modeType == "BOT")
                {
                    string botName = string.IsNullOrWhiteSpace(request.BotName)
                        ? "Bot Trung Bình"
                        : request.BotName.Trim();

                    botId = GetOrCreateBotId(conn, tran, botName);
                }

                var insertGameCmd = new SqlCommand(
                    @"INSERT INTO VanCo
              (
                  PhongID,
                  NguoiTrangID,
                  NguoiDenID,
                  BotID,
                  PuzzleID,
                  LoaiCoID,
                  CheDoID,
                  TrangThai,
                  LuotDi,
                  KetQua,
                  FEN,
                  ThoiGianBatDau,
                  ThoiGianKetThuc
              )
              OUTPUT INSERTED.VanCoID
              VALUES
              (
                  NULL,
                  @nguoiTrangId,
                  NULL,
                  @botId,
                  NULL,
                  @loaiCoId,
                  @cheDoId,
                  N'END',
                  'WHITE',
                  @ketQua,
                  @fen,
                  SYSDATETIME(),
                  SYSDATETIME()
              )",
                    conn,
                    tran);

                insertGameCmd.Parameters.AddWithValue("@nguoiTrangId", userId);
                insertGameCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);
                insertGameCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
                insertGameCmd.Parameters.AddWithValue("@ketQua", result);
                insertGameCmd.Parameters.AddWithValue("@fen", fen);

                if (botId.HasValue)
                {
                    insertGameCmd.Parameters.AddWithValue("@botId", botId.Value);
                }
                else
                {
                    insertGameCmd.Parameters.AddWithValue("@botId", DBNull.Value);
                }

                int vanCoId = Convert.ToInt32(insertGameCmd.ExecuteScalar());

                for (int i = 0; i < moves.Count; i++)
                {
                    var moveText = moves[i];

                    if (string.IsNullOrWhiteSpace(moveText))
                    {
                        continue;
                    }

                    moveText = moveText.Trim();

                    if (moveText.Length > 10)
                    {
                        moveText = moveText.Substring(0, 10);
                    }

                    var insertMoveCmd = new SqlCommand(
                        @"INSERT INTO NuocDi (VanCoID, SoThuTu, Nuoc)
                  VALUES (@vanCoId, @soThuTu, @nuoc)",
                        conn,
                        tran);

                    insertMoveCmd.Parameters.AddWithValue("@vanCoId", vanCoId);
                    insertMoveCmd.Parameters.AddWithValue("@soThuTu", i + 1);
                    insertMoveCmd.Parameters.AddWithValue("@nuoc", moveText);

                    insertMoveCmd.ExecuteNonQuery();
                }

                // Cộng điểm thật sau khi lưu ván
                TinhDiemSauTran(conn, tran, vanCoId, result);

                tran.Commit();

                return Json(new
                {
                    success = true,
                    message = "Đã lưu lịch sử ván cờ và cộng điểm xếp hạng!",
                    vanCoId = vanCoId
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                return Json(new
                {
                    success = false,
                    message = "Lỗi khi lưu lịch sử và cộng điểm: " + ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult KetThucVanCo([FromBody] KetThucVanCoRequest request)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để kết thúc ván cờ!"
                });
            }

            if (request == null || request.VanCoId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Ván cờ không hợp lệ!"
                });
            }

            var ketQua = string.IsNullOrWhiteSpace(request.KetQua)
                ? "DRAW"
                : request.KetQua.Trim().ToUpper();

            if (ketQua != "WHITE_WIN" && ketQua != "BLACK_WIN" && ketQua != "DRAW")
            {
                return Json(new
                {
                    success = false,
                    message = "Kết quả ván cờ không hợp lệ!"
                });
            }

            var userIdText = HttpContext.Session.GetString("UserID");

            if (!int.TryParse(userIdText, out int userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Không đọc được UserID từ Session!"
                });
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var checkCmd = new SqlCommand(
                    @"SELECT 
                  VanCoID,
                  NguoiTrangID,
                  NguoiDenID,
                  KetQua,
                  TrangThai
              FROM VanCo
              WHERE VanCoID = @vanCoId",
                    conn,
                    tran);

                checkCmd.Parameters.AddWithValue("@vanCoId", request.VanCoId);

                int? nguoiTrangId = null;
                int? nguoiDenId = null;
                string trangThai = "";
                string ketQuaCu = "";

                using (var reader = checkCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        tran.Rollback();

                        return Json(new
                        {
                            success = false,
                            message = "Không tìm thấy ván cờ!"
                        });
                    }

                    nguoiTrangId = reader["NguoiTrangID"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["NguoiTrangID"]);

                    nguoiDenId = reader["NguoiDenID"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["NguoiDenID"]);

                    ketQuaCu = reader["KetQua"] == DBNull.Value
                        ? ""
                        : reader["KetQua"]?.ToString() ?? "";

                    trangThai = reader["TrangThai"] == DBNull.Value
                        ? ""
                        : reader["TrangThai"]?.ToString() ?? "";
                }

                if (nguoiTrangId != userId && nguoiDenId != userId)
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Bạn không có quyền kết thúc ván cờ này!"
                    });
                }

                if (trangThai == "END" || !string.IsNullOrWhiteSpace(ketQuaCu))
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Ván cờ này đã kết thúc và đã được tính điểm rồi!"
                    });
                }

                var updateCmd = new SqlCommand(
                    @"UPDATE VanCo
              SET TrangThai = N'END',
                  KetQua = @ketQua,
                  ThoiGianKetThuc = SYSDATETIME()
              WHERE VanCoID = @vanCoId",
                    conn,
                    tran);

                updateCmd.Parameters.AddWithValue("@ketQua", ketQua);
                updateCmd.Parameters.AddWithValue("@vanCoId", request.VanCoId);
                updateCmd.ExecuteNonQuery();

                TinhDiemSauTran(conn, tran, request.VanCoId, ketQua);

                tran.Commit();

                return Json(new
                {
                    success = true,
                    message = "Đã kết thúc ván cờ và cộng điểm xếp hạng!",
                    vanCoId = request.VanCoId,
                    ketQua = ketQua
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                return Json(new
                {
                    success = false,
                    message = "Lỗi kết thúc ván cờ: " + ex.Message
                });
            }
        }

        private int GetOrCreateCheDoId(SqlConnection conn, SqlTransaction tran, string tenCheDo, string modeType = "")
        {
            var findCmd = new SqlCommand(
                @"SELECT TOP 1 CheDoID
                  FROM CheDoChoi
                  WHERE TenCheDo = @tenCheDo",
                conn,
                tran);

            findCmd.Parameters.AddWithValue("@tenCheDo", tenCheDo);

            var found = findCmd.ExecuteScalar();

            if (found != null && found != DBNull.Value)
            {
                return Convert.ToInt32(found);
            }

            string loaiCheDo = "PVP";

            if (modeType == "BOT")
            {
                loaiCheDo = "BOT";
            }
            else if (modeType == "PUZZLE")
            {
                loaiCheDo = "PUZZLE";
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO CheDoChoi (TenCheDo, LoaiCheDo, ThoiGian)
                  OUTPUT INSERTED.CheDoID
                  VALUES (@tenCheDo, @loaiCheDo, 1800)",
                conn,
                tran);

            insertCmd.Parameters.AddWithValue("@tenCheDo", tenCheDo);
            insertCmd.Parameters.AddWithValue("@loaiCheDo", loaiCheDo);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private int GetOrCreateBotId(SqlConnection conn, SqlTransaction tran, string botName)
        {
            var findCmd = new SqlCommand(
                @"SELECT TOP 1 BotID
                  FROM Bot
                  WHERE TenBot = @botName",
                conn,
                tran);

            findCmd.Parameters.AddWithValue("@botName", botName);

            var found = findCmd.ExecuteScalar();

            if (found != null && found != DBNull.Value)
            {
                return Convert.ToInt32(found);
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO Bot (TenBot, DoKho, MoTa)
                  OUTPUT INSERTED.BotID
                  VALUES (@botName, 5, N'Bot dùng cho chế độ Chơi Với AI')",
                conn,
                tran);

            insertCmd.Parameters.AddWithValue("@botName", botName);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private int GetDefaultLoaiCoId(SqlConnection conn, SqlTransaction tran)
        {
            var findCmd = new SqlCommand(
                @"SELECT TOP 1 LoaiCoID
                  FROM LoaiCo
                  WHERE TenLoai = N'Cờ vua'
                  ORDER BY LoaiCoID",
                conn,
                tran);

            var found = findCmd.ExecuteScalar();

            if (found != null && found != DBNull.Value)
            {
                return Convert.ToInt32(found);
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO LoaiCo (TenLoai)
                  OUTPUT INSERTED.LoaiCoID
                  VALUES (N'Cờ vua')",
                conn,
                tran);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private void TinhDiemSauTran(
    SqlConnection conn,
    SqlTransaction tran,
    int vanCoId,
    string ketQua)
        {
            var gameCmd = new SqlCommand(
                @"SELECT 
              VanCoID,
              NguoiTrangID,
              NguoiDenID,
              BotID,
              PuzzleID,
              CheDoID
          FROM VanCo
          WHERE VanCoID = @vanCoId",
                conn,
                tran);

            gameCmd.Parameters.AddWithValue("@vanCoId", vanCoId);

            int? nguoiTrangId = null;
            int? nguoiDenId = null;
            int? puzzleId = null;
            int cheDoId = 0;

            using (var reader = gameCmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    throw new Exception("Không tìm thấy ván cờ để tính điểm.");
                }

                nguoiTrangId = reader["NguoiTrangID"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["NguoiTrangID"]);

                nguoiDenId = reader["NguoiDenID"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["NguoiDenID"]);

                puzzleId = reader["PuzzleID"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["PuzzleID"]);

                cheDoId = Convert.ToInt32(reader["CheDoID"]);
            }

            // Không tính điểm xếp hạng cho câu đố
            if (puzzleId.HasValue)
            {
                return;
            }

            if (!nguoiTrangId.HasValue)
            {
                return;
            }

            int diemTrang = 0;
            int diemDen = 0;

            bool trangThang = false;
            bool trangThua = false;
            bool denThang = false;
            bool denThua = false;
            bool hoa = false;

            if (ketQua == "WHITE_WIN")
            {
                diemTrang = 15;
                diemDen = -10;

                trangThang = true;
                denThua = true;
            }
            else if (ketQua == "BLACK_WIN")
            {
                diemTrang = -10;
                diemDen = 15;

                trangThua = true;
                denThang = true;
            }
            else
            {
                diemTrang = 5;
                diemDen = 5;

                hoa = true;
            }

            CapNhatXepHangNguoiChoi(
                conn,
                tran,
                nguoiTrangId.Value,
                cheDoId,
                vanCoId,
                diemTrang,
                trangThang,
                trangThua,
                hoa
            );

            if (nguoiDenId.HasValue)
            {
                CapNhatXepHangNguoiChoi(
                    conn,
                    tran,
                    nguoiDenId.Value,
                    cheDoId,
                    vanCoId,
                    diemDen,
                    denThang,
                    denThua,
                    hoa
                );
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;

            var userIdText = HttpContext.Session.GetString("UserID");

            return int.TryParse(userIdText, out userId);
        }

        private void CapNhatXepHangNguoiChoi(
    SqlConnection conn,
    SqlTransaction tran,
    int userId,
    int cheDoId,
    int vanCoId,
    int diemThayDoi,
    bool thang,
    bool thua,
    bool hoa)
        {
            var createRankCmd = new SqlCommand(
                @"IF NOT EXISTS
          (
              SELECT 1
              FROM XepHang
              WHERE UserID = @userId
                AND CheDoID = @cheDoId
          )
          BEGIN
              INSERT INTO XepHang
              (
                  UserID,
                  CheDoID,
                  Diem,
                  SoVan,
                  Thang,
                  Thua,
                  Hoa
              )
              VALUES
              (
                  @userId,
                  @cheDoId,
                  1200,
                  0,
                  0,
                  0,
                  0
              )
          END",
                conn,
                tran);

            createRankCmd.Parameters.AddWithValue("@userId", userId);
            createRankCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            createRankCmd.ExecuteNonQuery();

            var getPointCmd = new SqlCommand(
                @"SELECT Diem
          FROM XepHang
          WHERE UserID = @userId
            AND CheDoID = @cheDoId",
                conn,
                tran);

            getPointCmd.Parameters.AddWithValue("@userId", userId);
            getPointCmd.Parameters.AddWithValue("@cheDoId", cheDoId);

            int diemCu = Convert.ToInt32(getPointCmd.ExecuteScalar());
            int diemMoi = diemCu + diemThayDoi;

            if (diemMoi < 0)
            {
                diemMoi = 0;
            }

            if (diemMoi > 5000)
            {
                diemMoi = 5000;
            }

            var updateRankCmd = new SqlCommand(
                @"UPDATE XepHang
          SET Diem = @diemMoi,
              SoVan = SoVan + 1,
              Thang = Thang + @thang,
              Thua = Thua + @thua,
              Hoa = Hoa + @hoa
          WHERE UserID = @userId
            AND CheDoID = @cheDoId",
                conn,
                tran);

            updateRankCmd.Parameters.AddWithValue("@diemMoi", diemMoi);
            updateRankCmd.Parameters.AddWithValue("@thang", thang ? 1 : 0);
            updateRankCmd.Parameters.AddWithValue("@thua", thua ? 1 : 0);
            updateRankCmd.Parameters.AddWithValue("@hoa", hoa ? 1 : 0);
            updateRankCmd.Parameters.AddWithValue("@userId", userId);
            updateRankCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            updateRankCmd.ExecuteNonQuery();

            if (diemCu != diemMoi)
            {
                var insertHistoryCmd = new SqlCommand(
                    @"INSERT INTO LichSuDiem
              (
                  UserID,
                  CheDoID,
                  DiemCu,
                  DiemMoi,
                  VanCoID
              )
              VALUES
              (
                  @userId,
                  @cheDoId,
                  @diemCu,
                  @diemMoi,
                  @vanCoId
              )",
                    conn,
                    tran);

                insertHistoryCmd.Parameters.AddWithValue("@userId", userId);
                insertHistoryCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
                insertHistoryCmd.Parameters.AddWithValue("@diemCu", diemCu);
                insertHistoryCmd.Parameters.AddWithValue("@diemMoi", diemMoi);
                insertHistoryCmd.Parameters.AddWithValue("@vanCoId", vanCoId);
                insertHistoryCmd.ExecuteNonQuery();
            }
        }


        private Dictionary<string, object> ReadRow(SqlDataReader reader)
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader[i] == DBNull.Value ? "" : reader[i];
            }

            return row;
        }


        private void TryCreateNextTournamentRound(SqlConnection conn, SqlTransaction tran, int giaiDauId, string currentRound)
        {
            string? nextRound = null;
            int requiredWinnerCount = 0;

            if (currentRound == "VONG_BANG")
            {
                nextRound = "VONG_1_16";
                requiredWinnerCount = 16;
            }
            else if (currentRound == "VONG_1_16")
            {
                nextRound = "TU_KET";
                requiredWinnerCount = 8;
            }
            else if (currentRound == "TU_KET")
            {
                nextRound = "BAN_KET";
                requiredWinnerCount = 4;
            }
            else if (currentRound == "BAN_KET")
            {
                nextRound = "CHUNG_KET";
                requiredWinnerCount = 2;
            }
            else if (currentRound == "CHUNG_KET")
            {
                FinishTournamentIfFinalDone(conn, tran, giaiDauId);
                return;
            }

            if (string.IsNullOrWhiteSpace(nextRound))
            {
                return;
            }

            var winnerCmd = new SqlCommand(
                @"SELECT NguoiThangID
          FROM TranDauGiaiDau
          WHERE GiaiDauID = @giaiDauId
            AND VongDau = @currentRound
            AND NguoiThangID IS NOT NULL
          ORDER BY ThuTuTran",
                conn,
                tran);

            winnerCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
            winnerCmd.Parameters.AddWithValue("@currentRound", currentRound);

            var winners = new List<int>();

            using (var reader = winnerCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    winners.Add(Convert.ToInt32(reader["NguoiThangID"]));
                }
            }

            if (winners.Count != requiredWinnerCount)
            {
                return;
            }

            var existCmd = new SqlCommand(
                @"SELECT COUNT(*)
          FROM TranDauGiaiDau
          WHERE GiaiDauID = @giaiDauId
            AND VongDau = @nextRound",
                conn,
                tran);

            existCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
            existCmd.Parameters.AddWithValue("@nextRound", nextRound);

            int alreadyCreated = Convert.ToInt32(existCmd.ExecuteScalar());

            if (alreadyCreated > 0)
            {
                return;
            }

            for (int i = 0; i < winners.Count; i += 2)
            {
                var insertCmd = new SqlCommand(
                    @"INSERT INTO TranDauGiaiDau
              (
                  GiaiDauID,
                  BangDauID,
                  VongDau,
                  ThuTuTran,
                  NguoiChoi1ID,
                  NguoiChoi2ID,
                  TrangThai
              )
              VALUES
              (
                  @giaiDauId,
                  NULL,
                  @nextRound,
                  @thuTuTran,
                  @nguoiChoi1,
                  @nguoiChoi2,
                  N'CHO_DAU'
              )",
                    conn,
                    tran);

                insertCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
                insertCmd.Parameters.AddWithValue("@nextRound", nextRound);
                insertCmd.Parameters.AddWithValue("@thuTuTran", (i / 2) + 1);
                insertCmd.Parameters.AddWithValue("@nguoiChoi1", winners[i]);
                insertCmd.Parameters.AddWithValue("@nguoiChoi2", winners[i + 1]);

                insertCmd.ExecuteNonQuery();
            }
        }


        private void FinishTournamentIfFinalDone(SqlConnection conn, SqlTransaction tran, int giaiDauId)
        {
            var finalCmd = new SqlCommand(
                @"SELECT TOP 1 NguoiThangID
          FROM TranDauGiaiDau
          WHERE GiaiDauID = @giaiDauId
            AND VongDau = N'CHUNG_KET'
            AND NguoiThangID IS NOT NULL",
                conn,
                tran);

            finalCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);

            var winnerObj = finalCmd.ExecuteScalar();

            if (winnerObj == null || winnerObj == DBNull.Value)
            {
                return;
            }

            int championId = Convert.ToInt32(winnerObj);

            var updateTournamentCmd = new SqlCommand(
                @"UPDATE GiaiDau
          SET TrangThai = N'DA_KET_THUC',
              NguoiVoDichID = @championId,
              ThoiGianKetThuc = SYSDATETIME()
          WHERE GiaiDauID = @giaiDauId",
                conn,
                tran);

            updateTournamentCmd.Parameters.AddWithValue("@championId", championId);
            updateTournamentCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
            updateTournamentCmd.ExecuteNonQuery();

            var updateChampionCmd = new SqlCommand(
                @"UPDATE NguoiChoiGiaiDau
          SET TrangThai = N'VO_DICH'
          WHERE GiaiDauID = @giaiDauId
            AND UserID = @championId",
                conn,
                tran);

            updateChampionCmd.Parameters.AddWithValue("@giaiDauId", giaiDauId);
            updateChampionCmd.Parameters.AddWithValue("@championId", championId);
            updateChampionCmd.ExecuteNonQuery();
        }




    }



}