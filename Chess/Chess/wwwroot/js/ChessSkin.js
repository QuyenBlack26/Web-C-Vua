/* ========================= */
/* CHESS SKIN DÙNG CHUNG */
/* ========================= */

window.ChessSkin = {
    boardSkin: null,
    pieceSkin: null,
    pieceDetails: null,
    observers: [],
    isApplying: false
};

const unicodeToPieceCode = {
    "♔": "wK",
    "♕": "wQ",
    "♖": "wR",
    "♗": "wB",
    "♘": "wN",
    "♙": "wP",

    "♚": "bK",
    "♛": "bQ",
    "♜": "bR",
    "♝": "bB",
    "♞": "bN",
    "♟": "bP"
};

function findChessBoards() {
    return document.querySelectorAll(
        "#chessBoard, " +
        ".chess-board, " +
        "#localBoard, " +
        ".local-board, " +
        "#reviewBoard, " +
        ".review-board, " +
        "#puzzleBoard, " +
        ".puzzle-board, " +
        "#skinPreviewBoard, " +
        ".skin-preview-board"
    );
}

function applyBoardSkin(boardSkin) {
    if (!boardSkin) {
        return;
    }

    const boards = findChessBoards();

    boards.forEach(function (board) {
        board.style.setProperty("--skin-light-square", boardSkin.mauOTrang || "#f0d9b5");
        board.style.setProperty("--skin-dark-square", boardSkin.mauODen || "#b58863");

        if (boardSkin.anhOSang && boardSkin.anhOSang.trim() !== "") {
            board.style.setProperty("--skin-light-image", `url("${boardSkin.anhOSang}")`);
            board.classList.add("skin-has-square-texture");
        } else {
            board.style.setProperty("--skin-light-image", "none");
        }

        if (boardSkin.anhODen && boardSkin.anhODen.trim() !== "") {
            board.style.setProperty("--skin-dark-image", `url("${boardSkin.anhODen}")`);
            board.classList.add("skin-has-square-texture");
        } else {
            board.style.setProperty("--skin-dark-image", "none");
        }

        if (
            (!boardSkin.anhOSang || boardSkin.anhOSang.trim() === "") &&
            (!boardSkin.anhODen || boardSkin.anhODen.trim() === "")
        ) {
            board.classList.remove("skin-has-square-texture");
        }

        board.classList.remove("skin-has-board-image");
        board.style.setProperty("--skin-board-image", "none");
    });
}

function clearPieceClasses(board) {
    board.classList.remove(
        "piece-classic",
        "piece-shadow",
        "piece-glow",
        "piece-png-default",
        "piece-image-classic"
    );
}

function applyPieceClass(pieceSkin) {
    if (!pieceSkin) {
        return;
    }

    const boards = findChessBoards();

    boards.forEach(function (board) {
        clearPieceClasses(board);

        if (pieceSkin.cssClass) {
            board.classList.add(pieceSkin.cssClass);
        }
    });
}

function getPieceImagePath(pieceCode) {
    const details = window.ChessSkin.pieceDetails;
    const pieceSkin = window.ChessSkin.pieceSkin;

    if (!details || !pieceSkin) {
        return "";
    }

    const item = details[pieceCode];

    if (!item) {
        return "";
    }

    if (item.fullImagePath) {
        return item.fullImagePath;
    }

    if (item.fileAnh && pieceSkin.duongDanThuMuc) {
        const folder = pieceSkin.duongDanThuMuc.endsWith("/")
            ? pieceSkin.duongDanThuMuc
            : pieceSkin.duongDanThuMuc + "/";

        return folder + item.fileAnh;
    }

    return "";
}

function makePieceImage(pieceCode) {
    const src = getPieceImagePath(pieceCode);

    if (!src) {
        return null;
    }

    const img = document.createElement("img");
    img.src = src;
    img.alt = pieceCode;
    img.dataset.piece = pieceCode;
    img.className = "skin-piece-img";

    return img;
}

function convertSquarePieceToImage(square) {
    const pieceSkin = window.ChessSkin.pieceSkin;

    if (!pieceSkin || pieceSkin.kieuHienThi !== "IMAGE") {
        return;
    }

    if (square.querySelector("img.skin-piece-img")) {
        return;
    }

    const spanPiece = square.querySelector(".local-piece");

    if (spanPiece) {
        const text = spanPiece.textContent.trim();
        const pieceCode = unicodeToPieceCode[text];

        if (!pieceCode) {
            return;
        }

        const img = makePieceImage(pieceCode);

        if (!img) {
            return;
        }

        spanPiece.textContent = "";
        spanPiece.appendChild(img);
        return;
    }

    const text = square.textContent.trim();
    const pieceCode = unicodeToPieceCode[text];

    if (!pieceCode) {
        return;
    }

    const img = makePieceImage(pieceCode);

    if (!img) {
        return;
    }

    square.textContent = "";
    square.appendChild(img);
}

function convertAllPiecesToImages() {
    const pieceSkin = window.ChessSkin.pieceSkin;

    if (!pieceSkin || pieceSkin.kieuHienThi !== "IMAGE") {
        return;
    }

    const squares = document.querySelectorAll(
        "#chessBoard .square, " +
        ".chess-board .square, " +
        "#localBoard .local-square, " +
        ".local-board .local-square, " +
        "#reviewBoard .review-square, " +
        ".review-board .review-square, " +
        "#puzzleBoard .puzzle-square, " +
        ".puzzle-board .puzzle-square, " +
        "#skinPreviewBoard .preview-square, " +
        ".skin-preview-board .preview-square"
    );

    squares.forEach(convertSquarePieceToImage);
}

