USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DuLieuCoVua')
BEGIN
    CREATE DATABASE DuLieuCoVua;
END
GO

USE DuLieuCoVua;
GO

/* ========================================================= */
/* ======================= TAO BANG ======================== */
/* ========================================================= */

----------------------------------------
--========== VaiTro ==========--
----------------------------------------
CREATE TABLE VaiTro
(


    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(20) NOT NULL
        CHECK (TenVaiTro IN (N'ADMIN', N'USER'))
);
GO

----------------------------------------
--========== ThongTinUser ==========--
----------------------------------------
CREATE TABLE ThongTinUser
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    Avatar NVARCHAR(255) NULl 
        DEFAULT '/images/default-avatar.png',
    MatKhau NVARCHAR(100) NOT NULL, 
        CHECK (LEN(MatKhau) >= 6),
    HoTen NVARCHAR(100),
    NgaySinh DATE NULL,
        CONSTRAINT CK_Users_NgaySinh CHECK 
        (            
                NgaySinh <= CAST(GETDATE() AS DATE)
                -- độ tuổi được chơi cờ vua 
                AND DATEADD(YEAR, 6, NgaySinh) <= CAST(GETDATE() AS DATE)
                AND DATEADD(YEAR, 100, NgaySinh) >= CAST(GETDATE() AS DATE)
        ),
    GioiTinh NVARCHAR(10) NULL,
        CONSTRAINT CK_Users_GioiTinh CHECK 
        (
            GioiTinh IN (N'NAM', N'NU', N'KHAC')
        ),
    
    Gmail NVARCHAR(100) NOT NULL UNIQUE,
            CONSTRAINT CK_User_Gmail CHECK 
            (
                -- phải có đúng 1 @
                Gmail LIKE '%@%' 
                AND Gmail NOT LIKE '%@%@%'

                -- phần trước @ không rỗng
                AND LEFT(Gmail, CHARINDEX('@', Gmail) - 1) <> ''

                -- phần sau @ phải có dấu .
                AND CHARINDEX('.', Gmail, CHARINDEX('@', Gmail)) > 0

                -- không có .. liên tiếp
                AND Gmail NOT LIKE '%..%'

                -- không bắt đầu hoặc kết thúc bằng . hoặc @
                AND Gmail NOT LIKE '.%' 
                AND Gmail NOT LIKE '%.' 
                AND Gmail NOT LIKE '@%' 
                AND Gmail NOT LIKE '%@'

                -- không có dạng @. hoặc .@
                AND Gmail NOT LIKE '%@.%' 
                AND Gmail NOT LIKE '%.@%'

                -- ký tự hợp lệ (giới hạn cơ bản)
                AND Gmail NOT LIKE '%[^A-Za-z0-9@._%+-]%'
            ),

    SoDienThoai VARCHAR(16) NULL,
        CONSTRAINT CK_Users_SoDienThoai CHECK 
        (
            SoDienThoai IS NULL OR 
            (
                -- bắt đầu bằng +
                SoDienThoai LIKE '+%'

                -- chỉ có 1 dấu + ở đầu
                AND CHARINDEX('+', SoDienThoai) = 1

                -- chỉ chứa số sau dấu +
                AND SoDienThoai NOT LIKE '%[^0-9+]%'

                -- không có khoảng trắng
                AND SoDienThoai NOT LIKE '% %'

                -- độ dài chuẩn E.164: tối đa 15 số + dấu +
                AND LEN(SoDienThoai) BETWEEN 9 AND 16

                -- không cho dạng +0 (sai chuẩn quốc tế)
                AND SoDienThoai NOT LIKE '+0%'
            )
        ),

    TrangThai BIT DEFAULT 1,

    NgayTao DATETIME2(0) NULL 
        CONSTRAINT DF_Users_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL 
        CONSTRAINT DF_Users_NgayCapNhat DEFAULT SYSDATETIME()       
    
);
GO

----------------------------------------
--========== NguoiDungVaiTro ==========--
----------------------------------------
CREATE TABLE NguoiDungVaiTro
(
    UserID INT,
    RoleID INT,

    PRIMARY KEY (UserID, RoleID),

    FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID)
        ON DELETE CASCADE,

    FOREIGN KEY (RoleID) REFERENCES VaiTro(RoleID)
        ON DELETE CASCADE
);
GO

----------------------------------------
--========== ThongKeNguoiDung ==========--
----------------------------------------
CREATE TABLE ThongKeNguoiDung
(
    UserID INT PRIMARY KEY,

    TongVan INT NOT NULL DEFAULT 0,
    Thang INT NOT NULL DEFAULT 0,
    Thua INT NOT NULL DEFAULT 0,
    Hoa INT NOT NULL DEFAULT 0,

    CONSTRAINT CK_ThongKe_SoDuong 
        CHECK (TongVan >= 0 AND Thang >= 0 AND Thua >= 0 AND Hoa >= 0),

    CONSTRAINT CK_ThongKe_HopLe
        CHECK (Thang + Thua + Hoa <= TongVan),

    FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID)
        ON DELETE CASCADE
);
GO

----------------------------------------
--========== CheDoChoi ==========--
----------------------------------------
CREATE TABLE CheDoChoi 
(
    CheDoID INT IDENTITY(1,1) PRIMARY KEY,
    TenCheDo NVARCHAR(50) NOT NULL,
    LoaiCheDo NVARCHAR(20) NOT NULL,
    ThoiGian INT NOT NULL,

    CONSTRAINT UQ_CheDoChoi_Ten UNIQUE (TenCheDo),

    CONSTRAINT CK_CheDoChoi_Loai CHECK 
    (
        LoaiCheDo IN (N'PVP', N'BOT', N'PUZZLE')
    ),

    CONSTRAINT CK_CheDoChoi_Ten CHECK 
    (
        LEN(TenCheDo) BETWEEN 3 AND 50
        AND TenCheDo = LTRIM(RTRIM(TenCheDo))
        AND TenCheDo NOT LIKE '%  %'
    ),

    CONSTRAINT CK_CheDoChoi_ThoiGian CHECK 
    (
        ThoiGian IN (0, 60, 180, 300, 600, 900, 1800, 3600)
    )
);
GO

----------------------------------------
--========== Bot ==========--
----------------------------------------
CREATE TABLE Bot 
(
    BotID INT IDENTITY(1,1) PRIMARY KEY,

    TenBot NVARCHAR(50) NOT NULL,

    DoKho TINYINT NOT NULL,  -- 1 → 10

    MoTa NVARCHAR(255) NULL,

    -- tên bot không trùng
    CONSTRAINT UQ_Bot_Ten UNIQUE (TenBot),

    -- CHECK tên bot
    CONSTRAINT CK_Bot_Ten CHECK 
    (
        LEN(TenBot) BETWEEN 3 AND 50
        AND TenBot = LTRIM(RTRIM(TenBot))
        AND TenBot NOT LIKE '%  %'
    ),

    -- CHECK độ khó
    CONSTRAINT CK_Bot_DoKho CHECK 
    (
        DoKho BETWEEN 1 AND 10
    ),

    -- CHECK mô tả 
    CONSTRAINT CK_Bot_MoTa CHECK 
    (
        MoTa IS NULL OR (
            LEN(MoTa) <= 255
            AND MoTa = LTRIM(RTRIM(MoTa))
        )
    )
);
GO

----------------------------------------
--========== LoaiCo ==========--
----------------------------------------
CREATE TABLE LoaiCo 
(
    LoaiCoID INT IDENTITY(1,1) PRIMARY KEY,

    TenLoai NVARCHAR(50) NOT NULL,

    CONSTRAINT UQ_LoaiCo_Ten UNIQUE (TenLoai),

    CONSTRAINT CK_LoaiCo_Ten CHECK 
    (
        LTRIM(RTRIM(TenLoai)) IN 
        (
            N'Cờ vua', N'Chess'
        )
    )
);
GO

----------------------------------------
--========== CapDoCauDo ==========--
----------------------------------------
CREATE TABLE CapDoCauDo
(
    CapDoID INT IDENTITY(1,1) PRIMARY KEY,
    TenCapDo NVARCHAR(50) NOT NULL,
    MaCapDo NVARCHAR(50) NOT NULL UNIQUE,
    DiemCong INT NOT NULL DEFAULT 10,
    MoTa NVARCHAR(255) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE()
);
GO

----------------------------------------
--========== Puzzle ==========--
----------------------------------------
CREATE TABLE Puzzle 
(
    PuzzleID INT IDENTITY(1,1) PRIMARY KEY,

    FEN NVARCHAR(100) NOT NULL,
    LoiGiai NVARCHAR(500) NOT NULL,
    DoKho TINYINT NOT NULL,
    MoTa NVARCHAR(255) NULL,

    CapDoID INT NOT NULL,
    TieuDe NVARCHAR(100) NOT NULL,
    LoaiCauDo NVARCHAR(20) NOT NULL DEFAULT N'MATE',
    DiemThuong INT NOT NULL DEFAULT 10,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Puzzle_CapDoCauDo
        FOREIGN KEY (CapDoID) REFERENCES CapDoCauDo(CapDoID),

    CONSTRAINT UQ_Puzzle_FEN UNIQUE (FEN),

    CONSTRAINT CK_Puzzle_FEN CHECK 
    (
        LEN(FEN) BETWEEN 15 AND 100
        AND FEN = LTRIM(RTRIM(FEN))
    ),

    CONSTRAINT CK_Puzzle_LoiGiai CHECK 
    (
        LEN(LoiGiai) >= 3
        AND LoiGiai = LTRIM(RTRIM(LoiGiai))
        AND LoiGiai NOT LIKE '%  %'
    ),

    CONSTRAINT CK_Puzzle_DoKho CHECK 
    (
        DoKho BETWEEN 1 AND 10
    ),

    CONSTRAINT CK_Puzzle_LoaiCauDo CHECK
    (
        LoaiCauDo IN (N'MATE', N'SAVE')
    ),

    CONSTRAINT CK_Puzzle_MoTa CHECK 
    (
        MoTa IS NULL OR 
        (
            LEN(MoTa) <= 255
            AND MoTa = LTRIM(RTRIM(MoTa))
        )
    )
);
GO

