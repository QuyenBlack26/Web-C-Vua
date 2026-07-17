/* ========================= */
/* CỜ CÂU ĐỐ */
/* ========================= */

/* LẤY PHẦN TỬ GIAO DIỆN */
const puzzleBoardElement = document.getElementById("puzzleBoard");
const puzzleStatus = document.getElementById("puzzleStatus");
const currentMoveText = document.getElementById("currentMoveText");
const resetPuzzleBtn = document.getElementById("resetPuzzleBtn");

/* TRẠNG THÁI CÂU ĐỐ */
let puzzleId = window.PUZZLE_ID || 0;
let startFen = window.PUZZLE_FEN || "";
let fen = startFen;

let selectedSquare = null;
let lastMoveSquares = [];
let legalMoves = [];
let puzzleSolved = false;
let isLoadingLegalMoves = false;

/* AI dùng để lấy nước đi hợp lệ */
let ChessAI = null;
let ai = null;

/* KÝ HIỆU QUÂN */
const puzzlePieces = {
    p: "♟",
    r: "♜",
    n: "♞",
    b: "♝",
    q: "♛",
    k: "♚",
    P: "♙",
    R: "♖",
    N: "♘",
    B: "♗",
    Q: "♕",
    K: "♔"
};

/* FEN mặc định để tránh trắng bàn nếu database bị rỗng FEN */
const defaultFen = "8/8/8/8/8/8/8/8 w - - 0 1";

/* HIỂN THỊ TRẠNG THÁI */
function setStatus(message, type = "") {
    if (!puzzleStatus) {
        return;
    }

    puzzleStatus.textContent = message;
    puzzleStatus.classList.remove("success", "error", "info");

    if (type) {
        puzzleStatus.classList.add(type);
    }
}

/* HIỂN THỊ NƯỚC ĐANG CHỌN */
function setCurrentMoveText(text) {
    if (currentMoveText) {
        currentMoveText.textContent = text || "...";
    }
}

/* TẢI CHESSAI SAU KHI BÀN CỜ ĐÃ CÓ THỂ VẼ */
async function loadChessAiForPuzzle() {
    try {
        const module = await import("/js/chessai.js");
        ChessAI = module.default;
        ai = new ChessAI("easy");
        console.log("Đã tải chessai.js cho Puzzle");
    } catch (error) {
        console.error("Không tải được chessai.js:", error);
        ai = null;
        setStatus("Vẫn xem được bàn cờ, nhưng chưa tải được gợi ý nước đi.", "error");
    }
}

/* FEN -> MẢNG BÀN CỜ */
function fenToBoard(fenText) {
    if (!fenText || typeof fenText !== "string" || !fenText.includes("/")) {
        fenText = defaultFen;
    }

    const boardPart = fenText.split(" ")[0];
    const rows = boardPart.split("/");
    const board = [];

    for (let r = 0; r < 8; r++) {
        const row = rows[r] || "8";
        const currentRow = [];

        for (const char of row) {
            if (Number.isNaN(Number(char))) {
                currentRow.push(char);
            } else {
                for (let i = 0; i < Number(char); i++) {
                    currentRow.push("");
                }
            }
        }

        while (currentRow.length < 8) {
            currentRow.push("");
        }

        board.push(currentRow.slice(0, 8));
    }

    return board;
}

/* LẤY TÊN Ô */
function getSquareName(row, col) {
    const files = "abcdefgh";
    return files[col] + (8 - row);
}

/* LẤY VỊ TRÍ Ô */
function getSquarePosition(squareName) {
    const files = "abcdefgh";

    return {
        col: files.indexOf(squareName[0]),
        row: 8 - Number(squareName[1])
    };
}

/* LẤY QUÂN TẠI Ô */
function getPieceAtSquare(squareName) {
    const board = fenToBoard(fen);
    const pos = getSquarePosition(squareName);

    if (pos.row < 0 || pos.row > 7 || pos.col < 0 || pos.col > 7) {
        return "";
    }

    return board[pos.row][pos.col];
}

/* KIỂM TRA QUÂN TRẮNG / ĐEN */
function isWhitePiece(piece) {
    return piece && piece === piece.toUpperCase();
}

function isBlackPiece(piece) {
    return piece && piece === piece.toLowerCase();
}

/* LẤY LƯỢT ĐI TỪ FEN */
function getTurnColor() {
    const parts = fen.split(" ");
    return parts.length > 1 ? parts[1] : "w";
}

/* KIỂM TRA QUÂN CÓ ĐÚNG LƯỢT KHÔNG */
function isPieceOfTurn(piece) {
    const turn = getTurnColor();

    if (turn === "w") {
        return isWhitePiece(piece);
    }

    return isBlackPiece(piece);
}

