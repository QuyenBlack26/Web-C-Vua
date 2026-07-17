let selectedVanCoId = null;
let currentMoves = [];
let currentAnalysis = null;
let selectedMoveIndex = 0;

const pieceIcons = {
    r: "♜", n: "♞", b: "♝", q: "♛", k: "♚", p: "♟",
    R: "♖", N: "♘", B: "♗", Q: "♕", K: "♔", P: "♙"
};

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

function squareToPos(square) {
    const files = "abcdefgh";
    const col = files.indexOf(square[0]);
    const row = 8 - Number(square[1]);

    return { row, col };
}

function cloneBoard(board) {
    return board.map(row => [...row]);
}

function applyMove(board, moveText) {
    if (!moveText || moveText.length < 4) {
        return board;
    }

    const from = moveText.substring(0, 2);
    const to = moveText.substring(2, 4);
    const promotion = moveText.length >= 5 ? moveText[4] : null;

    const fromPos = squareToPos(from);
    const toPos = squareToPos(to);

    if (
        fromPos.row < 0 || fromPos.row > 7 ||
        fromPos.col < 0 || fromPos.col > 7 ||
        toPos.row < 0 || toPos.row > 7 ||
        toPos.col < 0 || toPos.col > 7
    ) {
        return board;
    }

    let piece = board[fromPos.row][fromPos.col];

    if (!piece) {
        return board;
    }

    // Nhập thành
    if (piece === "K" && from === "e1" && to === "g1") {
        board[7][6] = "K";
        board[7][5] = "R";
        board[7][4] = "";
        board[7][7] = "";
        return board;
    }

    if (piece === "K" && from === "e1" && to === "c1") {
        board[7][2] = "K";
        board[7][3] = "R";
        board[7][4] = "";
        board[7][0] = "";
        return board;
    }

    if (piece === "k" && from === "e8" && to === "g8") {
        board[0][6] = "k";
        board[0][5] = "r";
        board[0][4] = "";
        board[0][7] = "";
        return board;
    }

    if (piece === "k" && from === "e8" && to === "c8") {
        board[0][2] = "k";
        board[0][3] = "r";
        board[0][4] = "";
        board[0][0] = "";
        return board;
    }

    // Phong cấp
    if (promotion) {
        if (piece === "P") {
            piece = promotion.toUpperCase();
        } else if (piece === "p") {
            piece = promotion.toLowerCase();
        }
    }

    board[toPos.row][toPos.col] = piece;
    board[fromPos.row][fromPos.col] = "";

    return board;
}

function getBoardAtMove(moveIndex) {
    let board = createStartBoard();

    for (let i = 0; i < moveIndex; i++) {
        const move = currentMoves[i];

        if (move) {
            board = applyMove(board, move.nuoc);
        }
    }

    return board;
}

function renderBoard(moveIndex = 0) {
    const boardEl = document.getElementById("reviewBoard");

    if (!boardEl) {
        return;
    }

    const board = getBoardAtMove(moveIndex);
    boardEl.innerHTML = "";

    let lastMove = null;

    if (moveIndex > 0 && currentMoves[moveIndex - 1]) {
        lastMove = currentMoves[moveIndex - 1].nuoc;
    }

    let fromSquare = "";
    let toSquare = "";

    if (lastMove && lastMove.length >= 4) {
        fromSquare = lastMove.substring(0, 2);
        toSquare = lastMove.substring(2, 4);
    }

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            const squareName = "abcdefgh"[col] + (8 - row);
            const piece = board[row][col];

            square.className = `review-square ${(row + col) % 2 === 0 ? "light" : "dark"}`;

            if (squareName === fromSquare || squareName === toSquare) {
                square.classList.add("last-move");
            }

            if (piece) {
                square.textContent = pieceIcons[piece] || "";
            }

            boardEl.appendChild(square);
        }
    }
}

