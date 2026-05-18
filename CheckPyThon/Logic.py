#!/usr/bin/env python3
"""
Chess AI - Minimax + Alpha-Beta Pruning
3 Cấp độ: Dễ | Trung bình | Khó
Tác giả: Chess AI Project
"""

import chess
import math
import random
import tkinter as tk
from tkinter import ttk, messagebox
import threading

# ═══════════════════════════════════════════════════
#                   AI ENGINE
# ═══════════════════════════════════════════════════

PIECE_VALUES = {
    chess.PAWN:   100,
    chess.KNIGHT: 320,
    chess.BISHOP: 330,
    chess.ROOK:   500,
    chess.QUEEN:  900,
    chess.KING:  20000,
}

# Piece-Square Tables (từ góc nhìn của Đen, được flip cho Trắng)
PAWN_TABLE = [
     0,  0,  0,  0,  0,  0,  0,  0,
    50, 50, 50, 50, 50, 50, 50, 50,
    10, 10, 20, 30, 30, 20, 10, 10,
     5,  5, 10, 25, 25, 10,  5,  5,
     0,  0,  0, 20, 20,  0,  0,  0,
     5, -5,-10,  0,  0,-10, -5,  5,
     5, 10, 10,-20,-20, 10, 10,  5,
     0,  0,  0,  0,  0,  0,  0,  0,
]
KNIGHT_TABLE = [
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,  0,  0,  0,  0,-20,-40,
    -30,  0, 10, 15, 15, 10,  0,-30,
    -30,  5, 15, 20, 20, 15,  5,-30,
    -30,  0, 15, 20, 20, 15,  0,-30,
    -30,  5, 10, 15, 15, 10,  5,-30,
    -40,-20,  0,  5,  5,  0,-20,-40,
    -50,-40,-30,-30,-30,-30,-40,-50,
]
BISHOP_TABLE = [
    -20,-10,-10,-10,-10,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5, 10, 10,  5,  0,-10,
    -10,  5,  5, 10, 10,  5,  5,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10, 10, 10, 10, 10, 10, 10,-10,
    -10,  5,  0,  0,  0,  0,  5,-10,
    -20,-10,-10,-10,-10,-10,-10,-20,
]
ROOK_TABLE = [
     0,  0,  0,  0,  0,  0,  0,  0,
     5, 10, 10, 10, 10, 10, 10,  5,
    -5,  0,  0,  0,  0,  0,  0, -5,
    -5,  0,  0,  0,  0,  0,  0, -5,
    -5,  0,  0,  0,  0,  0,  0, -5,
    -5,  0,  0,  0,  0,  0,  0, -5,
    -5,  0,  0,  0,  0,  0,  0, -5,
     0,  0,  0,  5,  5,  0,  0,  0,
]
QUEEN_TABLE = [
    -20,-10,-10, -5, -5,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5,  5,  5,  5,  0,-10,
     -5,  0,  5,  5,  5,  5,  0, -5,
      0,  0,  5,  5,  5,  5,  0, -5,
    -10,  5,  5,  5,  5,  5,  0,-10,
    -10,  0,  5,  0,  0,  0,  0,-10,
    -20,-10,-10, -5, -5,-10,-10,-20,
]
KING_TABLE = [
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -20,-30,-30,-40,-40,-30,-30,-20,
    -10,-20,-20,-20,-20,-20,-20,-10,
     20, 20,  0,  0,  0,  0, 20, 20,
     20, 30, 10,  0,  0, 10, 30, 20,
]

PIECE_TABLES = {
    chess.PAWN:   PAWN_TABLE,
    chess.KNIGHT: KNIGHT_TABLE,
    chess.BISHOP: BISHOP_TABLE,
    chess.ROOK:   ROOK_TABLE,
    chess.QUEEN:  QUEEN_TABLE,
    chess.KING:   KING_TABLE,
}

def get_piece_square_value(piece_type: int, square: int, color: bool) -> int:
    table = PIECE_TABLES[piece_type]
    if color == chess.WHITE:
        index = (7 - chess.square_rank(square)) * 8 + chess.square_file(square)
    else:
        index = chess.square_rank(square) * 8 + chess.square_file(square)
    return table[index]