/* KIỂM TRA PHONG CẤP */
function needPromotion(fromSquare, toSquare, piece) {
    if (piece !== "P" && piece !== "p") {
        return false;
    }

    const toRank = toSquare[1];

    if (piece === "P" && toRank === "8") {
        return true;
    }

    if (piece === "p" && toRank === "1") {
        return true;
    }

    return false;
}

/* VẼ BÀN CỜ */
function drawPuzzleBoard() {
    if (!puzzleBoardElement) {
        console.error("Không tìm thấy #puzzleBoard");
        return;
    }

    puzzleBoardElement.innerHTML = "";

    const board = fenToBoard(fen);

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            const squareName = getSquareName(row, col);
            const piece = board[row][col];

            square.classList.add("puzzle-square");
            square.classList.add((row + col) % 2 === 0 ? "light" : "dark");
            square.dataset.square = squareName;

            if (selectedSquare === squareName) {
                square.classList.add("selected");
            }

            if (lastMoveSquares.includes(squareName)) {
                square.classList.add("last-move");
            }

            if (!puzzleSolved && legalMoves.some(move => move.substring(2, 4) === squareName)) {
                square.classList.add("legal-hint");
            }

            square.textContent = puzzlePieces[piece] || "";

            square.addEventListener("click", function () {
                handlePuzzleSquareClick(squareName, piece);
            });

            puzzleBoardElement.appendChild(square);
        }
    }

    /* Áp lại skin nếu có */
    if (window.ChessSkinRefresh) {
        window.ChessSkinRefresh();
    }
}

/* TẢI GỢI Ý NƯỚC ĐI HỢP LỆ */
async function loadLegalMoves(squareName) {
    if (!ai || puzzleSolved) {
        legalMoves = [];
        setStatus("Chưa có gợi ý vì chessai.js chưa tải được.", "error");
        return;
    }

    isLoadingLegalMoves = true;
    setStatus("Đang lấy gợi ý nước đi...", "info");

    try {
        const currentFen = fen;
        const moves = await ai.getLegalMoves(currentFen, squareName);

        if (fen !== currentFen || selectedSquare !== squareName || puzzleSolved) {
            return;
        }

        legalMoves = moves || [];

        if (legalMoves.length > 0) {
            setStatus("Các ô có chấm xanh là nước có thể đi.", "info");
        } else {
            setStatus("Quân này không có nước đi hợp lệ.", "error");
        }

        drawPuzzleBoard();
    } catch (error) {
        console.error("Lỗi lấy gợi ý nước đi:", error);
        legalMoves = [];
        setStatus("Không lấy được gợi ý nước đi.", "error");
        drawPuzzleBoard();
    } finally {
        isLoadingLegalMoves = false;
    }
}

/* ĐI QUÂN TRÊN FEN ĐỂ HIỂN THỊ SAU KHI ĐÚNG */
function movePieceInFen(move) {
    const from = move.substring(0, 2);
    const to = move.substring(2, 4);
    const promotion = move.length >= 5 ? move[4] : "";

    const board = fenToBoard(fen);

    const fromPos = getSquarePosition(from);
    const toPos = getSquarePosition(to);

    let piece = board[fromPos.row][fromPos.col];

    if (promotion) {
        piece = piece === piece.toUpperCase()
            ? promotion.toUpperCase()
            : promotion.toLowerCase();
    }

    board[fromPos.row][fromPos.col] = "";
    board[toPos.row][toPos.col] = piece;

    const boardPart = board.map(function (row) {
        let text = "";
        let emptyCount = 0;

        row.forEach(function (cell) {
            if (!cell) {
                emptyCount++;
            } else {
                if (emptyCount > 0) {
                    text += emptyCount;
                    emptyCount = 0;
                }

                text += cell;
            }
        });

        if (emptyCount > 0) {
            text += emptyCount;
        }

        return text;
    }).join("/");

    const fenParts = fen.split(" ");
    const nextTurn = fenParts[1] === "w" ? "b" : "w";

    fen = [
        boardPart,
        nextTurn,
        fenParts[2] || "-",
        "-",
        "0",
        fenParts[5] || "1"
    ].join(" ");
}

