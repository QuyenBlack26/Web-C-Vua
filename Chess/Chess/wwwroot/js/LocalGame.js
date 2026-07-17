const localPage = document.querySelector(".local-game-page");
const boardElement = document.getElementById("localBoard");

const whiteTimeEl = document.getElementById("whiteTime");
const blackTimeEl = document.getElementById("blackTime");
const statusEl = document.getElementById("localStatus");

const whiteClockBox = document.getElementById("whiteClockBox");
const blackClockBox = document.getElementById("blackClockBox");

const whiteCapturedEl = document.getElementById("whiteCaptured");
const blackCapturedEl = document.getElementById("blackCaptured");

const resetBtn = document.getElementById("resetLocalGame");
const flipBoardBtn = document.getElementById("flipBoardBtn");

let minutes = Number(localPage.dataset.minutes || 30);

if (minutes < 10) {
    minutes = 10;
}

let whiteTime = minutes * 60;
let blackTime = minutes * 60;

let currentTurn = "w";
let selected = null;
let legalMoves = [];
let gameOver = false;
let isBoardFlipped = false;

let lastMove = [];
let whiteCaptured = [];
let blackCaptured = [];

let moveHistoryForSave = [];
let historySaved = false;

let enPassantTarget = null;

let halfMoveClock = 0;
let positionHistory = {};
let pendingPromotionMove = null;

const promotionModal = document.getElementById("promotionModal");
const promotionChoices = document.querySelectorAll(".promotion-choice");

let castlingRights = {
    w: { kingSide: true, queenSide: true },
    b: { kingSide: true, queenSide: true }
};

const pieces = {
    r: "♜", n: "♞", b: "♝", q: "♛", k: "♚", p: "♟",
    R: "♖", N: "♘", B: "♗", Q: "♕", K: "♔", P: "♙"
};

let board = createStartBoard();

function createStartBoard() {
    return [
        ["r", "n", "b", "q", "k", "b", "n", "r"],
        ["p", "p", "p", "p", "p", "p", "p", "p"],
        ["", "", "", "", "", "", "", ""],
        ["", "", "", "", "", "", "", ""],
        ["", "", "", "", "", "", "", ""],
        ["", "", "", "", "", "", "", ""],
        ["P", "P", "P", "P", "P", "P", "P", "P"],
        ["R", "N", "B", "Q", "K", "B", "N", "R"]
    ];
}

function drawBoard() {
    boardElement.innerHTML = "";

    if (gameOver) {
        boardElement.classList.add("game-over-board");
    } else {
        boardElement.classList.remove("game-over-board");
    }

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            square.className = `local-square ${(row + col) % 2 === 0 ? "light" : "dark"}`;

            const piece = board[row][col];

            if (selected && selected.row === row && selected.col === col) {
                square.classList.add("selected");
            }

            if (lastMove.some(pos => pos.row === row && pos.col === col)) {
                square.classList.add("last-move");
            }

            const move = legalMoves.find(m => m.toRow === row && m.toCol === col);

            if (move) {
                if (move.capture || move.enPassant) {
                    square.classList.add("capture-hint");
                } else {
                    square.classList.add("hint");
                }
            }

            if (piece) {
                const pieceSpan = document.createElement("span");
                pieceSpan.className = "local-piece";
                pieceSpan.textContent = pieces[piece];
                square.appendChild(pieceSpan);
            }

            square.addEventListener("click", function () {
                handleClick(row, col);
            });

            boardElement.appendChild(square);
        }
    }
}

function handleClick(row, col) {
    if (gameOver || pendingPromotionMove) {
        return;
    }

    const piece = board[row][col];

    if (!selected) {
        selectPiece(row, col, piece);
        return;
    }

    const move = legalMoves.find(m => m.toRow === row && m.toCol === col);

    if (!move) {
        if (piece && isCurrentTurnPiece(piece)) {
            selectPiece(row, col, piece);
            return;
        }

        selected = null;
        legalMoves = [];
        drawBoard();
        return;
    }

    selected = null;
    legalMoves = [];

    if (isPromotionMove(move)) {
        pendingPromotionMove = move;
        promotionModal.classList.add("show");
        drawBoard();
        return;
    }

    makeMove(move);
    finishTurn();
}

