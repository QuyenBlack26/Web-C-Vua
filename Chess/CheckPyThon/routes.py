from flask import request, jsonify
import chess
import traceback

from ai_engine import get_best_move
from evaluation import evaluate_white_perspective


def register_routes(app):

    @app.route("/api/move", methods=["POST", "OPTIONS"])
    @app.route("/api/move", methods=["POST", "OPTIONS"])
    def api_move():
        if request.method == "OPTIONS":
            return jsonify({"status": "ok"}), 200

        try:
            data = request.get_json(silent=True)

            if not data or "fen" not in data:
                return jsonify({
                    "error": "Thiếu FEN"
                }), 400

            fen = data["fen"]
            level = data.get("level", "medium")
            fen_history = data.get("fen_history", [])
            last_ai_move = data.get("last_ai_move", None)

            try:
                board = chess.Board(fen)
            except ValueError:
                return jsonify({
                    "error": "FEN không hợp lệ",
                    "fen": fen
                }), 400

            if board.is_game_over():
                return jsonify({
                    "move": None,
                    "fen_after": board.fen(),
                    "is_game_over": True,
                    "is_checkmate": board.is_checkmate(),
                    "is_stalemate": board.is_stalemate()
                }), 200

            # QUAN TRỌNG:
            # Cho AI tính trên bản copy để tránh AI làm thay đổi board gốc
            ai_board = board.copy()
            move, score, info = get_best_move(ai_board, level, fen_history, last_ai_move)

            legal_moves = list(board.legal_moves)

            if move is None:
                return jsonify({
                    "error": "AI không tìm được nước đi",
                    "legal_moves": [m.uci() for m in legal_moves]
                }), 400

            # Nếu AI trả nước không hợp lệ trên board gốc thì chọn tạm nước hợp lệ đầu tiên
            if move not in legal_moves:
                print("AI trả nước không hợp lệ:", move.uci())
                print("FEN gốc:", board.fen())
                print("Legal moves:", [m.uci() for m in legal_moves])

                move = legal_moves[0]
                info = {
                    "fallback": True,
                    "reason": "AI trả nước không hợp lệ trên board gốc"
                }
                score = 0

            # PHẢI lấy SAN trước khi push
            san = board.san(move)

            # Sau đó mới push
            board.push(move)

            return jsonify({
                "move": move.uci(),
                "move_san": san,
                "fen_after": board.fen(),
                "score": evaluate_white_perspective(board),
                "search_score_side_to_move": score,
                "search_info": info,
                "level": level,
                "is_check": board.is_check(),
                "is_checkmate": board.is_checkmate(),
                "is_stalemate": board.is_stalemate(),
                "is_game_over": board.is_game_over()
            }), 200

        except Exception as error:
            traceback.print_exc()
            return jsonify({
                "error": str(error),
                "type": type(error).__name__
            }), 500


    @app.route("/api/validate", methods=["POST", "OPTIONS"])
    def api_validate():
        if request.method == "OPTIONS":
            return jsonify({"status": "ok"}), 200

        try:
            data = request.get_json(silent=True)

            if not data or "fen" not in data or "move" not in data:
                return jsonify({
                    "valid": False,
                    "error": "Thiếu fen hoặc move"
                }), 400

            board = chess.Board(data["fen"])
            move = chess.Move.from_uci(data["move"])

            if move not in board.legal_moves:
                return jsonify({
                    "valid": False
                }), 200

            board.push(move)

            return jsonify({
                "valid": True,
                "fen_after": board.fen(),
                "is_check": board.is_check(),
                "is_checkmate": board.is_checkmate(),
                "is_stalemate": board.is_stalemate(),
                "is_game_over": board.is_game_over()
            }), 200

        except Exception as error:
            traceback.print_exc()
            return jsonify({
                "valid": False,
                "error": str(error),
                "type": type(error).__name__
            }), 400


    @app.route("/api/legal-moves", methods=["POST", "OPTIONS"])
    def api_legal_moves():
        if request.method == "OPTIONS":
            return jsonify({"status": "ok"}), 200

        try:
            data = request.get_json(silent=True)

            if not data or "fen" not in data or "square" not in data:
                return jsonify({
                    "legal_moves": [],
                    "error": "Thiếu fen hoặc square"
                }), 400

            board = chess.Board(data["fen"])
            square = chess.parse_square(data["square"])

            moves = [
                move.uci()
                for move in board.legal_moves
                if move.from_square == square
            ]

            return jsonify({
                "legal_moves": moves
            }), 200

        except Exception as error:
            traceback.print_exc()
            return jsonify({
                "legal_moves": [],
                "error": str(error),
                "type": type(error).__name__
            }), 400