async function loadMoves(vanCoId) {
    selectedVanCoId = vanCoId;
    currentMoves = [];
    currentAnalysis = null;
    selectedMoveIndex = 0;

    const reviewTitle = document.getElementById("reviewTitle");
    const reviewSubTitle = document.getElementById("reviewSubTitle");
    const movesBox = document.getElementById("movesBox");
    const btnAnalyzeGame = document.getElementById("btnAnalyzeGame");
    const analysisResult = document.getElementById("analysisResult");

    if (btnAnalyzeGame) {
        btnAnalyzeGame.disabled = true;
    }

    if (analysisResult) {
        analysisResult.innerHTML = "Đang tải ván cờ...";
    }

    reviewTitle.textContent = "Xem lại ván #" + vanCoId;
    reviewSubTitle.textContent = "Đang tải danh sách nước đi...";

    movesBox.innerHTML = `
        <div class="moves-empty">
            Đang tải nước đi...
        </div>
    `;

    renderBoard(0);

    try {
        const res = await fetch(`/Play/AnalysisMoves?vanCoId=${vanCoId}`);
        const data = await res.json();

        if (!data.success) {
            movesBox.innerHTML = `
                <div class="moves-empty error">
                    ${data.message || "Không tải được nước đi."}
                </div>
            `;
            return;
        }

        currentMoves = (data.moves || []).map(move => {
            return {
                soThuTu: move.soThuTu ?? move.SoThuTu,
                nuoc: move.nuoc ?? move.Nuoc
            };
        });

        reviewTitle.textContent = "Xem lại ván #" + vanCoId;
        reviewSubTitle.textContent = "Tổng cộng " + currentMoves.length + " nước đi.";

        if (currentMoves.length === 0) {
            movesBox.innerHTML = `
                <div class="moves-empty">
                    Ván này chưa có nước đi nào được lưu.
                </div>
            `;

            if (analysisResult) {
                analysisResult.innerHTML = "Ván này chưa có nước đi để phân tích.";
            }

            return;
        }

        if (btnAnalyzeGame) {
            btnAnalyzeGame.disabled = false;
        }

        renderMoveList();
        renderBoard(0);

        if (analysisResult) {
            analysisResult.innerHTML = "Bấm vào một nước đi để xem bàn cờ đến nước đó. Sau đó bấm Phân tích ván này để xem đánh giá.";
        }
    } catch (error) {
        console.error("Lỗi tải nước đi:", error);

        movesBox.innerHTML = `
            <div class="moves-empty error">
                Lỗi kết nối khi tải nước đi.
            </div>
        `;
    }
}

function renderMoveList() {
    const movesBox = document.getElementById("movesBox");

    if (!movesBox) {
        return;
    }

    if (!currentMoves || currentMoves.length === 0) {
        movesBox.innerHTML = `
            <div class="moves-empty">
                Chưa có nước đi nào.
            </div>
        `;
        return;
    }

    let html = `<div class="review-move-list">`;

    currentMoves.forEach((move, index) => {
        const moveIndex = index + 1;
        const activeClass = selectedMoveIndex === moveIndex ? "active" : "";

        const analysis = currentAnalysis?.moves?.find(x =>
            Number(x.soThuTu) === Number(move.soThuTu)
        );

        let label = "";

        if (analysis) {
            label = `<span class="move-label ${analysis.danhGia}">${analysis.danhGia}</span>`;
        }

        html += `
            <button type="button"
                    class="review-move-row ${activeClass}"
                    onclick="selectMove(${moveIndex})">
                <span class="move-step">Bước ${move.soThuTu}</span>
                <strong class="move-code">${move.nuoc}</strong>
                ${label}
            </button>
        `;
    });

    html += `</div>`;

    movesBox.innerHTML = html;
}

function selectMove(moveIndex) {
    selectedMoveIndex = moveIndex;

    renderBoard(moveIndex);
    renderMoveList();
    renderSelectedMoveAnalysis();
}