/* XỬ LÝ CLICK Ô CỜ */
async function handlePuzzleSquareClick(squareName, piece) {
    if (puzzleSolved) {
        setStatus("Câu này đã hoàn thành rồi.", "success");
        return;
    }

    if (isLoadingLegalMoves) {
        setStatus("Đang lấy gợi ý nước đi...", "info");
        return;
    }

    if (!selectedSquare) {
        if (!piece) {
            return;
        }

        if (!isPieceOfTurn(piece)) {
            setStatus("Bạn chỉ được đi quân đúng lượt trong FEN.", "error");
            return;
        }

        selectedSquare = squareName;
        legalMoves = [];
        setCurrentMoveText(selectedSquare + "...");
        drawPuzzleBoard();

        await loadLegalMoves(squareName);
        return;
    }

    const from = selectedSquare;
    const to = squareName;
    const fromPiece = getPieceAtSquare(from);

    if (from === to) {
        selectedSquare = null;
        legalMoves = [];
        setCurrentMoveText("...");
        setStatus("Đã bỏ chọn quân.", "info");
        drawPuzzleBoard();
        return;
    }

    if (piece && isPieceOfTurn(piece)) {
        selectedSquare = squareName;
        legalMoves = [];
        setCurrentMoveText(selectedSquare + "...");
        drawPuzzleBoard();

        await loadLegalMoves(squareName);
        return;
    }

    let move = from + to;

    if (needPromotion(from, to, fromPiece)) {
        move += "q";
    }

    if (legalMoves.length > 0) {
        const isLegalMove = legalMoves.some(x => x.toLowerCase() === move.toLowerCase());

        if (!isLegalMove) {
            setStatus("Nước này không hợp lệ. Hãy chọn ô có chấm xanh.", "error");
            return;
        }
    }

    selectedSquare = null;
    legalMoves = [];
    setCurrentMoveText(move);

    await checkPuzzleMove(move);
}

/* GỬI NƯỚC ĐI LÊN SERVER KIỂM TRA ĐÚNG SAI */
async function checkPuzzleMove(move) {
    setStatus("Đang kiểm tra nước đi...", "info");

    const formData = new FormData();
    formData.append("puzzleId", puzzleId);
    formData.append("move", move);

    try {
        const response = await fetch("/Puzzle/CheckMove", {
            method: "POST",
            body: formData
        });

        const data = await response.json();

        if (!data.success) {
            setStatus(data.message || "Lỗi kiểm tra câu đố.", "error");
            drawPuzzleBoard();
            return;
        }

        if (data.correct) {
            puzzleSolved = true;

            lastMoveSquares = [
                move.substring(0, 2),
                move.substring(2, 4)
            ];

            movePieceInFen(move);
            drawPuzzleBoard();

            setStatus(data.message || "Chính xác!", "success");
            showGameResultModal({
                icon: "🏆",
                title: "Chúc mừng!",
                message: data.message || "Bạn đã giải đúng câu đố.",
                score: data.diemNhan && data.diemNhan > 0 ? `+${data.diemNhan} điểm` : "Đã hoàn thành"
            });
            return;
        }

        setStatus(data.message || "Sai rồi, hãy thử lại. Không bị trừ điểm.", "error");
        drawPuzzleBoard();
    } catch (error) {
        console.error(error);
        setStatus("Lỗi kết nối khi kiểm tra câu đố.", "error");
        drawPuzzleBoard();
    }
}

/* RESET CÂU ĐỐ */
function resetPuzzle() {
    fen = startFen && startFen.includes("/") ? startFen : defaultFen;
    selectedSquare = null;
    lastMoveSquares = [];
    legalMoves = [];
    puzzleSolved = false;
    isLoadingLegalMoves = false;

    setCurrentMoveText("...");
    setStatus("Hãy chọn quân. Các nước hợp lệ sẽ hiện chấm xanh.", "info");

    drawPuzzleBoard();
}

/* KHỞI ĐỘNG */
async function initPuzzlePage() {
    resetPuzzle();
    await loadChessAiForPuzzle();
}

if (resetPuzzleBtn) {
    resetPuzzleBtn.addEventListener("click", resetPuzzle);
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initPuzzlePage);
} else {
    initPuzzlePage();
}

async function saveGameAndUpdateRank(result, fen, moves, modeName, modeType, botName) {
    try {
        const response = await fetch("/Play/SaveGameHistory", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                modeName: modeName,
                modeType: modeType,
                botName: botName,
                result: result,
                fen: fen,
                moves: moves
            })
        });

        const data = await response.json();

        if (data.success) {
            console.log("Đã lưu ván và cộng điểm:", data);
            alert("Đã lưu ván cờ và cộng điểm xếp hạng!");
        } else {
            console.error("Lỗi lưu ván:", data.message);
            alert(data.message);
        }
    } catch (error) {
        console.error("Lỗi gọi SaveGameHistory:", error);
        alert("Không thể lưu ván cờ và cộng điểm.");
    }
}