function isPromotionMove(move) {
    const piece = board[move.fromRow][move.fromCol];

    if (piece === "P" && move.toRow === 0) {
        return true;
    }

    if (piece === "p" && move.toRow === 7) {
        return true;
    }

    return false;
}

promotionChoices.forEach(button => {
    button.addEventListener("click", function () {
        if (!pendingPromotionMove) return;

        const chosenPiece = this.dataset.piece;
        promotionModal.classList.remove("show");

        makeMove(pendingPromotionMove, chosenPiece);

        pendingPromotionMove = null;

        finishTurn();
    });
});

function getPromotionPiece(color, pieceType) {
    if (color === "w") {
        return pieceType.toUpperCase();
    }

    return pieceType.toLowerCase();
}

function selectPiece(row, col, piece) {
    if (!piece) {
        return;
    }

    if (!isCurrentTurnPiece(piece)) {
        statusEl.textContent = currentTurn === "w"
            ? "Đang lượt Trắng, không được chọn quân Đen!"
            : "Đang lượt Đen, không được chọn quân Trắng!";
        return;
    }

    selected = { row, col };
    legalMoves = getLegalMoves(row, col, true);

    if (legalMoves.length === 0) {
        statusEl.textContent = "Quân này hiện không có nước đi hợp lệ.";
    } else {
        statusEl.textContent = currentTurn === "w"
            ? "Trắng đã chọn quân. Hãy chọn ô để đi."
            : "Đen đã chọn quân. Hãy chọn ô để đi.";
    }

    drawBoard();
}

function makeMove(move, promotionPiece = null) {
    const piece = board[move.fromRow][move.fromCol];
    let capturedPiece = board[move.toRow][move.toCol];

    updateCastlingRightsBeforeMove(piece, move);

    if (move.enPassant) {
        const capturedPawnRow = move.fromRow;
        const capturedPawnCol = move.toCol;
        capturedPiece = board[capturedPawnRow][capturedPawnCol];
        board[capturedPawnRow][capturedPawnCol] = "";
    }

    if (capturedPiece) {
        addCapturedPiece(capturedPiece);
    }

    board[move.toRow][move.toCol] = piece;
    board[move.fromRow][move.fromCol] = "";

    if (move.castle === "kingSide") {
        if (currentTurn === "w") {
            board[7][5] = "R";
            board[7][7] = "";
        } else {
            board[0][5] = "r";
            board[0][7] = "";
        }
    }

    if (move.castle === "queenSide") {
        if (currentTurn === "w") {
            board[7][3] = "R";
            board[7][0] = "";
        } else {
            board[0][3] = "r";
            board[0][0] = "";
        }
    }

    if (piece.toLowerCase() === "p" && (move.toRow === 0 || move.toRow === 7)) {
        const color = getPieceColor(piece);
        const finalPiece = promotionPiece
            ? getPromotionPiece(color, promotionPiece)
            : getPromotionPiece(color, "q");

        board[move.toRow][move.toCol] = finalPiece;

        statusEl.textContent = color === "w"
            ? "Tốt Trắng đã phong cấp!"
            : "Tốt Đen đã phong cấp!";
    }

    if (piece.toLowerCase() === "p" && Math.abs(move.toRow - move.fromRow) === 2) {
        enPassantTarget = {
            row: (move.fromRow + move.toRow) / 2,
            col: move.fromCol,
            pawnRow: move.toRow,
            pawnCol: move.toCol
        };
    } else {
        enPassantTarget = null;
    }

    lastMove = [
        { row: move.fromRow, col: move.fromCol },
        { row: move.toRow, col: move.toCol }
    ];

    moveHistoryForSave.push(
        convertMoveToText(move.fromRow, move.fromCol, move.toRow, move.toCol)
    );

    if (piece.toLowerCase() === "p" || capturedPiece) {
        halfMoveClock = 0;
    } else {
        halfMoveClock++;
    }

    updateCapturedUI();
}