def evaluate_board(board: chess.Board) -> int:
    """Đánh giá vị trí bàn cờ. Dương = có lợi cho Trắng, Âm = có lợi cho Đen."""
    if board.is_checkmate():
        return -99999 if board.turn == chess.WHITE else 99999
    if board.is_stalemate() or board.is_insufficient_material():
        return 0

    score = 0
    for square in chess.SQUARES:
        piece = board.piece_at(square)
        if piece:
            value    = PIECE_VALUES[piece.piece_type]
            pos_val  = get_piece_square_value(piece.piece_type, square, piece.color)
            if piece.color == chess.WHITE:
                score += value + pos_val
            else:
                score -= value + pos_val
    return score

def minimax(board: chess.Board, depth: int, alpha: float, beta: float, maximizing: bool) -> int:
    """Minimax với Alpha-Beta pruning."""
    if depth == 0 or board.is_game_over():
        return evaluate_board(board)

    moves = list(board.legal_moves)

    if maximizing:
        best = -math.inf
        for move in moves:
            board.push(move)
            best = max(best, minimax(board, depth - 1, alpha, beta, False))
            board.pop()
            alpha = max(alpha, best)
            if beta <= alpha:
                break
        return best
    else:
        best = math.inf
        for move in moves:
            board.push(move)
            best = min(best, minimax(board, depth - 1, alpha, beta, True))
            board.pop()
            beta = min(beta, best)
            if beta <= alpha:
                break
        return best

# Cấu hình từng cấp độ
DIFFICULTY_CONFIG = {
    "Dễ":        {"depth": 1, "random_chance": 0.50, "label": "🟢 Dễ"},
    "Trung bình": {"depth": 3, "random_chance": 0.10, "label": "🟡 Trung bình"},
    "Khó":       {"depth": 4, "random_chance": 0.00, "label": "🔴 Khó"},
}

def get_best_move(board: chess.Board, difficulty: str) -> chess.Move | None:
    """Trả về nước đi tốt nhất theo cấp độ."""
    cfg   = DIFFICULTY_CONFIG[difficulty]
    moves = list(board.legal_moves)
    if not moves:
        return None

    # Cấp Dễ: đôi khi đi ngẫu nhiên
    if random.random() < cfg["random_chance"]:
        return random.choice(moves)

    random.shuffle(moves)  # Thêm đa dạng
    is_white = (board.turn == chess.WHITE)
    best_move  = moves[0]
    best_score = -math.inf if is_white else math.inf

    for move in moves:
        board.push(move)
        score = minimax(board, cfg["depth"] - 1, -math.inf, math.inf, not is_white)
        board.pop()
        if (is_white and score > best_score) or (not is_white and score < best_score):
            best_score = score
            best_move  = move

    return best_move


# ═══════════════════════════════════════════════════
#                  GUI (tkinter)
# ═══════════════════════════════════════════════════

# Màu sắc bàn cờ
COLOR_LIGHT    = "#F0D9B5"
COLOR_DARK     = "#B58863"
COLOR_SELECTED = "#7FC97F"
COLOR_LEGAL    = "#4E7C4E"
COLOR_LAST     = "#CDD16E"
COLOR_CHECK    = "#E25757"
COLOR_BG       = "#1C1C1C"
COLOR_PANEL    = "#252525"
COLOR_ACCENT   = "#E8A020"

PIECE_SYMBOLS = {
    (chess.KING,   chess.WHITE): "♔",
    (chess.QUEEN,  chess.WHITE): "♕",
    (chess.ROOK,   chess.WHITE): "♖",
    (chess.BISHOP, chess.WHITE): "♗",
    (chess.KNIGHT, chess.WHITE): "♘",
    (chess.PAWN,   chess.WHITE): "♙",
    (chess.KING,   chess.BLACK): "♚",
    (chess.QUEEN,  chess.BLACK): "♛",
    (chess.ROOK,   chess.BLACK): "♜",
    (chess.BISHOP, chess.BLACK): "♝",
    (chess.KNIGHT, chess.BLACK): "♞",
    (chess.PAWN,   chess.BLACK): "♟",
}

SQ = 72  # Kích thước mỗi ô cờ (pixel)


class ChessApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Chess AI ♟")
        self.configure(bg=COLOR_BG)
        self.resizable(False, False)

        self.board              = chess.Board()
        self.selected_sq        = None
        self.legal_from_sel     = []
        self.last_move          = None
        self.player_color       = chess.WHITE
        self.difficulty         = "Dễ"
        self.ai_thinking        = False
        self.move_history_san   = []

        self._build_ui()
        self.draw_board()
        self._update_status()

    # ─── Build UI ─────────────────────────────────

    def _build_ui(self):
        self._build_topbar()
        self._build_main()
        self._build_statusbar()

    def _build_topbar(self):
        bar = tk.Frame(self, bg="#111111", pady=10)
        bar.pack(fill="x")

        tk.Label(bar, text="♟  Chess AI", font=("Georgia", 17, "bold"),
                 bg="#111111", fg=COLOR_ACCENT).pack(side="left", padx=16)

        # Difficulty buttons
        diff_frame = tk.Frame(bar, bg="#111111")
        diff_frame.pack(side="left", padx=20)
        tk.Label(diff_frame, text="Cấp độ:", bg="#111111", fg="#888",
                 font=("Helvetica", 10)).pack(side="left")
        self.diff_var = tk.StringVar(value="Dễ")
        for lvl in ["Dễ", "Trung bình", "Khó"]:
            tk.Radiobutton(
                diff_frame, text=DIFFICULTY_CONFIG[lvl]["label"],
                variable=self.diff_var, value=lvl,
                command=self._on_diff_change,
                bg="#111111", fg="white", selectcolor="#333",
                activebackground="#111111", activeforeground=COLOR_ACCENT,
                font=("Helvetica", 10), indicatoron=False,
                relief="flat", padx=8, pady=3, cursor="hand2",
            ).pack(side="left", padx=2)

        # Color choice
        color_frame = tk.Frame(bar, bg="#111111")
        color_frame.pack(side="left", padx=20)
        tk.Label(color_frame, text="Màu:", bg="#111111", fg="#888",
                 font=("Helvetica", 10)).pack(side="left")
        self.color_var = tk.StringVar(value="white")
        for txt, val in [("⬜ Trắng", "white"), ("⬛ Đen", "black")]:
            tk.Radiobutton(
                color_frame, text=txt, variable=self.color_var, value=val,
                command=self._on_color_change,
                bg="#111111", fg="white", selectcolor="#333",
                activebackground="#111111", activeforeground=COLOR_ACCENT,
                font=("Helvetica", 10), indicatoron=False,
                relief="flat", padx=8, pady=3, cursor="hand2",
            ).pack(side="left", padx=2)

        tk.Button(bar, text="🔄  Ván mới", command=self.new_game,
                  bg=COLOR_ACCENT, fg="#111", font=("Helvetica", 10, "bold"),
                  relief="flat", padx=12, pady=4, cursor="hand2",
                  activebackground="#c8860a").pack(side="right", padx=16)

    def _build_main(self):
        main = tk.Frame(self, bg=COLOR_BG)
        main.pack(padx=16, pady=12)

        # Board canvas
        self.canvas = tk.Canvas(main, width=SQ * 8, height=SQ * 8,
                                highlightthickness=2,
                                highlightbackground=COLOR_ACCENT)
        self.canvas.grid(row=0, column=0, padx=(0, 16))
        self.canvas.bind("<Button-1>", self._on_click)

        # Side panel
        panel = tk.Frame(main, bg=COLOR_PANEL, width=200)
        panel.grid(row=0, column=1, sticky="nsew")
        panel.grid_propagate(False)

        tk.Label(panel, text="📋 Lịch sử", font=("Helvetica", 11, "bold"),
                 bg=COLOR_PANEL, fg=COLOR_ACCENT).pack(pady=(12, 4))

        sep = tk.Frame(panel, bg=COLOR_ACCENT, height=1)
        sep.pack(fill="x", padx=8)

        hist_container = tk.Frame(panel, bg=COLOR_PANEL)
        hist_container.pack(fill="both", expand=True, padx=6, pady=6)

        scrollbar = tk.Scrollbar(hist_container)
        scrollbar.pack(side="right", fill="y")

        self.hist_text = tk.Text(
            hist_container, bg=COLOR_PANEL, fg="white",
            font=("Courier", 10), relief="flat",
            yscrollcommand=scrollbar.set,
            state="disabled", wrap="word",
        )
        self.hist_text.pack(fill="both", expand=True)
        scrollbar.config(command=self.hist_text.yview)

        # Score label
        self.score_var = tk.StringVar(value="Điểm: 0")
        tk.Label(panel, textvariable=self.score_var, bg=COLOR_PANEL,
                 fg="#AAAAAA", font=("Helvetica", 10)).pack(pady=4)

    def _build_statusbar(self):
        self.status_var = tk.StringVar(value="")
        tk.Label(self, textvariable=self.status_var,
                 font=("Helvetica", 12, "bold"),
                 bg="#111111", fg="#FFD700", pady=8).pack(fill="x")

    # ─── Game Logic ───────────────────────────────

    def _on_diff_change(self):
        self.difficulty = self.diff_var.get()

    def _on_color_change(self):
        self.player_color = chess.WHITE if self.color_var.get() == "white" else chess.BLACK
        self.new_game()

    def new_game(self):
        self.board             = chess.Board()
        self.selected_sq       = None
        self.legal_from_sel    = []
        self.last_move         = None
        self.ai_thinking       = False
        self.move_history_san  = []
        self._refresh_history()
        self.score_var.set("Điểm: 0")
        self.draw_board()
        self._update_status()
        if self.player_color == chess.BLACK:
            self.after(400, self._ai_move)

    def _on_click(self, event):
        if self.ai_thinking or self.board.is_game_over():
            return
        if self.board.turn != self.player_color:
            return

        flip = (self.player_color == chess.BLACK)
        col  = event.x // SQ
        row  = event.y // SQ
        file = (7 - col) if flip else col
        rank = row        if flip else (7 - row)
        clicked = chess.square(file, rank)

        # Try to make a move if something is already selected
        if self.selected_sq is not None:
            promotion = None
            piece = self.board.piece_at(self.selected_sq)
            if piece and piece.piece_type == chess.PAWN:
                target_rank = chess.square_rank(clicked)
                if (self.player_color == chess.WHITE and target_rank == 7) or \
                   (self.player_color == chess.BLACK and target_rank == 0):
                    promotion = chess.QUEEN

            move = chess.Move(self.selected_sq, clicked, promotion=promotion)
            if move in self.board.legal_moves:
                self._do_move(move)
                self.selected_sq    = None
                self.legal_from_sel = []
                self.draw_board()
                self._update_status()
                if not self.board.is_game_over():
                    self.after(150, self._ai_move)
                else:
                    self.after(200, self._show_game_over)
                return

        # Select piece
        piece = self.board.piece_at(clicked)
        if piece and piece.color == self.player_color:
            self.selected_sq    = clicked
            self.legal_from_sel = [m for m in self.board.legal_moves
                                    if m.from_square == clicked]
        else:
            self.selected_sq    = None
            self.legal_from_sel = []

        self.draw_board()

    def _do_move(self, move: chess.Move):
        san = self.board.san(move)
        self.last_move = move
        self.board.push(move)
        self.move_history_san.append(san)
        self._refresh_history()
        self.score_var.set(f"Điểm: {evaluate_board(self.board):+d}")

    def _ai_move(self):
        if self.board.is_game_over() or self.board.turn == self.player_color:
            return
        self.ai_thinking = True
        self.status_var.set("🤖  AI đang suy nghĩ...")

        def think():
            move = get_best_move(self.board, self.difficulty)
            self.after(0, lambda: self._apply_ai(move))

        threading.Thread(target=think, daemon=True).start()

    def _apply_ai(self, move):
        self.ai_thinking = False
        if move:
            self._do_move(move)
        self.draw_board()
        self._update_status()
        if self.board.is_game_over():
            self.after(200, self._show_game_over)

    # ─── Drawing ──────────────────────────────────

    def draw_board(self):
        self.canvas.delete("all")
        flip    = (self.player_color == chess.BLACK)
        king_sq = self.board.king(self.board.turn) if self.board.is_check() else None

        legal_targets = {m.to_square for m in self.legal_from_sel}
        last_sqs = {self.last_move.from_square, self.last_move.to_square} \
                   if self.last_move else set()

        for r in range(8):
            for f in range(8):
                # Map display (r,f) → actual square
                actual_file = (7 - f) if flip else f
                actual_rank = r        if flip else (7 - r)
                sq_idx = chess.square(actual_file, actual_rank)

                x1, y1 = f * SQ, r * SQ
                x2, y2 = x1 + SQ, y1 + SQ

                # Base color
                light = (actual_file + actual_rank) % 2 == 1
                color = COLOR_LIGHT if light else COLOR_DARK

                if sq_idx in last_sqs:      color = COLOR_LAST
                if sq_idx == self.selected_sq: color = COLOR_SELECTED
                if sq_idx == king_sq:        color = COLOR_CHECK

                self.canvas.create_rectangle(x1, y1, x2, y2, fill=color, outline="")

                # Legal move indicator
                if sq_idx in legal_targets:
                    cx, cy = x1 + SQ // 2, y1 + SQ // 2
                    r2 = SQ // 7
                    self.canvas.create_oval(cx - r2, cy - r2, cx + r2, cy + r2,
                                            fill=COLOR_LEGAL, outline="")

                # Piece symbol
                piece = self.board.piece_at(sq_idx)
                if piece:
                    symbol = PIECE_SYMBOLS[(piece.piece_type, piece.color)]
                    fg = "#FFFFFF" if piece.color == chess.WHITE else "#1A1A1A"
                    shadow_color = "#555" if piece.color == chess.WHITE else "#888"
                    # Drop shadow
                    self.canvas.create_text(x1 + SQ // 2 + 1, y1 + SQ // 2 + 2,
                                            text=symbol,
                                            font=("Arial", int(SQ * 0.58)),
                                            fill=shadow_color)
                    self.canvas.create_text(x1 + SQ // 2, y1 + SQ // 2,
                                            text=symbol,
                                            font=("Arial", int(SQ * 0.58)),
                                            fill=fg)

        # Rank & file labels
        for i in range(8):
            rank_lbl = str(8 - i) if not flip else str(i + 1)
            file_lbl = "abcdefgh"[i] if not flip else "abcdefgh"[7 - i]
            lc = COLOR_DARK if (i % 2 == 0) else COLOR_LIGHT
            self.canvas.create_text(3, i * SQ + 5, text=rank_lbl,
                                    font=("Helvetica", 8, "bold"),
                                    fill=lc, anchor="nw")
            lc2 = COLOR_LIGHT if (i % 2 == 0) else COLOR_DARK
            self.canvas.create_text(i * SQ + SQ - 3, SQ * 8 - 3,
                                    text=file_lbl,
                                    font=("Helvetica", 8, "bold"),
                                    fill=lc2, anchor="se")

    def _update_status(self):
        board = self.board
        if board.is_checkmate():
            winner = "Đen" if board.turn == chess.WHITE else "Trắng"
            self.status_var.set(f"♛  Chiếu hết!  {winner} thắng!")
        elif board.is_stalemate():
            self.status_var.set("🤝  Hòa cờ — Stalemate")
        elif board.is_insufficient_material():
            self.status_var.set("🤝  Hòa cờ — Thiếu quân")
        elif board.is_check():
            who = "Bạn bị" if board.turn == self.player_color else "AI bị"
            self.status_var.set(f"⚠️  {who} chiếu!")
        else:
            who = "Bạn" if board.turn == self.player_color else "🤖 AI"
            clr = "Trắng" if board.turn == chess.WHITE else "Đen"
            self.status_var.set(f"Lượt của {who} ({clr})")

    def _refresh_history(self):
        self.hist_text.config(state="normal")
        self.hist_text.delete("1.0", "end")
        lines = []
        for i in range(0, len(self.move_history_san), 2):
            move_num = i // 2 + 1
            white = self.move_history_san[i]
            black = self.move_history_san[i + 1] if i + 1 < len(self.move_history_san) else ""
            lines.append(f"{move_num:>3}. {white:<8} {black}")
        self.hist_text.insert("end", "\n".join(lines))
        self.hist_text.see("end")
        self.hist_text.config(state="disabled")

    def _show_game_over(self):
        board = self.board
        if board.is_checkmate():
            winner = "Đen" if board.turn == chess.WHITE else "Trắng"
            msg = f"♛  {winner} thắng bằng chiếu hết!\n\n"
            msg += "Bạn thắng! 🎉" if (winner == "Trắng") == (self.player_color == chess.WHITE) \
                  else "AI thắng! Cố lên nhé 💪"
        elif board.is_stalemate():
            msg = "🤝  Hòa cờ — Stalemate!"
        else:
            msg = "🤝  Hòa cờ!"
        msg += f"\n\nTổng số nước: {len(self.move_history_san)}"
        if messagebox.askyesno("Ván cờ kết thúc", msg + "\n\nChơi ván mới?"):
            self.new_game()


# ═══════════════════════════════════════════════════
#                    ENTRY POINT
# ═══════════════════════════════════════════════════

if __name__ == "__main__":
    app = ChessApp()
    app.mainloop()