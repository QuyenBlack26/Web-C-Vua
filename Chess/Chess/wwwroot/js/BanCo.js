/* IMPORT AI CỜ VUA */
let ChessAI = null;

try {
    const module = await import("./chessai.js");
    ChessAI = module.default;
} catch (error) {
    console.error("Không tải được chessai.js:", error);
}

/* LẤY CÁC PHẦN TỬ TRÊN GIAO DIỆN */
const boardElement = document.getElementById("chessBoard");
const statusText = document.getElementById("statusText");
const moveHistory = document.getElementById("moveHistory");
const levelSelect = document.getElementById("levelSelect");
const newGameBtn = document.getElementById("newGameBtn");
const resignBtn = document.getElementById("resignBtn");

const aiSetupOverlay = document.getElementById("aiSetupOverlay");
const setupLevelSelect = document.getElementById("setupLevelSelect");
const startAIGameBtn = document.getElementById("startAIGameBtn");
const aiGamePage = document.getElementById("aiGamePage");

const playerCapturedElement = document.getElementById("playerCapturedPieces");
const aiCapturedElement = document.getElementById("aiCapturedPieces");

const promotionOverlay = document.getElementById("promotionOverlay");
const promotionChoices = document.querySelectorAll(".promotion-choice");

/* TRẠNG THÁI BAN ĐẦU CỦA VÁN CỜ */
let fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
let selectedSquare = null;
let lastMoveSquares = [];
let history = [];
let moveHistoryForSave = [];
let historySaved = false;
let gameOver = false;
let gameStarted = false;

let fenHistory = [fen];
let lastAiMove = null;

let isBusy = false;
let isLoadingLegalMoves = false;
let gameId = 0;
let legalSquares = [];

let playerCapturedPieces = [];
let aiCapturedPieces = [];

let pendingPromotionBaseMove = null;

/* KHỞI TẠO AI */
let ai = ChessAI && levelSelect ? new ChessAI(levelSelect.value) : null;