function finishTurn() {
    currentTurn = currentTurn === "w" ? "b" : "w";

    updateTurnUI();
    checkGameState();

    if (!gameOver) {
        checkDrawRules();
    }

    drawBoard();
}

function updateCastlingRightsBeforeMove(piece, move) {
    if (piece === "K") {
        castlingRights.w.kingSide = false;
        castlingRights.w.queenSide = false;
    }

    if (piece === "k") {
        castlingRights.b.kingSide = false;
        castlingRights.b.queenSide = false;
    }

    if (piece === "R" && move.fromRow === 7 && move.fromCol === 7) {
        castlingRights.w.kingSide = false;
    }

    if (piece === "R" && move.fromRow === 7 && move.fromCol === 0) {
        castlingRights.w.queenSide = false;
    }

    if (piece === "r" && move.fromRow === 0 && move.fromCol === 7) {
        castlingRights.b.kingSide = false;
    }

    if (piece === "r" && move.fromRow === 0 && move.fromCol === 0) {
        castlingRights.b.queenSide = false;
    }

    const capturedPiece = board[move.toRow][move.toCol];

    if (capturedPiece === "R" && move.toRow === 7 && move.toCol === 7) {
        castlingRights.w.kingSide = false;
    }

    if (capturedPiece === "R" && move.toRow === 7 && move.toCol === 0) {
        castlingRights.w.queenSide = false;
    }

    if (capturedPiece === "r" && move.toRow === 0 && move.toCol === 7) {
        castlingRights.b.kingSide = false;
    }

    if (capturedPiece === "r" && move.toRow === 0 && move.toCol === 0) {
        castlingRights.b.queenSide = false;
    }
}

function addCapturedPiece(piece) {
    if (piece === piece.toUpperCase()) {
        blackCaptured.push(piece);
    } else {
        whiteCaptured.push(piece);
    }
}

function updateCapturedUI() {
    whiteCapturedEl.textContent = "Ăn: " + formatCapturedPieces(whiteCaptured);
    blackCapturedEl.textContent = "Ăn: " + formatCapturedPieces(blackCaptured);
}

function formatCapturedPieces(list) {
    if (!list || list.length === 0) {
        return "chưa có";
    }

    return list.map(p => pieces[p]).join(" ");
}

function isCurrentTurnPiece(piece) {
    if (!piece) return false;

    if (currentTurn === "w") {
        return piece === piece.toUpperCase();
    }

    return piece === piece.toLowerCase();
}

function getPieceColor(piece) {
    if (!piece) return null;
    return piece === piece.toUpperCase() ? "w" : "b";
}

function isFriendly(piece, color = currentTurn) {
    if (!piece) return false;
    return getPieceColor(piece) === color;
}

function isEnemy(piece, color = currentTurn) {
    if (!piece) return false;
    return getPieceColor(piece) !== color;
}

function getLegalMoves(row, col, filterCheck = true) {
    const piece = board[row][col];

    if (!piece) return [];

    const color = getPieceColor(piece);
    const rawMoves = getRawMoves(row, col, color);

    if (!filterCheck) {
        return rawMoves;
    }

    return rawMoves.filter(move => !wouldLeaveKingInCheck(move, color));
}

function getRawMoves(row, col, color) {
    const piece = board[row][col];

    if (!piece) return [];

    const lower = piece.toLowerCase();

    if (lower === "p") return pawnMoves(row, col, piece, color);
    if (lower === "r") return lineMoves(row, col, color, [[1, 0], [-1, 0], [0, 1], [0, -1]]);
    if (lower === "b") return lineMoves(row, col, color, [[1, 1], [1, -1], [-1, 1], [-1, -1]]);
    if (lower === "q") return lineMoves(row, col, color, [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]]);
    if (lower === "n") return knightMoves(row, col, color);
    if (lower === "k") return kingMoves(row, col, color);

    return [];
}

