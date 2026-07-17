# evaluation.py — NÂNG CẤP: Piece-Square Tables + King Safety + Pawn Structure
import chess
from config import PIECE_VALUES, MATE_SCORE

# ══════════════════════════════════════════════════════════════
#  PIECE-SQUARE TABLES (từ góc nhìn trắng, a1=index 0)
# ══════════════════════════════════════════════════════════════

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

KING_MIDDLE_TABLE = [
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -20,-30,-30,-40,-40,-30,-30,-20,
    -10,-20,-20,-20,-20,-20,-20,-10,
     20, 20,  0,  0,  0,  0, 20, 20,
     20, 30, 10,  0,  0, 10, 30, 20,
]

KING_ENDGAME_TABLE = [
    -50,-40,-30,-20,-20,-30,-40,-50,
    -30,-20,-10,  0,  0,-10,-20,-30,
    -30,-10, 20, 30, 30, 20,-10,-30,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -30,-10, 20, 30, 30, 20,-10,-30,
    -30,-30,  0,  0,  0,  0,-30,-30,
    -50,-30,-30,-30,-30,-30,-30,-50,
]

def _is_endgame(board: chess.Board) -> bool:
    total = 0
    for sq in chess.SQUARES:
        p = board.piece_at(sq)
        if p and p.piece_type != chess.KING:
            total += PIECE_VALUES.get(p.piece_type, 0)
    return total < 2600

def _pst_score(piece_type, sq, color, endgame) -> int:
    if piece_type == chess.KING:
        table = KING_ENDGAME_TABLE if endgame else KING_MIDDLE_TABLE
    elif piece_type == chess.PAWN:
        table = PAWN_TABLE
    elif piece_type == chess.KNIGHT:
        table = KNIGHT_TABLE
    elif piece_type == chess.BISHOP:
        table = BISHOP_TABLE
    elif piece_type == chess.ROOK:
        table = ROOK_TABLE
    elif piece_type == chess.QUEEN:
        table = QUEEN_TABLE
    else:
        return 0
    # Trắng đọc thẳng, Đen mirror rank
    idx = sq if color == chess.WHITE else chess.square_mirror(sq)
    return table[idx]

def _pawn_structure(board: chess.Board, color: chess.Color) -> int:
    score = 0
    pawns = board.pieces(chess.PAWN, color)
    files = [chess.square_file(sq) for sq in pawns]

    # Tốt đôi — phạt
    for f in range(8):
        cnt = files.count(f)
        if cnt > 1:
            score -= 20 * (cnt - 1)

    # Tốt cô lập — phạt
    for sq in pawns:
        f = chess.square_file(sq)
        if not any(ff in files for ff in [f - 1, f + 1] if 0 <= ff < 8):
            score -= 15

    # Tốt thông — thưởng
    opp_pawns = board.pieces(chess.PAWN, not color)
    for sq in pawns:
        f  = chess.square_file(sq)
        r  = chess.square_rank(sq)
        passed = True
        for opp_sq in opp_pawns:
            of = chess.square_file(opp_sq)
            or_ = chess.square_rank(opp_sq)
            if abs(of - f) <= 1:
                if color == chess.WHITE and or_ > r:
                    passed = False; break
                elif color == chess.BLACK and or_ < r:
                    passed = False; break
        if passed:
            bonus = [0, 10, 20, 35, 60, 100, 150, 0]
            rr = r if color == chess.WHITE else 7 - r
            score += bonus[rr]

    return score

def _king_safety(board: chess.Board, color: chess.Color) -> int:
    if _is_endgame(board):
        return 0
    score = 0
    king_sq = board.king(color)
    if king_sq is None:
        return 0

    # Đếm ô quanh vua bị tấn công
    for sq in chess.SQUARES:
        if board.is_attacked_by(not color, sq):
            if chess.square_distance(sq, king_sq) <= 2:
                score -= 8

    # Thưởng tốt che chắn
    kf = chess.square_file(king_sq)
    kr = chess.square_rank(king_sq)
    shield_rank = kr + 1 if color == chess.WHITE else kr - 1
    if 0 <= shield_rank < 8:
        for df in [-1, 0, 1]:
            sf = kf + df
            if 0 <= sf < 8:
                sq = chess.square(sf, shield_rank)
                p  = board.piece_at(sq)
                if p and p.piece_type == chess.PAWN and p.color == color:
                    score += 12
    return score

# ══════════════════════════════════════════════════════════════
#  HÀM ĐÁNH GIÁ CHÍNH
# ══════════════════════════════════════════════════════════════

def evaluate_white_perspective(board: chess.Board) -> int:
    if board.is_checkmate():
        return -MATE_SCORE if board.turn == chess.WHITE else MATE_SCORE
    if board.is_stalemate() or board.is_insufficient_material():
        return 0

    endgame = _is_endgame(board)
    score   = 0

    # Vật chất + PST
    for sq in chess.SQUARES:
        piece = board.piece_at(sq)
        if not piece:
            continue
        val = PIECE_VALUES[piece.piece_type]
        pst = _pst_score(piece.piece_type, sq, piece.color, endgame)
        if piece.color == chess.WHITE:
            score += val + pst
        else:
            score -= val + pst

    # Cấu trúc tốt
    score += _pawn_structure(board, chess.WHITE)
    score -= _pawn_structure(board, chess.BLACK)

    # An toàn vua
    score += _king_safety(board, chess.WHITE)
    score -= _king_safety(board, chess.BLACK)

    # Cặp tượng
    if len(board.pieces(chess.BISHOP, chess.WHITE)) >= 2:
        score += 30
    if len(board.pieces(chess.BISHOP, chess.BLACK)) >= 2:
        score -= 30

    # Xe trên cột mở
    for sq in board.pieces(chess.ROOK, chess.WHITE):
        f = chess.square_file(sq)
        if not any(
            board.piece_at(chess.square(f, r)) and
            board.piece_at(chess.square(f, r)).piece_type == chess.PAWN
            for r in range(8)
        ):
            score += 20
    for sq in board.pieces(chess.ROOK, chess.BLACK):
        f = chess.square_file(sq)
        if not any(
            board.piece_at(chess.square(f, r)) and
            board.piece_at(chess.square(f, r)).piece_type == chess.PAWN
            for r in range(8)
        ):
            score -= 20

    return score


def evaluate_side_to_move(board: chess.Board) -> int:
    score = evaluate_white_perspective(board)
    return score if board.turn == chess.WHITE else -score