function applyCurrentSkinAgain() {
    if (window.ChessSkin.isApplying) {
        return;
    }

    window.ChessSkin.isApplying = true;

    requestAnimationFrame(function () {
        if (window.ChessSkin.boardSkin) {
            applyBoardSkin(window.ChessSkin.boardSkin);
        }

        if (window.ChessSkin.pieceSkin) {
            applyPieceClass(window.ChessSkin.pieceSkin);
        }

        convertAllPiecesToImages();

        window.ChessSkin.isApplying = false;
    });
}

function observeBoards() {
    window.ChessSkin.observers.forEach(function (observer) {
        observer.disconnect();
    });

    window.ChessSkin.observers = [];

    const boards = findChessBoards();

    boards.forEach(function (board) {
        const observer = new MutationObserver(function () {
            applyCurrentSkinAgain();
        });

        observer.observe(board, {
            childList: true,
            subtree: true
        });

        window.ChessSkin.observers.push(observer);
    });
}

async function loadPieceDetails(skinQuanCoId) {
    const response = await fetch(`/Skin/GetPieceSkinDetail?skinQuanCoId=${skinQuanCoId}`);
    const data = await response.json();

    if (!data.success) {
        console.log("Lỗi lấy chi tiết quân:", data.message);
        return null;
    }

    return data.pieces;
}

async function loadAndApplyMySkin() {
    try {
        const response = await fetch("/Skin/GetMySkin");
        const data = await response.json();

        if (!data.success) {
            console.log("Chưa áp dụng skin:", data.message);
            return;
        }

        window.ChessSkin.boardSkin = data.boardSkin;
        window.ChessSkin.pieceSkin = data.pieceSkin;
        window.ChessSkin.pieceDetails = null;

        if (data.pieceSkin && data.pieceSkin.kieuHienThi === "IMAGE") {
            window.ChessSkin.pieceDetails = await loadPieceDetails(data.pieceSkin.skinQuanCoId);
        }

        applyBoardSkin(data.boardSkin);
        applyPieceClass(data.pieceSkin);
        convertAllPiecesToImages();
        observeBoards();

        setTimeout(applyCurrentSkinAgain, 100);
        setTimeout(applyCurrentSkinAgain, 500);
        setTimeout(applyCurrentSkinAgain, 1000);
    } catch (error) {
        console.log("Lỗi load skin:", error);
    }
}

async function loadSkinOptions() {
    const boardSelect = document.getElementById("skinBanCoSelect");
    const pieceSelect = document.getElementById("skinQuanCoSelect");

    if (!boardSelect || !pieceSelect) {
        return;
    }

    const response = await fetch("/Skin/GetSkins");
    const data = await response.json();

    if (!data.success) {
        console.log("Lỗi lấy danh sách skin:", data.message);
        return;
    }

    boardSelect.innerHTML = "";
    pieceSelect.innerHTML = "";

    data.boardSkins.forEach(function (skin) {
        const option = document.createElement("option");
        option.value = skin.skinBanCoId;
        option.textContent = skin.tenSkin;

        option.dataset.maSkin = skin.maSkin || "";
        option.dataset.mauOTrang = skin.mauOTrang || "";
        option.dataset.mauODen = skin.mauODen || "";
        option.dataset.anhNenBanCo = skin.anhNenBanCo || "";
        option.dataset.anhOSang = skin.anhOSang || "";
        option.dataset.anhODen = skin.anhODen || "";

        boardSelect.appendChild(option);
    });

    data.pieceSkins.forEach(function (skin) {
        const option = document.createElement("option");
        option.value = skin.skinQuanCoId;
        option.textContent = skin.tenSkin;

        option.dataset.maSkin = skin.maSkin || "";
        option.dataset.kieuHienThi = skin.kieuHienThi || "";
        option.dataset.duongDanThuMuc = skin.duongDanThuMuc || "";
        option.dataset.cssClass = skin.cssClass || "";

        pieceSelect.appendChild(option);
    });

    const mySkinResponse = await fetch("/Skin/GetMySkin");
    const mySkin = await mySkinResponse.json();

    if (mySkin.success) {
        if (mySkin.boardSkin && mySkin.boardSkin.skinBanCoId) {
            boardSelect.value = mySkin.boardSkin.skinBanCoId;
        }

        if (mySkin.pieceSkin && mySkin.pieceSkin.skinQuanCoId) {
            pieceSelect.value = mySkin.pieceSkin.skinQuanCoId;
        }
    }
}

async function saveSelectedSkin() {
    const boardSelect = document.getElementById("skinBanCoSelect");
    const pieceSelect = document.getElementById("skinQuanCoSelect");
    const message = document.getElementById("skinSaveMessage");

    if (!boardSelect || !pieceSelect) {
        return;
    }

    const formData = new FormData();
    formData.append("loaiCoId", "0");
    formData.append("skinBanCoId", boardSelect.value);
    formData.append("skinQuanCoId", pieceSelect.value);

    const response = await fetch("/Skin/SaveSkin", {
        method: "POST",
        body: formData
    });

    const data = await response.json();

    if (message) {
        message.textContent = data.message || "";
        message.style.color = data.success ? "#15803d" : "#b91c1c";
    }

    if (data.success) {
        await loadAndApplyMySkin();
    }
}

function bindSkinEvents() {
    const saveButton = document.getElementById("btnSaveSkin");

    if (saveButton) {
        saveButton.addEventListener("click", saveSelectedSkin);
    }
}

window.ChessSkinRefresh = loadAndApplyMySkin;

document.addEventListener("DOMContentLoaded", function () {
    loadAndApplyMySkin();
    loadSkinOptions();
    bindSkinEvents();
});