/* KÝ HIỆU QUÂN CỜ */
const pieces = {
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

/* HIỆN MODAL KẾT QUẢ, CÓ FALLBACK NẾU CHƯA NẠP FILE MODAL */
function showResultModalSafe(options) {
    if (typeof showGameResultModal === "function") {
        showGameResultModal(options);
        return;
    }

    alert(`${options.title}\n${options.message}\n${options.score || ""}`);
}

/* CHUYỂN FEN THÀNH MẢNG BÀN CỜ */
function fenToBoard(fenText) {
    const boardPart = fenText.split(" ")[0];
    const rows = boardPart.split("/");
    const board = [];

    for (const row of rows) {
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

    while (board.length < 8) {
        board.push(["", "", "", "", "", "", "", ""]);
    }

    return board.slice(0, 8);
}

/* LẤY TÊN Ô CỜ THEO HÀNG VÀ CỘT */
function getSquareName(row, col) {
    const files = "abcdefgh";
    return files[col] + (8 - row);
}

/* LẤY VỊ TRÍ Ô TỪ TÊN Ô */
function getSquarePosition(squareName) {
    const files = "abcdefgh";

    return {
        col: files.indexOf(squareName[0]),
        row: 8 - Number(squareName[1])
    };
}

/* LẤY QUÂN CỜ TẠI MỘT Ô */
function getPieceAtSquare(squareName, fenText = fen) {
    const board = fenToBoard(fenText);
    const pos = getSquarePosition(squareName);

    if (pos.row < 0 || pos.row > 7 || pos.col < 0 || pos.col > 7) {
        return "";
    }

    return board[pos.row][pos.col];
}

/* LẤY QUÂN BỊ ĂN TRƯỚC KHI NƯỚC ĐI ĐƯỢC ÁP DỤNG */
function getCapturedPieceBeforeMove(fenBeforeMove, move) {
    const to = move.substring(2, 4);
    return getPieceAtSquare(to, fenBeforeMove);
}

/* KIỂM TRA CÓ PHẢI LƯỢT TRẮNG KHÔNG */
function isWhiteTurn() {
    return fen.split(" ")[1] === "w";
}

/* KIỂM TRA QUÂN TRẮNG */
function isWhitePiece(piece) {
    return piece && piece === piece.toUpperCase();
}

/* KIỂM TRA PHONG CẤP TỐT */
function needPromotion(fromSquare, toSquare, piece) {
    if (piece !== "P") {
        return false;
    }

    const fromRank = fromSquare[1];
    const toRank = toSquare[1];

    return fromRank === "7" && toRank === "8";
}

/* MỞ KHUNG CHỌN CHẾ ĐỘ */
function openSetupModal() {
    if (aiSetupOverlay) {
        aiSetupOverlay.classList.add("show");
        gameStarted = false;
    } else {
        gameStarted = true;
    }

    if (aiGamePage) {
        aiGamePage.classList.add("ai-game-locked");
    }
}

/* ĐÓNG KHUNG CHỌN CHẾ ĐỘ */
function closeSetupModal() {
    if (aiSetupOverlay) {
        aiSetupOverlay.classList.remove("show");
    }

    if (aiGamePage) {
        aiGamePage.classList.remove("ai-game-locked");
    }

    gameStarted = true;
}

/* HIỂN THỊ QUÂN BỊ ĂN */
function renderCapturedPieces(element, capturedList) {
    if (!element) {
        return;
    }

    element.innerHTML = "";

    if (!capturedList || capturedList.length === 0) {
        element.textContent = "Chưa có";
        element.classList.add("empty");
        return;
    }

    element.classList.remove("empty");

    capturedList.forEach(function (piece) {
        const span = document.createElement("span");
        span.className = "captured-piece";
        span.textContent = pieces[piece] || piece;
        element.appendChild(span);
    });
}

/* CẬP NHẬT 2 KHUNG QUÂN BỊ ĂN */
function updateCapturedPanels() {
    renderCapturedPieces(playerCapturedElement, playerCapturedPieces);
    renderCapturedPieces(aiCapturedElement, aiCapturedPieces);
}

/* THÊM QUÂN BỊ ĂN VÀO ĐÚNG BÊN */
function addCapturedPiece(piece, byPlayer) {
    if (!piece) {
        return;
    }

    if (byPlayer) {
        playerCapturedPieces.push(piece);
    } else {
        aiCapturedPieces.push(piece);
    }

    updateCapturedPanels();
}

/* VẼ BÀN CỜ */
function drawBoard() {
    if (!boardElement) {
        return;
    }

    boardElement.innerHTML = "";

    const board = fenToBoard(fen);

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            const squareName = getSquareName(row, col);
            const piece = board[row][col];

            square.classList.add("square");
            square.classList.add((row + col) % 2 === 0 ? "light" : "dark");
            square.dataset.square = squareName;

            if (selectedSquare === squareName) {
                square.classList.add("selected");
            }

            if (lastMoveSquares.includes(squareName)) {
                square.classList.add("last-move");
            }

            if (!gameOver && gameStarted && legalSquares.some(move => move.substring(2, 4) === squareName)) {
                square.classList.add("legal-hint");
            }

            square.textContent = pieces[piece] || "";

            square.addEventListener("click", function () {
                handleSquareClick(squareName, piece);
            });

            boardElement.appendChild(square);
        }
    }

    if (window.ChessSkinRefresh) {
        window.ChessSkinRefresh();
    }
}

/* TẢI CÁC NƯỚC ĐI HỢP LỆ */
async function loadLegalMoves(squareName) {
    if (!ai || gameOver || !gameStarted) {
        return;
    }

    isLoadingLegalMoves = true;

    try {
        const currentGameId = gameId;
        const currentFen = fen;

        const moves = await ai.getLegalMoves(currentFen, squareName);

        if (currentGameId !== gameId) {
            return;
        }

        if (fen !== currentFen) {
            return;
        }

        if (selectedSquare !== squareName) {
            return;
        }

        if (gameOver) {
            return;
        }

        legalSquares = moves || [];
        drawBoard();
    } catch (error) {
        console.error("Lỗi lấy nước đi hợp lệ:", error);
        legalSquares = [];
    } finally {
        isLoadingLegalMoves = false;
    }
}

/* XỬ LÝ KHI NGƯỜI CHƠI BẤM VÀO Ô CỜ */
async function handleSquareClick(squareName, piece) {
    if (!gameStarted) {
        statusText.textContent = "Hãy chọn chế độ để bắt đầu.";
        return;
    }

    if (gameOver) {
        statusText.textContent = "Ván cờ đã kết thúc. Bấm Ván mới để chơi lại.";
        return;
    }

    if (isLoadingLegalMoves) {
        statusText.textContent = "Đang lấy nước đi hợp lệ...";
        return;
    }

    if (isBusy) {
        statusText.textContent = "Đợi AI đi xong đã.";
        return;
    }

    if (!ai) {
        statusText.textContent = "Chưa tải được AI. Kiểm tra file chessai.js.";
        return;
    }

    if (!isWhiteTurn()) {
        statusText.textContent = "Chưa tới lượt của bạn.";
        return;
    }

    if (!selectedSquare) {
        if (!piece) {
            return;
        }

        if (!isWhitePiece(piece)) {
            statusText.textContent = "Bạn chỉ được đi quân Trắng.";
            return;
        }

        selectedSquare = squareName;
        legalSquares = [];
        drawBoard();

        await loadLegalMoves(squareName);
        return;
    }

    if (piece && isWhitePiece(piece)) {
        selectedSquare = squareName;
        legalSquares = [];
        drawBoard();

        await loadLegalMoves(squareName);
        return;
    }

    const from = selectedSquare;
    const to = squareName;
    const fromPiece = getPieceAtSquare(from);
    const baseMove = from + to;

    selectedSquare = null;
    legalSquares = [];

    if (needPromotion(from, to, fromPiece)) {
        pendingPromotionBaseMove = baseMove;
        showPromotionModal();
        drawBoard();
        return;
    }

    await playerMove(baseMove);
}