function pawnMoves(row, col, piece, color) {
    const moves = [];
    const dir = color === "w" ? -1 : 1;
    const startRow = color === "w" ? 6 : 1;

    const oneRow = row + dir;

    if (inside(oneRow, col) && board[oneRow][col] === "") {
        moves.push(createMove(row, col, oneRow, col));

        const twoRow = row + dir * 2;

        if (row === startRow && inside(twoRow, col) && board[twoRow][col] === "") {
            moves.push(createMove(row, col, twoRow, col));
        }
    }

    for (const dc of [-1, 1]) {
        const nr = row + dir;
        const nc = col + dc;

        if (inside(nr, nc) && isEnemy(board[nr][nc], color)) {
            moves.push(createMove(row, col, nr, nc, true));
        }
    }

    if (enPassantTarget) {
        if (row === enPassantTarget.pawnRow && Math.abs(col - enPassantTarget.pawnCol) === 1) {
            if (oneRow === enPassantTarget.row && enPassantTarget.col === enPassantTarget.pawnCol) {
                moves.push({
                    fromRow: row,
                    fromCol: col,
                    toRow: enPassantTarget.row,
                    toCol: enPassantTarget.col,
                    capture: true,
                    enPassant: true
                });
            }
        }
    }

    return moves;
}

function lineMoves(row, col, color, directions) {
    const moves = [];

    for (const [dr, dc] of directions) {
        let nr = row + dr;
        let nc = col + dc;

        while (inside(nr, nc)) {
            const target = board[nr][nc];

            if (!target) {
                moves.push(createMove(row, col, nr, nc));
            } else {
                if (isEnemy(target, color)) {
                    moves.push(createMove(row, col, nr, nc, true));
                }

                break;
            }

            nr += dr;
            nc += dc;
        }
    }

    return moves;
}

function knightMoves(row, col, color) {
    const jumps = [
        [-2, -1], [-2, 1],
        [-1, -2], [-1, 2],
        [1, -2], [1, 2],
        [2, -1], [2, 1]
    ];

    return jumps
        .map(([dr, dc]) => {
            const nr = row + dr;
            const nc = col + dc;

            if (!inside(nr, nc)) return null;
            if (isFriendly(board[nr][nc], color)) return null;

            return createMove(row, col, nr, nc, isEnemy(board[nr][nc], color));
        })
        .filter(Boolean);
}

function kingMoves(row, col, color) {
    const moves = [];

    for (let dr = -1; dr <= 1; dr++) {
        for (let dc = -1; dc <= 1; dc++) {
            if (dr === 0 && dc === 0) continue;

            const nr = row + dr;
            const nc = col + dc;

            if (!inside(nr, nc)) continue;
            if (isFriendly(board[nr][nc], color)) continue;

            moves.push(createMove(row, col, nr, nc, isEnemy(board[nr][nc], color)));
        }
    }

    moves.push(...castlingMoves(row, col, color));

    return moves;
}

function castlingMoves(row, col, color) {
    const moves = [];

    if (color === "w") {
        if (row !== 7 || col !== 4) return moves;
    } else {
        if (row !== 0 || col !== 4) return moves;
    }

    if (isKingInCheck(color)) {
        return moves;
    }

    const enemyColor = color === "w" ? "b" : "w";
    const homeRow = color === "w" ? 7 : 0;

    if (castlingRights[color].kingSide) {
        if (
            board[homeRow][5] === "" &&
            board[homeRow][6] === "" &&
            board[homeRow][7].toLowerCase() === "r" &&
            !isSquareAttacked(homeRow, 5, enemyColor) &&
            !isSquareAttacked(homeRow, 6, enemyColor)
        ) {
            moves.push({
                fromRow: row,
                fromCol: col,
                toRow: homeRow,
                toCol: 6,
                castle: "kingSide"
            });
        }
    }

    if (castlingRights[color].queenSide) {
        if (
            board[homeRow][1] === "" &&
            board[homeRow][2] === "" &&
            board[homeRow][3] === "" &&
            board[homeRow][0].toLowerCase() === "r" &&
            !isSquareAttacked(homeRow, 3, enemyColor) &&
            !isSquareAttacked(homeRow, 2, enemyColor)
        ) {
            moves.push({
                fromRow: row,
                fromCol: col,
                toRow: homeRow,
                toCol: 2,
                castle: "queenSide"
            });
        }
    }

    return moves;
}

