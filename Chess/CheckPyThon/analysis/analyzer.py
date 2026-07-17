import chess

from evaluation import evaluate_white_perspective
from analysis.classify import classify_move, build_move_comment


ANALYSIS_DEPTH = 2


def score_for_side(board, side):
    """
    Đổi điểm bàn cờ về góc nhìn của bên đang xét.
    Nếu side là Trắng: điểm dương là tốt cho Trắng.
    Nếu side là Đen: điểm dương là tốt cho Đen.
    """
    white_score = evaluate_white_perspective(board)

    if side == chess.WHITE:
        return white_score

    return -white_score


def minimax_score(board, depth, root_side, alpha=-10**9, beta=10**9):
    """
    Tìm điểm tốt nhất bằng minimax nhỏ.
    root_side là bên đang cần đánh giá.
    depth = 2 nghĩa là xét:
        bên vừa đi
        đối thủ phản hồi lại
    """

    if depth == 0 or board.is_game_over():
        return score_for_side(board, root_side)

    legal_moves = list(board.legal_moves)

    if not legal_moves:
        return score_for_side(board, root_side)

    if board.turn == root_side:
        best_score = -10**9

        for move in legal_moves:
            next_board = board.copy()
            next_board.push(move)

            score = minimax_score(next_board, depth - 1, root_side, alpha, beta)

            if score > best_score:
                best_score = score

            alpha = max(alpha, best_score)

            if beta <= alpha:
                break

        return best_score

    best_score = 10**9

    for move in legal_moves:
        next_board = board.copy()
        next_board.push(move)

        score = minimax_score(next_board, depth - 1, root_side, alpha, beta)

        if score < best_score:
            best_score = score

        beta = min(beta, best_score)

        if beta <= alpha:
            break

    return best_score


def find_best_move(board, root_side, depth=ANALYSIS_DEPTH):
    """
    Tìm nước tốt nhất cho bên đang đi ở vị trí hiện tại.
    """
    legal_moves = list(board.legal_moves)

    if not legal_moves:
        return None, score_for_side(board, root_side)

    best_move = None
    best_score = -10**9

    for move in legal_moves:
        next_board = board.copy()
        next_board.push(move)

        score = minimax_score(next_board, depth - 1, root_side)

        if score > best_score:
            best_score = score
            best_move = move

    return best_move, best_score


def safe_san(board, move):
    try:
        return board.san(move)
    except Exception:
        return move.uci()