----------------------------------------
--========== DiemCauDoNguoiDung ==========--
----------------------------------------
CREATE TABLE DiemCauDoNguoiDung
(
    DiemCauDoID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CapDoID INT NOT NULL,
    TongDiem INT NOT NULL DEFAULT 0,
    SoCauDung INT NOT NULL DEFAULT 0,
    NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_DiemCauDo_User
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_DiemCauDo_CapDo
        FOREIGN KEY (CapDoID) REFERENCES CapDoCauDo(CapDoID),

    CONSTRAINT UQ_DiemCauDo_User_CapDo
        UNIQUE (UserID, CapDoID)
);
GO

----------------------------------------
--========== LichSuLamCauDo ==========--
----------------------------------------
CREATE TABLE LichSuLamCauDo
(
    LichSuID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    PuzzleID INT NOT NULL,
    CapDoID INT NOT NULL,
    NuocDaDi NVARCHAR(20) NOT NULL,
    KetQua BIT NOT NULL DEFAULT 0,
    DiemNhan INT NOT NULL DEFAULT 0,
    DaCongDiem BIT NOT NULL DEFAULT 0,
    ThoiGianLam DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_LichSuCauDo_User
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_LichSuCauDo_Puzzle
        FOREIGN KEY (PuzzleID) REFERENCES Puzzle(PuzzleID),

    CONSTRAINT FK_LichSuCauDo_CapDo
        FOREIGN KEY (CapDoID) REFERENCES CapDoCauDo(CapDoID)
);
GO

----------------------------------------
--========== Phong ==========--
----------------------------------------
CREATE TABLE Phong 
(
    PhongID INT IDENTITY(1,1) PRIMARY KEY,

    ChuPhongID INT NOT NULL,
    KhachID INT NULL,

    CheDoID INT NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL 
        CONSTRAINT DF_Phong_TrangThai DEFAULT N'WAITING',

    ThoiGianTao DATETIME2(0) NOT NULL 
        CONSTRAINT DF_Phong_Time DEFAULT SYSDATETIME(),

    FOREIGN KEY (ChuPhongID) 
        REFERENCES ThongTinUser(UserID) ON DELETE CASCADE,

    FOREIGN KEY (KhachID) 
        REFERENCES ThongTinUser(UserID) 
        ON DELETE NO ACTION,   

    FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    CONSTRAINT CK_Phong_TrangThai CHECK 
    (
        TrangThai IN (N'WAITING', N'PLAYING', N'FINISHED', N'CANCELLED')
    ),

    CONSTRAINT CK_Phong_KhongTrung CHECK (
        KhachID IS NULL OR KhachID <> ChuPhongID
    )
);
GO