function createMove(fromRow, fromCol, toRow, toCol, capture = false) {
    return {
        fromRow,
        fromCol,
        toRow,
        toCol,
        capture
    };
}

function wouldLeaveKingInCheck(move, color) {
    const snapshot = cloneBoard(board);
    const oldEnPassant = enPassantTarget ? { ...enPassantTarget } : null;

    applyMoveToBoardOnly(move);

    const result = isKingInCheck(color);

    board = snapshot;
    enPassantTarget = oldEnPassant;

    return result;
}

function applyMoveToBoardOnly(move) {
    const piece = board[move.fromRow][move.fromCol];

    if (move.enPassant) {
        board[move.fromRow][move.toCol] = "";
    }

    board[move.toRow][move.toCol] = piece;
    board[move.fromRow][move.fromCol] = "";

    if (move.castle === "kingSide") {
        if (piece === "K") {
            board[7][5] = "R";
            board[7][7] = "";
        } else {
            board[0][5] = "r";
            board[0][7] = "";
        }
    }

    if (move.castle === "queenSide") {
        if (piece === "K") {
            board[7][3] = "R";
            board[7][0] = "";
        } else {
            board[0][3] = "r";
            board[0][0] = "";
        }
    }
}

function cloneBoard(sourceBoard) {
    return sourceBoard.map(row => [...row]);
}

function isKingInCheck(color) {
    const king = color === "w" ? "K" : "k";
    const enemyColor = color === "w" ? "b" : "w";

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            if (board[row][col] === king) {
                return isSquareAttacked(row, col, enemyColor);
            }
        }
    }

    return true;
}

function isSquareAttacked(row, col, attackerColor) {
    return isAttackedByPawn(row, col, attackerColor) ||
        isAttackedByKnight(row, col, attackerColor) ||
        isAttackedByLinePiece(row, col, attackerColor) ||
        isAttackedByKing(row, col, attackerColor);
}

function isAttackedByPawn(row, col, attackerColor) {
    const dir = attackerColor === "w" ? -1 : 1;
    const pawn = attackerColor === "w" ? "P" : "p";

    const attackingRows = [
        { r: row - dir, c: col - 1 },
        { r: row - dir, c: col + 1 }
    ];

    return attackingRows.some(pos =>
        inside(pos.r, pos.c) && board[pos.r][pos.c] === pawn
    );
}

function isAttackedByKnight(row, col, attackerColor) {
    const knight = attackerColor === "w" ? "N" : "n";
    const jumps = [
        [-2, -1], [-2, 1],
        [-1, -2], [-1, 2],
        [1, -2], [1, 2],
        [2, -1], [2, 1]
    ];

    return jumps.some(([dr, dc]) => {
        const nr = row + dr;
        const nc = col + dc;
        return inside(nr, nc) && board[nr][nc] === knight;
    });
}

function isAttackedByLinePiece(row, col, attackerColor) {
    const rook = attackerColor === "w" ? "R" : "r";
    const bishop = attackerColor === "w" ? "B" : "b";
    const queen = attackerColor === "w" ? "Q" : "q";

    const rookDirections = [[1, 0], [-1, 0], [0, 1], [0, -1]];
    const bishopDirections = [[1, 1], [1, -1], [-1, 1], [-1, -1]];

    for (const [dr, dc] of rookDirections) {
        let nr = row + dr;
        let nc = col + dc;

        while (inside(nr, nc)) {
            const piece = board[nr][nc];

            if (piece) {
                if (piece === rook || piece === queen) return true;
                break;
            }

            nr += dr;
            nc += dc;
        }
    }

    for (const [dr, dc] of bishopDirections) {
        let nr = row + dr;
        let nc = col + dc;

        while (inside(nr, nc)) {
            const piece = board[nr][nc];

            if (piece) {
                if (piece === bishop || piece === queen) return true;
                break;
            }

            nr += dr;
            nc += dc;
        }
    }

    return false;
}