/* HIỆN MODAL PHONG CẤP */
function showPromotionModal() {
    if (promotionOverlay) {
        promotionOverlay.classList.add("show");
        return;
    }

    pendingPromotionBaseMove += "q";
    playerMove(pendingPromotionBaseMove);
    pendingPromotionBaseMove = null;
}

/* ẨN MODAL PHONG CẤP */
function hidePromotionModal() {
    if (promotionOverlay) {
        promotionOverlay.classList.remove("show");
    }
}

/* BẮT SỰ KIỆN CHỌN QUÂN PHONG CẤP */
promotionChoices.forEach(function (button) {
    button.addEventListener("click", async function () {
        if (!pendingPromotionBaseMove) {
            return;
        }

        const piece = this.dataset.piece || "q";
        const move = pendingPromotionBaseMove + piece;

        pendingPromotionBaseMove = null;
        hidePromotionModal();

        await playerMove(move);
    });
});

/* XỬ LÝ NƯỚC ĐI CỦA NGƯỜI CHƠI */
async function playerMove(move) {
    if (isBusy || gameOver) {
        return;
    }

    if (!ai) {
        statusText.textContent = "Chưa tải được AI.";
        return;
    }

    isBusy = true;

    const currentGameId = gameId;
    const fenBeforePlayer = fen;
    const capturedPiece = getCapturedPieceBeforeMove(fenBeforePlayer, move);

    statusText.textContent = "Đang kiểm tra nước đi...";

    try {
        const result = await ai.validateMove(fenBeforePlayer, move);

        if (currentGameId !== gameId) {
            return;
        }

        if (fen !== fenBeforePlayer) {
            return;
        }

        if (gameOver) {
            return;
        }

        if (!result || !result.valid) {
            statusText.textContent = result?.error || "Nước đi không hợp lệ.";
            selectedSquare = null;
            legalSquares = [];
            drawBoard();
            return;
        }

        fen = result.fen_after;
        fenHistory.push(fen);

        if (capturedPiece) {
            addCapturedPiece(capturedPiece, true);
        }

        lastMoveSquares = [
            move.substring(0, 2),
            move.substring(2, 4)
        ];

        history.push({
            side: "Bạn",
            move: move,
            type: "player"
        });

        moveHistoryForSave.push(move);

        selectedSquare = null;
        legalSquares = [];

        updateHistory();
        drawBoard();

        if (result.is_checkmate) {
            gameOver = true;
            statusText.textContent = "Chiếu hết! Bạn thắng.";
            saveAIHistory("WHITE_WIN", true);
            return;
        }

        if (result.is_game_over || result.is_stalemate) {
            gameOver = true;
            statusText.textContent = "Ván cờ kết thúc.";
            saveAIHistory("DRAW", true);
            return;
        }

        await aiMove(currentGameId);
    } catch (error) {
        console.error("Lỗi khi người chơi đi:", error);
        statusText.textContent = "Lỗi kết nối khi kiểm tra nước đi.";
    } finally {
        if (currentGameId === gameId && !gameOver) {
            isBusy = false;
        }
    }
}

