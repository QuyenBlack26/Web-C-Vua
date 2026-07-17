# search.py — NÂNG CẤP: Transposition Table + Null Move + Killer Moves + LMR
import time
import chess
import chess.polyglot

from config import INF, MATE_SCORE
from evaluation import evaluate_side_to_move
from move_ordering import order_moves, update_history

class SearchTimeout(Exception):
    pass

# ══════════════════════════════════════════════════════════════
#  TRANSPOSITION TABLE
# ══════════════════════════════════════════════════════════════
TT_EXACT = 0
TT_LOWER = 1
TT_UPPER = 2

_tt: dict = {}
TT_MAX_SIZE = 300_000

def _tt_key(board: chess.Board) -> int:
    return chess.polyglot.zobrist_hash(board)

def tt_lookup(board, depth, alpha, beta):
    entry = _tt.get(_tt_key(board))
    if not entry:
        return None
    e_depth, e_flag, e_score = entry
    if e_depth < depth:
        return None
    if e_flag == TT_EXACT:
        return e_score
    if e_flag == TT_LOWER and e_score >= beta:
        return e_score
    if e_flag == TT_UPPER and e_score <= alpha:
        return e_score
    return None

def tt_store(board, depth, flag, score):
    if len(_tt) >= TT_MAX_SIZE:
        keys = list(_tt.keys())[:TT_MAX_SIZE // 5]
        for k in keys:
            del _tt[k]
    _tt[_tt_key(board)] = (depth, flag, score)

def clear_tt():
    _tt.clear()

# ══════════════════════════════════════════════════════════════
#  KILLER MOVES
# ══════════════════════════════════════════════════════════════
MAX_DEPTH = 20
killer_moves: list[list] = [[] for _ in range(MAX_DEPTH + 1)]

def store_killer(move: chess.Move, ply: int):
    if ply > MAX_DEPTH:
        return
    km = killer_moves[ply]
    if move not in km:
        km.insert(0, move)
        if len(km) > 2:
            km.pop()

# ══════════════════════════════════════════════════════════════
#  QUIESCENCE SEARCH
# ══════════════════════════════════════════════════════════════
def quiescence(board: chess.Board, alpha: int, beta: int, end_time: float) -> int:
    if time.time() >= end_time:
        raise SearchTimeout()

    stand_pat = evaluate_side_to_move(board)

    if stand_pat >= beta:
        return beta
    if alpha < stand_pat:
        alpha = stand_pat

    for move in order_moves(board):
        if not board.is_capture(move):
            continue
        board.push(move)
        score = -quiescence(board, -beta, -alpha, end_time)
        board.pop()
        if score >= beta:
            return beta
        if score > alpha:
            alpha = score

    return alpha

# ══════════════════════════════════════════════════════════════
#  NEGAMAX CHÍNH
# ══════════════════════════════════════════════════════════════
def negamax(
    board: chess.Board,
    depth: int,
    alpha: int,
    beta: int,
    end_time: float,
    null_allowed: bool = True,
    ply: int = 0
) -> int:
    if time.time() >= end_time:
        raise SearchTimeout()

    # Tra TT
    cached = tt_lookup(board, depth, alpha, beta)
    if cached is not None:
        return cached

    # Terminal
    if board.is_checkmate():
        return -MATE_SCORE + ply
    if board.is_stalemate() or board.is_insufficient_material():
        return 0
    if board.is_repetition(2):
        return 0

    # Leaf
    if depth == 0:
        return quiescence(board, alpha, beta, end_time)

    in_check = board.is_check()

    # Null Move Pruning
    NULL_R = 2 if depth < 6 else 3
    if (null_allowed and not in_check and depth >= 3):
        board.push(chess.Move.null())
        null_score = -negamax(board, depth - 1 - NULL_R, -beta, -beta + 1,
                              end_time, null_allowed=False, ply=ply + 1)
        board.pop()
        if null_score >= beta:
            return beta

    orig_alpha    = alpha
    best_score    = -INF
    best_move     = None
    moves_searched = 0

    km = killer_moves[ply] if ply <= MAX_DEPTH else []
    ordered = order_moves(board, km)

    for move in ordered:
        board.push(move)
        moves_searched += 1

        # Late Move Reduction
        reduction = 0
        if (moves_searched > 4 and depth >= 3 and
                not in_check and not board.is_check() and
                not board.is_capture(move) and not move.promotion):
            reduction = 1
            if moves_searched > 10:
                reduction = 2

        score = -negamax(board, depth - 1 - reduction, -beta, -alpha,
                         end_time, ply=ply + 1)

        # Re-search nếu LMR tìm được nước tốt
        if reduction > 0 and score > alpha:
            score = -negamax(board, depth - 1, -beta, -alpha,
                             end_time, ply=ply + 1)

        board.pop()

        if score > best_score:
            best_score = score
            best_move  = move

        if score > alpha:
            alpha = score

        if alpha >= beta:
            if not board.is_capture(move):
                store_killer(move, ply)
                update_history(move, depth)
            break

    # Lưu TT
    if best_score <= orig_alpha:
        flag = TT_UPPER
    elif best_score >= beta:
        flag = TT_LOWER
    else:
        flag = TT_EXACT
    tt_store(board, depth, flag, best_score)

    return best_score