function isAttackedByKing(row, col, attackerColor) {
    const king = attackerColor === "w" ? "K" : "k";

    for (let dr = -1; dr <= 1; dr++) {
        for (let dc = -1; dc <= 1; dc++) {
            if (dr === 0 && dc === 0) continue;

            const nr = row + dr;
            const nc = col + dc;

            if (inside(nr, nc) && board[nr][nc] === king) {
                return true;
            }
        }
    }

    return false;
}

function hasAnyLegalMove(color) {
    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const piece = board[row][col];

            if (piece && getPieceColor(piece) === color) {
                const moves = getLegalMoves(row, col, true);

                if (moves.length > 0) {
                    return true;
                }
            }
        }
    }

    return false;
}

function checkGameState() {
    const inCheck = isKingInCheck(currentTurn);
    const hasMove = hasAnyLegalMove(currentTurn);

    if (inCheck && !hasMove) {
        gameOver = true;

        const result = currentTurn === "w" ? "BLACK_WIN" : "WHITE_WIN";

        statusEl.textContent = currentTurn === "w"
            ? "Chiếu hết! Đen thắng."
            : "Chiếu hết! Trắng thắng.";

        saveGameHistory(result);
        drawBoard();
        return;
    }

    if (!inCheck && !hasMove) {
        gameOver = true;
        statusEl.textContent = "Hòa! Bên tới lượt không còn nước đi hợp lệ.";

        saveGameHistory("DRAW");
        drawBoard();
        return;
    }

    if (inCheck) {
        statusEl.textContent = currentTurn === "w"
            ? "Trắng đang bị chiếu!"
            : "Đen đang bị chiếu!";
    }
}

function checkDrawRules() {
    if (halfMoveClock >= 100) {
        gameOver = true;
        statusEl.textContent = "Hòa! 50 nước mỗi bên không ăn quân và không đi Tốt.";

        saveGameHistory("DRAW");
        drawBoard();
        return;
    }

    const key = getPositionKey();
    positionHistory[key] = (positionHistory[key] || 0) + 1;

    if (positionHistory[key] >= 3) {
        gameOver = true;
        statusEl.textContent = "Hòa! Một thế cờ đã lặp lại 3 lần.";

        saveGameHistory("DRAW");
        drawBoard();
    }
}

function getPositionKey() {
    const boardKey = board.map(row => row.join("")).join("/");

    const castleKey =
        `${castlingRights.w.kingSide ? "K" : ""}` +
        `${castlingRights.w.queenSide ? "Q" : ""}` +
        `${castlingRights.b.kingSide ? "k" : ""}` +
        `${castlingRights.b.queenSide ? "q" : ""}`;

    const enPassantKey = enPassantTarget
        ? `${enPassantTarget.row},${enPassantTarget.col}`
        : "-";

    return `${boardKey} ${currentTurn} ${castleKey || "-"} ${enPassantKey}`;
}

function inside(row, col) {
    return row >= 0 && row < 8 && col >= 0 && col < 8;
}

function updateTurnUI() {
    whiteClockBox.classList.toggle("active", currentTurn === "w");
    blackClockBox.classList.toggle("active", currentTurn === "b");

    if (!gameOver) {
        statusEl.textContent = currentTurn === "w" ? "Lượt Trắng" : "Lượt Đen";
    }
}

function formatTime(seconds) {
    const m = Math.floor(seconds / 60).toString().padStart(2, "0");
    const s = (seconds % 60).toString().padStart(2, "0");
    return `${m}:${s}`;
}

function updateClocks() {
    whiteTimeEl.textContent = formatTime(whiteTime);
    blackTimeEl.textContent = formatTime(blackTime);
}

