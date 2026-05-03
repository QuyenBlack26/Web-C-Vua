USE master
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'DuLieuCoVua') 
BEGIN
    ALTER DATABASE DuLieuCoVua SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DuLieuCoVua;
END
GO

CREATE DATABASE DuLieuCoVua;
GO

USE DuLieuCoVua;
GO


----------------------------------------
--==========ThongTinNguoiDung==========--
----------------------------------------
CREATE TABLE VaiTro
(


    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(20) NOT NULL
        CHECK (TenVaiTro IN (N'ADMIN', N'USER'))
)



----------------------------------------
--==========ThongTinTaiKhoan==========--
----------------------------------------
CREATE TABLE ThongTinUser
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    Avatar NVARCHAR(255) NOT NULL 
        DEFAULT '/images/default-avatar.png',
    MatKhau NVARCHAR(100) NOT NULL, 
        CHECK (LEN(MatKhau) >= 6),
    HoTen NVARCHAR(100),
    NgaySinh DATE NOT NULL,
        CONSTRAINT CK_Users_NgaySinh CHECK 
        (            
                NgaySinh <= CAST(GETDATE() AS DATE)
                -- độ tuổi được chơi cờ vua 
                AND DATEADD(YEAR, 6, NgaySinh) <= CAST(GETDATE() AS DATE)
                AND DATEADD(YEAR, 100, NgaySinh) >= CAST(GETDATE() AS DATE)
        ),
    GioiTinh NVARCHAR(10) NOT NULL,
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

    SoDienThoai VARCHAR(16) UNIQUE,
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

    NgayTao DATETIME2(0) NOT NULL 
        CONSTRAINT DF_Users_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL 
        CONSTRAINT DF_Users_NgayCapNhat DEFAULT SYSDATETIME(),       
    
);



---------------------------------------
--==========NguoiDungVaiTro==========--
---------------------------------------
CREATE TABLE NguoiDungVaiTro
(
    UserID INT,
    RoleID INT,

    PRIMARY KEY (UserID, RoleID),

    FOREIGN KEY (UserID) REFERENCES ThongTinUser(UserID)
        ON DELETE CASCADE,

    FOREIGN KEY (RoleID) REFERENCES VaiTro(RoleID)
        ON DELETE CASCADE
)
CREATE INDEX IX_NguoiDungVaiTro_UserID 
ON NguoiDungVaiTro(UserID);

CREATE INDEX IX_NguoiDungVaiTro_RoleID 
ON NguoiDungVaiTro(RoleID);



----------------------------------------
--==========ThongKeNguoiDung==========--
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



---------------------------------
--==========CheDoChoi==========--
---------------------------------
CREATE TABLE CheDoChoi 
(
    CheDoID INT IDENTITY(1,1) PRIMARY KEY,

    TenCheDo NVARCHAR(50) NOT NULL,

    CONSTRAINT UQ_CheDoChoi_Ten UNIQUE (TenCheDo),
    LoaiCheDo NVARCHAR(20) NOT NULL,
        CHECK (LoaiCheDo IN (N'PVP', N'BOT', N'PUZZLE')),

    CONSTRAINT CK_CheDoChoi_Ten CHECK 
    (
        LEN(TenCheDo) BETWEEN 3 AND 50
        AND TenCheDo = LTRIM(RTRIM(TenCheDo))
        AND TenCheDo NOT LIKE '%  %'
        AND TenCheDo NOT LIKE '%[^A-Za-zÀ-ỹà-ỹ ]%'
    ),

    
    
    ThoiGian INT NOT NULL,
    CONSTRAINT CK_CheDoChoi_ThoiGian 
        CHECK ( ThoiGian IN (60, 180, 300, 600, 900, 1800, 3600) ), 
    
);



-------------------------------
--==========Bot(AI)==========--
-------------------------------
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



--------------------------------
---==========loaiCo==========---
--------------------------------
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



