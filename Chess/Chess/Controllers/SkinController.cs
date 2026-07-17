using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Chess.Controllers
{
    public class SkinController : Controller
    {
        private readonly string _connStr;

        public SkinController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult Choose()
        {
            return View();
        }

        // Lấy toàn bộ skin bàn cờ + quân cờ
        [HttpGet]
        public IActionResult GetSkins(int loaiCoId = 0)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                if (loaiCoId == 0)
                {
                    loaiCoId = GetDefaultLoaiCoId(conn);
                }

                var boardSkins = new List<object>();
                var pieceSkins = new List<object>();

                var boardCmd = new SqlCommand(
                    @"SELECT 
                          SkinBanCoID,
                          LoaiCoID,
                          TenSkin,
                          MaSkin,
                          MauOTrang,
                          MauODen,
                          AnhNenBanCo,
                          AnhOSang,
                          AnhODen,
                          MoTa
                      FROM SkinBanCo
                      WHERE TrangThai = 1
                        AND (LoaiCoID = @loaiCoId OR LoaiCoID IS NULL)
                      ORDER BY SkinBanCoID",
                    conn);

                boardCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

                using (var reader = boardCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        boardSkins.Add(new
                        {
                            skinBanCoId = Convert.ToInt32(reader["SkinBanCoID"]),
                            loaiCoId = reader["LoaiCoID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LoaiCoID"]),
                            tenSkin = reader["TenSkin"].ToString(),
                            maSkin = reader["MaSkin"].ToString(),
                            mauOTrang = reader["MauOTrang"].ToString(),
                            mauODen = reader["MauODen"].ToString(),
                            anhNenBanCo = reader["AnhNenBanCo"] == DBNull.Value ? "" : reader["AnhNenBanCo"].ToString(),
                            anhOSang = reader["AnhOSang"] == DBNull.Value ? "" : reader["AnhOSang"].ToString(),
                            anhODen = reader["AnhODen"] == DBNull.Value ? "" : reader["AnhODen"].ToString(),
                            moTa = reader["MoTa"] == DBNull.Value ? "" : reader["MoTa"].ToString()
                        });
                    }
                }

                var pieceCmd = new SqlCommand(
                    @"SELECT 
                          SkinQuanCoID,
                          LoaiCoID,
                          TenSkin,
                          MaSkin,
                          KieuHienThi,
                          DuongDanThuMuc,
                          CssClass,
                          MoTa
                      FROM SkinQuanCo
                      WHERE TrangThai = 1
                        AND (LoaiCoID = @loaiCoId OR LoaiCoID IS NULL)
                      ORDER BY SkinQuanCoID",
                    conn);

                pieceCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

                using (var reader = pieceCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pieceSkins.Add(new
                        {
                            skinQuanCoId = Convert.ToInt32(reader["SkinQuanCoID"]),
                            loaiCoId = reader["LoaiCoID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LoaiCoID"]),
                            tenSkin = reader["TenSkin"].ToString(),
                            maSkin = reader["MaSkin"].ToString(),
                            kieuHienThi = reader["KieuHienThi"].ToString(),
                            duongDanThuMuc = reader["DuongDanThuMuc"] == DBNull.Value ? "" : reader["DuongDanThuMuc"].ToString(),
                            cssClass = reader["CssClass"] == DBNull.Value ? "" : reader["CssClass"].ToString(),
                            moTa = reader["MoTa"] == DBNull.Value ? "" : reader["MoTa"].ToString()
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    loaiCoId,
                    boardSkins,
                    pieceSkins
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Lấy skin hiện tại của user
        [HttpGet]
        public IActionResult GetMySkin(int loaiCoId = 0)
        {
            try
            {
                int userId = GetCurrentUserId();

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                if (loaiCoId == 0)
                {
                    loaiCoId = GetDefaultLoaiCoId(conn);
                }

                var cmd = new SqlCommand(
                    @"SELECT TOP 1
                          cds.UserID,
                          cds.LoaiCoID,

                          sb.SkinBanCoID,
                          sb.TenSkin AS TenSkinBanCo,
                          sb.MaSkin AS MaSkinBanCo,
                          sb.MauOTrang,
                          sb.MauODen,
                          sb.AnhNenBanCo,
                          sb.AnhOSang,
                          sb.AnhODen,

                          sq.SkinQuanCoID,
                          sq.TenSkin AS TenSkinQuanCo,
                          sq.MaSkin AS MaSkinQuanCo,
                          sq.KieuHienThi,
                          sq.DuongDanThuMuc,
                          sq.CssClass
                      FROM CaiDatSkinNguoiDung cds
                      INNER JOIN SkinBanCo sb ON cds.SkinBanCoID = sb.SkinBanCoID
                      INNER JOIN SkinQuanCo sq ON cds.SkinQuanCoID = sq.SkinQuanCoID
                      WHERE cds.UserID = @userId
                        AND cds.LoaiCoID = @loaiCoId",
                    conn);

                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Json(new
                    {
                        success = true,
                        hasCustomSkin = true,
                        loaiCoId,

                        boardSkin = new
                        {
                            skinBanCoId = Convert.ToInt32(reader["SkinBanCoID"]),
                            tenSkin = reader["TenSkinBanCo"].ToString(),
                            maSkin = reader["MaSkinBanCo"].ToString(),
                            mauOTrang = reader["MauOTrang"].ToString(),
                            mauODen = reader["MauODen"].ToString(),
                            anhNenBanCo = reader["AnhNenBanCo"] == DBNull.Value ? "" : reader["AnhNenBanCo"].ToString(),
                            anhOSang = reader["AnhOSang"] == DBNull.Value ? "" : reader["AnhOSang"].ToString(),
                            anhODen = reader["AnhODen"] == DBNull.Value ? "" : reader["AnhODen"].ToString()
                        },

                        pieceSkin = new
                        {
                            skinQuanCoId = Convert.ToInt32(reader["SkinQuanCoID"]),
                            tenSkin = reader["TenSkinQuanCo"].ToString(),
                            maSkin = reader["MaSkinQuanCo"].ToString(),
                            kieuHienThi = reader["KieuHienThi"].ToString(),
                            duongDanThuMuc = reader["DuongDanThuMuc"] == DBNull.Value ? "" : reader["DuongDanThuMuc"].ToString(),
                            cssClass = reader["CssClass"] == DBNull.Value ? "" : reader["CssClass"].ToString()
                        }
                    });
                }

                reader.Close();

                var defaultSkin = GetDefaultSkin(conn, loaiCoId);

                return Json(new
                {
                    success = true,
                    hasCustomSkin = false,
                    loaiCoId,
                    boardSkin = defaultSkin.boardSkin,
                    pieceSkin = defaultSkin.pieceSkin
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Lưu skin user chọn
        [HttpPost]
        public IActionResult SaveSkin(int loaiCoId, int skinBanCoId, int skinQuanCoId)
        {
            try
            {
                int userId = GetCurrentUserId();

                using var conn = new SqlConnection(_connStr);
                conn.Open();

                if (loaiCoId == 0)
                {
                    loaiCoId = GetDefaultLoaiCoId(conn);
                }

                var checkCmd = new SqlCommand(
                    @"SELECT COUNT(*)
                      FROM CaiDatSkinNguoiDung
                      WHERE UserID = @userId
                        AND LoaiCoID = @loaiCoId",
                    conn);

                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    var updateCmd = new SqlCommand(
                        @"UPDATE CaiDatSkinNguoiDung
                          SET SkinBanCoID = @skinBanCoId,
                              SkinQuanCoID = @skinQuanCoId,
                              NgayCapNhat = SYSDATETIME()
                          WHERE UserID = @userId
                            AND LoaiCoID = @loaiCoId",
                        conn);

                    updateCmd.Parameters.AddWithValue("@skinBanCoId", skinBanCoId);
                    updateCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);
                    updateCmd.Parameters.AddWithValue("@userId", userId);
                    updateCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    var insertCmd = new SqlCommand(
                        @"INSERT INTO CaiDatSkinNguoiDung
                          (
                              UserID,
                              LoaiCoID,
                              SkinBanCoID,
                              SkinQuanCoID
                          )
                          VALUES
                          (
                              @userId,
                              @loaiCoId,
                              @skinBanCoId,
                              @skinQuanCoId
                          )",
                        conn);

                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);
                    insertCmd.Parameters.AddWithValue("@skinBanCoId", skinBanCoId);
                    insertCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);

                    insertCmd.ExecuteNonQuery();
                }

                return Json(new
                {
                    success = true,
                    message = "Đã lưu skin thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Lấy chi tiết quân theo skin
        [HttpGet]
        public IActionResult GetPieceSkinDetail(int skinQuanCoId)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();

                string duongDanThuMuc = "";
                string kieuHienThi = "";

                var skinCmd = new SqlCommand(
                    @"SELECT 
                          KieuHienThi,
                          ISNULL(DuongDanThuMuc, '') AS DuongDanThuMuc
                      FROM SkinQuanCo
                      WHERE SkinQuanCoID = @skinQuanCoId",
                    conn);

                skinCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);

                using (var reader = skinCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        kieuHienThi = reader["KieuHienThi"].ToString() ?? "";
                        duongDanThuMuc = reader["DuongDanThuMuc"].ToString() ?? "";
                    }
                }

                var pieces = new Dictionary<string, object>();

                var detailCmd = new SqlCommand(
                    @"SELECT 
                          MaQuan,
                          KyTuUnicode,
                          FileAnh
                      FROM ChiTietSkinQuanCo
                      WHERE SkinQuanCoID = @skinQuanCoId",
                    conn);

                detailCmd.Parameters.AddWithValue("@skinQuanCoId", skinQuanCoId);

                using (var reader = detailCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string maQuan = reader["MaQuan"].ToString() ?? "";

                        string kyTuUnicode = reader["KyTuUnicode"] == DBNull.Value
                            ? ""
                            : reader["KyTuUnicode"].ToString() ?? "";

                        string fileAnh = reader["FileAnh"] == DBNull.Value
                            ? ""
                            : reader["FileAnh"].ToString() ?? "";

                        string fullImagePath = "";

                        if (!string.IsNullOrWhiteSpace(fileAnh))
                        {
                            fullImagePath = duongDanThuMuc.TrimEnd('/') + "/" + fileAnh;
                        }

                        pieces[maQuan] = new
                        {
                            maQuan,
                            kyTuUnicode,
                            fileAnh,
                            fullImagePath
                        };
                    }
                }

                return Json(new
                {
                    success = true,
                    skinQuanCoId,
                    kieuHienThi,
                    duongDanThuMuc,
                    pieces
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdText = HttpContext.Session.GetString("UserID");

            if (string.IsNullOrWhiteSpace(userIdText))
            {
                throw new Exception("Bạn cần đăng nhập để dùng skin.");
            }

            if (!int.TryParse(userIdText, out int userId))
            {
                throw new Exception("UserID trong Session không hợp lệ.");
            }

            return userId;
        }

        private int GetDefaultLoaiCoId(SqlConnection conn)
        {
            var cmd = new SqlCommand(
                @"SELECT TOP 1 LoaiCoID
                  FROM LoaiCo
                  WHERE TenLoai IN (N'Cờ vua', N'Chess')
                  ORDER BY LoaiCoID",
                conn);

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
            {
                throw new Exception("Không tìm thấy LoaiCo mặc định.");
            }

            return Convert.ToInt32(result);
        }

        private (object boardSkin, object pieceSkin) GetDefaultSkin(SqlConnection conn, int loaiCoId)
        {
            object boardSkin = new { };
            object pieceSkin = new { };

            var boardCmd = new SqlCommand(
                @"SELECT TOP 1
                      SkinBanCoID,
                      TenSkin,
                      MaSkin,
                      MauOTrang,
                      MauODen,
                      AnhNenBanCo,
                      AnhOSang,
                      AnhODen
                  FROM SkinBanCo
                  WHERE TrangThai = 1
                    AND (LoaiCoID = @loaiCoId OR LoaiCoID IS NULL)
                  ORDER BY SkinBanCoID",
                conn);

            boardCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

            using (var reader = boardCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    boardSkin = new
                    {
                        skinBanCoId = Convert.ToInt32(reader["SkinBanCoID"]),
                        tenSkin = reader["TenSkin"].ToString(),
                        maSkin = reader["MaSkin"].ToString(),
                        mauOTrang = reader["MauOTrang"].ToString(),
                        mauODen = reader["MauODen"].ToString(),
                        anhNenBanCo = reader["AnhNenBanCo"] == DBNull.Value ? "" : reader["AnhNenBanCo"].ToString(),
                        anhOSang = reader["AnhOSang"] == DBNull.Value ? "" : reader["AnhOSang"].ToString(),
                        anhODen = reader["AnhODen"] == DBNull.Value ? "" : reader["AnhODen"].ToString()
                    };
                }
            }

            var pieceCmd = new SqlCommand(
                @"SELECT TOP 1
                      SkinQuanCoID,
                      TenSkin,
                      MaSkin,
                      KieuHienThi,
                      DuongDanThuMuc,
                      CssClass
                  FROM SkinQuanCo
                  WHERE TrangThai = 1
                    AND (LoaiCoID = @loaiCoId OR LoaiCoID IS NULL)
                  ORDER BY 
                      CASE WHEN MaSkin = 'png-default' THEN 0 ELSE 1 END,
                      SkinQuanCoID",
                conn);

            pieceCmd.Parameters.AddWithValue("@loaiCoId", loaiCoId);

            using (var reader = pieceCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    pieceSkin = new
                    {
                        skinQuanCoId = Convert.ToInt32(reader["SkinQuanCoID"]),
                        tenSkin = reader["TenSkin"].ToString(),
                        maSkin = reader["MaSkin"].ToString(),
                        kieuHienThi = reader["KieuHienThi"].ToString(),
                        duongDanThuMuc = reader["DuongDanThuMuc"] == DBNull.Value ? "" : reader["DuongDanThuMuc"].ToString(),
                        cssClass = reader["CssClass"] == DBNull.Value ? "" : reader["CssClass"].ToString()
                    };
                }
            }

            return (boardSkin, pieceSkin);
        }
    }
}