function checkTimeWinner() {
    if (whiteTime > blackTime) {
        statusEl.textContent = "Hết giờ! Trắng thắng vì còn nhiều thời gian hơn.";
    } else if (blackTime > whiteTime) {
        statusEl.textContent = "Hết giờ! Đen thắng vì còn nhiều thời gian hơn.";
    } else {
        statusEl.textContent = "Hết giờ! Hai bên hòa vì còn thời gian bằng nhau.";
    }

    gameOver = true;

    if (whiteTime > blackTime) {
        saveGameHistory("WHITE_WIN");
    } else if (blackTime > whiteTime) {
        saveGameHistory("BLACK_WIN");
    } else {
        saveGameHistory("DRAW");
    }

    drawBoard();
}

setInterval(function () {
    if (gameOver) return;

    if (currentTurn === "w") {
        whiteTime--;
    } else {
        blackTime--;
    }

    if (whiteTime <= 0 || blackTime <= 0) {
        whiteTime = Math.max(0, whiteTime);
        blackTime = Math.max(0, blackTime);
        updateClocks();
        checkTimeWinner();
        return;
    }

    updateClocks();
}, 1000);

resetBtn.addEventListener("click", function () {
    board = createStartBoard();

    whiteTime = minutes * 60;
    blackTime = minutes * 60;

    currentTurn = "w";
    selected = null;
    legalMoves = [];
    gameOver = false;
    lastMove = [];
    whiteCaptured = [];
    blackCaptured = [];

    moveHistoryForSave = [];
    historySaved = false;

    enPassantTarget = null;

    halfMoveClock = 0;
    positionHistory = {};
    pendingPromotionMove = null;

    if (promotionModal) {
        promotionModal.classList.remove("show");
    }

    castlingRights = {
        w: { kingSide: true, queenSide: true },
        b: { kingSide: true, queenSide: true }
    };

    updateClocks();
    updateCapturedUI();
    updateTurnUI();
    recordStartPosition();
    drawBoard();
});

flipBoardBtn.addEventListener("click", function () {
    isBoardFlipped = !isBoardFlipped;
    boardElement.classList.toggle("flipped", isBoardFlipped);
});

function recordStartPosition() {
    const key = getPositionKey();
    positionHistory[key] = 1;
}

function convertMoveToText(fromRow, fromCol, toRow, toCol) {
    const files = ["a", "b", "c", "d", "e", "f", "g", "h"];

    const from = files[fromCol] + (8 - fromRow);
    const to = files[toCol] + (8 - toRow);

    return from + to;
}

function saveGameHistory(result) {
    if (historySaved) {
        return;
    }

    if (!moveHistoryForSave || moveHistoryForSave.length === 0) {
        console.log("Không có nước đi để lưu.");
        return;
    }

    historySaved = true;

    fetch("/Play/SaveGameHistory", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            modeName: "Hai Người Một Máy",
            result: result,
            fen: getSimpleFen(),
            moves: moveHistoryForSave
        })
    })
        .then(res => res.json())
        .then(data => {
            console.log("Lưu lịch sử:", data);

            if (data.success) {
                let title = "Trận đấu kết thúc";
                let message = "Kết quả đã được lưu.";
                let score = "";

                if (result === "WHITE_WIN") {
                    title = "Chúc mừng!";
                    message = "Quân trắng đã chiến thắng!";
                    score = "+15 điểm";
                } else if (result === "BLACK_WIN") {
                    title = "Chiến thắng!";
                    message = "Quân đen đã chiến thắng!";
                    score = "+15 điểm";
                } else {
                    title = "Ván cờ hòa!";
                    message = "Hai bên bất phân thắng bại.";
                    score = "+5 điểm";
                }

                showGameResultModal({
                    icon: result === "DRAW" ? "🤝" : "🏆",
                    title: title,
                    message: message,
                    score: score
                });
            }
        })
        .catch(error => {
            console.error("Lỗi lưu lịch sử:", error);
        });
}

function getSimpleFen() {
    return "8/8/8/8/8/8/8/8 w - - 0 1";
}

updateClocks();
updateCapturedUI();
updateTurnUI();
recordStartPosition();
drawBoard();