
Copy

/**
 * chessAI.js — Kết nối Frontend với Chess AI API (Python/Flask)
 * Dự án: Web-C-Vua
 * 
 * Dùng trong TrangChinh.js:
 *   import ChessAI from './chessAI.js';
 *   const ai = new ChessAI('medium');
 */
 
const API_BASE = "http://localhost:5000/api";
 
class ChessAI {
    /**
     * @param {"easy"|"medium"|"hard"} level - Cấp độ AI
     */
    constructor(level = "medium") {
        this.level = level;
    }
 
    /**
     * Yêu cầu AI đi nước tiếp theo.
     * @param {string} fen - Trạng thái bàn cờ (FEN string)
     * @returns {Promise<{move, move_san, fen_after, score, is_checkmate, is_game_over}>}
     */
    async getMove(fen) {
        try {
            const res = await fetch(`${API_BASE}/move`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fen, level: this.level }),
            });
            if (!res.ok) throw new Error(`API lỗi: ${res.status}`);
            return await res.json();
        } catch (err) {
            console.error("Chess AI API lỗi:", err);
            return null;
        }
    }
 
    /**
     * Kiểm tra nước đi của người chơi.
     * @param {string} fen   - FEN trước khi đi
     * @param {string} move  - Nước đi (UCI, vd: "e2e4")
     * @returns {Promise<{valid, fen_after, is_check, is_checkmate, is_game_over}>}
     */
    async validateMove(fen, move) {
        try {
            const res = await fetch(`${API_BASE}/validate`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fen, move }),
            });
            return await res.json();
        } catch (err) {
            console.error("Validate move lỗi:", err);
            return { valid: false };
        }
    }
 
    /**
     * Lấy danh sách nước đi hợp lệ cho 1 ô.
     * @param {string} fen    - FEN hiện tại
     * @param {string} square - Ô cờ (vd: "e2")
     * @returns {Promise<string[]>} - Mảng các nước đi UCI (vd: ["e2e4", "e2e3"])
     */
    async getLegalMoves(fen, square) {
        try {
            const res = await fetch(`${API_BASE}/legal-moves`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fen, square }),
            });
            const data = await res.json();
            return data.legal_moves || [];
        } catch (err) {
            console.error("Legal moves lỗi:", err);
            return [];
        }
    }
 
    /** Đổi cấp độ: "easy" | "medium" | "hard" */
    setLevel(level) {
        this.level = level;
        console.log(`Chess AI cấp độ: ${level}`);
    }
}
 
// ─── Ví dụ tích hợp vào game loop ───────────────
//
// const ai = new ChessAI("medium");
//
// // Khi người chơi đi xong:
// async function onPlayerMove(fen) {
//     const result = await ai.getMove(fen);
//     if (result && result.move) {
//         applyMoveToBoard(result.move);       // Áp dụng nước đi lên bàn cờ
//         if (result.is_checkmate) showWin();  // Kết thúc ván
//     }
// }
//
// // Khi người chơi chọn quân:
// async function onSquareClick(fen, square) {
//     const moves = await ai.getLegalMoves(fen, square);
//     highlightSquares(moves);  // Highlight các ô hợp lệ
// }
 
export default ChessAI;