def analyze_game_moves(moves):
    board = chess.Board()

    analyzed_moves = []

    summary = {
        "BEST": 0,
        "GOOD": 0,
        "INACCURACY": 0,
        "MISTAKE": 0,
        "BLUNDER": 0,
        "INVALID": 0
    }

    total_loss = 0
    valid_move_count = 0

    for index, move_text in enumerate(moves, start=1):
        move_text = str(move_text).strip()

        if not move_text:
            continue

        side_to_move = board.turn
        score_before = score_for_side(board, side_to_move)

        try:
            move = chess.Move.from_uci(move_text)
        except ValueError:
            summary["INVALID"] += 1

            analyzed_moves.append({
                "soThuTu": index,
                "nuoc": move_text,
                "san": "",
                "danhGia": "INVALID",
                "diemTruoc": score_before,
                "diemSau": score_before,
                "chenhLech": 0,
                "loss": 0,
                "bestMove": "",
                "bestSan": "",
                "nhanXet": "Nước đi không đúng định dạng UCI, ví dụ đúng là e2e4 hoặc e7e8q."
            })

            continue

        if move not in board.legal_moves:
            summary["INVALID"] += 1

            analyzed_moves.append({
                "soThuTu": index,
                "nuoc": move_text,
                "san": "",
                "danhGia": "INVALID",
                "diemTruoc": score_before,
                "diemSau": score_before,
                "chenhLech": 0,
                "loss": 0,
                "bestMove": "",
                "bestSan": "",
                "nhanXet": "Nước đi không hợp lệ trong thế cờ hiện tại."
            })

            continue

        valid_move_count += 1

        best_move, best_score = find_best_move(board, side_to_move, ANALYSIS_DEPTH)

        san = safe_san(board, move)
        best_san = safe_san(board, best_move) if best_move else ""

        is_capture = board.is_capture(move)
        gives_check = board.gives_check(move)

        board_after_move = board.copy()
        board_after_move.push(move)

        is_checkmate = board_after_move.is_checkmate()

        score_after_direct = score_for_side(board_after_move, side_to_move)

        played_score = minimax_score(
            board_after_move,
            ANALYSIS_DEPTH - 1,
            side_to_move
        )

        delta_direct = score_after_direct - score_before
        loss = best_score - played_score

        if loss < 0:
            loss = 0

        total_loss += loss

        is_best = best_move is not None and move == best_move

        label, short_comment = classify_move(
            loss=loss,
            delta=delta_direct,
            is_best=is_best,
            is_checkmate=is_checkmate
        )

        summary[label] += 1

        full_comment = build_move_comment(
            label=label,
            san=san,
            best_san=best_san,
            loss=loss,
            delta=delta_direct,
            is_capture=is_capture,
            gives_check=gives_check,
            is_checkmate=is_checkmate
        )

        analyzed_moves.append({
            "soThuTu": index,
            "nuoc": move_text,
            "san": san,
            "danhGia": label,

            "diemTruoc": round(score_before, 2),
            "diemSau": round(score_after_direct, 2),
            "diemSauPhanHoi": round(played_score, 2),

            "chenhLech": round(delta_direct, 2),
            "loss": round(loss, 2),

            "bestMove": best_move.uci() if best_move else "",
            "bestSan": best_san,

            "isCapture": is_capture,
            "givesCheck": gives_check,
            "isCheckmate": is_checkmate,

            "nhanXet": full_comment
        })

        board.push(move)

    advice = build_advice(summary, total_loss, valid_move_count)
    accuracy = calculate_accuracy(total_loss, valid_move_count)

    return {
        "success": True,
        "summary": summary,
        "accuracy": accuracy,
        "averageLoss": round(total_loss / valid_move_count, 2) if valid_move_count > 0 else 0,
        "advice": advice,
        "moves": analyzed_moves
    }


def calculate_accuracy(total_loss, valid_move_count):
    if valid_move_count <= 0:
        return 0

    average_loss = total_loss / valid_move_count

    accuracy = 100 - (average_loss / 4)

    if accuracy < 0:
        accuracy = 0

    if accuracy > 100:
        accuracy = 100

    return round(accuracy, 1)


def build_advice(summary, total_loss, valid_move_count):
    mistake_count = summary.get("MISTAKE", 0)
    blunder_count = summary.get("BLUNDER", 0)
    inaccuracy_count = summary.get("INACCURACY", 0)
    invalid_count = summary.get("INVALID", 0)

    if invalid_count > 0:
        return "Có một số nước đi không hợp lệ hoặc sai định dạng. Cần kiểm tra lại dữ liệu nước đi đã lưu."

    if valid_move_count == 0:
        return "Ván này chưa có đủ nước đi để phân tích."

    average_loss = total_loss / valid_move_count

    if blunder_count == 0 and mistake_count == 0 and inaccuracy_count <= 2 and average_loss <= 35:
        return "Ván này khá tốt. Bạn ít bỏ lỡ nước mạnh và không mắc lỗi lớn. Nên tiếp tục luyện khai cuộc, chiến thuật và tàn cuộc."

    if blunder_count == 0 and mistake_count <= 2 and average_loss <= 80:
        return "Bạn chơi tương đối ổn, nhưng vẫn có vài nước chưa chính xác. Nên xem lại các nước INACCURACY và MISTAKE để hiểu vì sao AI chọn phương án khác."

    if blunder_count <= 2:
        return "Ván này có một vài lỗi nặng. Bạn nên chú ý trước khi đi: quân nào đang bị tấn công, Vua có an toàn không, và đối thủ có nước phản công trực tiếp không."

    return "Ván này có nhiều lỗi lớn. Bạn nên tập trung luyện cách tránh mất quân, kiểm tra chiếu hết, và so sánh ít nhất 2 đến 3 nước ứng viên trước khi đi."