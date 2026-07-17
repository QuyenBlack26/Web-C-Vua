/* KẾT NỐI FRONTEND VỚI CHESS AI API PYTHON/FLASK */
const API_BASE = "http://localhost:5000/api";

/* CLASS QUẢN LÝ CHESS AI */
class ChessAI {
    constructor(level = "medium") {
        this.level = level;
    }

    /* GỬI FEN LÊN API ĐỂ AI TÍNH NƯỚC ĐI */
    async getMove(fen, fenHistory = [], lastAiMove = null) {
        try {
            const res = await fetch(`${API_BASE}/move`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    fen: fen,
                    level: this.level,
                    fen_history: fenHistory,
                    last_ai_move: lastAiMove
                }),
            });

            const data = await res.json();

            if (!res.ok) {
                console.error("API /move lỗi:", data);
                return {
                    move: null,
                    error: data.error || `API lỗi ${res.status}`,
                    raw: data
                };
            }

            return data;
        } catch (err) {
            console.error("Chess AI API lỗi:", err);
            return {
                move: null,
                error: err.message
            };
        }
    }

    /* KIỂM TRA NƯỚC ĐI CỦA NGƯỜI CHƠI */
    async validateMove(fen, move) {
        try {
            const res = await fetch(`${API_BASE}/validate`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    fen: fen,
                    move: move
                }),
            });

            const data = await res.json();

            if (!res.ok) {
                console.error("API /validate lỗi:", data);
                return {
                    valid: false,
                    error: data.error || `API lỗi ${res.status}`,
                    raw: data
                };
            }

            return data;
        } catch (err) {
            console.error("Validate move lỗi:", err);
            return {
                valid: false,
                error: err.message
            };
        }
    }

    /* LẤY DANH SÁCH NƯỚC ĐI HỢP LỆ */
    async getLegalMoves(fen, square) {
        try {
            const res = await fetch(`${API_BASE}/legal-moves`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    fen: fen,
                    square: square
                }),
            });

            const data = await res.json();

            if (!res.ok) {
                console.error("API /legal-moves lỗi:", data);
                return [];
            }

            return data.legal_moves || [];
        } catch (err) {
            console.error("Legal moves lỗi:", err);
            return [];
        }
    }

    /* ĐỔI CẤP ĐỘ AI */
    setLevel(level) {
        this.level = level;
        console.log(`Chess AI cấp độ: ${level}`);
    }
}

/* XUẤT CLASS CHESS AI */
export default ChessAI;

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
        } else {
            console.error("Lỗi lưu ván:", data.message);
            alert(data.message);
        }
    } catch (error) {
        console.error("Lỗi gọi SaveGameHistory:", error);
        alert("Không thể lưu ván cờ và cộng điểm.");
    }
}