----------------------------------------
--========== VanCo ==========--
----------------------------------------
CREATE TABLE VanCo 
(
    VanCoID INT IDENTITY(1,1) PRIMARY KEY,

    PhongID INT NULL,

    NguoiTrangID INT NULL,
    NguoiDenID INT NULL,

    BotID INT NULL,
    PuzzleID INT NULL,

    LoaiCoID INT NOT NULL,
    CheDoID INT NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL 
        CONSTRAINT DF_VanCo_TrangThai DEFAULT N'PLAYING',

    LuotDi NVARCHAR(5) NULL,

    KetQua NVARCHAR(10) NULL,

    FEN NVARCHAR(100) NULL,

    ThoiGianBatDau DATETIME2(0) NOT NULL 
        CONSTRAINT DF_VanCo_Start DEFAULT SYSDATETIME(),

    ThoiGianKetThuc DATETIME2(0) NULL,

    CONSTRAINT FK_VanCo_Phong FOREIGN KEY (PhongID) REFERENCES Phong(PhongID),
    CONSTRAINT FK_VanCo_Trang FOREIGN KEY (NguoiTrangID) REFERENCES ThongTinUser(UserID),
    CONSTRAINT FK_VanCo_Den FOREIGN KEY (NguoiDenID) REFERENCES ThongTinUser(UserID),
    CONSTRAINT FK_VanCo_Bot FOREIGN KEY (BotID) REFERENCES Bot(BotID),
    CONSTRAINT FK_VanCo_Puzzle FOREIGN KEY (PuzzleID) REFERENCES Puzzle(PuzzleID),
    CONSTRAINT FK_VanCo_LoaiCo FOREIGN KEY (LoaiCoID) REFERENCES LoaiCo(LoaiCoID),
    CONSTRAINT FK_VanCo_CheDo FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    CONSTRAINT CK_VanCo_LuotDi CHECK 
    (
        LuotDi IS NULL OR LuotDi IN ('WHITE','BLACK')
    ),

    CONSTRAINT CK_VanCo_TrangThai CHECK 
    (
        TrangThai IN (N'PLAYING', N'END')
    ),

    CONSTRAINT CK_VanCo_KetQua CHECK 
    (
        KetQua IS NULL OR KetQua IN (N'WHITE_WIN', N'BLACK_WIN', N'DRAW')
    ),

    CONSTRAINT CK_VanCo_KhongTrung CHECK 
    (
        NguoiTrangID IS NULL OR 
        NguoiDenID IS NULL OR 
        NguoiTrangID <> NguoiDenID
    ),

    CONSTRAINT CK_VanCo_Time CHECK 
    (
        ThoiGianKetThuc IS NULL OR ThoiGianKetThuc >= ThoiGianBatDau
    ),

    CONSTRAINT CK_VanCo_Mode CHECK
    (
        -- Local: 2 người 1 máy
        (
            NguoiTrangID IS NOT NULL
            AND NguoiDenID IS NULL
            AND BotID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- PvP Online
        (
            NguoiTrangID IS NOT NULL
            AND NguoiDenID IS NOT NULL
            AND BotID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- Chơi với Bot
        (
            NguoiTrangID IS NOT NULL
            AND BotID IS NOT NULL
            AND NguoiDenID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- Puzzle
        (
            NguoiTrangID IS NOT NULL
            AND PuzzleID IS NOT NULL
            AND NguoiDenID IS NULL
            AND BotID IS NULL
        )
    ),

    CONSTRAINT CK_VanCo_PhongLogic CHECK
    (
        -- Local: không có phòng
        (
            PhongID IS NULL
            AND NguoiTrangID IS NOT NULL
            AND NguoiDenID IS NULL
            AND BotID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- PvP Online: có phòng
        (
            PhongID IS NOT NULL
            AND NguoiTrangID IS NOT NULL
            AND NguoiDenID IS NOT NULL
            AND BotID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- Bot: không có phòng
        (
            PhongID IS NULL
            AND NguoiTrangID IS NOT NULL
            AND BotID IS NOT NULL
            AND NguoiDenID IS NULL
            AND PuzzleID IS NULL
        )

        OR

        -- Puzzle: không có phòng
        (
            PhongID IS NULL
            AND NguoiTrangID IS NOT NULL
            AND PuzzleID IS NOT NULL
            AND NguoiDenID IS NULL
            AND BotID IS NULL
        )
    )
);
GO

----------------------------------------
--========== NuocDi ==========--
----------------------------------------
CREATE TABLE NuocDi 
(
    NuocDiID INT IDENTITY(1,1) PRIMARY KEY,

    VanCoID INT NOT NULL,

    SoThuTu INT NOT NULL,

    Nuoc NVARCHAR(10) NOT NULL,

    ThoiGian DATETIME2(0) NOT NULL 
        CONSTRAINT DF_NuocDi_Time DEFAULT SYSDATETIME(),

    -- ===== FK =====
    CONSTRAINT FK_NuocDi_VanCo 
        FOREIGN KEY (VanCoID) 
        REFERENCES VanCo(VanCoID)
        ON DELETE CASCADE,

    -- ===== UNIQUE =====
    CONSTRAINT UQ_NuocDi UNIQUE (VanCoID, SoThuTu),

    -- ===== CHECK =====

    -- thứ tự phải hợp lệ
    CONSTRAINT CK_NuocDi_ThuTu CHECK (
        SoThuTu > 0 AND SoThuTu <= 500
    ),

    -- nước đi cơ bản hợp lệ
    CONSTRAINT CK_NuocDi_Nuoc CHECK (
        LEN(Nuoc) BETWEEN 2 AND 10
        AND Nuoc = LTRIM(RTRIM(Nuoc))
        AND Nuoc NOT LIKE '%  %'
        AND Nuoc NOT LIKE '%[^a-zA-Z0-9+#=xO\-]%'
    )
);
GO

----------------------------------------
--========== XepHang ==========--
----------------------------------------
CREATE TABLE XepHang 
(
    XepHangID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT NOT NULL,
    CheDoID INT NOT NULL,

    Diem INT NOT NULL 
        CONSTRAINT DF_XepHang_Diem DEFAULT 1200,

    SoVan INT NOT NULL 
        CONSTRAINT DF_XepHang_SoVan DEFAULT 0,

    Thang INT NOT NULL 
        CONSTRAINT DF_XepHang_Thang DEFAULT 0,

    Thua INT NOT NULL 
        CONSTRAINT DF_XepHang_Thua DEFAULT 0,

    Hoa INT NOT NULL 
        CONSTRAINT DF_XepHang_Hoa DEFAULT 0,

    CONSTRAINT FK_XepHang_User 
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_XepHang_CheDo 
        FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    -- mỗi user chỉ có 1 xếp hạng cho mỗi chế độ
    CONSTRAINT UQ_XepHang UNIQUE (UserID, CheDoID),

    -- điểm hợp lệ
    CONSTRAINT CK_XepHang_Diem CHECK 
    (
        Diem BETWEEN 0 AND 5000
    ),

    -- số trận không âm
    CONSTRAINT CK_XepHang_SoVan CHECK 
    (
        SoVan >= 0
    ),

    CONSTRAINT CK_XepHang_Thang CHECK 
    (
        Thang >= 0
    ),

    CONSTRAINT CK_XepHang_Thua CHECK 
    (
        Thua >= 0
    ),

    CONSTRAINT CK_XepHang_Hoa CHECK 
    (
        Hoa >= 0
    ),

    -- tổng phải khớp
    CONSTRAINT CK_XepHang_Tong CHECK 
    (
        SoVan = Thang + Thua + Hoa
    )
);
GO

----------------------------------------
--========== LichSuDiem ==========--
----------------------------------------
CREATE TABLE LichSuDiem 
(
    LichSuID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT NOT NULL,
    CheDoID INT NOT NULL,

    DiemCu INT NOT NULL,
    DiemMoi INT NOT NULL,

    ThayDoi AS (DiemMoi - DiemCu) PERSISTED, -- tự tính

    ThoiGian DATETIME2(0) NOT NULL 
        CONSTRAINT DF_LichSuDiem_Time DEFAULT SYSDATETIME(),


    CONSTRAINT FK_LichSuDiem_User 
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_LichSuDiem_CheDo 
        FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    VanCoID INT,
    CONSTRAINT FK_LichSuDiem_VanCo 
        FOREIGN KEY (VanCoID) REFERENCES VanCo(VanCoID),

    CONSTRAINT CK_LichSuDiem_Diem CHECK 
    (
        DiemCu BETWEEN 0 AND 5000
        AND DiemMoi BETWEEN 0 AND 5000
    ),

    -- không cho giữ nguyên (phải có thay đổi)
    CONSTRAINT CK_LichSuDiem_Change CHECK (
        DiemCu <> DiemMoi
    )
);
GO

----------------------------------------
--========== GiaoDien ==========--
----------------------------------------
CREATE TABLE GiaoDien 
(
    GiaoDienID INT IDENTITY(1,1) PRIMARY KEY,

    Ten NVARCHAR(50) NOT NULL,

    MauBan NVARCHAR(50) NOT NULL,     -- tên theme (Classic, Dark…)
    KieuQuan NVARCHAR(50) NOT NULL,   -- Standard, Modern…

    -- UNIQUE
    CONSTRAINT UQ_GiaoDien_Ten UNIQUE (Ten),

    -- CHECK tên
    CONSTRAINT CK_GiaoDien_Ten CHECK (
        LEN(Ten) BETWEEN 3 AND 50
        AND Ten = LTRIM(RTRIM(Ten))
        AND Ten NOT LIKE '%  %'
    ),

    -- CHECK MauBan (không quá chặt để sau mở rộng)
    CONSTRAINT CK_GiaoDien_MauBan CHECK (
        LEN(MauBan) BETWEEN 3 AND 50
        AND MauBan = LTRIM(RTRIM(MauBan))
        AND MauBan NOT LIKE '%  %'
    ),

    -- CHECK KieuQuan
    CONSTRAINT CK_GiaoDien_KieuQuan CHECK (
        LEN(KieuQuan) BETWEEN 3 AND 50
        AND KieuQuan = LTRIM(RTRIM(KieuQuan))
        AND KieuQuan NOT LIKE '%  %'
    )
);
GO

----------------------------------------
--========== QuanCo ==========--
----------------------------------------
CREATE TABLE QuanCo 
(
    QuanCoID INT IDENTITY(1,1) PRIMARY KEY,

    Ten NVARCHAR(20) NOT NULL,
    KyHieu NVARCHAR(5) NOT NULL,
    GiaTri INT NOT NULL,
    MoTa NVARCHAR(500) NULL,

    -- không trùng tên/ký hiệu
    CONSTRAINT UQ_QuanCo_Ten UNIQUE (Ten),
    CONSTRAINT UQ_QuanCo_KyHieu UNIQUE (KyHieu),

    -- CHECK tên
    CONSTRAINT CK_QuanCo_Ten CHECK 
    (
        LEN(Ten) BETWEEN 2 AND 20
        AND Ten = LTRIM(RTRIM(Ten))
        AND Ten NOT LIKE '%  %'
    ),

    -- CHECK ký hiệu (ví dụ: K, Q, R, N, B, P)
    CONSTRAINT CK_QuanCo_KyHieu CHECK 
    (
        LEN(KyHieu) BETWEEN 1 AND 5
        AND KyHieu = LTRIM(RTRIM(KyHieu))
        AND KyHieu NOT LIKE '%  %'
    ),

    -- CHECK giá trị quân
    CONSTRAINT CK_QuanCo_GiaTri CHECK 
    (
        GiaTri BETWEEN 0 AND 100
    ),

    -- CHECK mô tả
    CONSTRAINT CK_QuanCo_MoTa CHECK 
    (
        MoTa IS NULL OR 
        (
            LEN(MoTa) <= 500
            AND MoTa = LTRIM(RTRIM(MoTa))
        )
    )
);
GO

----------------------------------------
--========== LuatDiChuyen ==========--
----------------------------------------
CREATE TABLE LuatDiChuyen 
(
    LuatID INT IDENTITY(1,1) PRIMARY KEY,

    QuanCoID INT NOT NULL,

    MoTa NVARCHAR(500) NOT NULL,

    CONSTRAINT FK_Luat_QuanCo 
        FOREIGN KEY (QuanCoID) REFERENCES QuanCo(QuanCoID)
        ON DELETE CASCADE,

    -- không trùng luật cho 1 quân
    CONSTRAINT UQ_Luat UNIQUE (QuanCoID, MoTa),

    -- CHECK mô tả
    CONSTRAINT CK_Luat_MoTa CHECK 
    (
        LEN(MoTa) BETWEEN 5 AND 500
        AND MoTa = LTRIM(RTRIM(MoTa))
        AND MoTa NOT LIKE '%  %'
    )
);
GO

----------------------------------------
--========== GiaiDau ==========--
----------------------------------------
CREATE TABLE GiaiDau
(
    GiaiDauID INT IDENTITY(1,1) PRIMARY KEY,

    TenGiaiDau NVARCHAR(100) NOT NULL,

    CheDoID INT NOT NULL,
    LoaiCoID INT NOT NULL,

    SoBang INT NOT NULL 
        CONSTRAINT DF_GiaiDau_SoBang DEFAULT 16,

    SoNguoiToiDa INT NOT NULL 
        CONSTRAINT DF_GiaiDau_SoNguoiToiDa DEFAULT 32,

    TrangThai NVARCHAR(30) NOT NULL 
        CONSTRAINT DF_GiaiDau_TrangThai DEFAULT N'CHO_DANG_KY',

    NguoiTaoID INT NULL,
    NguoiVoDichID INT NULL,

    ThoiGianTao DATETIME2(0) NOT NULL 
        CONSTRAINT DF_GiaiDau_ThoiGianTao DEFAULT SYSDATETIME(),

    ThoiGianBatDau DATETIME2(0) NULL,
    ThoiGianKetThuc DATETIME2(0) NULL,

    CONSTRAINT FK_GiaiDau_CheDo 
        FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    CONSTRAINT FK_GiaiDau_LoaiCo 
        FOREIGN KEY (LoaiCoID) REFERENCES LoaiCo(LoaiCoID),

    CONSTRAINT FK_GiaiDau_NguoiTao
        FOREIGN KEY (NguoiTaoID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_GiaiDau_NguoiVoDich
        FOREIGN KEY (NguoiVoDichID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT CK_GiaiDau_SoBang CHECK 
    (
        SoBang = 16
    ),

    CONSTRAINT CK_GiaiDau_SoNguoi CHECK 
    (
        SoNguoiToiDa = 32
    ),

    CONSTRAINT CK_GiaiDau_TrangThai CHECK 
    (
        TrangThai IN 
        (
            N'CHO_DANG_KY',
            N'DANG_DIEN_RA',
            N'DA_KET_THUC',
            N'DA_HUY'
        )
    ),

    CONSTRAINT CK_GiaiDau_Time CHECK 
    (
        ThoiGianKetThuc IS NULL 
        OR ThoiGianBatDau IS NULL
        OR ThoiGianKetThuc >= ThoiGianBatDau
    )
);
GO

----------------------------------------
--========== NguoiChoiGiaiDau ==========--
----------------------------------------
CREATE TABLE NguoiChoiGiaiDau
(
    NguoiChoiGiaiDauID INT IDENTITY(1,1) PRIMARY KEY,

    GiaiDauID INT NOT NULL,
    UserID INT NOT NULL,

    SoThuTu INT NOT NULL,

    NgayThamGia DATETIME2(0) NOT NULL
        CONSTRAINT DF_NguoiChoiGiaiDau_NgayThamGia DEFAULT SYSDATETIME(),

    TrangThai NVARCHAR(30) NOT NULL
        CONSTRAINT DF_NguoiChoiGiaiDau_TrangThai DEFAULT N'DANG_THAM_GIA',

    CONSTRAINT FK_NguoiChoiGiaiDau_GiaiDau
        FOREIGN KEY (GiaiDauID) REFERENCES GiaiDau(GiaiDauID)
        ON DELETE CASCADE,

    CONSTRAINT FK_NguoiChoiGiaiDau_User
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT UQ_NguoiChoi_GiaiDau
        UNIQUE (GiaiDauID, UserID),

    CONSTRAINT UQ_NguoiChoi_GiaiDau_ThuTu
        UNIQUE (GiaiDauID, SoThuTu),

    CONSTRAINT CK_NguoiChoiGiaiDau_SoThuTu CHECK
    (
        SoThuTu BETWEEN 1 AND 32
    ),

    CONSTRAINT CK_NguoiChoiGiaiDau_TrangThai CHECK
    (
        TrangThai IN 
        (
            N'DANG_THAM_GIA',
            N'DA_BI_LOAI',
            N'VO_DICH'
        )
    )
);
GO

----------------------------------------
--========== BangDau ==========--
----------------------------------------
CREATE TABLE BangDau
(
    BangDauID INT IDENTITY(1,1) PRIMARY KEY,

    GiaiDauID INT NOT NULL,

    TenBang NVARCHAR(20) NOT NULL,
    ThuTuBang INT NOT NULL,

    NguoiChoi1ID INT NULL,
    NguoiChoi2ID INT NULL,

    NguoiThangID INT NULL,

    TrangThai NVARCHAR(30) NOT NULL
        CONSTRAINT DF_BangDau_TrangThai DEFAULT N'CHO_DAU',

    CONSTRAINT FK_BangDau_GiaiDau
        FOREIGN KEY (GiaiDauID) REFERENCES GiaiDau(GiaiDauID)
        ON DELETE CASCADE,

    CONSTRAINT FK_BangDau_NguoiChoi1
        FOREIGN KEY (NguoiChoi1ID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_BangDau_NguoiChoi2
        FOREIGN KEY (NguoiChoi2ID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_BangDau_NguoiThang
        FOREIGN KEY (NguoiThangID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT UQ_BangDau_GiaiDau_ThuTu
        UNIQUE (GiaiDauID, ThuTuBang),

    CONSTRAINT CK_BangDau_ThuTu CHECK
    (
        ThuTuBang BETWEEN 1 AND 16
    ),

    CONSTRAINT CK_BangDau_TrangThai CHECK
    (
        TrangThai IN 
        (
            N'CHO_DAU',
            N'DANG_DAU',
            N'DA_KET_THUC'
        )
    ),

    CONSTRAINT CK_BangDau_KhongTrung CHECK
    (
        NguoiChoi1ID IS NULL
        OR NguoiChoi2ID IS NULL
        OR NguoiChoi1ID <> NguoiChoi2ID
    )
);
GO

----------------------------------------
--========== TranDauGiaiDau ==========--
----------------------------------------
CREATE TABLE TranDauGiaiDau
(
    TranDauGiaiDauID INT IDENTITY(1,1) PRIMARY KEY,

    GiaiDauID INT NOT NULL,
    BangDauID INT NULL,

    VongDau NVARCHAR(30) NOT NULL,

    ThuTuTran INT NOT NULL,

    NguoiChoi1ID INT NULL,
    NguoiChoi2ID INT NULL,

    NguoiThangID INT NULL,

    VanCoID INT NULL,

    TrangThai NVARCHAR(30) NOT NULL
        CONSTRAINT DF_TranDauGiaiDau_TrangThai DEFAULT N'CHO_DAU',

    ThoiGianTao DATETIME2(0) NOT NULL
        CONSTRAINT DF_TranDauGiaiDau_ThoiGianTao DEFAULT SYSDATETIME(),

    ThoiGianBatDau DATETIME2(0) NULL,
    ThoiGianKetThuc DATETIME2(0) NULL,

    CONSTRAINT FK_TranDauGiaiDau_GiaiDau
        FOREIGN KEY (GiaiDauID) REFERENCES GiaiDau(GiaiDauID)
        ON DELETE CASCADE,

    CONSTRAINT FK_TranDauGiaiDau_BangDau
        FOREIGN KEY (BangDauID) REFERENCES BangDau(BangDauID),

    CONSTRAINT FK_TranDauGiaiDau_NguoiChoi1
        FOREIGN KEY (NguoiChoi1ID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_TranDauGiaiDau_NguoiChoi2
        FOREIGN KEY (NguoiChoi2ID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_TranDauGiaiDau_NguoiThang
        FOREIGN KEY (NguoiThangID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_TranDauGiaiDau_VanCo
        FOREIGN KEY (VanCoID) REFERENCES VanCo(VanCoID),

    CONSTRAINT CK_TranDauGiaiDau_VongDau CHECK
    (
        VongDau IN 
        (
            N'VONG_BANG',
            N'VONG_1_16',
            N'TU_KET',
            N'BAN_KET',
            N'CHUNG_KET'
        )
    ),

    CONSTRAINT CK_TranDauGiaiDau_TrangThai CHECK
    (
        TrangThai IN 
        (
            N'CHO_DAU',
            N'DANG_DAU',
            N'DA_KET_THUC'
        )
    ),

    CONSTRAINT CK_TranDauGiaiDau_KhongTrung CHECK
    (
        NguoiChoi1ID IS NULL
        OR NguoiChoi2ID IS NULL
        OR NguoiChoi1ID <> NguoiChoi2ID
    ),

    CONSTRAINT CK_TranDauGiaiDau_Time CHECK 
    (
        ThoiGianKetThuc IS NULL 
        OR ThoiGianBatDau IS NULL
        OR ThoiGianKetThuc >= ThoiGianBatDau
    )
);
GO

----------------------------------------
--========== SkinBanCo ==========--
----------------------------------------
CREATE TABLE SkinBanCo
(
    SkinBanCoID INT IDENTITY(1,1) PRIMARY KEY,

    LoaiCoID INT NULL,

    TenSkin NVARCHAR(100) NOT NULL,
    MaSkin NVARCHAR(50) NOT NULL UNIQUE,

    MauOTrang VARCHAR(20) NOT NULL,
    MauODen VARCHAR(20) NOT NULL,

    AnhNenBanCo NVARCHAR(255) NULL,
    AnhOSang NVARCHAR(255) NULL,
    AnhODen NVARCHAR(255) NULL,

    MoTa NVARCHAR(255) NULL,

    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_SkinBanCo_LoaiCo
        FOREIGN KEY (LoaiCoID) REFERENCES LoaiCo(LoaiCoID)
);
GO

----------------------------------------
--========== SkinQuanCo ==========--
----------------------------------------
CREATE TABLE SkinQuanCo
(
    SkinQuanCoID INT IDENTITY(1,1) PRIMARY KEY,

    LoaiCoID INT NULL,

    TenSkin NVARCHAR(100) NOT NULL,
    MaSkin NVARCHAR(50) NOT NULL UNIQUE,

    KieuHienThi NVARCHAR(20) NOT NULL,

    DuongDanThuMuc NVARCHAR(255) NULL,
    CssClass NVARCHAR(100) NULL,

    MoTa NVARCHAR(255) NULL,

    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_SkinQuanCo_LoaiCo
        FOREIGN KEY (LoaiCoID) REFERENCES LoaiCo(LoaiCoID),

    CONSTRAINT CK_SkinQuanCo_KieuHienThi CHECK
    (
        KieuHienThi IN (N'UNICODE', N'IMAGE')
    )
);
GO

----------------------------------------
--========== ChiTietSkinQuanCo ==========--
----------------------------------------
CREATE TABLE ChiTietSkinQuanCo
(
    ChiTietSkinQuanCoID INT IDENTITY(1,1) PRIMARY KEY,

    SkinQuanCoID INT NOT NULL,

    MaQuan NVARCHAR(10) NOT NULL,
    KyTuUnicode NVARCHAR(10) NULL,
    FileAnh NVARCHAR(255) NULL,

    CONSTRAINT FK_ChiTietSkinQuanCo_SkinQuanCo
        FOREIGN KEY (SkinQuanCoID) REFERENCES SkinQuanCo(SkinQuanCoID)
        ON DELETE CASCADE,

    CONSTRAINT UQ_ChiTietSkinQuanCo
        UNIQUE (SkinQuanCoID, MaQuan)
);
GO

----------------------------------------
--========== CaiDatSkinNguoiDung ==========--
----------------------------------------
CREATE TABLE CaiDatSkinNguoiDung
(
    CaiDatSkinNguoiDungID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT NOT NULL,
    LoaiCoID INT NOT NULL,

    SkinBanCoID INT NOT NULL,
    SkinQuanCoID INT NOT NULL,

    NgayCapNhat DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_CaiDatSkin_User
        FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_CaiDatSkin_LoaiCo
        FOREIGN KEY (LoaiCoID) REFERENCES LoaiCo(LoaiCoID),

    CONSTRAINT FK_CaiDatSkin_SkinBanCo
        FOREIGN KEY (SkinBanCoID) REFERENCES SkinBanCo(SkinBanCoID),

    CONSTRAINT FK_CaiDatSkin_SkinQuanCo
        FOREIGN KEY (SkinQuanCoID) REFERENCES SkinQuanCo(SkinQuanCoID),

    CONSTRAINT UQ_CaiDatSkin_User_LoaiCo
        UNIQUE (UserID, LoaiCoID)
);
GO

/* ========================================================= */
/* ========================= INDEX ========================= */
/* ========================================================= */

CREATE INDEX IX_VanCo_NguoiTrang ON VanCo(NguoiTrangID);
GO
CREATE INDEX IX_VanCo_NguoiDen ON VanCo(NguoiDenID);
GO
CREATE INDEX IX_VanCo_Phong ON VanCo(PhongID);
GO
CREATE INDEX IX_VanCo_User ON VanCo(NguoiTrangID, NguoiDenID);
GO
CREATE INDEX IX_VanCo_TrangThai ON VanCo(TrangThai);
GO

CREATE INDEX IX_LichSuDiem_User ON LichSuDiem(UserID, ThoiGian DESC);
GO

CREATE INDEX IX_GiaiDau_TrangThai ON GiaiDau(TrangThai, ThoiGianTao DESC);
GO
CREATE INDEX IX_NguoiChoiGiaiDau_GiaiDau ON NguoiChoiGiaiDau(GiaiDauID, SoThuTu);
GO
CREATE INDEX IX_BangDau_GiaiDau ON BangDau(GiaiDauID, ThuTuBang);
GO
CREATE INDEX IX_TranDauGiaiDau_GiaiDau_Vong ON TranDauGiaiDau(GiaiDauID, VongDau, ThuTuTran);
GO

/* ========================================================= */


/* ========================================================= */
/* ======================= KET BAN ========================= */
/* ========================================================= */

CREATE TABLE LoiMoiKetBan
(
    LoiMoiID INT IDENTITY(1,1) PRIMARY KEY,
    NguoiGuiID INT NOT NULL,
    NguoiNhanID INT NOT NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'PENDING',
    ThoiGianGui DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ThoiGianPhanHoi DATETIME2 NULL,

    CONSTRAINT FK_LoiMoiKetBan_NguoiGui
        FOREIGN KEY (NguoiGuiID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_LoiMoiKetBan_NguoiNhan
        FOREIGN KEY (NguoiNhanID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT CK_LoiMoiKetBan_KhongTuGui
        CHECK (NguoiGuiID <> NguoiNhanID),

    CONSTRAINT CK_LoiMoiKetBan_TrangThai
        CHECK (TrangThai IN (N'PENDING', N'ACCEPTED', N'REJECTED', N'CANCELLED'))
);
GO

CREATE UNIQUE INDEX UX_LoiMoiKetBan_Pending
ON LoiMoiKetBan(NguoiGuiID, NguoiNhanID)
WHERE TrangThai = N'PENDING';
GO

CREATE TABLE BanBe
(
    BanBeID INT IDENTITY(1,1) PRIMARY KEY,
    UserID1 INT NOT NULL,
    UserID2 INT NOT NULL,
    ThoiGianKetBan DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_BanBe_User1
        FOREIGN KEY (UserID1) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_BanBe_User2
        FOREIGN KEY (UserID2) REFERENCES ThongTinUser(UserID),

    CONSTRAINT CK_BanBe_KhongTuKetBan
        CHECK (UserID1 <> UserID2),

    CONSTRAINT CK_BanBe_ThuTuUser
        CHECK (UserID1 < UserID2)
);
GO

CREATE UNIQUE INDEX UX_BanBe_CapUser
ON BanBe(UserID1, UserID2);
GO

CREATE TABLE LichHenPhong
(
    LichHenID INT IDENTITY(1,1) PRIMARY KEY,
    NguoiTaoID INT NOT NULL,
    NguoiDuocMoiID INT NOT NULL,
    CheDoID INT NOT NULL,
    ThoiGianHen DATETIME2 NOT NULL,
    GhiChu NVARCHAR(255) NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'PENDING',
    PhongID INT NULL,
    ThoiGianTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ThoiGianCapNhat DATETIME2 NULL,

    CONSTRAINT FK_LichHenPhong_NguoiTao
        FOREIGN KEY (NguoiTaoID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_LichHenPhong_NguoiDuocMoi
        FOREIGN KEY (NguoiDuocMoiID) REFERENCES ThongTinUser(UserID),

    CONSTRAINT FK_LichHenPhong_CheDo
        FOREIGN KEY (CheDoID) REFERENCES CheDoChoi(CheDoID),

    CONSTRAINT FK_LichHenPhong_Phong
        FOREIGN KEY (PhongID) REFERENCES Phong(PhongID),

    CONSTRAINT CK_LichHenPhong_KhongTuHen
        CHECK (NguoiTaoID <> NguoiDuocMoiID),

    CONSTRAINT CK_LichHenPhong_TrangThai
        CHECK (TrangThai IN (N'PENDING', N'ACCEPTED', N'REJECTED', N'CANCELLED', N'CREATED'))
);
GO

CREATE INDEX IX_LoiMoiKetBan_NguoiNhan
ON LoiMoiKetBan(NguoiNhanID, TrangThai);
GO

CREATE INDEX IX_BanBe_User1
ON BanBe(UserID1);
GO

CREATE INDEX IX_BanBe_User2
ON BanBe(UserID2);
GO

CREATE INDEX IX_LichHenPhong_User
ON LichHenPhong(NguoiTaoID, NguoiDuocMoiID, ThoiGianHen DESC);
GO

/* ======================== TRIGGER ======================== */
/* ========================================================= */

CREATE TRIGGER TRG_ThongTinUser_NoUpdate_NgayTao
ON ThongTinUser
AFTER UPDATE
AS
BEGIN
    IF EXISTS 
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON i.UserID = d.UserID
        WHERE i.NgayTao <> d.NgayTao
    )
    BEGIN
        RAISERROR (N'Không được phép sửa NgàyTao', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO

CREATE TRIGGER TRG_Users_Update_NgayCapNhat
ON ThongTinUser
AFTER UPDATE
AS
BEGIN
    IF UPDATE(NgayCapNhat)
        RETURN;

    UPDATE t
    SET NgayCapNhat = SYSDATETIME()
    FROM ThongTinUser t
    JOIN inserted i ON t.UserID = i.UserID;
END
GO

CREATE TRIGGER TRG_CreateThongKe
ON ThongTinUser
AFTER INSERT
AS
BEGIN
    INSERT INTO ThongKeNguoiDung (UserID)
    SELECT UserID
    FROM inserted;
END
GO

/* ========================================================= */
/* ======================= DU LIEU MAU ===================== */
/* ========================================================= */

INSERT INTO VaiTro (TenVaiTro)
VALUES (N'ADMIN'), (N'USER');
GO

INSERT INTO CheDoChoi (TenCheDo, LoaiCheDo, ThoiGian)
VALUES
(N'Hai Người Một Máy', N'PVP', 1800),
(N'Chơi Với AI', N'BOT', 1800),
(N'Cờ Câu Đố', N'PUZZLE', 0);
GO

INSERT INTO Bot (TenBot, DoKho, MoTa) 
VALUES
(N'Bot Dễ', 1, N'Phù hợp người mới'),
(N'Bot Trung Bình', 5, N'Cân bằng'),
(N'Bot Khó', 8, N'Thách thức cao'),
(N'Bot Siêu Cấp', 10, N'Gần như không thể thắng');
GO

INSERT INTO LoaiCo (TenLoai)
VALUES (N'Cờ vua');
GO

INSERT INTO CapDoCauDo (TenCapDo, MaCapDo, DiemCong, MoTa)
VALUES
(N'Dễ', N'de', 10, N'Câu đố cơ bản, tìm nước chiếu hết hoặc cứu nguy đơn giản'),
(N'Trung bình', N'trung-binh', 20, N'Câu đố cần tính kỹ hơn'),
(N'Khó', N'kho', 30, N'Câu đố khó, cần nhìn chiến thuật tốt');
GO

INSERT INTO ThongTinUser (TenDangNhap, MatKhau, GioiTinh, NgaySinh, Gmail, SoDienThoai)
VALUES 
(N'admin1', N'123456', N'NAM', '2000-01-01', 'admin1@gmail.com', '+84911111111'),
(N'user1',  N'123456', N'NU',  '2002-05-10', 'user1@gmail.com',  '+84922222222'),
(N'user2',  N'123456', N'NAM', '1999-03-15', 'user2@gmail.com',  '+84933333333'),
(N'user3',  N'123456', N'KHAC','2005-07-20', 'user3@gmail.com',  '+84944444444');
GO

INSERT INTO NguoiDungVaiTro (UserID, RoleID)
SELECT u.UserID, r.RoleID
FROM ThongTinUser u
INNER JOIN VaiTro r ON r.TenVaiTro =
    CASE 
        WHEN u.TenDangNhap = N'admin1' THEN N'ADMIN'
        ELSE N'USER'
    END;
GO

UPDATE tk
SET TongVan = 10, Thang = 6, Thua = 3, Hoa = 1
FROM ThongKeNguoiDung tk
INNER JOIN ThongTinUser u ON tk.UserID = u.UserID
WHERE u.TenDangNhap = N'user1';
GO

UPDATE tk
SET TongVan = 20, Thang = 10, Thua = 5, Hoa = 5
FROM ThongKeNguoiDung tk
INNER JOIN ThongTinUser u ON tk.UserID = u.UserID
WHERE u.TenDangNhap = N'user2';
GO

UPDATE tk
SET TongVan = 5, Thang = 2, Thua = 2, Hoa = 1
FROM ThongKeNguoiDung tk
INNER JOIN ThongTinUser u ON tk.UserID = u.UserID
WHERE u.TenDangNhap = N'user3';
GO


INSERT INTO Phong (ChuPhongID, CheDoID)
SELECT u.UserID, c.CheDoID
FROM ThongTinUser u
CROSS JOIN CheDoChoi c
WHERE u.TenDangNhap = N'user1'
  AND c.TenCheDo = N'Cờ chớp';
GO

UPDATE p
SET KhachID = u.UserID,
    TrangThai = N'PLAYING'
FROM Phong p
CROSS JOIN ThongTinUser u
WHERE p.PhongID = 1
  AND u.TenDangNhap = N'user2';
GO

INSERT INTO VanCo (PhongID, NguoiTrangID, NguoiDenID, LoaiCoID, CheDoID)
SELECT  p.PhongID, chu.UserID, khach.UserID, lc.LoaiCoID, c.CheDoID
FROM Phong p
INNER JOIN ThongTinUser chu ON chu.UserID = p.ChuPhongID
INNER JOIN ThongTinUser khach ON khach.UserID = p.KhachID
INNER JOIN CheDoChoi c ON c.CheDoID = p.CheDoID
CROSS JOIN LoaiCo lc
WHERE p.PhongID = 1
  AND lc.TenLoai = N'Cờ vua';
GO

INSERT INTO VanCo (NguoiTrangID, BotID, LoaiCoID, CheDoID)
SELECT TOP 1 u.UserID, b.BotID, lc.LoaiCoID, c.CheDoID
FROM ThongTinUser u
CROSS JOIN Bot b
CROSS JOIN LoaiCo lc
CROSS JOIN CheDoChoi c
WHERE u.TenDangNhap = N'admin1'
  AND b.TenBot = N'Bot Dễ'
  AND lc.TenLoai = N'Cờ vua'
  AND c.TenCheDo = N'Chơi Với AI';
GO

INSERT INTO VanCo (NguoiTrangID, PuzzleID, LoaiCoID, CheDoID)
SELECT TOP 1
    u.UserID,
    pz.PuzzleID,
    lc.LoaiCoID,
    c.CheDoID
FROM ThongTinUser u
CROSS JOIN Puzzle pz
CROSS JOIN LoaiCo lc
CROSS JOIN CheDoChoi c
WHERE u.TenDangNhap = N'admin1'
  AND lc.TenLoai = N'Cờ vua'
  AND c.TenCheDo = N'Cờ Câu Đố'
ORDER BY pz.PuzzleID;
GO

INSERT INTO NuocDi (VanCoID, SoThuTu, Nuoc)
VALUES (1, 1, N'e4');
GO

INSERT INTO XepHang (UserID, CheDoID, Diem, SoVan, Thang, Thua, Hoa)
SELECT  u.UserID, c.CheDoID, 1200, 0, 0, 0, 0
FROM ThongTinUser u
CROSS JOIN CheDoChoi c;
GO

INSERT INTO LichSuDiem (UserID, CheDoID, DiemCu, DiemMoi, VanCoID)
SELECT TOP 1
    u.UserID,
    c.CheDoID,
    1200,
    1215,
    v.VanCoID
FROM ThongTinUser u
CROSS JOIN CheDoChoi c
CROSS JOIN VanCo v
WHERE u.TenDangNhap = N'admin1'
  AND c.TenCheDo = N'Cờ chớp'
ORDER BY v.VanCoID;
GO

INSERT INTO GiaoDien (Ten, MauBan, KieuQuan) 
VALUES
(N'Classic', N'Classic', N'Standard'),
(N'Dark Mode', N'Dark', N'Modern'),
(N'Blue Ocean', N'Blue', N'Fantasy');
GO

INSERT INTO QuanCo (Ten, KyHieu, GiaTri)
VALUES
(N'Vua', N'K', 100),
(N'Hậu', N'Q', 9),
(N'Xe', N'R', 5),
(N'Tượng', N'B', 3),
(N'Mã', N'N', 3),
(N'Tốt', N'P', 1);
GO

INSERT INTO LuatDiChuyen (QuanCoID, MoTa)
SELECT QuanCoID, N'Đi 1 ô theo mọi hướng'
FROM QuanCo
WHERE Ten = N'Vua';
GO

INSERT INTO LuatDiChuyen (QuanCoID, MoTa)
SELECT QuanCoID, N'Nhập thành nếu chưa di chuyển'
FROM QuanCo
WHERE Ten = N'Vua';
GO

INSERT INTO LuatDiChuyen (QuanCoID, MoTa)
SELECT QuanCoID, N'Đi thẳng 1 ô, ăn chéo'
FROM QuanCo
WHERE Ten = N'Tốt';
GO

INSERT INTO LuatDiChuyen (QuanCoID, MoTa)
SELECT QuanCoID, N'Đi 2 ô ở nước đầu'
FROM QuanCo
WHERE Ten = N'Tốt';
GO

INSERT INTO LuatDiChuyen (QuanCoID, MoTa)
SELECT QuanCoID, N'Bắt tốt qua đường'
FROM QuanCo
WHERE Ten = N'Tốt';
GO

DECLARE @LoaiCoVuaID INT;

SELECT TOP 1 @LoaiCoVuaID = LoaiCoID
FROM LoaiCo
WHERE TenLoai IN (N'Cờ vua', N'Chess')
ORDER BY LoaiCoID;

INSERT INTO SkinBanCo( LoaiCoID, TenSkin, MaSkin,  MauOTrang, MauODen, AnhNenBanCo, AnhOSang, AnhODen, MoTa)
VALUES
( @LoaiCoVuaID, N'Bàn cờ mặc định', 'board-default', '#f0d9b5', '#b58863', '/IMG/BanCo/BanCo.png', NULL, NULL, N'Skin bàn cờ mặc định dùng màu dự phòng hoặc ảnh từng ô');
GO

DECLARE @LoaiCoVuaID2 INT;

SELECT TOP 1 @LoaiCoVuaID2 = LoaiCoID
FROM LoaiCo
WHERE TenLoai IN (N'Cờ vua', N'Chess')
ORDER BY LoaiCoID;

INSERT INTO SkinQuanCo( LoaiCoID, TenSkin, MaSkin, KieuHienThi, DuongDanThuMuc, CssClass, MoTa)
VALUES
( @LoaiCoVuaID2, N'Quân cờ PNG mặc định', 'png-default', N'IMAGE', '/IMG/QuanCo/', 'piece-png-default', N'Bộ quân cờ PNG đang có trong project');
GO



/* ========================= */
/* CÂU ĐỐ CẤP ĐỘ DỄ */
/* ========================= */

INSERT INTO Puzzle( FEN, LoiGiai, DoKho, MoTa, CapDoID, TieuDe, LoaiCauDo, DiemThuong, TrangThai)
VALUES
( '6k1/5ppp/8/8/8/8/8/6RQ w - - 0 1', 'g1g8', 1, N'Xe trắng đi lên g8 để chiếu hết vua đen.',
(SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'de'),
 N'Chiếu hết bằng xe', N'MATE', 10, 1),
( '7k/6pp/8/8/8/8/8/6RQ w - - 0 1', 'g1g8', 1, N'Xe trắng chiếu hết trên cột g.',
(SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'de'),
 N'Xe lên hàng 8', N'MATE', 10, 1),
( '6k1/6pp/8/8/8/8/8/5Q1K w - - 0 1', 'f1f8', 1, N'Hậu trắng đi lên f8 để chiếu hết vua đen.',(SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'de'),
 N'Chiếu hết bằng hậu', N'MATE', 10,  1);
GO


/* ========================= */
/* CÂU ĐỐ CẤP ĐỘ TRUNG BÌNH */
/* ========================= */

INSERT INTO Puzzle( FEN, LoiGiai, DoKho, MoTa, CapDoID, TieuDe, LoaiCauDo, DiemThuong, TrangThai)
VALUES
(
    '6k1/6pp/8/8/8/8/5Q2/6K1 w - - 0 1',
    'f2f8',
    4,
    N'Hậu trắng lên f8 tạo thế chiếu hết.',
    (SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'trung-binh'),
    N'Hậu khóa vua',
    N'MATE',
    20,
    1
),
(
    '7k/6pp/8/8/8/8/6R1/6K1 w - - 0 1',
    'g2g8',
    4,
    N'Xe trắng đi lên g8 để chiếu hết.',
    (SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'trung-binh'),
    N'Xe chiếu dọc',
    N'MATE',
    20,
    1
);
GO


/* ========================= */
/* CÂU ĐỐ CẤP ĐỘ KHÓ */
/* ========================= */

INSERT INTO Puzzle( FEN, LoiGiai, DoKho, MoTa, CapDoID, TieuDe, LoaiCauDo, DiemThuong, TrangThai)
VALUES
(
    '6k1/6pp/8/8/8/8/5R2/6KQ w - - 0 1',
    'h1a8',
    7,
    N'Hậu trắng đi đường chéo lên a8 để chiếu hết.',
    (SELECT CapDoID FROM CapDoCauDo WHERE MaCapDo = N'kho'),
    N'Hậu chiếu đường chéo',
    N'MATE',
    30,
    1
);
GO


/* KIỂM TRA LẠI DỮ LIỆU */
SELECT 
    p.PuzzleID,
    c.TenCapDo,
    p.TieuDe,
    p.FEN,
    p.LoiGiai,
    p.LoaiCauDo,
    p.DiemThuong,
    p.TrangThai
FROM Puzzle p
INNER JOIN CapDoCauDo c ON p.CapDoID = c.CapDoID
ORDER BY c.CapDoID, p.PuzzleID;
GO

DECLARE @SkinPngDefaultID INT;

SELECT TOP 1 @SkinPngDefaultID = SkinQuanCoID
FROM SkinQuanCo
WHERE MaSkin = 'png-default';

INSERT INTO ChiTietSkinQuanCo
(
    SkinQuanCoID,
    MaQuan,
    KyTuUnicode,
    FileAnh
)
VALUES
(@SkinPngDefaultID, 'wK', NULL, 'wK.png'),
(@SkinPngDefaultID, 'wQ', NULL, 'wQ.png'),
(@SkinPngDefaultID, 'wR', NULL, 'wR.png'),
(@SkinPngDefaultID, 'wB', NULL, 'wB.png'),
(@SkinPngDefaultID, 'wN', NULL, 'wN.png'),
(@SkinPngDefaultID, 'wP', NULL, 'wP.png'),
(@SkinPngDefaultID, 'bK', NULL, 'bK.png'),
(@SkinPngDefaultID, 'bQ', NULL, 'bQ.png'),
(@SkinPngDefaultID, 'bR', NULL, 'bR.png'),
(@SkinPngDefaultID, 'bB', NULL, 'bB.png'),
(@SkinPngDefaultID, 'bN', NULL, 'bN.png'),
(@SkinPngDefaultID, 'bP', NULL, 'bP.png');
GO

/*INSERT INTO ThongTinUser(TenDangNhap,Avatar,MatKhau,HoTen,NgaySinh,GioiTinh,Gmail,SoDienThoai,TrangThai,NgayTao,NgayCapNhat)*/
INSERT INTO ThongTinUser ( TenDangNhap,  MatKhau,  GioiTinh,  NgaySinh,  Gmail,  SoDienThoai)
VALUES
('user001', '123456', 'NAM',  '2004-01-15', 'user001@gmail.com', '+84901000001'),
('user002', '123456', 'NU',   '2003-03-22', 'user002@gmail.com', '+84901000002'),
('user003', '123456', 'NAM',  '2002-07-10', 'user003@gmail.com', '+84901000003'),
('user004', '123456', 'NU',   '2005-11-05', 'user004@gmail.com', '+84901000004'),
('user005', '123456', 'KHAC', '2001-09-18', 'user005@gmail.com', '+84901000005'),

('user006', '123456', 'NAM',  '2004-12-25', 'user006@gmail.com', '+84901000006'),
('user007', '123456', 'NU',   '2003-06-30', 'user007@gmail.com', '+84901000007'),
('user008', '123456', 'NAM',  '2002-04-14', 'user008@gmail.com', '+84901000008'),
('user009', '123456', 'NU',   '2005-08-09', 'user009@gmail.com', '+84901000009'),
('user010', '123456', 'KHAC', '2001-02-20', 'user010@gmail.com', '+84901000010'),

('user011', '123456', 'NAM',  '2004-05-11', 'user011@gmail.com', '+84901000011'),
('user012', '123456', 'NU',   '2003-10-03', 'user012@gmail.com', '+84901000012'),
('user013', '123456', 'NAM',  '2002-01-27', 'user013@gmail.com', '+84901000013'),
('user014', '123456', 'NU',   '2005-03-19', 'user014@gmail.com', '+84901000014'),
('user015', '123456', 'KHAC', '2001-07-07', 'user015@gmail.com', '+84901000015'),

('user016', '123456', 'NAM',  '2004-09-29', 'user016@gmail.com', '+84901000016'),
('user017', '123456', 'NU',   '2003-12-12', 'user017@gmail.com', '+84901000017'),
('user018', '123456', 'NAM',  '2002-06-06', 'user018@gmail.com', '+84901000018'),
('user019', '123456', 'NU',   '2005-04-23', 'user019@gmail.com', '+84901000019'),
('user020', '123456', 'KHAC', '2001-11-16', 'user020@gmail.com', '+84901000020');
GO

/* ========================================================= */
/* ========== GAN ROLE USER CHO TAI KHOAN CHUA CO =========== */
/* ========================================================= */

INSERT INTO NguoiDungVaiTro(UserID, RoleID)
SELECT u.UserID, v.RoleID
FROM ThongTinUser u
CROSS JOIN VaiTro v
WHERE v.TenVaiTro = N'USER'
  AND NOT EXISTS
  (
      SELECT 1
      FROM NguoiDungVaiTro n
      WHERE n.UserID = u.UserID
        AND n.RoleID = v.RoleID
  );
GO

/* ========================================================= */
/* ========== TAO XEP HANG MAC DINH CHO USER MOI ============ */
/* ========================================================= */

INSERT INTO XepHang(UserID, CheDoID, Diem, SoVan, Thang, Thua, Hoa)
SELECT 
    u.UserID,
    c.CheDoID,
    1200,
    0,
    0,
    0,
    0
FROM ThongTinUser u
CROSS JOIN CheDoChoi c
WHERE NOT EXISTS
(
    SELECT 1
    FROM XepHang x
    WHERE x.UserID = u.UserID
      AND x.CheDoID = c.CheDoID
);
GO

/* ========================================================= */
/* =============== DU LIEU MAU KET BAN ====================== */
/* ========================================================= */

INSERT INTO BanBe(UserID1, UserID2)
SELECT 1, 2
WHERE EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 1)
  AND EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 2)
  AND NOT EXISTS
  (
      SELECT 1 FROM BanBe
      WHERE UserID1 = 1 AND UserID2 = 2
  );
GO

INSERT INTO BanBe(UserID1, UserID2)
SELECT 1, 3
WHERE EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 1)
  AND EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 3)
  AND NOT EXISTS
  (
      SELECT 1 FROM BanBe
      WHERE UserID1 = 1 AND UserID2 = 3
  );
GO

INSERT INTO LoiMoiKetBan(NguoiGuiID, NguoiNhanID, TrangThai)
SELECT 4, 1, N'PENDING'
WHERE EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 4)
  AND EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 1)
  AND NOT EXISTS
  (
      SELECT 1 FROM LoiMoiKetBan
      WHERE NguoiGuiID = 4
        AND NguoiNhanID = 1
        AND TrangThai = N'PENDING'
  );
GO

/* ========================================================= */
/* =============== DU LIEU MAU HEN PHONG ==================== */
/* ========================================================= */

INSERT INTO LichHenPhong( NguoiTaoID, NguoiDuocMoiID, CheDoID, ThoiGianHen, GhiChu, TrangThai)
SELECT TOP 1
    1,
    2,
    c.CheDoID,
    DATEADD(HOUR, 2, SYSDATETIME()),
    N'Hẹn chơi sau 2 giờ',
    N'PENDING'
FROM CheDoChoi c
WHERE EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 1)
  AND EXISTS (SELECT 1 FROM ThongTinUser WHERE UserID = 2)
  AND NOT EXISTS
  (
      SELECT 1
      FROM LichHenPhong
      WHERE NguoiTaoID = 1
        AND NguoiDuocMoiID = 2
        AND TrangThai = N'PENDING'
  );
GO


DECLARE @LoaiCoVuaID INT;

SELECT TOP 1 @LoaiCoVuaID = LoaiCoID
FROM LoaiCo
WHERE TenLoai IN (N'Cờ vua', N'Chess')
ORDER BY LoaiCoID;

--------------------------------------------------
-- 1. INSERT SKIN BÀN CỜ
--------------------------------------------------

INSERT INTO SkinBanCo
(
    LoaiCoID,
    TenSkin,
    MaSkin,
    MauOTrang,
    MauODen,
    AnhNenBanCo,
    AnhOSang,
    AnhODen,
    MoTa
)
VALUES
(@LoaiCoVuaID, N'Bàn cờ xanh cổ điển', 'board-banco', '#eeeed2', '#769656', N'/IMG/BanCo/BanCo.png', NULL, NULL, N'Ảnh bàn cờ BanCo'),
(@LoaiCoVuaID, N'Bàn cờ xanh vàng', 'board-banco1', '#d9c074', '#5b6b35', N'/IMG/BanCo/BanCo1.png', NULL, NULL, N'Ảnh bàn cờ BanCo1'),
(@LoaiCoVuaID, N'Bàn cờ trắng đen đá', 'board-banco2', '#d8d8d8', '#303030', N'/IMG/BanCo/BanCo2.png', NULL, NULL, N'Ảnh bàn cờ BanCo2'),
(@LoaiCoVuaID, N'Bàn cờ xanh dương', 'board-banco3', '#dbeafe', '#1d4ed8', N'/IMG/BanCo/BanCo3.png', NULL, NULL, N'Ảnh bàn cờ BanCo3'),
(@LoaiCoVuaID, N'Bàn cờ rêu cỏ', 'board-banco4', '#c6b98a', '#557238', N'/IMG/BanCo/BanCo4.png', NULL, NULL, N'Ảnh bàn cờ BanCo4'),
(@LoaiCoVuaID, N'Bàn cờ tối kem', 'board-banco5', '#f1d9b5', '#111827', N'/IMG/BanCo/BanCo5.png', NULL, NULL, N'Ảnh bàn cờ BanCo5'),
(@LoaiCoVuaID, N'Bàn cờ trắng đen cổ điển', 'board-banco6', '#ffffff', '#000000', N'/IMG/BanCo/BanCo6.png', NULL, NULL, N'Ảnh bàn cờ BanCo6'),

(@LoaiCoVuaID, N'Ô đá trắng đen 2', 'tile-stone-2', '#f5efe5', '#202020', NULL, N'/IMG/BanCo/o_trang2.png', N'/IMG/BanCo/o_den2.png', N'Cặp ô đá trắng đen'),
(@LoaiCoVuaID, N'Ô xanh đậm nhạt 3', 'tile-blue-3', '#dbeafe', '#0f3f7a', NULL, N'/IMG/BanCo/o_xanh_nhat3.png', N'/IMG/BanCo/o_xanh_dam3.png', N'Cặp ô xanh đậm nhạt'),
(@LoaiCoVuaID, N'Ô nâu cổ điển 1', 'tile-brown-1', '#c8a56a', '#6b4f2a', NULL, N'/IMG/BanCo/OBanCo1.png', N'/IMG/BanCo/OBanCo1a.png', N'Cặp ô nâu cổ điển'),
(@LoaiCoVuaID, N'Ô cỏ đá 4', 'tile-grass-stone-4', '#bfa77a', '#3f7f1f', NULL, N'/IMG/BanCo/OCatBanCo4.png', N'/IMG/BanCo/OCoBanCo4.png', N'Cặp ô cát và cỏ'),
(@LoaiCoVuaID, N'Ô trắng đen 6', 'tile-white-black-6', '#ffffff', '#0b0b0b', NULL, N'/IMG/BanCo/OTrangBanCo6.png', N'/IMG/BanCo/ODenBanCo6.png', N'Cặp ô trắng đen'),
(@LoaiCoVuaID, N'Ô vàng đen 5', 'tile-gold-dark-5', '#e8c980', '#111827', NULL, N'/IMG/BanCo/OVangBanCo5.png', N'/IMG/BanCo/ODenBanCo5.png', N'Cặp ô vàng đen');
GO

--------------------------------------------------
-- 2. INSERT BỘ QUÂN CỜ
--------------------------------------------------

DECLARE @LoaiCoVuaID2 INT;

SELECT TOP 1 @LoaiCoVuaID2 = LoaiCoID
FROM LoaiCo
WHERE TenLoai IN (N'Cờ vua', N'Chess')
ORDER BY LoaiCoID;

INSERT INTO SkinQuanCo
(
    LoaiCoID,
    TenSkin,
    MaSkin,
    KieuHienThi,
    DuongDanThuMuc,
    CssClass,
    MoTa
)
VALUES
(@LoaiCoVuaID2, N'Quân cờ PNG 2', 'piece-png-2', N'IMAGE', N'/IMG/QuanCo/', 'piece-png-2', N'Bộ quân cờ đen trắng ảnh thật số 2'),
(@LoaiCoVuaID2, N'Quân cờ viền 1', 'piece-outline-1', N'IMAGE', N'/IMG/QuanCo/', 'piece-outline-1', N'Bộ quân cờ nét đen trắng số 1'),
(@LoaiCoVuaID2, N'Quân cờ xanh rêu', 'piece-green', N'IMAGE', N'/IMG/QuanCo/', 'piece-green', N'Bộ quân cờ xanh rêu và vàng'),
(@LoaiCoVuaID2, N'Quân cờ vàng đen', 'piece-gold-black', N'IMAGE', N'/IMG/QuanCo/', 'piece-gold-black', N'Bộ quân cờ vàng đen đơn giản');
GO

--------------------------------------------------
-- 3. INSERT CHI TIẾT QUÂN CỜ PNG 2
--------------------------------------------------

DECLARE @SkinPng2ID INT;

SELECT @SkinPng2ID = SkinQuanCoID
FROM SkinQuanCo
WHERE MaSkin = 'piece-png-2';

INSERT INTO ChiTietSkinQuanCo
(
    SkinQuanCoID,
    MaQuan,
    KyTuUnicode,
    FileAnh
)
VALUES
(@SkinPng2ID, N'wK', NULL, N'vua_trang2.png'),
(@SkinPng2ID, N'wQ', NULL, N'hau_trang2.png'),
(@SkinPng2ID, N'wR', NULL, N'xe_trang2.png'),
(@SkinPng2ID, N'wB', NULL, N'tuong_trang2.png'),
(@SkinPng2ID, N'wN', NULL, N'ma_trang2.png'),
(@SkinPng2ID, N'wP', NULL, N'tot_trang2.png'),

(@SkinPng2ID, N'bK', NULL, N'vua_den2.png'),
(@SkinPng2ID, N'bQ', NULL, N'hau_den2.png'),
(@SkinPng2ID, N'bR', NULL, N'xe_den2.png'),
(@SkinPng2ID, N'bB', NULL, N'tuong_den2.png'),
(@SkinPng2ID, N'bN', NULL, N'ma_den2.png'),
(@SkinPng2ID, N'bP', NULL, N'tot_den2.png');
GO

--------------------------------------------------
-- 4. INSERT CHI TIẾT QUÂN CỜ VIỀN 1
--------------------------------------------------

DECLARE @SkinOutline1ID INT;

SELECT @SkinOutline1ID = SkinQuanCoID
FROM SkinQuanCo
WHERE MaSkin = 'piece-outline-1';

INSERT INTO ChiTietSkinQuanCo
(
    SkinQuanCoID,
    MaQuan,
    KyTuUnicode,
    FileAnh
)
VALUES
(@SkinOutline1ID, N'wK', NULL, N'VuaTrang1.png'),
(@SkinOutline1ID, N'wQ', NULL, N'HauTrang1.png'),
(@SkinOutline1ID, N'wR', NULL, N'XeTrang1.png'),
(@SkinOutline1ID, N'wB', NULL, N'TuongTrang1.png'),
(@SkinOutline1ID, N'wN', NULL, N'NguaTrang1.png'),
(@SkinOutline1ID, N'wP', NULL, N'TotTrang1.png'),

(@SkinOutline1ID, N'bK', NULL, N'VuaDen1.png'),
(@SkinOutline1ID, N'bQ', NULL, N'HauDen1.png'),
(@SkinOutline1ID, N'bR', NULL, N'XeDen1.png'),
(@SkinOutline1ID, N'bB', NULL, N'TuongDen1.png'),
(@SkinOutline1ID, N'bN', NULL, N'NguaDen1.png'),
(@SkinOutline1ID, N'bP', NULL, N'TotDen1.png');
GO

--------------------------------------------------
-- 5. INSERT CHI TIẾT QUÂN CỜ XANH RÊU
--------------------------------------------------

DECLARE @SkinGreenID INT;

SELECT @SkinGreenID = SkinQuanCoID
FROM SkinQuanCo
WHERE MaSkin = 'piece-green';

INSERT INTO ChiTietSkinQuanCo
(
    SkinQuanCoID,
    MaQuan,
    KyTuUnicode,
    FileAnh
)
VALUES
(@SkinGreenID, N'wK', NULL, N'wK.png'),
(@SkinGreenID, N'wQ', NULL, N'wQ.png'),
(@SkinGreenID, N'wR', NULL, N'wR.png'),
(@SkinGreenID, N'wB', NULL, N'wB.png'),
(@SkinGreenID, N'wN', NULL, N'wN.png'),
(@SkinGreenID, N'wP', NULL, N'wP.png'),

(@SkinGreenID, N'bK', NULL, N'bK.png'),
(@SkinGreenID, N'bQ', NULL, N'bQ.png'),
(@SkinGreenID, N'bR', NULL, N'bR.png'),
(@SkinGreenID, N'bB', NULL, N'bB.png'),
(@SkinGreenID, N'bN', NULL, N'bN.png'),
(@SkinGreenID, N'bP', NULL, N'bP.png');
GO

--------------------------------------------------
-- 6. INSERT CHI TIẾT QUÂN CỜ VÀNG ĐEN
--------------------------------------------------

DECLARE @SkinGoldBlackID INT;

SELECT @SkinGoldBlackID = SkinQuanCoID
FROM SkinQuanCo
WHERE MaSkin = 'piece-gold-black';

INSERT INTO ChiTietSkinQuanCo
(
    SkinQuanCoID,
    MaQuan,
    KyTuUnicode,
    FileAnh
)
VALUES
(@SkinGoldBlackID, N'wK', NULL, N'vua_trang.png'),
(@SkinGoldBlackID, N'wQ', NULL, N'hau_trang.png'),
(@SkinGoldBlackID, N'wR', NULL, N'xe_trang.png'),
(@SkinGoldBlackID, N'wB', NULL, N'tuong_trang.png'),
(@SkinGoldBlackID, N'wN', NULL, N'ma_trang.png'),
(@SkinGoldBlackID, N'wP', NULL, N'tot_trang.png'),

(@SkinGoldBlackID, N'bK', NULL, N'vua_den.png'),
(@SkinGoldBlackID, N'bQ', NULL, N'hau_den.png'),
(@SkinGoldBlackID, N'bR', NULL, N'xe_den.png'),
(@SkinGoldBlackID, N'bB', NULL, N'tuong_den.png'),
(@SkinGoldBlackID, N'bN', NULL, N'ma_den.png'),
(@SkinGoldBlackID, N'bP', NULL, N'tot_den.png');
GO



/* ========================================================= */
/* ========================= SELECT ======================== */
/* ========================================================= */

SELECT * FROM VaiTro;
SELECT * FROM ThongTinUser;
SELECT * FROM NguoiDungVaiTro;
SELECT * FROM ThongKeNguoiDung;
SELECT * FROM CheDoChoi;
SELECT * FROM Bot;
SELECT * FROM LoaiCo;
SELECT * FROM CapDoCauDo;
SELECT * FROM Puzzle;
SELECT * FROM DiemCauDoNguoiDung;
SELECT * FROM LichSuLamCauDo;
SELECT * FROM Phong;
SELECT * FROM VanCo;
SELECT * FROM NuocDi ORDER BY SoThuTu;
SELECT * FROM XepHang;
SELECT * FROM LichSuDiem;
SELECT * FROM GiaoDien;
SELECT * FROM QuanCo;
SELECT * FROM LuatDiChuyen;
SELECT * FROM GiaiDau;
SELECT * FROM NguoiChoiGiaiDau;
SELECT * FROM BangDau;
SELECT * FROM TranDauGiaiDau;
SELECT * FROM SkinBanCo;
SELECT * FROM SkinQuanCo;
SELECT * FROM ChiTietSkinQuanCo;
SELECT * FROM CaiDatSkinNguoiDung;
SELECT * FROM LoiMoiKetBan;
SELECT * FROM BanBe;
SELECT * FROM LichHenPhong;

SELECT COUNT(*) AS TongUser FROM ThongTinUser;
SELECT COUNT(*) AS TongRoleUser FROM NguoiDungVaiTro;
SELECT COUNT(*) AS TongXepHang FROM XepHang;

SELECT *
FROM DiemCauDoNguoiDung
ORDER BY NgayCapNhat DESC;

SELECT *
FROM XepHang
ORDER BY Diem DESC;

SELECT *
FROM LichSuDiem
ORDER BY ThoiGian DESC;


SELECT 
    x.UserID,
    u.TenDangNhap,
    c.TenCheDo,
    c.LoaiCheDo,
    x.Diem,
    x.SoVan,
    x.Thang,
    x.Thua,
    x.Hoa
FROM XepHang x
INNER JOIN ThongTinUser u ON x.UserID = u.UserID
INNER JOIN CheDoChoi c ON x.CheDoID = c.CheDoID
WHERE c.LoaiCheDo = N'PUZZLE'
ORDER BY x.Diem DESC;

SELECT 
    UserID,
    RoleID,
    COUNT(*) AS SoLanTrung
FROM NguoiDungVaiTro
GROUP BY UserID, RoleID
HAVING COUNT(*) > 1;

SELECT * FROM SkinBanCo;

SELECT * FROM SkinQuanCo;

SELECT 
    sq.TenSkin,
    sq.MaSkin,
    ct.MaQuan,
    ct.FileAnh
FROM SkinQuanCo sq
JOIN ChiTietSkinQuanCo ct 
    ON sq.SkinQuanCoID = ct.SkinQuanCoID
ORDER BY sq.SkinQuanCoID, ct.MaQuan;
GO

SELECT name, definition
FROM sys.check_constraints
WHERE name = 'CK_CheDoChoi_ThoiGian';

ALTER TABLE dbo.CheDoChoi
DROP CONSTRAINT CK_CheDoChoi_ThoiGian;
GO

ALTER TABLE dbo.CheDoChoi
ADD CONSTRAINT CK_CheDoChoi_ThoiGian
CHECK (ThoiGian IN (0, 60, 180, 300, 600, 900, 1800, 3600));
GO