---------------------------------
---==========CoCauDo==========---
---------------------------------
CREATE TABLE Puzzle 
(
    PuzzleID INT IDENTITY(1,1) PRIMARY KEY,

    FEN NVARCHAR(100) NOT NULL,        
    LoiGiai NVARCHAR(500) NOT NULL,    
    DoKho TINYINT NOT NULL,            
    MoTa NVARCHAR(255) NULL,

    -- FEN không trùng
    CONSTRAINT UQ_Puzzle_FEN UNIQUE (FEN),

    -- CHECK FEN 
    CONSTRAINT CK_Puzzle_FEN CHECK 
    (
        LEN(FEN) BETWEEN 15 AND 100
        AND FEN = LTRIM(RTRIM(FEN))
    ),

    -- CHECK lời giải
    CONSTRAINT CK_Puzzle_LoiGiai CHECK 
    (
        LEN(LoiGiai) >= 3
        AND LoiGiai = LTRIM(RTRIM(LoiGiai))
        AND LoiGiai NOT LIKE '%  %'
    ),

    -- CHECK độ khó
    CONSTRAINT CK_Puzzle_DoKho CHECK 
    (
        DoKho BETWEEN 1 AND 10
    ),

    -- CHECK mô tả
    CONSTRAINT CK_Puzzle_MoTa CHECK 
    (
        MoTa IS NULL OR 
        (
            LEN(MoTa) <= 255
            AND MoTa = LTRIM(RTRIM(MoTa))
        )
    )
);



----------------------------------------
---==========CheDoPhongChoi==========---
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
CREATE UNIQUE INDEX UQ_Phong_ChuPhong_Waiting
ON Phong (ChuPhongID)
WHERE TrangThai = N'WAITING';

CREATE UNIQUE INDEX UQ_Phong_Khach_Playing
ON Phong (KhachID)
WHERE TrangThai = N'PLAYING';



-------------------------------
---==========VanCo==========---
-------------------------------
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

    LuotDi NVARCHAR(5) CHECK (LuotDi IN ('WHITE','BLACK')),

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


    -- Trạng thái
    CONSTRAINT CK_VanCo_TrangThai CHECK 
    (
        TrangThai IN (N'PLAYING', N'END')
    ),

    -- Kết quả
    CONSTRAINT CK_VanCo_KetQua CHECK 
    (
        KetQua IS NULL OR KetQua IN (N'WHITE_WIN', N'BLACK_WIN', N'DRAW')
    ),

    -- Không cho 1 người chơi cả 2 bên
    CONSTRAINT CK_VanCo_KhongTrung CHECK 
    (
        NguoiTrangID IS NULL OR 
        NguoiDenID IS NULL OR 
        NguoiTrangID <> NguoiDenID
    ),

    -- Logic chế độ chơi (chỉ 1 trong các kiểu)
    CONSTRAINT CK_VanCo_Mode CHECK 
    (
        -- PvP
        (NguoiTrangID IS NOT NULL AND NguoiDenID IS NOT NULL AND BotID IS NULL AND PuzzleID IS NULL)

        OR

        -- PvE (đánh với bot)
        (NguoiTrangID IS NOT NULL AND BotID IS NOT NULL AND NguoiDenID IS NULL AND PuzzleID IS NULL)

        OR

        -- Puzzle
        (PuzzleID IS NOT NULL AND NguoiTrangID IS NOT NULL AND NguoiDenID IS NULL AND BotID IS NULL)
    ),

    -- Thời gian hợp lệ
    CONSTRAINT CK_VanCo_Time CHECK 
    (
        ThoiGianKetThuc IS NULL OR ThoiGianKetThuc >= ThoiGianBatDau
    ),

    CONSTRAINT CK_VanCo_PhongLogic
    CHECK (
        -- PvP phải có phòng
        (PhongID IS NOT NULL AND NguoiTrangID IS NOT NULL AND NguoiDenID IS NOT NULL AND BotID IS NULL AND PuzzleID IS NULL)

        OR

        -- Bot không có phòng
        (PhongID IS NULL AND BotID IS NOT NULL AND NguoiTrangID IS NOT NULL AND NguoiDenID IS NULL AND PuzzleID IS NULL)

        OR

        -- Puzzle không có phòng
        (PhongID IS NULL AND PuzzleID IS NOT NULL AND NguoiTrangID IS NOT NULL AND NguoiDenID IS NULL AND BotID IS NULL)
    )
);
CREATE INDEX IX_VanCo_NguoiTrang ON VanCo(NguoiTrangID);
CREATE INDEX IX_VanCo_NguoiDen ON VanCo(NguoiDenID);
CREATE INDEX IX_VanCo_Phong ON VanCo(PhongID);
CREATE INDEX IX_VanCo_User ON VanCo (NguoiTrangID, NguoiDenID);
CREATE INDEX IX_VanCo_TrangThai ON VanCo (TrangThai);