/* XỬ LÝ NƯỚC ĐI CỦA AI */
async function aiMove(currentGameId) {
    if (!ai || gameOver) {
        return;
    }

    statusText.textContent = "AI đang suy nghĩ...";

    try {
        const fenBeforeAI = fen;

        console.log("FEN gửi cho AI:", fenBeforeAI);

        const result = await ai.getMove(fenBeforeAI, fenHistory, lastAiMove);

        console.log("Kết quả AI trả về:", result);

        if (currentGameId !== gameId) {
            return;
        }

        if (fen !== fenBeforeAI) {
            console.warn("Bỏ qua nước AI cũ vì FEN đã thay đổi.");
            return;
        }

        if (gameOver) {
            return;
        }

        if (!result) {
            statusText.textContent = "Không kết nối được AI Python.";
            return;
        }

        if (result.error) {
            statusText.textContent = "Lỗi AI: " + result.error;
            console.error("Chi tiết lỗi AI:", result);
            return;
        }

        if (!result.move || result.move.length < 4) {
            gameOver = true;
            statusText.textContent = "AI không còn nước đi hợp lệ. Ván cờ kết thúc.";
            saveAIHistory("DRAW", true);
            return;
        }

        const capturedPiece = getCapturedPieceBeforeMove(fenBeforeAI, result.move);

        fen = result.fen_after;
        fenHistory.push(fen);
        lastAiMove = result.move;

        if (capturedPiece) {
            addCapturedPiece(capturedPiece, false);
        }

        lastMoveSquares = [
            result.move.substring(0, 2),
            result.move.substring(2, 4)
        ];

        const moveSan = result.move_san || result.move;

        history.push({
            side: "AI",
            move: result.move,
            san: moveSan,
            type: "ai"
        });

        moveHistoryForSave.push(result.move);

        selectedSquare = null;
        legalSquares = [];

        updateHistory();
        drawBoard();

        if (result.is_checkmate) {
            gameOver = true;
            statusText.textContent = "Chiếu hết! AI thắng.";
            saveAIHistory("BLACK_WIN", true);
        } else if (result.is_game_over || result.is_stalemate) {
            gameOver = true;
            statusText.textContent = "Ván cờ kết thúc.";
            saveAIHistory("DRAW", true);
        } else if (result.is_check) {
            statusText.textContent = "Bạn đang bị chiếu!";
        } else {
            statusText.textContent = "Lượt của bạn.";
        }
    } catch (error) {
        console.error("Lỗi khi AI đi:", error);
        statusText.textContent = "Lỗi kết nối AI.";
    } finally {
        if (currentGameId === gameId && !gameOver) {
            isBusy = false;
        }
    }
}

/* CẬP NHẬT LỊCH SỬ NƯỚC ĐI */
function updateHistory() {
    if (!moveHistory) {
        return;
    }

    moveHistory.innerHTML = "";

    if (history.length === 0) {
        const empty = document.createElement("p");
        empty.textContent = "Chưa có nước đi.";
        empty.style.color = "#6b7280";
        empty.style.fontWeight = "700";
        moveHistory.appendChild(empty);
        return;
    }

    history.forEach(function (item, index) {
        const row = document.createElement("div");
        row.className = `ai-move-row ${item.type === "ai" ? "ai-move" : "player-move"}`;

        const no = document.createElement("div");
        no.className = "ai-move-no";
        no.textContent = index + 1;

        const main = document.createElement("div");
        main.className = "ai-move-main";

        const side = document.createElement("span");
        side.className = "ai-move-side";
        side.textContent = item.side;

        const move = document.createElement("span");
        move.className = "ai-move-text";

        if (item.san && item.san !== item.move) {
            move.textContent = `${item.move} (${item.san})`;
        } else {
            move.textContent = item.move;
        }

        main.appendChild(side);
        main.appendChild(move);

        row.appendChild(no);
        row.appendChild(main);

        moveHistory.appendChild(row);
    });

    moveHistory.scrollTop = moveHistory.scrollHeight;
}

/* ĐỔI CẤP ĐỘ AI */
if (levelSelect) {
    levelSelect.addEventListener("change", function () {
        if (!ai) {
            return;
        }

        ai.setLevel(levelSelect.value);

        if (setupLevelSelect) {
            setupLevelSelect.value = levelSelect.value;
        }

        statusText.textContent = "Đã đổi cấp độ AI: " + levelSelect.options[levelSelect.selectedIndex].text;
    });
}

/* RESET DỮ LIỆU VÁN CỜ */
function resetGameState() {
    gameId++;

    fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    selectedSquare = null;
    lastMoveSquares = [];
    history = [];
    moveHistoryForSave = [];
    historySaved = false;
    fenHistory = [fen];
    lastAiMove = null;
    legalSquares = [];
    isBusy = false;
    isLoadingLegalMoves = false;
    gameOver = false;
    pendingPromotionBaseMove = null;

    playerCapturedPieces = [];
    aiCapturedPieces = [];

    hidePromotionModal();
    updateCapturedPanels();
    updateHistory();
    drawBoard();
}

