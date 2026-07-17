using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace Chess.Controllers
{
    /* KẾT QUẢ TRẢ VỀ TỪ PYTHON API /api/validate */
    public class PuzzleValidateMoveResult
    {
        public bool valid { get; set; }

        public string? fen_after { get; set; }

        public bool is_check { get; set; }

        public bool is_checkmate { get; set; }

        public bool is_game_over { get; set; }

        public bool is_stalemate { get; set; }

        public string? error { get; set; }
    }

    /* CONTROLLER CỜ CÂU ĐỐ */
    public class PuzzleController : Controller
    {
        private readonly string _connStr = string.Empty;

        public PuzzleController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        }

        /* KIỂM TRA ĐĂNG NHẬP */
        private bool IsLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserID"));
        }

        /* LẤY USER ID HIỆN TẠI */
        private int GetCurrentUserId()
        {
            string? userIdText = HttpContext.Session.GetString("UserID");

            if (string.IsNullOrWhiteSpace(userIdText))
            {
                throw new Exception("Bạn cần đăng nhập để chơi câu đố.");
            }

            if (!int.TryParse(userIdText, out int userId))
            {
                throw new Exception("UserID trong Session không hợp lệ.");
            }

            return userId;
        }

        /* GỌI PYTHON API ĐỂ KIỂM TRA NƯỚC ĐI */
        private async Task<PuzzleValidateMoveResult?> ValidateMoveWithPython(string fen, string move)
        {
            try
            {
                using var http = new HttpClient();

                var body = new
                {
                    fen = fen,
                    move = move
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await http.PostAsync("http://localhost:5000/api/validate", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new PuzzleValidateMoveResult
                    {
                        valid = false,
                        error = "Python API lỗi: " + responseText
                    };
                }

                var result = JsonSerializer.Deserialize<PuzzleValidateMoveResult>(
                    responseText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                return result;
            }
            catch (Exception ex)
            {
                return new PuzzleValidateMoveResult
                {
                    valid = false,
                    error = "Không gọi được Python API. Kiểm tra Flask đã chạy chưa. Chi tiết: " + ex.Message
                };
            }
        }

        /* TRANG CHỌN CẤP ĐỘ */
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để chơi cờ câu đố!";
                return RedirectToAction("DangNhap", "Home");
            }

            var levels = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT 
                      cd.CapDoID,
                      cd.TenCapDo,
                      cd.MaCapDo,
                      cd.DiemCong,
                      cd.MoTa,
                      cd.TrangThai,
                      (
                          SELECT COUNT(*)
                          FROM Puzzle p
                          WHERE p.CapDoID = cd.CapDoID
                            AND ISNULL(p.TrangThai, 1) = 1
                      ) AS SoCau
                  FROM CapDoCauDo cd
                  WHERE cd.TrangThai = 1
                  ORDER BY cd.CapDoID",
                conn);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                levels.Add(ReadRow(reader));
            }

            ViewBag.Levels = levels;

            return View();
        }

        /* DANH SÁCH CÂU ĐỐ THEO CẤP ĐỘ */
        [HttpGet]
        public IActionResult List(int capDoId)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để chơi cờ câu đố!";
                return RedirectToAction("DangNhap", "Home");
            }

            if (capDoId <= 0)
            {
                TempData["Error"] = "Cấp độ không hợp lệ!";
                return RedirectToAction("Index");
            }

            int userId = GetCurrentUserId();

            var puzzles = new List<Dictionary<string, object>>();
            Dictionary<string, object>? level = null;
            Dictionary<string, object>? score = null;

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var levelCmd = new SqlCommand(
                @"SELECT 
                      CapDoID,
                      TenCapDo,
                      MaCapDo,
                      DiemCong,
                      MoTa
                  FROM CapDoCauDo
                  WHERE CapDoID = @capDoId
                    AND TrangThai = 1",
                conn);

            levelCmd.Parameters.AddWithValue("@capDoId", capDoId);

            using (var reader = levelCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    level = ReadRow(reader);
                }
            }

            if (level == null)
            {
                TempData["Error"] = "Không tìm thấy cấp độ câu đố!";
                return RedirectToAction("Index");
            }

            var scoreCmd = new SqlCommand(
                @"SELECT 
                      TongDiem,
                      SoCauDung,
                      NgayCapNhat
                  FROM DiemCauDoNguoiDung
                  WHERE UserID = @userId
                    AND CapDoID = @capDoId",
                conn);

            scoreCmd.Parameters.AddWithValue("@userId", userId);
            scoreCmd.Parameters.AddWithValue("@capDoId", capDoId);

            using (var reader = scoreCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    score = ReadRow(reader);
                }
            }

            var puzzleCmd = new SqlCommand(
                @"SELECT
                      p.PuzzleID,
                      p.TieuDe,
                      p.MoTa,
                      p.DoKho,
                      p.LoaiCauDo,
                      p.DiemThuong,
                      p.CapDoID,
                      CASE
                          WHEN EXISTS
                          (
                              SELECT 1
                              FROM LichSuLamCauDo ls
                              WHERE ls.UserID = @userId
                                AND ls.PuzzleID = p.PuzzleID
                                AND ls.KetQua = 1
                          )
                          THEN 1
                          ELSE 0
                      END AS DaGiaiDung
                  FROM Puzzle p
                  WHERE p.CapDoID = @capDoId
                    AND ISNULL(p.TrangThai, 1) = 1
                  ORDER BY p.PuzzleID",
                conn);

            puzzleCmd.Parameters.AddWithValue("@userId", userId);
            puzzleCmd.Parameters.AddWithValue("@capDoId", capDoId);

            using (var reader = puzzleCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    puzzles.Add(ReadRow(reader));
                }
            }

            ViewBag.Level = level;
            ViewBag.Score = score;
            ViewBag.Puzzles = puzzles;

            return View();
        }

        /* TRANG CHƠI 1 CÂU ĐỐ */
        [HttpGet]
        public IActionResult Play(int puzzleId)
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Bạn cần đăng nhập để chơi cờ câu đố!";
                return RedirectToAction("DangNhap", "Home");
            }

            if (puzzleId <= 0)
            {
                TempData["Error"] = "Câu đố không hợp lệ!";
                return RedirectToAction("Index");
            }

            Dictionary<string, object>? puzzle = null;

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT
                      p.PuzzleID,
                      p.TieuDe,
                      p.MoTa,
                      p.FEN,
                      p.LoiGiai,
                      p.DoKho,
                      p.LoaiCauDo,
                      p.DiemThuong,
                      p.CapDoID,
                      cd.TenCapDo,
                      cd.MaCapDo
                  FROM Puzzle p
                  INNER JOIN CapDoCauDo cd ON p.CapDoID = cd.CapDoID
                  WHERE p.PuzzleID = @puzzleId
                    AND ISNULL(p.TrangThai, 1) = 1",
                conn);

            cmd.Parameters.AddWithValue("@puzzleId", puzzleId);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                puzzle = ReadRow(reader);
            }

            if (puzzle == null)
            {
                TempData["Error"] = "Không tìm thấy câu đố!";
                return RedirectToAction("Index");
            }

            ViewBag.Puzzle = puzzle;

            return View();
        }

        /* API LẤY THÔNG TIN CÂU ĐỐ */
        [HttpGet]
        public IActionResult GetPuzzle(int puzzleId)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập."
                });
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(
                @"SELECT
                      p.PuzzleID,
                      p.TieuDe,
                      p.MoTa,
                      p.FEN,
                      p.DoKho,
                      p.LoaiCauDo,
                      p.DiemThuong,
                      p.CapDoID,
                      cd.TenCapDo
                  FROM Puzzle p
                  INNER JOIN CapDoCauDo cd ON p.CapDoID = cd.CapDoID
                  WHERE p.PuzzleID = @puzzleId
                    AND ISNULL(p.TrangThai, 1) = 1",
                conn);

            cmd.Parameters.AddWithValue("@puzzleId", puzzleId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy câu đố."
                });
            }

            return Json(new
            {
                success = true,
                puzzle = new
                {
                    puzzleId = Convert.ToInt32(reader["PuzzleID"]),
                    tieuDe = reader["TieuDe"] == DBNull.Value ? "" : reader["TieuDe"].ToString(),
                    moTa = reader["MoTa"] == DBNull.Value ? "" : reader["MoTa"].ToString(),
                    fen = reader["FEN"].ToString(),
                    doKho = reader["DoKho"] == DBNull.Value ? 1 : Convert.ToInt32(reader["DoKho"]),
                    loaiCauDo = reader["LoaiCauDo"] == DBNull.Value ? "MATE" : reader["LoaiCauDo"].ToString(),
                    diemThuong = Convert.ToInt32(reader["DiemThuong"]),
                    capDoId = Convert.ToInt32(reader["CapDoID"]),
                    tenCapDo = reader["TenCapDo"].ToString()
                }
            });
        }

        /* API KIỂM TRA NƯỚC ĐI CÂU ĐỐ */
        [HttpPost]
        public async Task<IActionResult> CheckMove(int puzzleId, string move)
        {
            if (!IsLoggedIn())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để làm câu đố."
                });
            }

            if (puzzleId <= 0 || string.IsNullOrWhiteSpace(move))
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu câu đố hoặc nước đi không hợp lệ."
                });
            }

            int userId = GetCurrentUserId();
            move = move.Trim();

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                var puzzleCmd = new SqlCommand(
                    @"SELECT
                          PuzzleID,
                          CapDoID,
                          FEN,
                          LoiGiai,
                          LoaiCauDo,
                          DiemThuong
                      FROM Puzzle
                      WHERE PuzzleID = @puzzleId
                        AND ISNULL(TrangThai, 1) = 1",
                    conn,
                    tran);

                puzzleCmd.Parameters.AddWithValue("@puzzleId", puzzleId);

                int capDoId;
                string fen;
                string correctMove;
                string loaiCauDo;
                int diemThuong;

                using (var reader = puzzleCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        tran.Rollback();

                        return Json(new
                        {
                            success = false,
                            message = "Không tìm thấy câu đố."
                        });
                    }

                    capDoId = Convert.ToInt32(reader["CapDoID"]);
                    fen = reader["FEN"] == DBNull.Value ? "" : reader["FEN"].ToString() ?? "";
                    correctMove = reader["LoiGiai"] == DBNull.Value ? "" : reader["LoiGiai"].ToString() ?? "";
                    loaiCauDo = reader["LoaiCauDo"] == DBNull.Value ? "MATE" : reader["LoaiCauDo"].ToString() ?? "MATE";
                    diemThuong = Convert.ToInt32(reader["DiemThuong"]);
                }

                if (string.IsNullOrWhiteSpace(fen))
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Câu đố này bị thiếu FEN."
                    });
                }

                if (string.IsNullOrWhiteSpace(correctMove))
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Câu đố này bị thiếu lời giải."
                    });
                }

                var validateResult = await ValidateMoveWithPython(fen, move);

                if (validateResult == null)
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Không kiểm tra được nước đi."
                    });
                }

                if (!validateResult.valid)
                {
                    var historyWrongInvalidCmd = new SqlCommand(
                        @"INSERT INTO LichSuLamCauDo
                          (
                              UserID,
                              PuzzleID,
                              CapDoID,
                              NuocDaDi,
                              KetQua,
                              DiemNhan,
                              DaCongDiem
                          )
                          VALUES
                          (
                              @userId,
                              @puzzleId,
                              @capDoId,
                              @nuocDaDi,
                              0,
                              0,
                              0
                          )",
                        conn,
                        tran);

                    historyWrongInvalidCmd.Parameters.AddWithValue("@userId", userId);
                    historyWrongInvalidCmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                    historyWrongInvalidCmd.Parameters.AddWithValue("@capDoId", capDoId);
                    historyWrongInvalidCmd.Parameters.AddWithValue("@nuocDaDi", move);

                    historyWrongInvalidCmd.ExecuteNonQuery();

                    tran.Commit();

                    return Json(new
                    {
                        success = true,
                        correct = false,
                        message = string.IsNullOrWhiteSpace(validateResult.error)
                            ? "Nước đi không hợp lệ."
                            : validateResult.error
                    });
                }

                bool dungLoiGiai = string.Equals(
                    move,
                    correctMove.Trim(),
                    StringComparison.OrdinalIgnoreCase
                );

                bool dungTheoLoaiCauDo = true;

                if (loaiCauDo.Equals("MATE", StringComparison.OrdinalIgnoreCase))
                {
                    dungTheoLoaiCauDo = validateResult.is_checkmate;
                }

                if (loaiCauDo.Equals("SAVE", StringComparison.OrdinalIgnoreCase))
                {
                    dungTheoLoaiCauDo = validateResult.valid;
                }

                bool isCorrect = dungLoiGiai && dungTheoLoaiCauDo;

                bool daCongDiem = false;
                int diemNhan = 0;

                if (isCorrect)
                {
                    var checkOldCmd = new SqlCommand(
                        @"SELECT COUNT(*)
                          FROM LichSuLamCauDo
                          WHERE UserID = @userId
                            AND PuzzleID = @puzzleId
                            AND KetQua = 1
                            AND DaCongDiem = 1",
                        conn,
                        tran);

                    checkOldCmd.Parameters.AddWithValue("@userId", userId);
                    checkOldCmd.Parameters.AddWithValue("@puzzleId", puzzleId);

                    int solvedBefore = Convert.ToInt32(checkOldCmd.ExecuteScalar());

                    if (solvedBefore == 0)
                    {
                        diemNhan = diemThuong;
                        daCongDiem = true;

                        var scoreExistsCmd = new SqlCommand(
                            @"SELECT COUNT(*)
                              FROM DiemCauDoNguoiDung
                              WHERE UserID = @userId
                                AND CapDoID = @capDoId",
                            conn,
                            tran);

                        scoreExistsCmd.Parameters.AddWithValue("@userId", userId);
                        scoreExistsCmd.Parameters.AddWithValue("@capDoId", capDoId);

                        int scoreExists = Convert.ToInt32(scoreExistsCmd.ExecuteScalar());

                        if (scoreExists > 0)
                        {
                            var updateScoreCmd = new SqlCommand(
                                @"UPDATE DiemCauDoNguoiDung
                                  SET TongDiem = TongDiem + @diemNhan,
                                      SoCauDung = SoCauDung + 1,
                                      NgayCapNhat = GETDATE()
                                  WHERE UserID = @userId
                                    AND CapDoID = @capDoId",
                                conn,
                                tran);

                            updateScoreCmd.Parameters.AddWithValue("@diemNhan", diemNhan);
                            updateScoreCmd.Parameters.AddWithValue("@userId", userId);
                            updateScoreCmd.Parameters.AddWithValue("@capDoId", capDoId);

                            updateScoreCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            var insertScoreCmd = new SqlCommand(
                                @"INSERT INTO DiemCauDoNguoiDung
                                  (
                                      UserID,
                                      CapDoID,
                                      TongDiem,
                                      SoCauDung
                                  )
                                  VALUES
                                  (
                                      @userId,
                                      @capDoId,
                                      @diemNhan,
                                      1
                                  )",
                                conn,
                                tran);

                            insertScoreCmd.Parameters.AddWithValue("@userId", userId);
                            insertScoreCmd.Parameters.AddWithValue("@capDoId", capDoId);
                            insertScoreCmd.Parameters.AddWithValue("@diemNhan", diemNhan);

                            insertScoreCmd.ExecuteNonQuery();

                        }
                        // Cộng điểm câu đố vào bảng xếp hạng
                        CapNhatXepHangCauDo(conn, tran, userId, diemNhan);
                    }
                }

                var historyCmd = new SqlCommand(
                    @"INSERT INTO LichSuLamCauDo
                      (
                          UserID,
                          PuzzleID,
                          CapDoID,
                          NuocDaDi,
                          KetQua,
                          DiemNhan,
                          DaCongDiem
                      )
                      VALUES
                      (
                          @userId,
                          @puzzleId,
                          @capDoId,
                          @nuocDaDi,
                          @ketQua,
                          @diemNhan,
                          @daCongDiem
                      )",
                    conn,
                    tran);

                historyCmd.Parameters.AddWithValue("@userId", userId);
                historyCmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                historyCmd.Parameters.AddWithValue("@capDoId", capDoId);
                historyCmd.Parameters.AddWithValue("@nuocDaDi", move);
                historyCmd.Parameters.AddWithValue("@ketQua", isCorrect);
                historyCmd.Parameters.AddWithValue("@diemNhan", diemNhan);
                historyCmd.Parameters.AddWithValue("@daCongDiem", daCongDiem);

                historyCmd.ExecuteNonQuery();

                tran.Commit();

                if (isCorrect)
                {
                    return Json(new
                    {
                        success = true,
                        correct = true,
                        message = daCongDiem
                            ? $"Chính xác! Bạn được cộng {diemNhan} điểm."
                            : "Chính xác! Câu này bạn đã từng giải đúng nên không cộng điểm nữa.",
                        diemNhan,
                        daCongDiem,
                        fenAfter = validateResult.fen_after,
                        isCheckmate = validateResult.is_checkmate
                    });
                }

                string wrongMessage = "Sai rồi, hãy thử lại. Không bị trừ điểm.";

                if (loaiCauDo.Equals("MATE", StringComparison.OrdinalIgnoreCase) && !validateResult.is_checkmate)
                {
                    wrongMessage = "Nước này đi được, nhưng chưa chiếu hết. Hãy thử nước khác.";
                }

                if (!dungLoiGiai)
                {
                    wrongMessage = "Nước đi chưa đúng lời giải. Hãy thử lại.";
                }

                return Json(new
                {
                    success = true,
                    correct = false,
                    message = wrongMessage,
                    isCheckmate = validateResult.is_checkmate
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();

                return Json(new
                {
                    success = false,
                    message = "Lỗi kiểm tra câu đố: " + ex.Message
                });
            }
        }

        private int GetOrCreatePuzzleCheDoId(SqlConnection conn, SqlTransaction tran)
        {
            var findCmd = new SqlCommand(
                @"SELECT TOP 1 CheDoID
          FROM CheDoChoi
          WHERE LoaiCheDo = N'PUZZLE'
             OR TenCheDo = N'Câu Đố'
             OR TenCheDo = N'Cờ Câu Đố'
          ORDER BY CheDoID",
                conn,
                tran);

            var found = findCmd.ExecuteScalar();

            if (found != null && found != DBNull.Value)
            {
                return Convert.ToInt32(found);
            }

            var insertCmd = new SqlCommand(
                @"INSERT INTO CheDoChoi
          (
              TenCheDo,
              LoaiCheDo,
              ThoiGian
          )
          OUTPUT INSERTED.CheDoID
          VALUES
          (
              N'Cờ Câu Đố',
              N'PUZZLE',
              0
          )",
                conn,
                tran);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private void CapNhatXepHangCauDo(
    SqlConnection conn,
    SqlTransaction tran,
    int userId,
    int diemCong)
        {
            if (diemCong <= 0)
            {
                return;
            }

            int cheDoId = GetOrCreatePuzzleCheDoId(conn, tran);

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
            int diemMoi = diemCu + diemCong;

            if (diemMoi > 5000)
            {
                diemMoi = 5000;
            }

            if (diemMoi < 0)
            {
                diemMoi = 0;
            }

            var updateRankCmd = new SqlCommand(
                @"UPDATE XepHang
          SET Diem = @diemMoi,
              SoVan = SoVan + 1,
              Thang = Thang + 1
          WHERE UserID = @userId
            AND CheDoID = @cheDoId",
                conn,
                tran);

            updateRankCmd.Parameters.AddWithValue("@diemMoi", diemMoi);
            updateRankCmd.Parameters.AddWithValue("@userId", userId);
            updateRankCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
            updateRankCmd.ExecuteNonQuery();

            if (diemCu != diemMoi)
            {
                var historyCmd = new SqlCommand(
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
                  NULL
              )",
                    conn,
                    tran);

                historyCmd.Parameters.AddWithValue("@userId", userId);
                historyCmd.Parameters.AddWithValue("@cheDoId", cheDoId);
                historyCmd.Parameters.AddWithValue("@diemCu", diemCu);
                historyCmd.Parameters.AddWithValue("@diemMoi", diemMoi);
                historyCmd.ExecuteNonQuery();
            }
        }

        /* ĐỌC DÒNG SQL THÀNH DICTIONARY */
        private Dictionary<string, object> ReadRow(SqlDataReader reader)
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader[i] == DBNull.Value ? "" : reader[i];
            }

            return row;
        }
    }
}