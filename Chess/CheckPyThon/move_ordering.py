# move_ordering.py — NÂNG CẤP: History Heuristic + Killer Moves tích hợp
import chess

MVV_LVA = {
    chess.PAWN:   100,
    chess.KNIGHT: 300,
    chess.BISHOP: 300,
    chess.ROOK:   500,
    chess.QUEEN:  900,
    chess.KING:   10000,
}

# History heuristic table: {(from_sq, to_sq): score}
_history: dict[tuple, int] = {}

def update_history(move: chess.Move, depth: int):
    """Cộng điểm cho nước đi gây cutoff theo depth^2"""
    key = (move.from_square, move.to_square)
    _history[key] = _history.get(key, 0) + depth * depth

def clear_history():
    _history.clear()

def score_move(board: chess.Board, move: chess.Move, killers: list = None) -> int:
    score = 0

    # 1. Hash move (PV move) — ưu tiên cao nhất nếu có
    # (để trống, sẽ tích hợp sau nếu dùng PV table)

    # 2. Capture — MVV/LVA
    if board.is_capture(move):
        victim   = board.piece_at(move.to_square)
        attacker = board.piece_at(move.from_square)
        if victim and attacker:
            # Nước ăn quân giá trị cao bằng quân nhỏ → ưu tiên cao
            score += 10_000 + 10 * MVV_LVA[victim.piece_type] - MVV_LVA[attacker.piece_type]
        else:
            score += 10_000

    # 3. Promotion
    elif move.promotion:
        score += 9_000 + MVV_LVA.get(move.promotion, 0)

    # 4. Killer moves
    elif killers and move in killers:
        score += 8_000 - killers.index(move) * 10

    # 5. Check bonus
    elif board.gives_check(move):
        score += 500

    # 6. History heuristic
    key = (move.from_square, move.to_square)
    score += _history.get(key, 0)

    return score


def order_moves(board: chess.Board, killers: list = None):
    moves = list(board.legal_moves)
    moves.sort(
        key=lambda m: score_move(board, m, killers),
        reverse=True
    )
    return moves