async function analyzeSelectedGame() {
    const btnAnalyzeGame = document.getElementById("btnAnalyzeGame");
    const analysisResult = document.getElementById("analysisResult");

    if (!selectedVanCoId) {
        analysisResult.innerHTML = `
            <div class="analysis-error">
                Bạn cần chọn một ván cờ trước.
            </div>
        `;
        return;
    }

    btnAnalyzeGame.disabled = true;
    btnAnalyzeGame.textContent = "Đang phân tích...";

    analysisResult.innerHTML = `
        <div class="analysis-loading">
            AI đang phân tích ván #${selectedVanCoId}...
        </div>
    `;

    try {
        const res = await fetch("/Play/AnalyzeGame", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                vanCoId: selectedVanCoId
            })
        });

        const data = await res.json();

        if (!data.success) {
            analysisResult.innerHTML = `
                <div class="analysis-error">
                    ${data.message || "Không phân tích được ván cờ."}
                </div>
            `;
            return;
        }

        currentAnalysis = data;

        renderMoveList();

        if (selectedMoveIndex > 0) {
            renderSelectedMoveAnalysis();
        } else {
            renderSummaryAnalysis();
        }
    } catch (error) {
        console.error("Lỗi phân tích AI:", error);

        analysisResult.innerHTML = `
            <div class="analysis-error">
                Lỗi kết nối khi gọi AI phân tích.
            </div>
        `;
    } finally {
        btnAnalyzeGame.disabled = false;
        btnAnalyzeGame.textContent = "Phân tích ván này";
    }
}

function renderSummaryAnalysis() {
    const analysisResult = document.getElementById("analysisResult");

    if (!currentAnalysis) {
        return;
    }

    const summary = currentAnalysis.summary || {};

    analysisResult.innerHTML = `
        <div class="analysis-summary-grid">
            <div class="mini-result best">
                <strong>${summary.BEST || 0}</strong>
                <span>Rất tốt</span>
            </div>

            <div class="mini-result good">
                <strong>${summary.GOOD || 0}</strong>
                <span>Tốt</span>
            </div>

            <div class="mini-result inaccuracy">
                <strong>${summary.INACCURACY || 0}</strong>
                <span>Thiếu chính xác</span>
            </div>

            <div class="mini-result mistake">
                <strong>${summary.MISTAKE || 0}</strong>
                <span>Sai lầm</span>
            </div>

            <div class="mini-result blunder">
                <strong>${summary.BLUNDER || 0}</strong>
                <span>Lỗi nặng</span>
            </div>

            <div class="mini-result invalid">
                <strong>${summary.INVALID || 0}</strong>
                <span>Không hợp lệ</span>
            </div>
        </div>

        <div class="analysis-advice-real">
            <h4>Gợi ý tổng quát</h4>
            <p>${currentAnalysis.advice || "Chưa có gợi ý."}</p>
        </div>
    `;
}

function renderSelectedMoveAnalysis() {
    const analysisResult = document.getElementById("analysisResult");

    if (!selectedMoveIndex || selectedMoveIndex <= 0) {
        renderSummaryAnalysis();
        return;
    }

    const move = currentMoves[selectedMoveIndex - 1];

    if (!move) {
        analysisResult.innerHTML = "Không tìm thấy nước đang chọn.";
        return;
    }

    if (!currentAnalysis) {
        analysisResult.innerHTML = `
            <div class="selected-move-info">
                <h4>Nước ${move.soThuTu}: ${move.nuoc}</h4>
                <p>Bàn cờ đang hiển thị trạng thái sau nước này.</p>
                <p>Bấm <b>Phân tích ván này</b> để xem AI đánh giá nước này.</p>
            </div>
        `;
        return;
    }

    const analyzedMove = currentAnalysis.moves.find(x => Number(x.soThuTu) === Number(move.soThuTu));

    if (!analyzedMove) {
        analysisResult.innerHTML = `
            <div class="analysis-error">
                Không tìm thấy đánh giá cho nước ${move.soThuTu}.
            </div>
        `;
        return;
    }

    analysisResult.innerHTML = `
        <div class="selected-move-analysis ${analyzedMove.danhGia}">
            <h4>Nước ${analyzedMove.soThuTu}: ${analyzedMove.nuoc}</h4>

            <div class="selected-rating">
                <strong>${analyzedMove.danhGia}</strong>
                <span>Chênh lệch: ${analyzedMove.chenhLech}</span>
            </div>

            <p>${analyzedMove.nhanXet}</p>

            <div class="score-line">
                <span>Điểm trước: ${analyzedMove.diemTruoc}</span>
                <span>Điểm sau: ${analyzedMove.diemSau}</span>
            </div>
        </div>
    `;
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

window.loadMoves = loadMoves;
window.selectMove = selectMove;
window.analyzeSelectedGame = analyzeSelectedGame;

document.addEventListener("DOMContentLoaded", function () {
    renderBoard(0);
});