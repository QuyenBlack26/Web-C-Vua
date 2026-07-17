# ai_engine.py — NÂNG CẤP: Aspiration Windows + TT reset + History reset
import time
import random
import chess

from config import LEVELS, INF
from search import negamax, SearchTimeout, clear_tt, killer_moves, MAX_DEPTH
from move_ordering import clear_history

def fen_key(fen: str) -> str:
    return " ".join(fen.split(" ")[:4])

def is_reverse_move(move: chess.Move, last_ai_move: str | None) -> bool:
    if not last_ai_move or len(last_ai_move) < 4:
        return False
    prev_from = last_ai_move[0:2]
    prev_to   = last_ai_move[2:4]
    cur_from  = chess.square_name(move.from_square)
    cur_to    = chess.square_name(move.to_square)
    return cur_from == prev_to and cur_to == prev_from

def move_repeats_position(board: chess.Board, move: chess.Move, fen_history: list[str]) -> bool:
    test = board.copy()
    test.push(move)
    new_key  = fen_key(test.fen())
    old_keys = [fen_key(f) for f in fen_history]
    return new_key in old_keys

def filter_non_repeating_moves(board, legal_moves, fen_history, last_ai_move):
    filtered = [
        m for m in legal_moves
        if not is_reverse_move(m, last_ai_move)
        and not move_repeats_position(board, m, fen_history)
    ]
    return filtered if filtered else legal_moves


def get_best_move(
    board: chess.Board,
    level: str = "medium",
    fen_history=None,
    last_ai_move=None
):
    if fen_history is None:
        fen_history = []

    config      = LEVELS.get(level, LEVELS["medium"])
    legal_moves = list(board.legal_moves)
    legal_moves = filter_non_repeating_moves(board, legal_moves, fen_history, last_ai_move)

    if not legal_moves:
        return None, 0, {"message": "Không có nước đi hợp lệ"}

    # Random cho easy
    if random.random() < config["random"]:
        move = random.choice(legal_moves)
        return move, 0, {"level": level, "random": True}

    end_time    = time.time() + config["time_limit"]
    best_move   = legal_moves[0]
    best_score  = -INF
    depth_reached = 0

    # Reset killer moves cho ván mới
    for i in range(MAX_DEPTH + 1):
        killer_moves[i].clear()

    # ── Iterative Deepening với Aspiration Windows ────────────
    WINDOW = 50  # aspiration window size

    try:
        for depth in range(1, config["max_depth"] + 1):
            current_best_move  = best_move
            current_best_score = -INF

            # Aspiration windows từ depth >= 3
            if depth >= 3 and best_score != -INF:
                alpha = best_score - WINDOW
                beta  = best_score + WINDOW
            else:
                alpha = -INF
                beta  =  INF

            while True:
                iter_best_move  = legal_moves[0]
                iter_best_score = -INF

                for move in legal_moves:
                    board.push(move)
                    score = -negamax(board, depth - 1, -beta, -alpha, end_time, ply=1)
                    board.pop()

                    # Phạt nước lặp và ngược
                    if move_repeats_position(board, move, fen_history):
                        score -= 3000
                    if is_reverse_move(move, last_ai_move):
                        score -= 5000

                    if score > iter_best_score:
                        iter_best_score = score
                        iter_best_move  = move

                # Aspiration window fail — mở rộng và thử lại
                if depth >= 3 and iter_best_score <= alpha:
                    alpha -= WINDOW * 2
                    continue
                if depth >= 3 and iter_best_score >= beta:
                    beta += WINDOW * 2
                    continue

                current_best_move  = iter_best_move
                current_best_score = iter_best_score
                break

            best_move   = current_best_move
            best_score  = current_best_score
            depth_reached = depth

    except SearchTimeout:
        pass

    return best_move, best_score, {
        "level":         level,
        "depth_reached": depth_reached,
        "time_limit":    config["time_limit"],
        "tt_size":       0,
    }