/* BẮT ĐẦU VÁN SAU KHI CHỌN CHẾ ĐỘ */
function startAIGame() {
    const selectedLevel = setupLevelSelect ? setupLevelSelect.value : "medium";

    if (levelSelect) {
        levelSelect.value = selectedLevel;
    }

    if (ChessAI) {
        ai = new ChessAI(selectedLevel);
    }

    resetGameState();
    closeSetupModal();

    statusText.textContent = "Ván mới - Lượt của bạn.";
}

/* NÚT BẮT ĐẦU TRONG KHUNG CHỌN CHẾ ĐỘ */
if (startAIGameBtn) {
    startAIGameBtn.addEventListener("click", startAIGame);
}

/* TẠO VÁN MỚI */
if (newGameBtn) {
    newGameBtn.addEventListener("click", function () {
        resetGameState();
        openSetupModal();
        statusText.textContent = "Hãy chọn chế độ để bắt đầu ván mới.";
    });
}

/* ĐẦU HÀNG */
function resignAI() {
    if (!gameStarted) {
        statusText.textContent = "Bạn chưa bắt đầu ván cờ.";
        return;
    }

    if (gameOver) {
        statusText.textContent = "Ván cờ đã kết thúc. Bấm Ván mới để chơi lại.";
        return;
    }

    gameOver = true;
    isBusy = true;
    isLoadingLegalMoves = false;
    selectedSquare = null;
    legalSquares = [];
    pendingPromotionBaseMove = null;

    hidePromotionModal();

    statusText.textContent = "Bạn đã đầu hàng. AI thắng.";

    history.push({
        side: "Bạn",
        move: "Đầu hàng",
        type: "player"
    });

    updateHistory();
    drawBoard();

    saveAIHistory(
        "BLACK_WIN",
        true,
        "Bạn đã đầu hàng. AI thắng."
    );
}

/* GẮN NÚT ĐẦU HÀNG */
if (resignBtn) {
    resignBtn.addEventListener("click", resignAI);
}

window.resignAI = resignAI;

/* LƯU LỊCH SỬ VÁN CỜ AI */
function saveAIHistory(result, allowEmptyMoves = false, customMessage = "") {
    console.log("Bắt đầu lưu lịch sử AI:", {
        result: result,
        fen: fen,
        moves: moveHistoryForSave
    });

    if (historySaved) {
        console.log("Lịch sử đã lưu rồi, không lưu lại nữa.");
        return;
    }

    if (!allowEmptyMoves && (!moveHistoryForSave || moveHistoryForSave.length === 0)) {
        console.log("Không có nước đi AI để lưu.");
        return;
    }

    historySaved = true;

    fetch("/Play/SaveGameHistory", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            modeName: "Chơi Với AI",
            modeType: "BOT",
            botName: "Bot Trung Bình",
            result: result,
            fen: fen,
            moves: moveHistoryForSave
        })
    })
        .then(res => res.json())
        .then(data => {
            console.log("Lưu lịch sử:", data);

            if (data.success) {
                let title = "Trận đấu kết thúc";
                let message = customMessage || "Kết quả đã được lưu.";
                let score = "";

                if (result === "WHITE_WIN") {
                    title = "Chúc mừng!";
                    message = customMessage || "Bạn đã chiến thắng AI!";
                    score = "+15 điểm";
                } else if (result === "BLACK_WIN") {
                    title = "Bạn đã thua!";
                    message = customMessage || "AI đã chiến thắng ván này.";
                    score = "-10 điểm";
                } else {
                    title = "Ván cờ hòa!";
                    message = customMessage || "Bạn và AI đã hòa.";
                    score = "+5 điểm";
                }

                showResultModalSafe({
                    icon: result === "DRAW" ? "🤝" : result === "WHITE_WIN" ? "🏆" : "😓",
                    title: title,
                    message: message,
                    score: score,
                    onClose: function () {
                        resetGameState();
                        openSetupModal();
                        statusText.textContent = "Hãy chọn chế độ để bắt đầu ván mới.";
                    }
                });
            } else {
                historySaved = false;

                showResultModalSafe({
                    icon: "⚠️",
                    title: "Không lưu được kết quả",
                    message: data.message || "Có lỗi khi lưu lịch sử ván cờ.",
                    score: ""
                });
            }
        })
        .catch(error => {
            historySaved = false;
            console.error("Lỗi lưu lịch sử AI:", error);

            showResultModalSafe({
                icon: "⚠️",
                title: "Lỗi kết nối",
                message: "Không thể lưu lịch sử ván cờ AI.",
                score: ""
            });
        });
}

window.saveAIHistory = saveAIHistory;

/* KHỞI CHẠY BÀN CỜ */
updateCapturedPanels();
updateHistory();
drawBoard();
openSetupModal();

console.log("BanCo.js đã load bản nâng cấp AI.");