--------------------------------
---=========NuocDi===========---
--------------------------------
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



---------------------------------
---==========XepHang==========---
---------------------------------
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



------------------------------------
---==========LichSuDiem==========---
------------------------------------
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
CREATE INDEX IX_LichSuDiem_User ON LichSuDiem (UserID, ThoiGian DESC);



----------------------------------
---==========GiaoDien==========---
----------------------------------
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



---------------------------------
---==========QuanCo ==========---
---------------------------------
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



--------------------------------------
---==========LuatDiChuyen==========---
--------------------------------------
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



---------------------------------------
---==========TrigerNgayTao==========---
---------------------------------------
Go
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



-----------------------------------------
--==========TrigerNgayCapNhat==========--
-----------------------------------------
GO
CREATE TRIGGER TRG_Users_Update_NgayCapNhat
ON ThongTinUser
AFTER UPDATE
AS
BEGIN
    -- tránh loop vô hạn
    IF UPDATE(NgayCapNhat)
        RETURN;

    UPDATE t
    SET NgayCapNhat = SYSDATETIME()
    FROM ThongTinUser t
    JOIN inserted i ON t.UserID = i.UserID
END
GO



----------------------------------------
--=======TrigerThongKeNguoiDung=======--
----------------------------------------
CREATE TRIGGER TRG_CreateThongKe
ON ThongTinUser
AFTER INSERT
AS
BEGIN
    INSERT INTO ThongKeNguoiDung (UserID)
    SELECT UserID FROM inserted;
END
GO



-- thêm user

INSERT INTO ThongTinUser (TenDangNhap, MatKhau, GioiTinh, NgaySinh, Gmail, SoDienThoai)
VALUES 
(N'admin1', N'123456', N'NAM', '2000-01-01', 'admin1@gmail.com', '+84911111111'),
(N'user1',  N'123456', N'NU',  '2002-05-10', 'user1@gmail.com',  '+84922222222'),
(N'user2',  N'123456', N'NAM', '1999-03-15', 'user2@gmail.com',  '+84933333333'),
(N'user3',  N'123456', N'KHAC','2005-07-20', 'user3@gmail.com',  '+84944444444');

INSERT INTO VaiTro (TenVaiTro)
VALUES (N'ADMIN'), (N'USER');

INSERT INTO NguoiDungVaiTro VALUES (1,1);
INSERT INTO NguoiDungVaiTro VALUES (2,2),(3,2),(4,2);


-- thử update bình thường
UPDATE ThongKeNguoiDung SET TongVan=10,Thang=6,Thua=3,Hoa=1 WHERE UserID=2;
UPDATE ThongKeNguoiDung SET TongVan=20,Thang=10,Thua=5,Hoa=5 WHERE UserID=3;
UPDATE ThongKeNguoiDung SET TongVan=5,Thang=2,Thua=2,Hoa=1 WHERE UserID=4;


INSERT INTO CheDoChoi (TenCheDo,LoaiCheDo,ThoiGian) 
VALUES
(N'Cờ chớp',N'PVP',60),
(N'Cờ nhanh',N'PVP',300),
(N'Cờ tiêu chuẩn',N'PVP',900),
(N'Cờ cổ điển',N'PVP',1800);

INSERT INTO Bot (TenBot, DoKho, MoTa) 
VALUES
(N'Bot Dễ', 1, N'Phù hợp người mới'),
(N'Bot Trung Bình', 5, N'Cân bằng'),
(N'Bot Khó', 8, N'Thách thức cao'),
(N'Bot Siêu Cấp', 10, N'Gần như không thể thắng');


INSERT INTO Puzzle VALUES
(N'r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3',
 N'Nxe5',3,N'Ăn tốt'),

(N'8/8/8/8/8/8/5k2/R6K w - - 0 1',
 N'Ra3 Ke2 Ra2+',6,N'Mate 3');

 INSERT INTO LoaiCo VALUES (N'Cờ vua');



-- Tạo phòng chờ
-- phòng 1
INSERT INTO Phong (ChuPhongID, CheDoID)
VALUES (2,1);

-- join phòng
UPDATE Phong
SET KhachID = 3,
    TrangThai = N'PLAYING'
WHERE PhongID = 1;

-- phòng 2
INSERT INTO Phong (ChuPhongID, CheDoID)
VALUES (3,2);

INSERT INTO VanCo (PhongID, NguoiTrangID, NguoiDenID, LoaiCoID, CheDoID)
VALUES (1,2,3,1,1);



INSERT INTO VanCo (PhongID, NguoiTrangID, NguoiDenID, LoaiCoID, CheDoID)
VALUES (1, 2, 3, 1, 1);

INSERT INTO VanCo (NguoiTrangID, BotID, LoaiCoID, CheDoID)
VALUES (1,1,1,1);

INSERT INTO VanCo (NguoiTrangID, PuzzleID, LoaiCoID, CheDoID)
VALUES (1, 1, 1, 1);

INSERT INTO NuocDi (VanCoID, SoThuTu, Nuoc)
VALUES (1, 1, N'e4');

INSERT INTO XepHang (UserID, CheDoID, SoVan, Thang, Thua, Hoa)
VALUES (1, 1, 10, 5, 3, 2);

INSERT INTO LichSuDiem (UserID, CheDoID, DiemCu, DiemMoi)
VALUES (1, 1, 1200, 1215);

INSERT INTO GiaoDien (Ten, MauBan, KieuQuan) 
VALUES  (N'Classic', N'Classic', N'Standard'),
        (N'Dark Mode', N'Dark', N'Modern'),
        (N'Blue Ocean', N'Blue', N'Fantasy');

INSERT INTO QuanCo (Ten, KyHieu, GiaTri) VALUES
(N'Vua', N'K', 100),
(N'Hậu', N'Q', 9),
(N'Xe', N'R', 5),
(N'Tượng', N'B', 3),
(N'Mã', N'N', 3),
(N'Tốt', N'P', 1);

-- Vua
INSERT INTO LuatDiChuyen (QuanCoID, MoTa) VALUES
(1, N'Đi 1 ô theo mọi hướng'),
(1, N'Nhập thành nếu chưa di chuyển');

-- Tốt
INSERT INTO LuatDiChuyen (QuanCoID, MoTa) VALUES
(6, N'Đi thẳng 1 ô, ăn chéo'),
(6, N'Đi 2 ô ở nước đầu'),
(6, N'Bắt tốt qua đường');

SELECT * FROM VaiTro;
SELECT * FROM ThongTinUser;
SELECT * FROM NguoiDungVaiTro;
SELECT * FROM ThongKeNguoiDung;
SELECT * FROM CheDoChoi;
SELECT * FROM Bot;
SELECT * FROM Puzzle;
SELECT * FROM Phong;
SELECT * FROM VanCo;
SELECT * FROM NuocDi;
SELECT * FROM XepHang;
SELECT * FROM LichSuDiem;
SELECT * FROM  GiaoDien;
SELECT * FROM QuanCo;
SELECT * FROM LuatDiChuyen;